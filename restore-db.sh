#!/bin/bash

BACKUP_FILE="$1"
CONTAINER="lifehub-sql-dev"
DATABASE="LifeHubDB"

if [ -z "$BACKUP_FILE" ]; then
    echo "Uso: ./restore-db.sh <ruta-al-backup.bak>"
    exit 1
fi

# Load .env
if [ -f .env ]; then
    export $(grep -v '^\s*#' .env | xargs)
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
docker exec "$CONTAINER" /opt/mssql-tools/bin/sqlcmd \
    -S localhost -U sa -P "$DB_PASSWORD" \
    -Q "ALTER DATABASE [$DATABASE] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [$DATABASE] FROM DISK = N'$CONTAINER_PATH' WITH REPLACE, RECOVERY; ALTER DATABASE [$DATABASE] SET MULTI_USER;"

if [ $? -ne 0 ]; then
    echo "Error: la restauración falló. Revisa los mensajes anteriores."
    exit 1
fi

echo ""
echo "Base de datos restaurada correctamente desde: $BACKUP_FILE"
echo "Reinicia el backend si es necesario: docker restart lifehub-backend-dev"
