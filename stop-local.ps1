$ErrorActionPreference = "SilentlyContinue"

Write-Host "Stopping Angular dev server on port 4200 (if running)..." -ForegroundColor Yellow
try {
    $listeners = Get-NetTCPConnection -LocalPort 4200 -State Listen -ErrorAction Stop
    foreach ($listener in $listeners) {
        Stop-Process -Id $listener.OwningProcess -Force -ErrorAction SilentlyContinue
    }
} catch {
    # Port not in use, nothing to stop
}

Write-Host "Stopping Docker dev services..." -ForegroundColor Yellow
docker compose -f docker-compose.dev.yml down

Write-Host "Done." -ForegroundColor Green
