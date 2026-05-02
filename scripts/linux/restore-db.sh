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
docker exec "$CONTAINER" mkdir -p /var/opt/mssql/backup
docker cp "$BACKUP_FILE" "${CONTAINER}:${CONTAINER_PATH}"

echo "Ejecutando RESTORE DATABASE..."

# Security improvement: Pass password via stdin instead of command line argument
# to avoid exposing credentials in process list, shell history, or logs
RESTORE_SQL="ALTER DATABASE [$DATABASE] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [$DATABASE] FROM DISK = N'$CONTAINER_PATH' WITH REPLACE, RECOVERY; ALTER DATABASE [$DATABASE] SET MULTI_USER;"
echo "$DB_PASSWORD" | docker exec -i "$CONTAINER" /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -U sa -P -C \
    -Q "$RESTORE_SQL"

if [ $? -ne 0 ]; then
    echo "Error: la restauración falló. Revisa los mensajes anteriores."
    exit 1
fi

# Clean up sensitive data from memory
unset DB_PASSWORD

echo ""
echo "Base de datos restaurada correctamente desde: $BACKUP_FILE"
echo "Reinicia el backend si es necesario: docker restart lifehub-backend-dev"
