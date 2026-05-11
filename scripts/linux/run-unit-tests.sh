#!/bin/bash
# LifeHub - Tests unitarios (backend + frontend)
#
# Para tests de integración E2E contra el servidor en ejecución:
#   ./scripts/linux/run-tests.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

backend_failed=0
frontend_failed=0

echo ""
echo "============================================"
echo "  BACKEND TESTS  (dotnet test)"
echo "============================================"
echo ""

dotnet test "$ROOT/LifeHub-Backend.Tests" -v m
if [ $? -ne 0 ]; then backend_failed=1; fi

echo ""
echo "============================================"
echo "  FRONTEND TESTS  (ng test)"
echo "============================================"
echo ""

cd "$ROOT/LifeHub-Frontend"
npx ng test --watch=false --browsers=ChromeHeadless
if [ $? -ne 0 ]; then frontend_failed=1; fi
cd "$ROOT"

echo ""
echo "============================================"
echo "  RESULTADO FINAL"
echo "============================================"

if [ $backend_failed -eq 0 ] && [ $frontend_failed -eq 0 ]; then
    echo "  Todos los tests han pasado."
    exit 0
fi

[ $backend_failed  -eq 1 ] && echo "  Backend:  FALLIDO" || echo "  Backend:  OK"
[ $frontend_failed -eq 1 ] && echo "  Frontend: FALLIDO" || echo "  Frontend: OK"

exit 1
