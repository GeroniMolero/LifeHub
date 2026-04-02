$ErrorActionPreference = "SilentlyContinue"

Write-Host "Stopping Angular dev server on port 4200 (if running)..." -ForegroundColor Yellow
$listeners = Get-NetTCPConnection -LocalPort 4200 -State Listen
foreach ($listener in $listeners) {
    Stop-Process -Id $listener.OwningProcess -Force
}

Write-Host "Stopping Docker dev services..." -ForegroundColor Yellow
docker compose -f docker-compose.dev.yml down

Write-Host "Done." -ForegroundColor Green
