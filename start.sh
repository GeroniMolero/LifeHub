#!/bin/bash

# LifeHub - Quick Start Script

echo "🚀 Iniciando LifeHub con Docker..."
echo ""
echo "📘 Guia principal: revisa README.md para el flujo oficial de arranque"
echo ""

# Verificar si Docker está instalado
if ! command -v docker &> /dev/null; then
    echo "❌ Docker no está instalado. Por favor instala Docker desde https://www.docker.com/"
    exit 1
fi

# Verificar si docker-compose está disponible
if ! docker compose version &> /dev/null; then
    echo "❌ Docker Compose no está disponible"
    exit 1
fi

echo "✅ Docker está disponible"
echo ""

# Opción de ejecución
if [ "$1" == "dev" ]; then
    echo "🔧 Iniciando en modo DESARROLLO con hot-reload..."
    docker compose -f docker-compose.dev.yml up --build
elif [ "$1" == "prod" ]; then
    echo "🏭 Iniciando en modo PRODUCCIÓN..."
    docker compose up --build
else
    echo "📚 USO:"
    echo "  ./start.sh dev    - Modo desarrollo (hot-reload activado)"
    echo "  ./start.sh prod   - Modo producción (optimizado)"
    echo ""
    echo "🔧 Iniciando en modo DESARROLLO por defecto..."
    docker compose -f docker-compose.dev.yml up --build
fi
