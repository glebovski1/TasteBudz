param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $repoRoot

function Stop-TasteBudzProcesses {
    $patterns = @(
        'TasteBudz.Backend',
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

Write-Host "Starting backend..."
$backend = Start-Process -FilePath "dotnet" -ArgumentList @(
    "run",
    "--project", "src\TasteBudz.Backend\TasteBudz.Backend.csproj",
    "--launch-profile", "https",
    "--no-build"
) -WorkingDirectory $repoRoot -PassThru

Start-Sleep -Seconds 3

Write-Host "Starting MVC frontend..."
$frontend = Start-Process -FilePath "dotnet" -ArgumentList @(
    "run",
    "--project", "src\TasteBudz.Web.Mvc\TasteBudz.Web.Mvc.csproj",
    "--launch-profile", "https",
    "--no-build"
) -WorkingDirectory $repoRoot -PassThru

Write-Host ""
Write-Host "Backend PID:  $($backend.Id)"
Write-Host "Frontend PID: $($frontend.Id)"
Write-Host "Backend:  https://localhost:7118"
Write-Host "Frontend: https://localhost:7115"
Write-Host ""
Write-Host "Stop them with:"
Write-Host "  Stop-Process -Id $($backend.Id),$($frontend.Id)"
