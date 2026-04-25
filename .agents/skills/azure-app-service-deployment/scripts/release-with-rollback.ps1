[CmdletBinding()]
param(
    [string]$Subscription,
    [string]$ResourceGroup = "rg-tastebudz-prod",
    [string]$WebAppName = "tastebudz-prod-23df46c9",
    [string]$PublishRoot = "artifacts\publish",
    [string[]]$ScriptPath,
    [string[]]$DatabaseRollbackScriptPath,
    [switch]$AllowClientIp,
    [switch]$AllowForwardOnlyDatabaseChange,
    [string]$PostDeployVerificationScript,
    [switch]$KeepArtifacts,
    [switch]$DryRun
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$solutionPath = "TasteBudz.sln"
$mvcProjectPath = "src\TasteBudz.Web.Mvc\TasteBudz.Web.Mvc.csproj"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")).Path
$originalLocation = (Get-Location).Path
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$publishRootPath = if ([System.IO.Path]::IsPathRooted($PublishRoot)) {
    $PublishRoot
} else {
    Join-Path $repoRoot $PublishRoot
}
$publishRootPath = [System.IO.Path]::GetFullPath($publishRootPath)
$publishDir = Join-Path $publishRootPath "TasteBudz.Web.Mvc-$timestamp"
$zipPath = Join-Path $publishRootPath "TasteBudz.Web.Mvc-$timestamp.zip"
$rollbackZipPath = Join-Path $publishRootPath "TasteBudz.Web.Mvc-rollback-$timestamp.zip"
$databaseApplied = $false
$appDeployed = $false
$preserveArtifacts = $false

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

function Invoke-RequiredPowerShell {
    param([string[]]$Arguments)

    $combinedArguments = @("-NoProfile", "-ExecutionPolicy", "Bypass") + $Arguments
    Invoke-RequiredCommand "powershell" $combinedArguments
}

function Invoke-JsonCommand {
    param(
        [string]$FilePath,
        [string[]]$Arguments
    )

    $output = & $FilePath @Arguments 2>&1
    $text = $output -join "`n"

    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code ${LASTEXITCODE}: $text"
    }

    $objectStart = $text.IndexOf("{")
    $objectEnd = $text.LastIndexOf("}")
    $jsonText = $null

    if ($objectStart -ge 0 -and $objectEnd -gt $objectStart) {
        $jsonText = $text.Substring($objectStart, $objectEnd - $objectStart + 1)
    } else {
        $arrayStart = $text.IndexOf("[")
        $arrayEnd = $text.LastIndexOf("]")

        if ($arrayStart -ge 0 -and $arrayEnd -gt $arrayStart) {
            $jsonText = $text.Substring($arrayStart, $arrayEnd - $arrayStart + 1)
        }
    }

    if ([string]::IsNullOrWhiteSpace($jsonText)) {
        throw "Command did not return parseable JSON."
    }

    try {
        return $jsonText | ConvertFrom-Json
    }
    catch {
        throw "Command returned invalid JSON after trimming CLI output: $($_.Exception.Message)"
    }
}

function Get-AzureContext {
    $account = Invoke-JsonCommand "az" @("account", "show", "--output", "json")
    $token = Invoke-JsonCommand "az" @("account", "get-access-token", "--resource", "https://management.azure.com/", "--output", "json")

    return [pscustomobject]@{
        SubscriptionId = [string]$account.id
        AccessToken = ([string]$token.accessToken) -replace "\s", ""
    }
}

function Resolve-RequiredPath {
    param(
        [string]$InputPath,
        [string]$Label
    )

    if ([string]::IsNullOrWhiteSpace($InputPath)) {
        throw "$Label path is empty."
    }

    $candidate = if ([System.IO.Path]::IsPathRooted($InputPath)) {
        $InputPath
    } else {
        Join-Path $repoRoot $InputPath
    }

    if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
        throw "$Label path does not exist: $InputPath"
    }

    return (Resolve-Path -LiteralPath $candidate).Path
}

