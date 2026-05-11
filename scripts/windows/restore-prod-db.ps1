Param(
    [switch]$List,
    [string]$BackupFile = "",
    [string]$LocalFile  = "",
    [string]$EnvFile    = ""
)

$ErrorActionPreference = "Stop"

$ProjectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not $EnvFile) { $EnvFile = Join-Path $ProjectRoot ".env.production" }

if (-not (Test-Path $EnvFile)) { throw "Archivo de entorno no encontrado: $EnvFile" }
Write-Host "Usando entorno: $EnvFile" -ForegroundColor DarkGray

Get-Content $EnvFile | ForEach-Object {
    if ($_ -match '^\s*([^#][^=]+)=(.*)$') {
        [System.Environment]::SetEnvironmentVariable($Matches[1].Trim(), $Matches[2].Trim())
    }
}

$Required = @("DB_PASSWORD","DB_NAME","DB_USER","DB_HOST","SQL_CONTAINER","SQLCMD_PATH","BACKEND_CONTAINER","PROD_SERVER_HOST","PROD_SERVER_USER")
$Missing  = $Required | Where-Object { -not [System.Environment]::GetEnvironmentVariable($_) }
if ($Missing) { throw "Variables no definidas en $EnvFile`: $($Missing -join ', ')" }

$Password         = $env:DB_PASSWORD
$Database         = $env:DB_NAME
$DbUser           = $env:DB_USER
$DbHost           = $env:DB_HOST
$Container        = $env:SQL_CONTAINER
$SqlcmdPath       = $env:SQLCMD_PATH
$SqlcmdOpts       = if ($env:SQLCMD_OPTS) { $env:SQLCMD_OPTS } else { "" }
$BackendContainer = $env:BACKEND_CONTAINER
$SshHost          = $env:PROD_SERVER_HOST
$SshUser          = $env:PROD_SERVER_USER

$SshTarget = "${SshUser}@${SshHost}"
$BackupDir = "/var/opt/mssql/backup"

# ── LIST ─────────────────────────────────────────────────────────────────────────
if ($List) {
    Write-Host "Backups disponibles en el servidor de producción ($SshHost):" -ForegroundColor Cyan
    ssh $SshTarget "docker exec $Container ls -lht $BackupDir 2>/dev/null || echo '(sin backups)'"
    exit 0
}

# ── Validar argumentos ────────────────────────────────────────────────────────────
if (-not $BackupFile -and -not $LocalFile) {
    Write-Host "Uso:" -ForegroundColor Yellow
    Write-Host "  .\restore-prod-db.ps1 -List                         # ver backups en el servidor"
    Write-Host "  .\restore-prod-db.ps1 -BackupFile <nombre.bak>      # restaurar backup existente en servidor"
    Write-Host "  .\restore-prod-db.ps1 -LocalFile <ruta\local.bak>   # subir desde este equipo y restaurar"
    Write-Host "  (opcional) -EnvFile <ruta>   usa otro archivo .env  (por defecto .env.production)"
    exit 1
}

# ── Subir archivo local si se especificó ──────────────────────────────────────────
if ($LocalFile) {
    if (-not (Test-Path $LocalFile)) { throw "Archivo no encontrado: $LocalFile" }
    $BackupFile = Split-Path $LocalFile -Leaf
    $RemoteTmp  = "/tmp/$BackupFile"

    Write-Host "Subiendo $BackupFile al servidor ($SshHost)..." -ForegroundColor Yellow
    scp $LocalFile "${SshTarget}:${RemoteTmp}"
    if ($LASTEXITCODE -ne 0) { throw "Error al subir el archivo." }

    Write-Host "Copiando al contenedor SQL..." -ForegroundColor Yellow
    ssh $SshTarget "docker exec $Container mkdir -p $BackupDir && docker cp $RemoteTmp ${Container}:${BackupDir}/$BackupFile && rm -f $RemoteTmp"
    if ($LASTEXITCODE -ne 0) { throw "Error al copiar el archivo al contenedor." }
}

$ContainerPath = "$BackupDir/$BackupFile"

# ── Verificar que el backup existe dentro del contenedor ──────────────────────────
Write-Host "Verificando archivo en el contenedor..." -ForegroundColor Yellow
ssh $SshTarget "docker exec $Container test -f '$ContainerPath'"
if ($LASTEXITCODE -ne 0) {
    throw "El archivo '$BackupFile' no existe en $BackupDir dentro del contenedor. Usa -List para ver los disponibles."
}

# ── Confirmación ──────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "ATENCIÓN: Esta operación sobreescribirá la base de datos de PRODUCCIÓN." -ForegroundColor Red
Write-Host "  Servidor     : $SshHost" -ForegroundColor Yellow
Write-Host "  Backup       : $BackupFile" -ForegroundColor Yellow
Write-Host "  Base de datos: $Database" -ForegroundColor Yellow
Write-Host ""
$confirm = Read-Host "Escribe 'RESTAURAR' para confirmar (o Enter para cancelar)"
if ($confirm -ne 'RESTAURAR') {
    Write-Host "Operación cancelada." -ForegroundColor Gray
    exit 0
}

# ── Ejecutar restauración vía SSH ─────────────────────────────────────────────────
$RestoreSQL = "ALTER DATABASE [$Database] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [$Database] FROM DISK = N'$ContainerPath' WITH REPLACE, RECOVERY; ALTER DATABASE [$Database] SET MULTI_USER;"

$remoteScript = @"
set -e
echo '[1/3] Deteniendo backend...'
docker stop $BackendContainer
echo '[2/3] Restaurando base de datos...'
docker exec -e "SQLCMDPASSWORD=$Password" $Container $SqlcmdPath $SqlcmdOpts -S $DbHost -U $DbUser -Q "$RestoreSQL"
echo '[3/3] Reiniciando backend...'
docker start $BackendContainer
"@

Write-Host ""
$remoteScript | ssh $SshTarget "bash -s"

if ($LASTEXITCODE -ne 0) {
    throw "La restauración falló. Revisa los logs de Docker en el servidor."
}

Remove-Variable Password -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "Base de datos restaurada correctamente desde: $BackupFile" -ForegroundColor Green
Write-Host "Backend reiniciado en $SshHost." -ForegroundColor Green
