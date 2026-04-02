# LifeHub - Quick Start Script for Windows

Write-Host "Starting LifeHub..." -ForegroundColor Green
Write-Host ""
Write-Host "Main guide: check README.md for startup options." -ForegroundColor Yellow
Write-Host ""

# Verify Docker is available
$null = docker --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Docker is not installed or not available in PATH." -ForegroundColor Red
    Write-Host "Install Docker Desktop from https://www.docker.com/" -ForegroundColor Red
    exit 1
}

Write-Host "Docker detected." -ForegroundColor Green
Write-Host ""

$mode = ""
if ($args.Count -gt 0) {
    $mode = $args[0].ToLowerInvariant()
}

if ($mode -eq "dev") {
    Write-Host "Starting DEV mode (Docker dev stack)..." -ForegroundColor Cyan
    docker compose -f docker-compose.dev.yml up --build
}
elseif ($mode -eq "prod") {
    Write-Host "Starting PROD mode (Docker full stack)..." -ForegroundColor Cyan
    docker compose up --build
}
elseif ($mode -eq "local") {
    Write-Host "Starting LOCAL mode (Docker backend+db, frontend local)..." -ForegroundColor Cyan
    .\dev-local.ps1
}
elseif ($mode -eq "local-noinstall") {
    Write-Host "Starting LOCAL mode without npm ci..." -ForegroundColor Cyan
    .\dev-local.ps1 -SkipInstall
}
else {
    Write-Host "USAGE:" -ForegroundColor Yellow
    Write-Host "  .\start.ps1 dev               - Docker dev mode" -ForegroundColor Yellow
    Write-Host "  .\start.ps1 prod              - Docker production mode" -ForegroundColor Yellow
    Write-Host "  .\start.ps1 local             - Docker backend+db + local frontend (npm ci + ng serve)" -ForegroundColor Yellow
    Write-Host "  .\start.ps1 local-noinstall   - Same as local, but skips npm ci" -ForegroundColor Yellow
    Write-Host "  .\stop-local.ps1              - Stop local mode" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "No mode provided. Starting DEV mode by default..." -ForegroundColor Cyan
    docker compose -f docker-compose.dev.yml up --build
}
