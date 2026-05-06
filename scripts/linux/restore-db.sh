#!/bin/bash

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# Parse arguments: [-e envfile] <backup-file>
ENV_FILE=""
BACKUP_FILE=""
while [[ $# -gt 0 ]]; do
    case "$1" in
        -e|--env) ENV_FILE="$2"; shift 2 ;;
        *)        BACKUP_FILE="$1"; shift ;;
    esac
done

if [ -z "$BACKUP_FILE" ]; then
    echo "Uso: ./scripts/linux/restore-db.sh [-e envfile] <ruta-al-backup.bak>"
    exit 1
fi

if [ -z "$ENV_FILE" ]; then ENV_FILE="$PROJECT_ROOT/.env"; fi

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
[ -z "$DB_PASSWORD" ]       && MISSING+=("DB_PASSWORD")
[ -z "$DB_NAME" ]           && MISSING+=("DB_NAME")
[ -z "$DB_USER" ]           && MISSING+=("DB_USER")
[ -z "$DB_HOST" ]           && MISSING+=("DB_HOST")
[ -z "$SQL_CONTAINER" ]     && MISSING+=("SQL_CONTAINER")
[ -z "$SQLCMD_PATH" ]       && MISSING+=("SQLCMD_PATH")
[ -z "$BACKEND_CONTAINER" ] && MISSING+=("BACKEND_CONTAINER")

if [ ${#MISSING[@]} -gt 0 ]; then
    echo "Error: las siguientes variables no están definidas en $ENV_FILE:"
    for v in "${MISSING[@]}"; do echo "  - $v"; done
    exit 1
fi

echo "Contenedor: $SQL_CONTAINER"

if [ ! -f "$BACKUP_FILE" ]; then
    echo "Error: archivo de backup no encontrado: $BACKUP_FILE"
    exit 1
fi

FILENAME=$(basename "$BACKUP_FILE")
CONTAINER_PATH="/var/opt/mssql/backup/${FILENAME}"

echo "Copiando backup al contenedor..."
if [ -n "${MSYSTEM:-}" ]; then
    MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL="*" docker exec "$SQL_CONTAINER" mkdir -p /var/opt/mssql/backup
else
    docker exec "$SQL_CONTAINER" mkdir -p /var/opt/mssql/backup
fi

if command -v cygpath >/dev/null 2>&1; then
    docker cp "$(cygpath -w "$BACKUP_FILE")" "${SQL_CONTAINER}:${CONTAINER_PATH}"
elif [ -n "${MSYSTEM:-}" ]; then
    MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL="*" docker cp "$BACKUP_FILE" "${SQL_CONTAINER}:${CONTAINER_PATH}"
else
    docker cp "$BACKUP_FILE" "${SQL_CONTAINER}:${CONTAINER_PATH}"
fi

if [ $? -ne 0 ]; then
    echo "Error: no se pudo copiar el archivo de backup al contenedor."
    exit 1
fi

echo "Ejecutando RESTORE DATABASE..."

RESTORE_SQL="ALTER DATABASE [$DB_NAME] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [$DB_NAME] FROM DISK = N'$CONTAINER_PATH' WITH REPLACE, RECOVERY; ALTER DATABASE [$DB_NAME] SET MULTI_USER;"
# shellcheck disable=SC2086
if [ -n "${MSYSTEM:-}" ]; then
    MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL="*" docker exec -e SQLCMDPASSWORD="$DB_PASSWORD" "$SQL_CONTAINER" "$SQLCMD_PATH" \
        $SQLCMD_OPTS -S "$DB_HOST" -U "$DB_USER" -Q "$RESTORE_SQL"
else
    docker exec -e SQLCMDPASSWORD="$DB_PASSWORD" "$SQL_CONTAINER" "$SQLCMD_PATH" \
        $SQLCMD_OPTS -S "$DB_HOST" -U "$DB_USER" -Q "$RESTORE_SQL"
fi

if [ $? -ne 0 ]; then
    echo "Error: la restauración falló. Revisa los mensajes anteriores."
    exit 1
fi

unset DB_PASSWORD

echo ""
echo "Base de datos restaurada correctamente desde: $BACKUP_FILE"
echo "Reinicia el backend: docker restart $BACKEND_CONTAINER"
