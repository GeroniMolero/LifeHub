Param(
    [Parameter(Mandatory=$true)]
    [string]$BackupFile,
    [string]$EnvFile = ""
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not $EnvFile) { $EnvFile = Join-Path $ProjectRoot ".env" }

if (-not (Test-Path $EnvFile)) { throw "Archivo de entorno no encontrado: $EnvFile" }
Write-Host "Usando entorno: $EnvFile" -ForegroundColor DarkGray

# Load env file
Get-Content $EnvFile | ForEach-Object {
    if ($_ -match '^\s*([^#][^=]+)=(.*)$') {
        [System.Environment]::SetEnvironmentVariable($Matches[1].Trim(), $Matches[2].Trim())
    }
}

# Validate required variables
$Required = @("DB_PASSWORD", "DB_NAME", "DB_USER", "DB_HOST", "SQL_CONTAINER", "SQLCMD_PATH", "BACKEND_CONTAINER")
$Missing = $Required | Where-Object { -not [System.Environment]::GetEnvironmentVariable($_) }
if ($Missing) {
    throw "Las siguientes variables no están definidas en $EnvFile`: $($Missing -join ', ')"
}

$Password         = $env:DB_PASSWORD
$Database         = $env:DB_NAME
$DbUser           = $env:DB_USER
$DbHost           = $env:DB_HOST
$Container        = $env:SQL_CONTAINER
$SqlcmdPath       = $env:SQLCMD_PATH
$SqlcmdOpts       = if ($env:SQLCMD_OPTS) { $env:SQLCMD_OPTS -split '\s+' } else { @() }
$BackendContainer = $env:BACKEND_CONTAINER

Write-Host "Contenedor: $Container" -ForegroundColor Cyan

if (-not (Test-Path $BackupFile)) { throw "Archivo de backup no encontrado: $BackupFile" }

$FileName      = Split-Path $BackupFile -Leaf
$ContainerPath = "/var/opt/mssql/backup/$FileName"

Write-Host "Copiando backup al contenedor..." -ForegroundColor Yellow
docker exec $Container mkdir -p /var/opt/mssql/backup
docker cp $BackupFile "${Container}:${ContainerPath}"

if ($LASTEXITCODE -ne 0) {
    throw "No se pudo copiar el archivo de backup al contenedor."
}

Write-Host "Ejecutando RESTORE DATABASE..." -ForegroundColor Yellow

$RestoreSQL = @"
ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [$Database] FROM DISK = N'$ContainerPath' WITH REPLACE, RECOVERY;
ALTER DATABASE [$Database] SET MULTI_USER;
"@

$output = docker exec -e "SQLCMDPASSWORD=$Password" $Container $SqlcmdPath $SqlcmdOpts -S $DbHost -U $DbUser -Q "$RestoreSQL" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host $output -ForegroundColor Red
    throw "La restauración falló. Revisa los mensajes anteriores."
}

Remove-Variable Password -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Base de datos restaurada correctamente desde: $BackupFile" -ForegroundColor Green
Write-Host "Reinicia el backend: docker restart $BackendContainer" -ForegroundColor Yellow
