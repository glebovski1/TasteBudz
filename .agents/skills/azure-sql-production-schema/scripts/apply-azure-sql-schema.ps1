[CmdletBinding()]
param(
    [string]$Subscription,
    [string]$ResourceGroup = "rg-tastebudz-prod",
    [string]$WebAppName = "tastebudz-prod-23df46c9",
    [string]$SqlServerResourceGroup,
    [string[]]$ScriptPath,
    [switch]$AllowClientIp,
    [switch]$KeepClientIpRule,
    [switch]$SkipVerification,
    [switch]$DryRun
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")).Path
$originalLocation = (Get-Location).Path
$firewallRuleName = $null
$clientIp = $null
$sqlServerName = $null
$effectiveSqlServerResourceGroup = $null

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message"
}

function Format-Command {
    param(
        [string]$FilePath,
        [string[]]$Arguments
    )

    $parts = @($FilePath) + ($Arguments | ForEach-Object {
        if ($_ -match "\s") {
            '"' + ($_ -replace '"', '\"') + '"'
        } else {
            $_
        }
    })

    return $parts -join " "
}

function Invoke-RequiredCommand {
    param(
        [string]$FilePath,
        [string[]]$Arguments
    )

    Write-Host (Format-Command $FilePath $Arguments)

    if ($DryRun) {
        return
    }

    & $FilePath @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $(Format-Command $FilePath $Arguments)"
    }
}

function Invoke-CapturedCommand {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [switch]$Sensitive
    )

    if (-not $Sensitive) {
        Write-Host (Format-Command $FilePath $Arguments)
    }

    $output = & $FilePath @Arguments 2>&1

    if ($LASTEXITCODE -ne 0) {
        if ($Sensitive) {
            throw "Sensitive command failed with exit code ${LASTEXITCODE}."
        }

        throw "Command failed with exit code ${LASTEXITCODE}: $($output -join "`n")"
    }

    return ($output -join "`n").Trim()
}

function Invoke-ArmJsonRequest {
    param(
        [string]$Method,
        [string]$Uri,
        [string]$AccessToken,
        [AllowNull()][string]$Body
    )

    $arguments = @(
        "--ipv4",
        "--silent",
        "--show-error",
        "--request",
        $Method,
        "--header",
        "Authorization: Bearer $AccessToken",
        "--header",
        "Content-Type: application/json",
        "--write-out",
        "`nHTTP_STATUS:%{http_code}"
    )

    if (-not [string]::IsNullOrEmpty($Body)) {
        $arguments += @("--data", $Body)
    }

    $arguments += $Uri

    $output = & curl.exe @arguments 2>&1
    $text = $output -join "`n"

    if ($LASTEXITCODE -ne 0 -or $text -notmatch "HTTP_STATUS:(\d{3})\s*$") {
        throw "ARM request failed before returning an HTTP status."
    }

    $statusCode = [int]$Matches[1]
    $responseBody = $text -replace "(?s)\s*HTTP_STATUS:\d{3}\s*$", ""

    if ($statusCode -lt 200 -or $statusCode -gt 299) {
        throw "ARM request failed with HTTP $statusCode."
    }

    return $responseBody
}

function Resolve-ScriptPaths {
    param([string[]]$RequestedPaths)

    if ($RequestedPaths -and $RequestedPaths.Count -gt 0) {
        return ,@($RequestedPaths | ForEach-Object {
            (Resolve-Path -LiteralPath $_).Path
        })
    }

    return ,@(
        (Resolve-Path -LiteralPath (Join-Path $repoRoot "src\TasteBudz.Database\sqlserver\000_schema_versions.sql")).Path,
        (Resolve-Path -LiteralPath (Join-Path $repoRoot "src\TasteBudz.Database\sqlserver\010_schema.sql")).Path,
        (Resolve-Path -LiteralPath (Join-Path $repoRoot "src\TasteBudz.Database\sqlserver\020_seed_reference_data.sql")).Path
    )
}

