Param(
    [Parameter(Mandatory=$true)]
    [string]$BackupFile,
    [string]$Container = "lifehub-sql-dev"
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent

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

if (-not (Test-Path $BackupFile)) { throw "Archivo de backup no encontrado: $BackupFile" }

$Database      = "LifeHubDB"
$FileName      = Split-Path $BackupFile -Leaf
$ContainerPath = "/var/opt/mssql/backup/$FileName"

Write-Host "Copiando backup al contenedor..." -ForegroundColor Yellow
docker exec $Container mkdir -p /var/opt/mssql/backup
docker cp $BackupFile "${Container}:${ContainerPath}"

Write-Host "Ejecutando RESTORE DATABASE..." -ForegroundColor Yellow

$RestoreSQL = @"
ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [$Database] FROM DISK = N'$ContainerPath' WITH REPLACE, RECOVERY;
ALTER DATABASE [$Database] SET MULTI_USER;
"@

$output = docker exec -e "SQLCMDPASSWORD=$Password" $Container /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -Q "$RestoreSQL" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host $output -ForegroundColor Red
    throw "La restauración falló. Revisa los mensajes anteriores."
}

Remove-Variable Password -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Base de datos restaurada correctamente desde: $BackupFile" -ForegroundColor Green
Write-Host "Reinicia el backend para que reconecte: docker restart lifehub-backend-dev" -ForegroundColor Yellow
