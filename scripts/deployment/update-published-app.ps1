[CmdletBinding()]
param(
    [string]$Subscription,
    [string]$ResourceGroup = "rg-tastebudz-prod",
    [string]$WebAppName = "tastebudz-prod-23df46c9",
    [string]$PublishRoot = "artifacts\publish",
    [switch]$KeepArtifacts,
    [switch]$DryRun
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"

$solutionPath = "TasteBudz.sln"
$mvcProjectPath = "src\TasteBudz.Web.Mvc\TasteBudz.Web.Mvc.csproj"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
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

function Invoke-KuduZipDeploy {
    param(
        [string]$ResourceGroupName,
        [string]$AppName,
        [string]$PackagePath
    )

    Write-Step "Deploying with Kudu zipdeploy fallback"
    $subscriptionId = Invoke-CapturedCommand "az" @("account", "show", "--query", "id", "--output", "tsv")
    $accessToken = Invoke-CapturedCommand "az" @("account", "get-access-token", "--resource", "https://management.azure.com/", "--query", "accessToken", "--output", "tsv") -Sensitive
    $policyBase = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.Web/sites/$AppName/basicPublishingCredentialsPolicies"
    $publishUri = "https://management.azure.com/subscriptions/$subscriptionId/resourceGroups/$ResourceGroupName/providers/Microsoft.Web/sites/$AppName/config/publishingcredentials/list?api-version=2022-03-01"
    $originalPolicies = @{}
    $enabledPolicies = New-Object System.Collections.Generic.List[string]

    try {
        foreach ($policy in @("scm", "ftp")) {
            $policyUri = "$policyBase/${policy}?api-version=2022-03-01"
            $policyJson = Invoke-ArmJsonRequest "GET" $policyUri $accessToken $null | ConvertFrom-Json
            $originalPolicies[$policy] = [bool]$policyJson.properties.allow
        }

        if (-not $originalPolicies["scm"]) {
            $policyUri = "$policyBase/scm?api-version=2022-03-01"
            Invoke-ArmJsonRequest "PUT" $policyUri $accessToken '{"properties":{"allow":true}}' | Out-Null
            $enabledPolicies.Add("scm")
            Write-Host "Temporarily enabled SCM basic publishing."
        }

        Start-Sleep -Seconds 5
        $credentials = Get-PublishingCredentials $publishUri $accessToken

        if ($credentials.IsRedacted -and -not $originalPolicies["ftp"]) {
            $policyUri = "$policyBase/ftp?api-version=2022-03-01"
            Invoke-ArmJsonRequest "PUT" $policyUri $accessToken '{"properties":{"allow":true}}' | Out-Null
            $enabledPolicies.Add("ftp")
            Write-Host "Temporarily enabled FTP basic publishing because ARM returned redacted publishing credentials."
            Start-Sleep -Seconds 5
            $credentials = Get-PublishingCredentials $publishUri $accessToken
        }

        if ($credentials.IsRedacted) {
            throw "Azure returned redacted publishing credentials after temporary policy changes."
        }

        $deployUri = "https://$AppName.scm.azurewebsites.net/api/zipdeploy?isAsync=true&clean=true"
        $arguments = @(
            "--ipv4",
            "--silent",
            "--show-error",
            "--fail-with-body",
            "--request",
            "POST",
            "--user",
            "$($credentials.User):$($credentials.Password)",
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
            throw "Kudu zipdeploy failed before returning an HTTP status."
        }

        $statusCode = [int]$Matches[1]

        if ($statusCode -lt 200 -or $statusCode -gt 299) {
            throw "Kudu zipdeploy failed with HTTP $statusCode."
        }

        Write-Host "Kudu zipdeploy accepted package with HTTP $statusCode."
    }
    finally {
        for ($i = $enabledPolicies.Count - 1; $i -ge 0; $i--) {
            $policy = $enabledPolicies[$i]
            if (-not $originalPolicies[$policy]) {
                $policyUri = "$policyBase/${policy}?api-version=2022-03-01"
                Invoke-ArmJsonRequest "PUT" $policyUri $accessToken '{"properties":{"allow":false}}' | Out-Null
                Write-Host "Restored $policy basic publishing to disabled."
            }
        }
    }
}

try {
    Set-Location $repoRoot

    Write-Step "TasteBudz Azure App Service update"
    Write-Host "Repository: $repoRoot"
    Write-Host "Resource group: $ResourceGroup"
    Write-Host "Web app: $WebAppName"
    Write-Host "Publish root: $publishRootPath"

    if ($DryRun) {
        Write-Host "Dry run enabled. No build, publish, deploy, Azure configuration, or artifact cleanup will run."
        Write-Host "Would run Release restore, build, test, publish, zip, deploy, and smoke verification."
        return
    }

    if ($Subscription) {
        Write-Step "Selecting Azure subscription"
        Invoke-RequiredCommand "az" @("account", "set", "--subscription", $Subscription)
    }

    Write-Step "Restoring, building, and testing"
    Invoke-RequiredCommand "dotnet" @("restore", $solutionPath)
    Invoke-RequiredCommand "dotnet" @("build", $solutionPath, "-c", "Release", "--no-restore")
    Invoke-RequiredCommand "dotnet" @("test", $solutionPath, "-c", "Release", "--no-build")

    Write-Step "Publishing MVC host"
    New-Item -ItemType Directory -Path $publishRootPath -Force | Out-Null
    Invoke-RequiredCommand "dotnet" @("publish", $mvcProjectPath, "-c", "Release", "-o", $publishDir, "--no-build")
    New-ZipPackage $publishDir $zipPath
    Write-Host "Created package: $zipPath"

    Write-Step "Deploying package"
    $azDeploySucceeded = Invoke-AzWebAppDeploy $ResourceGroup $WebAppName $zipPath

    if (-not $azDeploySucceeded) {
        Invoke-KuduZipDeploy $ResourceGroup $WebAppName $zipPath
    }

    Write-Step "Verifying deployed app"
    $baseUrl = "https://$WebAppName.azurewebsites.net"
    Wait-ForHttpStatus "Homepage" "GET" "$baseUrl/" "200"
    Wait-ForHttpStatus "Unauthenticated restaurants API" "GET" "$baseUrl/api/v1/restaurants" "401"
    Wait-ForHttpStatus "Unauthenticated SignalR negotiate" "POST" "$baseUrl/hubs/chat/negotiate?negotiateVersion=1" "401"
    Write-Host "Update completed."
}
finally {
    Set-Location $originalLocation

    if (-not $DryRun -and -not $KeepArtifacts) {
        foreach ($path in @($zipPath, $publishDir)) {
            if (Test-Path -LiteralPath $path) {
                $resolved = (Resolve-Path -LiteralPath $path).Path
                if (-not $resolved.StartsWith($publishRootPath, [StringComparison]::OrdinalIgnoreCase)) {
                    throw "Refusing to remove generated artifact outside publish root: $resolved"
                }

                Remove-Item -LiteralPath $resolved -Recurse -Force
                Write-Host "Removed generated artifact: $resolved"
            }
        }
    }
}
