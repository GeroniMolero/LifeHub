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

# Security improvement: Pass password via stdin instead of command line argument
# to avoid exposing credentials in process list, shell history, or logs
$process = Start-Process -FilePath "cmd.exe" `
    -ArgumentList "/c echo $Password | docker exec -i $Container /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P -Q ""$RestoreSQL""" `
    -WindowStyle Hidden -PassThru -Wait

if ($process.ExitCode -ne 0) { 
    throw "La restauración falló. Revisa los mensajes anteriores." 
}

Write-Host ""
Write-Host "Base de datos restaurada correctamente desde: $BackupFile" -ForegroundColor Green
Write-Host "Reinicia el backend para que reconecte: docker restart $Container" -ForegroundColor Yellow
