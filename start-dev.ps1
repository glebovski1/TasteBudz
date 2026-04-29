<#
.SYNOPSIS
Starts the TasteBudz single-host local development app.

.DESCRIPTION
Builds the solution unless skipped, stops any existing TasteBudz web host
processes, configures the app for local SQLite persistence, and starts the MVC
host that also serves API controllers and SignalR.

.PARAMETER SkipBuild
Starts the already-built app without running `dotnet build`.

.PARAMETER ResetDatabase
Deletes the ignored local SQLite database and sidecar files before startup so
the app recreates them from the source-controlled SQL scripts.
#>
param(
    [switch]$SkipBuild,
    [switch]$ResetDatabase
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $repoRoot

function Stop-TasteBudzProcesses {
    # A local restart should not leave an old web host bound to the same ports.
    $patterns = @(
        'TasteBudz.Web.Mvc'
    )

    $running = @()

    $running += Get-CimInstance Win32_Process |
        Where-Object {
            $_.Name -in @('TasteBudz.Backend.exe', 'TasteBudz.Web.Mvc.exe')
        }

    $running += Get-CimInstance Win32_Process |
        Where-Object {
            $_.Name -eq 'dotnet.exe' -and
            $null -ne $_.CommandLine -and
            ($patterns | Where-Object { $_.CommandLine -like "*$_*" } | Select-Object -First 1)
        }

    if ($running)
    {
        $ids = $running.ProcessId | Sort-Object -Unique
        Write-Host "Stopping existing TasteBudz processes: $($ids -join ', ')"
        Stop-Process -Id $ids -Force
        Start-Sleep -Seconds 2
    }
}

Stop-TasteBudzProcesses

if (-not $SkipBuild)
{
    Write-Host "Building solution..."
    dotnet build TasteBudz.sln

    if ($LASTEXITCODE -ne 0)
    {
        throw "dotnet build failed with exit code $LASTEXITCODE."
    }
}

$localDataDirectory = Join-Path $repoRoot ".codex-temp"
$localDatabase = Join-Path $localDataDirectory "TasteBudz.local.sqlite"
New-Item -ItemType Directory -Force -Path $localDataDirectory | Out-Null

if ($ResetDatabase)
{
    # SQLite sidecar files must be removed with the database to avoid stale state.
    Remove-Item -LiteralPath $localDatabase -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath "$localDatabase-shm" -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath "$localDatabase-wal" -Force -ErrorAction SilentlyContinue
}

# Environment variables deliberately override launch profiles and user secrets
# so local runs always use the ignored source-first SQLite database.
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:BackendApi__BaseUrl = ""
$env:Persistence__Provider = "Sqlite"
$env:Persistence__InitializeSqliteOnStartup = "true"
$env:Persistence__SeedTestDataOnStartup = "true"
$env:ConnectionStrings__TasteBudz = "Data Source=$localDatabase;Foreign Keys=True;Pooling=False"

Write-Host "Starting TasteBudz web host..."
$web = Start-Process -FilePath "dotnet" -ArgumentList @(
    "run",
    "--project", "src\TasteBudz.Web.Mvc\TasteBudz.Web.Mvc.csproj",
    "--no-launch-profile",
    "--no-build",
    "--urls", "https://localhost:7115;http://localhost:5019"
) -WorkingDirectory $repoRoot -PassThru

Write-Host ""
Write-Host "Web host PID: $($web.Id)"
Write-Host "MVC + API + SignalR: https://localhost:7115"
Write-Host "Local SQLite database: $localDatabase"
Write-Host "Seeded test users use password: TasteBudz123!"
Write-Host ""
Write-Host "Stop them with:"
Write-Host "  Stop-Process -Id $($web.Id)"
