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
if [ -n "${MSYSTEM:-}" ]; then
    MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL="*" docker exec "$CONTAINER" mkdir -p /var/opt/mssql/backup
else
    docker exec "$CONTAINER" mkdir -p /var/opt/mssql/backup
fi

echo "Ejecutando BACKUP DATABASE..."

BACKUP_SQL="BACKUP DATABASE [$DATABASE] TO DISK = N'$CONTAINER_PATH' WITH FORMAT, INIT, COMPRESSION, STATS = 10"
if [ -n "${MSYSTEM:-}" ]; then
    MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL="*" docker exec -e SQLCMDPASSWORD="$DB_PASSWORD" "$CONTAINER" /opt/mssql-tools/bin/sqlcmd \
        -S localhost -U sa \
        -Q "$BACKUP_SQL"
else
    docker exec -e SQLCMDPASSWORD="$DB_PASSWORD" "$CONTAINER" /opt/mssql-tools/bin/sqlcmd \
        -S localhost -U sa \
        -Q "$BACKUP_SQL"
fi

if [ $? -ne 0 ]; then
    echo "Error: el backup falló. Revisa que el contenedor $CONTAINER está en ejecución."
    exit 1
fi

echo "Copiando backup al host..."
if command -v cygpath >/dev/null 2>&1; then
    docker cp "${CONTAINER}:${CONTAINER_PATH}" "$(cygpath -w "${BACKUP_DIR}/${BACKUP_FILE}")"
elif [ -n "${MSYSTEM:-}" ]; then
    MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL="*" docker cp "${CONTAINER}:${CONTAINER_PATH}" "${BACKUP_DIR}/${BACKUP_FILE}"
else
    docker cp "${CONTAINER}:${CONTAINER_PATH}" "${BACKUP_DIR}/${BACKUP_FILE}"
fi

if [ $? -ne 0 ]; then
    echo "Error: no se pudo copiar el backup al host."
    exit 1
fi

# Clean up sensitive data from memory
unset DB_PASSWORD

echo ""
echo "Backup completado: ${BACKUP_DIR}/${BACKUP_FILE}"
