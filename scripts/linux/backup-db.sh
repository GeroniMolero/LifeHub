#!/bin/bash

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
BACKUP_DIR="${1:-$PROJECT_ROOT/backups}"
CONTAINER="lifehub-sql-dev"
DATABASE="LifeHubDB"

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

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="LifeHub_${TIMESTAMP}.bak"
CONTAINER_PATH="/var/opt/mssql/backup/${BACKUP_FILE}"

mkdir -p "$BACKUP_DIR"

echo "Creando directorio de backup en el contenedor..."
docker exec "$CONTAINER" mkdir -p /var/opt/mssql/backup

echo "Ejecutando BACKUP DATABASE..."

# Security improvement: Pass password via stdin instead of command line argument
# to avoid exposing credentials in process list, shell history, or logs
BACKUP_SQL="BACKUP DATABASE [$DATABASE] TO DISK = N'$CONTAINER_PATH' WITH FORMAT, INIT, COMPRESSION, STATS = 10"
echo "$DB_PASSWORD" | docker exec -i "$CONTAINER" /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -U sa -P -C \
    -Q "$BACKUP_SQL"

if [ $? -ne 0 ]; then
    echo "Error: el backup falló. Revisa que el contenedor $CONTAINER está en ejecución."
    exit 1
fi

echo "Copiando backup al host..."
docker cp "${CONTAINER}:${CONTAINER_PATH}" "${BACKUP_DIR}/${BACKUP_FILE}"

# Clean up sensitive data from memory
unset DB_PASSWORD

echo ""
echo "Backup completado: ${BACKUP_DIR}/${BACKUP_FILE}"
