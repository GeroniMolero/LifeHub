# LifeHub - Quick Start Script for Windows

Write-Host "🚀 Iniciando LifeHub con Docker..." -ForegroundColor Green
Write-Host ""
Write-Host "📘 Guia principal: revisa README.md para el flujo oficial de arranque" -ForegroundColor Yellow
Write-Host ""

# Verificar si Docker está instalado
$dockerCheck = docker --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Docker no está instalado. Por favor instala Docker desde https://www.docker.com/" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Docker está disponible" -ForegroundColor Green
Write-Host ""

# Opción de ejecución
if ($args[0] -eq "dev") {
    Write-Host "🔧 Iniciando en modo DESARROLLO con hot-reload..." -ForegroundColor Cyan
    docker compose -f docker-compose.dev.yml up --build
} elseif ($args[0] -eq "prod") {
    Write-Host "🏭 Iniciando en modo PRODUCCIÓN..." -ForegroundColor Cyan
    docker compose up --build
} else {
    Write-Host "📚 USO:" -ForegroundColor Yellow
    Write-Host "  .\start.ps1 dev    - Modo desarrollo (hot-reload activado)" -ForegroundColor Yellow
    Write-Host "  .\start.ps1 prod   - Modo producción (optimizado)" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "🔧 Iniciando en modo DESARROLLO por defecto..." -ForegroundColor Cyan
    docker compose -f docker-compose.dev.yml up --build
}