function Resolve-RequiredPaths {
    param(
        [AllowNull()][string[]]$Paths,
        [string]$Label
    )

    $resolved = @()

    if (-not $Paths) {
        return $resolved
    }

    foreach ($path in $Paths) {
        $resolved += Resolve-RequiredPath $path $Label
    }

    return $resolved
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
        "--globoff",
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
        $arguments += @("--data-raw", $Body)
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

function Get-PublishingCredentials {
    param(
        [string]$PublishUri,
        [string]$AccessToken
    )

    $publishJson = Invoke-ArmJsonRequest "POST" $PublishUri $AccessToken "{}" | ConvertFrom-Json
    $user = [string]$publishJson.properties.publishingUserName
    $password = [string]$publishJson.properties.publishingPassword

    return [pscustomobject]@{
        User = $user
        Password = $password
        IsRedacted = [string]::IsNullOrWhiteSpace($user) -or
            [string]::IsNullOrWhiteSpace($password) -or
            $user -eq "REDACTED" -or
            $password -eq "REDACTED"
    }
}

function Use-KuduCredentials {
    param(
        [string]$ResourceGroupName,
        [string]$AppName,
        [scriptblock]$Operation
    )

    $context = Get-AzureContext
    $policyBase = "https://management.azure.com/subscriptions/$($context.SubscriptionId)/resourceGroups/$ResourceGroupName/providers/Microsoft.Web/sites/$AppName/basicPublishingCredentialsPolicies"
    $publishUri = "https://management.azure.com/subscriptions/$($context.SubscriptionId)/resourceGroups/$ResourceGroupName/providers/Microsoft.Web/sites/$AppName/config/publishingcredentials/list?api-version=2022-03-01"
    $originalPolicies = @{}
    $enabledPolicies = New-Object System.Collections.Generic.List[string]

    try {
        foreach ($policy in @("scm", "ftp")) {
            $policyUri = "$policyBase/${policy}?api-version=2022-03-01"
            $policyJson = Invoke-ArmJsonRequest "GET" $policyUri $context.AccessToken $null | ConvertFrom-Json
            $originalPolicies[$policy] = [bool]$policyJson.properties.allow
        }

        if (-not $originalPolicies["scm"]) {
            $policyUri = "$policyBase/scm?api-version=2022-03-01"
            Invoke-ArmJsonRequest "PUT" $policyUri $context.AccessToken '{"properties":{"allow":true}}' | Out-Null
            $enabledPolicies.Add("scm")
            Write-Host "Temporarily enabled SCM basic publishing."
        }

        Start-Sleep -Seconds 5
        $credentials = Get-PublishingCredentials $publishUri $context.AccessToken

        if ($credentials.IsRedacted -and -not $originalPolicies["ftp"]) {
            $policyUri = "$policyBase/ftp?api-version=2022-03-01"
            Invoke-ArmJsonRequest "PUT" $policyUri $context.AccessToken '{"properties":{"allow":true}}' | Out-Null
            $enabledPolicies.Add("ftp")
            Write-Host "Temporarily enabled FTP basic publishing because ARM returned redacted publishing credentials."
            Start-Sleep -Seconds 5
            $credentials = Get-PublishingCredentials $publishUri $context.AccessToken
        }

        if ($credentials.IsRedacted) {
            throw "Azure returned redacted publishing credentials after temporary policy changes."
        }

        & $Operation $credentials
    }
    finally {
        for ($i = $enabledPolicies.Count - 1; $i -ge 0; $i--) {
            $policy = $enabledPolicies[$i]
            if (-not $originalPolicies[$policy]) {
                $policyUri = "$policyBase/${policy}?api-version=2022-03-01"
                try {
                    Invoke-ArmJsonRequest "PUT" $policyUri $context.AccessToken '{"properties":{"allow":false}}' | Out-Null
                }
                catch {
                    $freshContext = Get-AzureContext
                    Invoke-ArmJsonRequest "PUT" $policyUri $freshContext.AccessToken '{"properties":{"allow":false}}' | Out-Null
                }

                Write-Host "Restored $policy basic publishing to disabled."
            }
        }
    }
}

function Get-ZipEntryNames {
    param([string]$PackagePath)

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    if (-not (Test-Path -LiteralPath $PackagePath -PathType Leaf) -or
        (Get-Item -LiteralPath $PackagePath).Length -le 0) {
        throw "Zip package was not created or is empty: $PackagePath"
    }

    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)

    try {
        return @($archive.Entries | ForEach-Object { $_.FullName })
    }
    finally {
        $archive.Dispose()
    }
}

