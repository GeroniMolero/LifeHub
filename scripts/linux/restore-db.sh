#!/bin/bash

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
BACKUP_FILE="$1"
CONTAINER="lifehub-sql-dev"
DATABASE="LifeHubDB"

if [ -z "$BACKUP_FILE" ]; then
    echo "Uso: ./scripts/linux/restore-db.sh <ruta-al-backup.bak>"
    exit 1
fi

# Load .env with proper variable handling
if [ -f "$PROJECT_ROOT/.env" ]; then
    set -a
    # shellcheck source=/dev/null
    source "$PROJECT_ROOT/.env"
    set +a
fi

if [ -z "$DB_PASSWORD" ]; then
    echo "Error: DB_PASSWORD no encontrado. Comprueba que existe el archivo .env"
    exit 1
fi

if [ ! -f "$BACKUP_FILE" ]; then
    echo "Error: archivo de backup no encontrado: $BACKUP_FILE"
    exit 1
fi

FILENAME=$(basename "$BACKUP_FILE")
CONTAINER_PATH="/var/opt/mssql/backup/${FILENAME}"

echo "Copiando backup al contenedor..."
if [ -n "${MSYSTEM:-}" ]; then
    MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL="*" docker exec "$CONTAINER" mkdir -p /var/opt/mssql/backup
else
    docker exec "$CONTAINER" mkdir -p /var/opt/mssql/backup
fi
docker cp "$BACKUP_FILE" "${CONTAINER}:${CONTAINER_PATH}"

echo "Ejecutando RESTORE DATABASE..."

RESTORE_SQL="ALTER DATABASE [$DATABASE] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [$DATABASE] FROM DISK = N'$CONTAINER_PATH' WITH REPLACE, RECOVERY; ALTER DATABASE [$DATABASE] SET MULTI_USER;"
if [ -n "${MSYSTEM:-}" ]; then
    MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL="*" docker exec -e SQLCMDPASSWORD="$DB_PASSWORD" "$CONTAINER" /opt/mssql-tools/bin/sqlcmd \
        -S localhost -U sa \
        -Q "$RESTORE_SQL"
else
    docker exec -e SQLCMDPASSWORD="$DB_PASSWORD" "$CONTAINER" /opt/mssql-tools/bin/sqlcmd \
        -S localhost -U sa \
        -Q "$RESTORE_SQL"
fi

if [ $? -ne 0 ]; then
    echo "Error: la restauración falló. Revisa los mensajes anteriores."
    exit 1
fi

# Clean up sensitive data from memory
unset DB_PASSWORD

echo ""
echo "Base de datos restaurada correctamente desde: $BACKUP_FILE"
echo "Reinicia el backend si es necesario: docker restart lifehub-backend-dev"
