Param(
    [switch]$SkipInstall
)

$ErrorActionPreference = "Stop"

function Test-PortListening {
    param([int]$Port)

    try {
        $connections = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction Stop
        return ($connections | Measure-Object).Count -gt 0
    }
    catch {
        return $false
    }
}

Write-Host "Starting LifeHub in local frontend mode..." -ForegroundColor Cyan

# 1) Backend + DB in Docker (detached)
docker compose -f docker-compose.dev.yml up -d

# 2) Frontend deps (optional skip)
if (-not $SkipInstall) {
    Write-Host "Installing frontend dependencies (npm ci)..." -ForegroundColor Yellow
    npm --prefix LifeHub-Frontend ci
}

# 3) Run Angular dev server in a separate process
Write-Host "Starting Angular dev server on http://localhost:4200 ..." -ForegroundColor Yellow

$projectRoot = $PWD.Path
$frontendPath = Join-Path $projectRoot "LifeHub-Frontend"
$cmdArgs = '/c start "LifeHub Frontend" cmd /k "cd /d ""' + $frontendPath + '"" && npm start"'

Start-Process -FilePath "cmd.exe" -ArgumentList $cmdArgs | Out-Null

$maxWaitSeconds = 30
$isUp = $false
for ($i = 0; $i -lt $maxWaitSeconds; $i++) {
    Start-Sleep -Seconds 1

    if (Test-PortListening -Port 4200) {
        $isUp = $true
        break
    }
}

if (-not $isUp) {
    throw "Frontend did not start on port 4200 in time. Check the 'LifeHub Frontend' CMD window for details."
}

Write-Host "Done." -ForegroundColor Green
Write-Host "Backend API: http://localhost:5000" -ForegroundColor Green
Write-Host "Frontend:    http://localhost:4200" -ForegroundColor Green
Write-Host "Use .\stop-local.ps1 to stop services." -ForegroundColor Green