function Test-ZipPackageContainsAny {
    param(
        [string[]]$Entries,
        [string[]]$Markers
    )

    foreach ($entry in $Entries) {
        $normalizedEntry = $entry.Replace("\", "/").TrimStart("/")

        foreach ($marker in $Markers) {
            $normalizedMarker = $marker.Replace("\", "/").TrimStart("/")

            if ($normalizedEntry.Equals($normalizedMarker, [StringComparison]::OrdinalIgnoreCase) -or
                $normalizedEntry.EndsWith("/$normalizedMarker", [StringComparison]::OrdinalIgnoreCase)) {
                return $true
            }
        }
    }

    return $false
}

function Assert-ZipPackage {
    param(
        [string]$PackagePath,
        [string]$Label,
        [string[]]$AnyRequiredEntry
    )

    $entries = @(Get-ZipEntryNames $PackagePath | Where-Object { -not $_.EndsWith("/") })

    if ($entries.Count -eq 0) {
        throw "$Label zip package contains no files: $PackagePath"
    }

    if ($AnyRequiredEntry -and
        -not (Test-ZipPackageContainsAny $entries $AnyRequiredEntry)) {
        throw "$Label zip package does not contain any expected app markers: $($AnyRequiredEntry -join ', ')"
    }

    Write-Host "$Label zip package contains $($entries.Count) files."
}

function Test-PublishPackageSafety {
    param([string]$PublishDirectory)

    foreach ($requiredFile in @("TasteBudz.Web.Mvc.dll", "web.config", "appsettings.json")) {
        $requiredPath = Join-Path $PublishDirectory $requiredFile

        if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
            throw "Publish output is missing required file: $requiredFile"
        }
    }

    $appSettingsFiles = @(Get-ChildItem -LiteralPath $PublishDirectory -Filter "appsettings*.json" -File |
        Select-Object -ExpandProperty Name)
    $unexpectedAppSettings = @($appSettingsFiles | Where-Object { $_ -ne "appsettings.json" })

    if ($unexpectedAppSettings.Count -gt 0) {
        throw "Publish output contains environment-specific appsettings files: $($unexpectedAppSettings -join ', ')"
    }

    $forbiddenPatterns = @("*.pfx", "*.publishsettings", "*.pubxml.user", "secrets.json", "*.user")
    $forbiddenFiles = @(Get-ChildItem -LiteralPath $PublishDirectory -Recurse -File | Where-Object {
        $matched = $false

        foreach ($pattern in $forbiddenPatterns) {
            if ($_.Name -like $pattern) {
                $matched = $true
                break
            }
        }

        $matched
    })

    if ($forbiddenFiles.Count -gt 0) {
        throw "Publish output contains forbidden secret or local-profile files: $($forbiddenFiles.Name -join ', ')"
    }

    Write-Host "Publish package safety check passed."
}

function Save-KuduWwwrootSnapshot {
    param(
        [string]$ResourceGroupName,
        [string]$AppName,
        [string]$DestinationPath
    )

    Write-Step "Saving current deployed app snapshot"

    Use-KuduCredentials $ResourceGroupName $AppName {
        param($Credentials)

        $downloadUri = "https://$AppName.scm.azurewebsites.net/api/zip/site/wwwroot/"
        $arguments = @(
            "--ipv4",
            "--silent",
            "--show-error",
            "--fail-with-body",
            "--request",
            "GET",
            "--user",
            "$($Credentials.User):$($Credentials.Password)",
            "--output",
            $DestinationPath,
            $downloadUri
        )

        & curl.exe @arguments

        if ($LASTEXITCODE -ne 0) {
            throw "Failed to download rollback snapshot from Kudu."
        }
    }

    if (-not (Test-Path -LiteralPath $DestinationPath) -or (Get-Item -LiteralPath $DestinationPath).Length -le 0) {
        throw "Rollback snapshot was not created or is empty."
    }

    Assert-ZipPackage $DestinationPath "Rollback snapshot" @("web.config", "TasteBudz.Web.Mvc.dll")
    Write-Host "Saved rollback snapshot: $DestinationPath"
}

function Invoke-KuduZipDeploy {
    param(
        [string]$ResourceGroupName,
        [string]$AppName,
        [string]$PackagePath,
        [string]$Label = "Kudu zipdeploy"
    )

    Write-Step $Label

    Use-KuduCredentials $ResourceGroupName $AppName {
        param($Credentials)

        $deployUri = "https://$AppName.scm.azurewebsites.net/api/zipdeploy?isAsync=false&clean=true"
        $arguments = @(
            "--ipv4",
            "--silent",
            "--show-error",
            "--fail-with-body",
            "--request",
            "POST",
            "--user",
            "$($Credentials.User):$($Credentials.Password)",
            "--header",
            "Content-Type: application/zip",
            "--data-binary",
            "@$PackagePath",
            "--write-out",
            "`nHTTP_STATUS:%{http_code}",
            $deployUri
        )

        $output = & curl.exe @arguments 2>&1
        $text = $output -join "`n"

        if ($LASTEXITCODE -ne 0 -or $text -notmatch "HTTP_STATUS:(\d{3})\s*$") {
            throw "$Label failed before returning an HTTP status."
        }

        $statusCode = [int]$Matches[1]

        if ($statusCode -lt 200 -or $statusCode -gt 299) {
            throw "$Label failed with HTTP $statusCode."
        }

        Write-Host "$Label completed with HTTP $statusCode."
    }
}

function Invoke-AzWebAppDeploy {
    param(
        [string]$ResourceGroupName,
        [string]$AppName,
        [string]$PackagePath
    )

    $arguments = @(
        "webapp",
        "deploy",
        "--resource-group",
        $ResourceGroupName,
        "--name",
        $AppName,
        "--src-path",
        $PackagePath,
        "--type",
        "zip",
        "--restart",
        "true",
        "--timeout",
        "900"
    )

    Write-Host (Format-Command "az" $arguments)
    & az @arguments

    return $LASTEXITCODE -eq 0
}

function Get-HttpStatus {
    param(
        [string]$Method,
        [string]$Uri
    )

    $output = & curl.exe --ipv4 --silent --show-error --output NUL --request $Method --write-out "%{http_code}" $Uri 2>&1

    if ($LASTEXITCODE -ne 0) {
        return "000"
    }

    return (($output -join "")).Trim()
}

function Wait-ForHttpStatus {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Uri,
        [string]$ExpectedStatus,
        [int]$Attempts = 12,
        [int]$DelaySeconds = 10
    )

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        $status = Get-HttpStatus $Method $Uri
        Write-Host "$Name attempt ${attempt}: HTTP $status"

        if ($status -eq $ExpectedStatus) {
            return
        }

        if ($attempt -lt $Attempts) {
            Start-Sleep -Seconds $DelaySeconds
        }
    }

    throw "$Name did not return expected HTTP $ExpectedStatus."
}

