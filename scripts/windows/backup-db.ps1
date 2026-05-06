Param(
    [string]$BackupDir = "",
    [string]$EnvFile   = ""
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not $BackupDir) { $BackupDir = Join-Path $ProjectRoot "backups" }
if (-not $EnvFile)   { $EnvFile   = Join-Path $ProjectRoot ".env" }

if (-not (Test-Path $EnvFile)) { throw "Archivo de entorno no encontrado: $EnvFile" }
Write-Host "Usando entorno: $EnvFile" -ForegroundColor DarkGray

# Load env file
Get-Content $EnvFile | ForEach-Object {
    if ($_ -match '^\s*([^#][^=]+)=(.*)$') {
        [System.Environment]::SetEnvironmentVariable($Matches[1].Trim(), $Matches[2].Trim())
    }
}

# Validate required variables
$Required = @("DB_PASSWORD", "DB_NAME", "DB_USER", "DB_HOST", "SQL_CONTAINER", "SQLCMD_PATH")
$Missing = $Required | Where-Object { -not [System.Environment]::GetEnvironmentVariable($_) }
if ($Missing) {
    throw "Las siguientes variables no están definidas en $EnvFile`: $($Missing -join ', ')"
}

$Password   = $env:DB_PASSWORD
$Database   = $env:DB_NAME
$DbUser     = $env:DB_USER
$DbHost     = $env:DB_HOST
$Container  = $env:SQL_CONTAINER
$SqlcmdPath = $env:SQLCMD_PATH
$SqlcmdOpts = if ($env:SQLCMD_OPTS) { $env:SQLCMD_OPTS -split '\s+' } else { @() }

Write-Host "Contenedor: $Container" -ForegroundColor Cyan

$Timestamp     = Get-Date -Format "yyyyMMdd_HHmmss"
$BackupFile    = "${Database}_$Timestamp.bak"
$ContainerPath = "/var/opt/mssql/backup/$BackupFile"

New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null

Write-Host "Creando directorio de backup en el contenedor..." -ForegroundColor Yellow
docker exec $Container mkdir -p /var/opt/mssql/backup

Write-Host "Ejecutando BACKUP DATABASE..." -ForegroundColor Yellow

$BackupSQL = "BACKUP DATABASE [$Database] TO DISK = N'$ContainerPath' WITH FORMAT, INIT, STATS = 10"

$output = docker exec -e "SQLCMDPASSWORD=$Password" $Container $SqlcmdPath $SqlcmdOpts -S $DbHost -U $DbUser -Q "$BackupSQL" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host $output -ForegroundColor Red
    throw "El backup falló. Revisa que el contenedor $Container está en ejecución."
}

Remove-Variable Password -ErrorAction SilentlyContinue

Write-Host "Copiando backup al host..." -ForegroundColor Yellow
docker cp "${Container}:${ContainerPath}" "$BackupDir\$BackupFile"

if ($LASTEXITCODE -ne 0) {
    throw "No se pudo copiar el backup al host."
}

Write-Host ""
Write-Host "Backup completado: $BackupDir\$BackupFile" -ForegroundColor Green