function Get-WebAppConnectionString {
    param(
        [string]$ResourceGroupName,
        [string]$AppName
    )

    $subscriptionId = Invoke-CapturedCommand "az" @("account", "show", "--query", "id", "--output", "tsv")
    $accessToken = Invoke-CapturedCommand "az" @("account", "get-access-token", "--resource", "https://management.azure.com/", "--query", "accessToken", "--output", "tsv") -Sensitive
    $uri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.Web/sites/$AppName/config/connectionstrings/list?api-version=2022-03-01"
    $json = Invoke-ArmJsonRequest "POST" $uri $accessToken "{}" | ConvertFrom-Json
    $connectionString = [string]$json.properties.TasteBudz.value

    if ([string]::IsNullOrWhiteSpace($connectionString)) {
        throw "The App Service connection string 'TasteBudz' is missing or blank."
    }

    return $connectionString
}

function Parse-ConnectionString {
    param([string]$ConnectionString)

    $builder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $ConnectionString

    if ([string]::IsNullOrWhiteSpace([string]$builder.DataSource) -or
        [string]::IsNullOrWhiteSpace([string]$builder.InitialCatalog) -or
        [string]::IsNullOrWhiteSpace([string]$builder.UserID) -or
        [string]::IsNullOrWhiteSpace([string]$builder.Password)) {
        throw "The resolved Azure SQL connection string is missing Data Source, Initial Catalog, User ID, or Password."
    }

    return [pscustomobject]@{
        DataSource = [string]$builder.DataSource
        Database = [string]$builder.InitialCatalog
        UserId = [string]$builder.UserID
        Password = [string]$builder.Password
    }
}

function Get-SqlServerNameFromDataSource {
    param([string]$DataSource)

    $server = $DataSource

    if ($server.StartsWith("tcp:", [StringComparison]::OrdinalIgnoreCase)) {
        $server = $server.Substring(4)
    }

    $commaIndex = $server.IndexOf(",")
    if ($commaIndex -ge 0) {
        $server = $server.Substring(0, $commaIndex)
    }

    $suffix = ".database.windows.net"
    if ($server.EndsWith($suffix, [StringComparison]::OrdinalIgnoreCase)) {
        $server = $server.Substring(0, $server.Length - $suffix.Length)
    }

    return $server
}

function Ensure-ClientFirewallRule {
    param(
        [string]$ResourceGroupName,
        [string]$ServerName
    )

    $script:clientIp = (Invoke-RestMethod "https://api.ipify.org").ToString().Trim()
    $script:firewallRuleName = "CodexClient-$([DateTime]::UtcNow.ToString('yyyyMMddHHmmss'))"

    Write-Host "Using temporary Azure SQL firewall rule '$script:firewallRuleName' for client IP $script:clientIp."
    $subscriptionId = Invoke-CapturedCommand "az" @("account", "show", "--query", "id", "--output", "tsv")
    $accessToken = Invoke-CapturedCommand "az" @("account", "get-access-token", "--resource", "https://management.azure.com/", "--query", "accessToken", "--output", "tsv") -Sensitive
    $uri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.Sql/servers/$ServerName/firewallRules/$($script:firewallRuleName)?api-version=2021-11-01"
    $body = @{
        properties = @{
            startIpAddress = $script:clientIp
            endIpAddress = $script:clientIp
        }
    } | ConvertTo-Json -Depth 4

    Invoke-RestMethod `
        -Method Put `
        -Uri $uri `
        -Headers @{ Authorization = "Bearer $accessToken" } `
        -ContentType "application/json" `
        -Body $body | Out-Null
}

function Remove-ClientFirewallRule {
    param(
        [string]$ResourceGroupName,
        [string]$ServerName
    )

    if ([string]::IsNullOrWhiteSpace($script:firewallRuleName)) {
        return
    }

    Write-Host "Removing temporary Azure SQL firewall rule '$script:firewallRuleName'."
    $subscriptionId = Invoke-CapturedCommand "az" @("account", "show", "--query", "id", "--output", "tsv")
    $accessToken = Invoke-CapturedCommand "az" @("account", "get-access-token", "--resource", "https://management.azure.com/", "--query", "accessToken", "--output", "tsv") -Sensitive
    $uri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.Sql/servers/$ServerName/firewallRules/$($script:firewallRuleName)?api-version=2021-11-01"

    try {
        Invoke-RestMethod `
            -Method Delete `
            -Uri $uri `
            -Headers @{ Authorization = "Bearer $accessToken" } | Out-Null
    }
    catch {
        $statusCode = if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $_.Exception.Response.StatusCode.value__
        } else {
            $null
        }

        if ($statusCode -ne 404) {
            throw
        }
    }
}