function Invoke-BasicSmokeVerification {
    param([string]$AppName)

    $baseUrl = "https://$AppName.azurewebsites.net"
    Wait-ForHttpStatus "Homepage" "GET" "$baseUrl/" "200"
    Wait-ForHttpStatus "Unauthenticated restaurants API" "GET" "$baseUrl/api/v1/restaurants" "401"
    Wait-ForHttpStatus "Unauthenticated SignalR negotiate" "POST" "$baseUrl/hubs/chat/negotiate?negotiateVersion=1" "401"
}

function New-ZipPackage {
    param(
        [string]$SourceDirectory,
        [string]$DestinationPath
    )

    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    if (Test-Path -LiteralPath $DestinationPath) {
        Remove-Item -LiteralPath $DestinationPath -Force
    }

    $sourcePath = (Resolve-Path -LiteralPath $SourceDirectory).Path
    $basePath = $sourcePath.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    $archive = [System.IO.Compression.ZipFile]::Open($DestinationPath, [System.IO.Compression.ZipArchiveMode]::Create)

    try {
        foreach ($file in [System.IO.Directory]::EnumerateFiles($sourcePath, "*", [System.IO.SearchOption]::AllDirectories)) {
            $entryName = $file.Substring($basePath.Length).Replace("\", "/")
            [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive,
                $file,
                $entryName,
                [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Invoke-DatabaseScripts {
    param(
        [string[]]$Paths,
        [string]$Label
    )

    if (-not $Paths -or $Paths.Count -eq 0) {
        return
    }

    Write-Step $Label
    $applyScript = Join-Path $repoRoot ".agents\skills\azure-sql-production-schema\scripts\apply-azure-sql-schema.ps1"
    $arguments = @("-File", $applyScript)

    if (-not [string]::IsNullOrWhiteSpace($Subscription)) {
        $arguments += @("-Subscription", $Subscription)
    }

    $arguments += @("-ResourceGroup", $ResourceGroup, "-WebAppName", $WebAppName, "-ScriptPath")
    $arguments += $Paths

    if ($AllowClientIp) {
        $arguments += "-AllowClientIp"
    }

    Invoke-RequiredPowerShell $arguments
}

function Invoke-PostDeployVerification {
    if ([string]::IsNullOrWhiteSpace($PostDeployVerificationScript)) {
        return
    }

    Write-Step "Running post-deploy verification script"
    $path = (Resolve-Path -LiteralPath $PostDeployVerificationScript).Path
    $previousBaseUrl = $env:TASTEBUDZ_BASE_URL
    $previousResourceGroup = $env:TASTEBUDZ_RESOURCE_GROUP
    $previousWebAppName = $env:TASTEBUDZ_WEB_APP_NAME

    try {
        $env:TASTEBUDZ_BASE_URL = "https://$WebAppName.azurewebsites.net"
        $env:TASTEBUDZ_RESOURCE_GROUP = $ResourceGroup
        $env:TASTEBUDZ_WEB_APP_NAME = $WebAppName
        Invoke-RequiredPowerShell @("-File", $path)
    }
    finally {
        if ($null -eq $previousBaseUrl) {
            Remove-Item Env:\TASTEBUDZ_BASE_URL -ErrorAction SilentlyContinue
        } else {
            $env:TASTEBUDZ_BASE_URL = $previousBaseUrl
        }

        if ($null -eq $previousResourceGroup) {
            Remove-Item Env:\TASTEBUDZ_RESOURCE_GROUP -ErrorAction SilentlyContinue
        } else {
            $env:TASTEBUDZ_RESOURCE_GROUP = $previousResourceGroup
        }

        if ($null -eq $previousWebAppName) {
            Remove-Item Env:\TASTEBUDZ_WEB_APP_NAME -ErrorAction SilentlyContinue
        } else {
            $env:TASTEBUDZ_WEB_APP_NAME = $previousWebAppName
        }
    }
}

function Invoke-Rollback {
    param([string]$FailureMessage)

    Write-Step "Release failed; attempting rollback"
    Write-Host $FailureMessage
    $rollbackFailures = New-Object System.Collections.Generic.List[string]

    if ($databaseApplied) {
        if ($DatabaseRollbackScriptPath -and $DatabaseRollbackScriptPath.Count -gt 0) {
            try {
                Invoke-DatabaseScripts $DatabaseRollbackScriptPath "Applying database rollback scripts"
            }
            catch {
                $rollbackFailures.Add("Database rollback failed: $($_.Exception.Message)")
            }
        } else {
            Write-Host "No database rollback scripts were supplied. App rollback can proceed, but database changes remain applied."
        }
    }

    if ($appDeployed -and (Test-Path -LiteralPath $rollbackZipPath)) {
        try {
            Invoke-KuduZipDeploy $ResourceGroup $WebAppName $rollbackZipPath "Rolling back App Service package"
            Write-Step "Verifying rolled back app"
            Invoke-BasicSmokeVerification $WebAppName
        }
        catch {
            $rollbackFailures.Add("App rollback failed: $($_.Exception.Message)")
        }
    } elseif (Test-Path -LiteralPath $rollbackZipPath) {
        Write-Host "App package was not deployed; rollback snapshot retained for manual recovery if needed."
    } else {
        Write-Host "No rollback app snapshot is available."
    }

    if ($rollbackFailures.Count -gt 0) {
        $joinedFailures = $rollbackFailures -join " | "
        throw "Rollback completed with failures: $joinedFailures"
    }
}

try {
    Set-Location $repoRoot

    Write-Step "TasteBudz reliable Azure release"
    Write-Host "Repository: $repoRoot"
    Write-Host "Resource group: $ResourceGroup"
    Write-Host "Web app: $WebAppName"
    Write-Host "Publish root: $publishRootPath"

    if ($ScriptPath -and $ScriptPath.Count -gt 0 -and
        (-not $DatabaseRollbackScriptPath -or $DatabaseRollbackScriptPath.Count -eq 0) -and
        -not $AllowForwardOnlyDatabaseChange) {
        throw "Database scripts were supplied without rollback scripts. Supply -DatabaseRollbackScriptPath or explicitly pass -AllowForwardOnlyDatabaseChange."
    }

    $ScriptPath = Resolve-RequiredPaths $ScriptPath "Database script"
    $DatabaseRollbackScriptPath = Resolve-RequiredPaths $DatabaseRollbackScriptPath "Database rollback script"

    if (-not [string]::IsNullOrWhiteSpace($PostDeployVerificationScript)) {
        $PostDeployVerificationScript = Resolve-RequiredPath $PostDeployVerificationScript "Post-deploy verification script"
    }

    if ($DryRun) {
        Write-Host "Dry run enabled. No build, SQL changes, deployment, rollback snapshot, or cleanup will run."
        Write-Host "Would validate, snapshot app, package app, apply database scripts, deploy app, verify, and rollback on failure."
        return
    }

    if ($Subscription) {
        Write-Step "Selecting Azure subscription"
        Invoke-RequiredCommand "az" @("account", "set", "--subscription", $Subscription)
    }

    Write-Step "Restoring, building, testing, and checking diff whitespace"
    Invoke-RequiredCommand "dotnet" @("restore", $solutionPath)
    Invoke-RequiredCommand "dotnet" @("build", $solutionPath, "-c", "Release", "--no-restore")
    Invoke-RequiredCommand "dotnet" @("test", $solutionPath, "-c", "Release", "--no-build")
    Invoke-RequiredCommand "git" @("diff", "--check")

    New-Item -ItemType Directory -Path $publishRootPath -Force | Out-Null
    Save-KuduWwwrootSnapshot $ResourceGroup $WebAppName $rollbackZipPath

    Write-Step "Publishing MVC host"
    Invoke-RequiredCommand "dotnet" @("publish", $mvcProjectPath, "-c", "Release", "-o", $publishDir, "--no-build")
    Test-PublishPackageSafety $publishDir
    New-ZipPackage $publishDir $zipPath
    Assert-ZipPackage $zipPath "New app" @("web.config", "TasteBudz.Web.Mvc.dll")
    Write-Host "Created package: $zipPath"

    try {
        if ($ScriptPath -and $ScriptPath.Count -gt 0) {
            $databaseApplied = $true
        }
        Invoke-DatabaseScripts $ScriptPath "Applying database migration scripts"

        Write-Step "Deploying package"
        $azDeploySucceeded = Invoke-AzWebAppDeploy $ResourceGroup $WebAppName $zipPath
        if (-not $azDeploySucceeded) {
            Invoke-KuduZipDeploy $ResourceGroup $WebAppName $zipPath
        }

        $appDeployed = $true

        Write-Step "Verifying deployed app"
        Invoke-BasicSmokeVerification $WebAppName
        Invoke-PostDeployVerification

        Write-Host "Reliable release completed."
    }
    catch {
        $preserveArtifacts = $true
        Invoke-Rollback $_.Exception.Message
        throw
    }
}
finally {
    Set-Location $originalLocation

    if (-not $DryRun -and -not $KeepArtifacts -and -not $preserveArtifacts) {
        foreach ($path in @($zipPath, $publishDir, $rollbackZipPath)) {
            if (Test-Path -LiteralPath $path) {
                $resolved = (Resolve-Path -LiteralPath $path).Path
                if (-not $resolved.StartsWith($publishRootPath, [StringComparison]::OrdinalIgnoreCase)) {
                    throw "Refusing to remove generated artifact outside publish root: $resolved"
                }

                Remove-Item -LiteralPath $resolved -Recurse -Force
                Write-Host "Removed generated artifact: $resolved"
            }
        }
    } elseif (-not $DryRun -and $preserveArtifacts) {
        Write-Host "Release failed; keeping generated artifacts in $publishRootPath for diagnosis or manual recovery."
    }
}
