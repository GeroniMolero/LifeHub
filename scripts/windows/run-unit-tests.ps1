# LifeHub - Tests unitarios (backend + frontend)
#
# Para tests de integración E2E contra el servidor en ejecución:
#   .\scripts\windows\run-tests.ps1

$root = Resolve-Path "$PSScriptRoot\..\.."
$backendFailed = $false
$frontendFailed = $false

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  BACKEND TESTS  (dotnet test)" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

dotnet test "$root\LifeHub-Backend.Tests" -v m
if ($LASTEXITCODE -ne 0) { $backendFailed = $true }

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  FRONTEND TESTS  (ng test)" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Push-Location "$root\LifeHub-Frontend"
npx ng test --watch=false --browsers=ChromeHeadless
if ($LASTEXITCODE -ne 0) { $frontendFailed = $true }
Pop-Location

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  RESULTADO FINAL" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan

if (-not $backendFailed -and -not $frontendFailed) {
    Write-Host "  Todos los tests han pasado." -ForegroundColor Green
    exit 0
}

if ($backendFailed)  { Write-Host "  Backend:  FALLIDO" -ForegroundColor Red }
else                 { Write-Host "  Backend:  OK" -ForegroundColor Green }

if ($frontendFailed) { Write-Host "  Frontend: FALLIDO" -ForegroundColor Red }
else                 { Write-Host "  Frontend: OK" -ForegroundColor Green }

exit 1
