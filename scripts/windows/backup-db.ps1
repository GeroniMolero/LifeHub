Param(
    [string]$BackupDir = "",
    [string]$Container = "lifehub-sql-dev"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not $BackupDir) { $BackupDir = Join-Path $ProjectRoot "backups" }

# Load .env
$EnvFile = Join-Path $ProjectRoot ".env"
if (Test-Path $EnvFile) {
    Get-Content $EnvFile | ForEach-Object {
        if ($_ -match '^\s*([^#][^=]+)=(.+)$') {
            [System.Environment]::SetEnvironmentVariable($Matches[1].Trim(), $Matches[2].Trim())
        }
    }
}

$Password = $env:DB_PASSWORD
if (-not $Password) { throw "DB_PASSWORD no encontrado. Comprueba que existe el archivo .env" }

$Database   = "LifeHubDB"
$Timestamp  = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupFile = "LifeHub_$Timestamp.bak"
$ContainerPath = "/var/opt/mssql/backup/$BackupFile"

New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null

Write-Host "Creando directorio de backup en el contenedor..." -ForegroundColor Yellow
docker exec $Container mkdir -p /var/opt/mssql/backup

Write-Host "Ejecutando BACKUP DATABASE..." -ForegroundColor Yellow
docker exec $Container /opt/mssql-tools/bin/sqlcmd `
    -S localhost -U sa -P $Password `
    -Q "BACKUP DATABASE [$Database] TO DISK = N'$ContainerPath' WITH FORMAT, INIT, COMPRESSION, STATS = 10"

if ($LASTEXITCODE -ne 0) { throw "El backup falló. Revisa que el contenedor $Container está en ejecución." }

Write-Host "Copiando backup al host..." -ForegroundColor Yellow
docker cp "${Container}:${ContainerPath}" "$BackupDir\$BackupFile"

Write-Host ""
Write-Host "Backup completado: $BackupDir\$BackupFile" -ForegroundColor Green