function Invoke-SqlScript {
    param(
        [string]$DataSource,
        [string]$Database,
        [string]$UserId,
        [string]$Password,
        [string]$Path
    )

    $arguments = @(
        "-S", $DataSource,
        "-d", $Database,
        "-U", $UserId,
        "-b",
        "-N",
        "-i", $Path
    )

    Write-Host ("sqlcmd -S {0} -d {1} -U {2} -b -N -i {3}" -f $DataSource, $Database, $UserId, $Path)

    if ($DryRun) {
        return
    }

    $env:SQLCMDPASSWORD = $Password

    try {
        & sqlcmd @arguments

        if ($LASTEXITCODE -ne 0) {
            throw "sqlcmd failed with exit code ${LASTEXITCODE} for script '$Path'."
        }
    }
    finally {
        Remove-Item Env:\SQLCMDPASSWORD -ErrorAction SilentlyContinue
    }
}

function Invoke-ReadinessProbe {
    param([string]$ConnectionString)

    $probeProject = Join-Path $PSScriptRoot "schema-readiness-probe\SchemaReadinessProbe.csproj"

    Write-Host "dotnet run --project `"$probeProject`" -c Release -v quiet"

    if ($DryRun) {
        return
    }

    $env:TASTEBUDZ_CONN = $ConnectionString

    try {
        $output = & dotnet run --project $probeProject -c Release -v quiet 2>&1
        $outputText = $output -join "`n"

        if ($LASTEXITCODE -ne 0) {
            throw "Readiness probe failed: $outputText"
        }

        $jsonText = (($output | ForEach-Object { $_.ToString() }) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -Last 1)

        if ([string]::IsNullOrWhiteSpace($jsonText)) {
            throw "Readiness probe produced no JSON output."
        }

        $json = $jsonText | ConvertFrom-Json
        Write-Host ($json | ConvertTo-Json -Depth 6)

        if (-not $json.ready) {
            throw "Readiness probe reported missing required schema objects."
        }
    }
    finally {
        Remove-Item Env:\TASTEBUDZ_CONN -ErrorAction SilentlyContinue
    }
}

try {
    Set-Location $repoRoot

    Write-Step "TasteBudz Azure SQL schema deployment"
    Write-Host "Repository: $repoRoot"
    Write-Host "Resource group: $ResourceGroup"
    Write-Host "Web app: $WebAppName"

    if ($DryRun) {
        Write-Host "Dry run enabled. No SQL scripts, firewall changes, or verification writes will run."
    }

    if ($Subscription) {
        Write-Step "Selecting Azure subscription"
        Invoke-RequiredCommand "az" @("account", "set", "--subscription", $Subscription)
    }

    Write-Step "Resolving SQL target"
    $scriptPaths = Resolve-ScriptPaths $ScriptPath
    $connectionString = Get-WebAppConnectionString $ResourceGroup $WebAppName
    $connection = Parse-ConnectionString $connectionString
    $sqlServerName = Get-SqlServerNameFromDataSource $connection.DataSource
    $effectiveSqlServerResourceGroup = if ([string]::IsNullOrWhiteSpace($SqlServerResourceGroup)) { $ResourceGroup } else { $SqlServerResourceGroup }

    Write-Host "SQL server: $sqlServerName"
    Write-Host "Database: $($connection.Database)"
    Write-Host "Script count: $($scriptPaths.Count)"
    foreach ($path in $scriptPaths) {
        Write-Host " - $path"
    }

    if ($AllowClientIp) {
        Write-Step "Opening temporary Azure SQL firewall rule"
        Ensure-ClientFirewallRule $effectiveSqlServerResourceGroup $sqlServerName
    }

    Write-Step "Applying SQL scripts"
    foreach ($path in $scriptPaths) {
        Invoke-SqlScript $connection.DataSource $connection.Database $connection.UserId $connection.Password $path
    }

    if (-not $SkipVerification) {
        Write-Step "Verifying required schema"
        Invoke-ReadinessProbe $connectionString
    }
}
finally {
    Set-Location $originalLocation

    if (-not $DryRun -and $AllowClientIp -and -not $KeepClientIpRule -and -not [string]::IsNullOrWhiteSpace($firewallRuleName) -and -not [string]::IsNullOrWhiteSpace($sqlServerName)) {
        Remove-ClientFirewallRule $effectiveSqlServerResourceGroup $sqlServerName
    }
}
