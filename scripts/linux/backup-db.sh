#!/bin/bash

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Parse arguments: [-e envfile] [backup-dir]
ENV_FILE=""
BACKUP_DIR=""
while [[ $# -gt 0 ]]; do
    case "$1" in
        -e|--env) ENV_FILE="$2"; shift 2 ;;
        *)        BACKUP_DIR="$1"; shift ;;
    esac
done

if [ -z "$ENV_FILE" ];   then ENV_FILE="$PROJECT_ROOT/.env"; fi
if [ -z "$BACKUP_DIR" ]; then BACKUP_DIR="$PROJECT_ROOT/backups"; fi

if [ ! -f "$ENV_FILE" ]; then
    echo "Error: archivo de entorno no encontrado: $ENV_FILE"
    exit 1
fi
echo "Usando entorno: $ENV_FILE"

# Load env file
set -a
# shellcheck source=/dev/null
source "$ENV_FILE"
set +a

# Validate required variables
MISSING=()
[ -z "$DB_PASSWORD" ]   && MISSING+=("DB_PASSWORD")
[ -z "$DB_NAME" ]       && MISSING+=("DB_NAME")
[ -z "$DB_USER" ]       && MISSING+=("DB_USER")
[ -z "$DB_HOST" ]       && MISSING+=("DB_HOST")
[ -z "$SQL_CONTAINER" ] && MISSING+=("SQL_CONTAINER")
[ -z "$SQLCMD_PATH" ]   && MISSING+=("SQLCMD_PATH")

if [ ${#MISSING[@]} -gt 0 ]; then
    echo "Error: las siguientes variables no están definidas en $ENV_FILE:"
    for v in "${MISSING[@]}"; do echo "  - $v"; done
    exit 1
fi

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="${DB_NAME}_${TIMESTAMP}.bak"
CONTAINER_PATH="/var/opt/mssql/backup/${BACKUP_FILE}"

mkdir -p "$BACKUP_DIR"

echo "Contenedor: $SQL_CONTAINER"
echo "Creando directorio de backup en el contenedor..."
if [ -n "${MSYSTEM:-}" ]; then
    MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL="*" docker exec "$SQL_CONTAINER" mkdir -p /var/opt/mssql/backup
else
    docker exec "$SQL_CONTAINER" mkdir -p /var/opt/mssql/backup
fi

echo "Ejecutando BACKUP DATABASE..."

BACKUP_SQL="BACKUP DATABASE [$DB_NAME] TO DISK = N'$CONTAINER_PATH' WITH FORMAT, INIT, STATS = 10"
# shellcheck disable=SC2086
if [ -n "${MSYSTEM:-}" ]; then
    MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL="*" docker exec -e SQLCMDPASSWORD="$DB_PASSWORD" "$SQL_CONTAINER" "$SQLCMD_PATH" \
        $SQLCMD_OPTS -S "$DB_HOST" -U "$DB_USER" -Q "$BACKUP_SQL"
else
    docker exec -e SQLCMDPASSWORD="$DB_PASSWORD" "$SQL_CONTAINER" "$SQLCMD_PATH" \
        $SQLCMD_OPTS -S "$DB_HOST" -U "$DB_USER" -Q "$BACKUP_SQL"
fi

if [ $? -ne 0 ]; then
    echo "Error: el backup falló. Revisa que el contenedor $SQL_CONTAINER está en ejecución."
    exit 1
fi

echo "Copiando backup al host..."
if command -v cygpath >/dev/null 2>&1; then
    docker cp "${SQL_CONTAINER}:${CONTAINER_PATH}" "$(cygpath -w "${BACKUP_DIR}/${BACKUP_FILE}")"
elif [ -n "${MSYSTEM:-}" ]; then
    MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL="*" docker cp "${SQL_CONTAINER}:${CONTAINER_PATH}" "${BACKUP_DIR}/${BACKUP_FILE}"
else
    docker cp "${SQL_CONTAINER}:${CONTAINER_PATH}" "${BACKUP_DIR}/${BACKUP_FILE}"
fi

if [ $? -ne 0 ]; then
    echo "Error: no se pudo copiar el backup al host."
    exit 1
fi

unset DB_PASSWORD

echo ""
echo "Backup completado: ${BACKUP_DIR}/${BACKUP_FILE}"
