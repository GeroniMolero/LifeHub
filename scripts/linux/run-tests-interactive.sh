#!/bin/bash

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
OUTPUT_DIR="${1:-$PROJECT_ROOT/documentacion}"

ADMIN_EMAIL=""; ADMIN_PASS=""
if [ -f "$PROJECT_ROOT/.env" ]; then
    while IFS= read -r line; do
        [[ "$line" =~ ^[[:space:]]*# ]] && continue
        [[ "$line" =~ ^[[:space:]]*$ ]] && continue
        key="${line%%=*}"; val="${line#*=}"
        key="${key#"${key%%[![:space:]]*}"}"; key="${key%"${key##*[![:space:]]}"}"
        case "$key" in
            ADMIN_EMAIL)    ADMIN_EMAIL="$val" ;;
            ADMIN_PASSWORD) ADMIN_PASS="$val" ;;
        esac
    done < "$PROJECT_ROOT/.env"
fi

# ── MENU ──────────────────────────────────────────────────────────────────────

clear
echo "============================================"
echo "   LifeHub  --  Test Runner Interactivo    "
echo "============================================"
echo ""

read -rp "URL de la API  [http://localhost:5000/api]: " url_input
BASE_URL="${url_input:-http://localhost:5000/api}"

echo ""
echo "Modulos disponibles:"
echo "  [1] AUTH             (8 tests)"
echo "  [2] ESPACIOS         (5 tests)"
echo "  [3] DOCUMENTOS       (9 tests)"
echo "  [4] PANEL ADMIN      (6 tests)"
echo "  [5] SEGURIDAD        (2 tests)"
echo "  [A] TODOS"
echo ""
read -rp "Seleccion  (ej: 1 3  o  A para todos): " sel_input
sel_upper=$(echo "$sel_input" | tr '[:lower:]' '[:upper:]')

DO_AUTH=false; DO_SPACE=false; DO_DOC=false; DO_ADMIN=false; DO_SEC=false

if [ -z "$sel_upper" ] || [ "$sel_upper" = "A" ]; then
    DO_AUTH=true; DO_SPACE=true; DO_DOC=true; DO_ADMIN=true; DO_SEC=true
else
    echo "$sel_upper" | grep -q "1" && DO_AUTH=true
    echo "$sel_upper" | grep -q "2" && DO_SPACE=true
    echo "$sel_upper" | grep -q "3" && DO_DOC=true
    echo "$sel_upper" | grep -q "4" && DO_ADMIN=true
    echo "$sel_upper" | grep -q "5" && DO_SEC=true
fi

NEEDS_USER=false; NEEDS_ADMIN=false
($DO_SPACE || $DO_DOC || $DO_ADMIN || $DO_SEC) && NEEDS_USER=true
$DO_ADMIN && NEEDS_ADMIN=true

SELECTED=""
$DO_AUTH  && SELECTED="${SELECTED:+$SELECTED, }AUTH"
$DO_SPACE && SELECTED="${SELECTED:+$SELECTED, }ESPACIOS"
$DO_DOC   && SELECTED="${SELECTED:+$SELECTED, }DOCUMENTOS"
$DO_ADMIN && SELECTED="${SELECTED:+$SELECTED, }ADMIN"
$DO_SEC   && SELECTED="${SELECTED:+$SELECTED, }SEGURIDAD"

echo ""
echo "  Modulos : $SELECTED"
echo "  Endpoint: $BASE_URL"

# ── VALORES PERSONALIZADOS ─────────────────────────────────────────────────────

TIMESTAMP=$(date +%Y%m%d_%H%M%S)

echo ""
echo "Valores de prueba (Enter para usar el valor por defecto):"
echo ""

def_email="autotest_${TIMESTAMP}@lifehub-auto.test"
def_pass="AutoTest123!"
read -rp "  Email de prueba        [$def_email]: " inp
TEST_EMAIL="${inp:-$def_email}"
read -rp "  Contrasena de prueba   [$def_pass]: " inp
TEST_PASS="${inp:-$def_pass}"

SPACE_NAME="Espacio AutoTest $TIMESTAMP"
DOC_TITLE="Doc AutoTest $TIMESTAMP"
DOC_CONTENT="# Test\nContenido de prueba."
ADMIN_DOMAIN="autotest-$TIMESTAMP.io"

if $DO_SPACE; then
    read -rp "  Nombre del espacio     [$SPACE_NAME]: " inp
    SPACE_NAME="${inp:-$SPACE_NAME}"
fi
if $DO_DOC; then
    read -rp "  Titulo del documento   [$DOC_TITLE]: " inp
    DOC_TITLE="${inp:-$DOC_TITLE}"
    read -rp "  Contenido del documento [$DOC_CONTENT]: " inp
    DOC_CONTENT="${inp:-$DOC_CONTENT}"
fi
if $DO_ADMIN; then
    read -rp "  Dominio admin          [$ADMIN_DOMAIN]: " inp
    ADMIN_DOMAIN="${inp:-$ADMIN_DOMAIN}"
fi

echo ""

USER_TOKEN=""; ADMIN_TOKEN=""
SPACE_ID=""; DOC_ID=""; VERSION_ID=""; WEBSITE_ID=""

PASS_COUNT=0; FAIL_COUNT=0; SKIP_COUNT=0; TOTAL=0
ALL_IDS=(); ALL_DESCS=(); ALL_EXPECTED=(); ALL_ACTUAL=(); ALL_STATUS=(); ALL_SECTION=()
FAIL_IDS=(); FAIL_DESCS=(); FAIL_EXPECTED=(); FAIL_ACTUAL=()
CURRENT_SECTION=""

section() { CURRENT_SECTION="$1"; echo ""; echo "--- $1 ---"; }

json_val() {
    local key="$1" json="$2" val
    val=$(echo "$json" | grep -o "\"$key\":\"[^\"]*\"" | sed "s/\"$key\":\"//;s/\"$//")
    [ -z "$val" ] && val=$(echo "$json" | grep -o "\"$key\":[0-9]*" | sed "s/\"$key\"://")
    echo "$val"
}

invoke_api_test() {
    local id="$1" desc="$2" method="$3" url="$4" body="${5:-}" token="${6:-}" expected="$7" contains="${8:-}"
    local curl_args=(-s -o /tmp/lh_resp.txt -w "%{http_code}" -X "$method" -H "Content-Type: application/json")
    [ -n "$token" ] && curl_args+=(-H "Authorization: Bearer $token")
    [ -n "$body" ]  && curl_args+=(-d "$body")
    local actual; actual=$(curl "${curl_args[@]}" "${BASE_URL}${url}" 2>/dev/null) || actual="0"
    local response_body; response_body=$(cat /tmp/lh_resp.txt 2>/dev/null)
    local pass=true
    [ "$actual" != "$expected" ] && pass=false
    if $pass && [ -n "$contains" ]; then echo "$response_body" | grep -qF "$contains" || pass=false; fi
    ALL_IDS+=("$id"); ALL_DESCS+=("$desc"); ALL_EXPECTED+=("$expected")
    ALL_ACTUAL+=("$actual"); ALL_SECTION+=("$CURRENT_SECTION"); TOTAL=$((TOTAL + 1))
    if $pass; then
        ALL_STATUS+=("PASS"); PASS_COUNT=$((PASS_COUNT + 1))
        echo "  [OK  ] $id - $desc  (esperado $expected, real $actual)"
        cp /tmp/lh_resp.txt /tmp/lh_last_resp.txt 2>/dev/null
        return 0
    else
        ALL_STATUS+=("FAIL"); FAIL_COUNT=$((FAIL_COUNT + 1))
        FAIL_IDS+=("$id"); FAIL_DESCS+=("$desc"); FAIL_EXPECTED+=("$expected"); FAIL_ACTUAL+=("$actual")
        echo "  [FAIL] $id - $desc  (esperado $expected, real $actual)"
        return 1
    fi
}

skip_test() {
    local id="$1" desc="$2" reason="$3"
    ALL_IDS+=("$id"); ALL_DESCS+=("$desc"); ALL_EXPECTED+=("-"); ALL_ACTUAL+=("SKIP")
    ALL_STATUS+=("SKIP"); ALL_SECTION+=("$CURRENT_SECTION"); TOTAL=$((TOTAL + 1)); SKIP_COUNT=$((SKIP_COUNT + 1))
    echo "  [SKIP] $id - $desc ($reason)"
}

# ── SETUP SILENCIOSO DE TOKENS ─────────────────────────────────────────────────

if $NEEDS_USER && ! $DO_AUTH; then
    echo "  [setup] Registrando usuario de prueba..."
    curl -s -o /dev/null -X POST -H "Content-Type: application/json" \
        -d "{\"email\":\"$TEST_EMAIL\",\"fullName\":\"Test AutoScript\",\"password\":\"$TEST_PASS\",\"confirmPassword\":\"$TEST_PASS\"}" \
        "$BASE_URL/auth/register" 2>/dev/null || true
    resp=$(curl -s -X POST -H "Content-Type: application/json" \
        -d "{\"email\":\"$TEST_EMAIL\",\"password\":\"$TEST_PASS\"}" \
        "$BASE_URL/auth/login" 2>/dev/null)
    USER_TOKEN=$(json_val "token" "$resp")
    [ -n "$USER_TOKEN" ] && echo "  [setup] Token de usuario obtenido." || echo "  [setup] No se pudo obtener token de usuario."
fi

if $NEEDS_ADMIN && ! $DO_AUTH && [ -n "$ADMIN_EMAIL" ] && [ -n "$ADMIN_PASS" ]; then
    resp=$(curl -s -X POST -H "Content-Type: application/json" \
        -d "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASS\"}" \
        "$BASE_URL/auth/login" 2>/dev/null)
    ADMIN_TOKEN=$(json_val "token" "$resp")
    [ -n "$ADMIN_TOKEN" ] && echo "  [setup] Token de admin obtenido." || echo "  [setup] No se pudo obtener token de admin."
fi

# ── BLOQUE 1: AUTH ─────────────────────────────────────────────────────────────

if $DO_AUTH; then
    section "AUTH"
    invoke_api_test "T-AUTH-01" "Registro nuevo usuario" POST /auth/register \
        "{\"email\":\"$TEST_EMAIL\",\"fullName\":\"Test AutoScript\",\"password\":\"$TEST_PASS\",\"confirmPassword\":\"$TEST_PASS\"}" \
        "" "200" '"success":true' || true
    invoke_api_test "T-AUTH-02" "Registro email duplicado" POST /auth/register \
        "{\"email\":\"$TEST_EMAIL\",\"fullName\":\"Test AutoScript\",\"password\":\"$TEST_PASS\",\"confirmPassword\":\"$TEST_PASS\"}" \
        "" "400" || true
    invoke_api_test "T-AUTH-03" "Registro email con formato invalido" POST /auth/register \
        "{\"email\":\"esto-no-es-email\",\"fullName\":\"X\",\"password\":\"$TEST_PASS\",\"confirmPassword\":\"$TEST_PASS\"}" \
        "" "400" || true
    if invoke_api_test "T-AUTH-04" "Login correcto - obtener token" POST /auth/login \
        "{\"email\":\"$TEST_EMAIL\",\"password\":\"$TEST_PASS\"}" "" "200" '"success":true'; then
        USER_TOKEN=$(json_val "token" "$(cat /tmp/lh_last_resp.txt)")
    fi
    invoke_api_test "T-AUTH-05" "Login contrasena incorrecta" POST /auth/login \
        "{\"email\":\"$TEST_EMAIL\",\"password\":\"WrongPass999!\"}" "" "401" || true
    invoke_api_test "T-AUTH-06" "Ruta protegida sin token -> 401" GET /creativespaces "" "" "401" || true
    invoke_api_test "T-AUTH-07" "Ruta protegida con token invalido -> 401" GET /creativespaces \
        "" "este.token.esinvalido" "401" || true
    if [ -n "$ADMIN_EMAIL" ] && [ -n "$ADMIN_PASS" ]; then
        if invoke_api_test "T-AUTH-08" "Login admin" POST /auth/login \
            "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASS\"}" "" "200" '"success":true'; then
            ADMIN_TOKEN=$(json_val "token" "$(cat /tmp/lh_last_resp.txt)")
        fi
    else
        skip_test "T-AUTH-08" "Login admin" "ADMIN_EMAIL/ADMIN_PASSWORD no definidos en .env"
    fi
fi

# ── BLOQUE 2: ESPACIOS ─────────────────────────────────────────────────────────

if $DO_SPACE; then
    section "ESPACIOS CREATIVOS"
    if [ -z "$USER_TOKEN" ]; then
        skip_test "T-SPACE-*" "Todos los tests de espacios" "UserToken no disponible"
    else
        if invoke_api_test "T-SPACE-01" "Crear espacio OK" POST /creativespaces \
            "{\"name\":\"$SPACE_NAME\",\"description\":\"\",\"privacy\":0,\"isPublicProfileVisible\":false}" \
            "$USER_TOKEN" "201"; then SPACE_ID=$(json_val "id" "$(cat /tmp/lh_last_resp.txt)"); fi
        invoke_api_test "T-SPACE-02" "Crear espacio sin nombre -> error" POST /creativespaces \
            "{\"name\":\"\",\"description\":\"\",\"privacy\":0,\"isPublicProfileVisible\":false}" "$USER_TOKEN" "400" || true
        if [ -n "$SPACE_ID" ]; then
            invoke_api_test "T-SPACE-03" "Editar espacio OK" PUT "/creativespaces/$SPACE_ID" \
                "{\"name\":\"$SPACE_NAME (editado)\",\"description\":\"Editado\",\"privacy\":0,\"isPublicProfileVisible\":false}" \
                "$USER_TOKEN" "200" || true
            invoke_api_test "T-SPACE-04" "Editar espacio de otro usuario -> 404" PUT "/creativespaces/99999" \
                "{\"name\":\"X\",\"description\":\"\",\"privacy\":0,\"isPublicProfileVisible\":false}" "$USER_TOKEN" "404" || true
        else
            skip_test "T-SPACE-03" "Editar espacio" "SpaceId no disponible"
            skip_test "T-SPACE-04" "Editar espacio ajeno" "SpaceId no disponible"
        fi
        invoke_api_test "T-SPACE-05" "Acceso a espacios autenticado -> 200" GET /creativespaces "" "$USER_TOKEN" "200" || true
    fi
fi

# ── BLOQUE 3: DOCUMENTOS ───────────────────────────────────────────────────────

if $DO_DOC; then
    section "DOCUMENTOS Y VERSIONES"
    if [ -z "$USER_TOKEN" ]; then
        skip_test "T-DOC-*" "Todos los tests de documentos" "UserToken no disponible"
    else
        if invoke_api_test "T-DOC-01" "Crear documento OK" POST /documents \
            "{\"title\":\"$DOC_TITLE\",\"content\":\"$DOC_CONTENT\",\"description\":\"\"}" \
            "$USER_TOKEN" "201"; then DOC_ID=$(json_val "id" "$(cat /tmp/lh_last_resp.txt)"); fi
        invoke_api_test "T-DOC-02" "Crear documento sin titulo -> error" POST /documents \
            "{\"title\":\"\",\"content\":\"x\",\"description\":\"\"}" "$USER_TOKEN" "400" || true
        if [ -n "$DOC_ID" ]; then
            invoke_api_test "T-DOC-03" "Editar documento OK" PUT "/documents/$DOC_ID" \
                "{\"title\":\"$DOC_TITLE\",\"content\":\"$DOC_CONTENT (editado)\",\"description\":\"\",\"creativeSpaceId\":null}" \
                "$USER_TOKEN" "200" || true
            invoke_api_test "T-DOC-04" "Contenido XSS almacenado (backend no sanitiza)" PUT "/documents/$DOC_ID" \
                "{\"title\":\"$DOC_TITLE\",\"content\":\"<script>alert(xss)</script>\",\"description\":\"\",\"creativeSpaceId\":null}" \
                "$USER_TOKEN" "200" "<script>" || true
            if invoke_api_test "T-DOC-05" "Crear snapshot de version" POST "/documentversions/document/$DOC_ID/snapshot" \
                "{\"comment\":\"snapshot-autotest\"}" "$USER_TOKEN" "201"; then
                VERSION_ID=$(json_val "id" "$(cat /tmp/lh_last_resp.txt)")
            fi
            invoke_api_test "T-DOC-06" "Listar versiones del documento" GET "/documentversions/document/$DOC_ID" \
                "" "$USER_TOKEN" "200" || true
            invoke_api_test "T-DOC-07" "Snapshot de documento ajeno -> 403" POST "/documentversions/document/1/snapshot" \
                "{\"comment\":\"intruso\"}" "$USER_TOKEN" "403" || true
            if [ -n "$VERSION_ID" ]; then
                invoke_api_test "T-DOC-08" "Restaurar version anterior" POST "/documentversions/$VERSION_ID/restore" \
                    "{}" "$USER_TOKEN" "200" || true
            else
                skip_test "T-DOC-08" "Restaurar version" "VersionId no disponible"
            fi
            invoke_api_test "T-DOC-09" "Eliminar documento OK" DELETE "/documents/$DOC_ID" "" "$USER_TOKEN" "204" || true
        else
            for tid in T-DOC-03 T-DOC-04 T-DOC-05 T-DOC-06 T-DOC-07 T-DOC-08 T-DOC-09; do
                skip_test "$tid" "Test de documento" "DocId no disponible"
            done
        fi
    fi
fi

# ── BLOQUE 4: ADMIN ────────────────────────────────────────────────────────────

if $DO_ADMIN; then
    section "PANEL DE ADMINISTRACION"
    invoke_api_test "T-ADMIN-01" "Acceso admin sin token -> 401" GET /admin/allowed-websites "" "" "401" || true
    if [ -n "$USER_TOKEN" ]; then
        invoke_api_test "T-ADMIN-02" "Acceso admin con rol User -> 403" GET /admin/allowed-websites "" "$USER_TOKEN" "403" || true
    else
        skip_test "T-ADMIN-02" "Acceso admin con rol User" "UserToken no disponible"
    fi
    if [ -n "$ADMIN_TOKEN" ]; then
        invoke_api_test "T-ADMIN-03" "Acceso admin con rol Admin -> 200" GET /admin/allowed-websites "" "$ADMIN_TOKEN" "200" || true
        if invoke_api_test "T-ADMIN-04" "Anadir dominio permitido" POST /admin/allowed-websites \
            "{\"domain\":\"$ADMIN_DOMAIN\",\"isActive\":true}" "$ADMIN_TOKEN" "201"; then
            WEBSITE_ID=$(json_val "id" "$(cat /tmp/lh_last_resp.txt)")
        fi
        if [ -n "$WEBSITE_ID" ]; then
            invoke_api_test "T-ADMIN-05" "Desactivar dominio" PUT "/admin/allowed-websites/$WEBSITE_ID" \
                "{\"domain\":\"$ADMIN_DOMAIN\",\"isActive\":false}" "$ADMIN_TOKEN" "200" || true
        else
            skip_test "T-ADMIN-05" "Desactivar dominio" "WebsiteId no disponible"
        fi
        invoke_api_test "T-ADMIN-06" "Listar usuarios (admin)" GET /users "" "$ADMIN_TOKEN" "200" || true
    else
        for tid in T-ADMIN-03 T-ADMIN-04 T-ADMIN-05 T-ADMIN-06; do
            skip_test "$tid" "Test admin" "AdminToken no disponible"
        done
    fi
fi

# ── BLOQUE 5: SEGURIDAD ────────────────────────────────────────────────────────

if $DO_SEC; then
    section "SEGURIDAD"
    invoke_api_test "T-SEC-01" "Token expirado/invalido -> 401" GET /creativespaces \
        "" "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE2MDAwMDAwMDB9.invalido" "401" || true
    if [ -n "$USER_TOKEN" ] && [ -n "$ADMIN_TOKEN" ]; then
        invoke_api_test "T-SEC-02" "Token User en endpoint Admin -> 403" GET /admin/allowed-websites \
            "" "$USER_TOKEN" "403" || true
    else
        skip_test "T-SEC-02" "Token User en endpoint Admin" "Tokens no disponibles"
    fi
fi

# ── LIMPIEZA ───────────────────────────────────────────────────────────────────

section "LIMPIEZA"
if [ -n "$SPACE_ID" ] && [ -n "$USER_TOKEN" ]; then
    curl -s -o /dev/null -X DELETE -H "Authorization: Bearer $USER_TOKEN" \
        "$BASE_URL/creativespaces/$SPACE_ID" 2>/dev/null && echo "  Espacio $SPACE_ID eliminado." || true
fi
if [ -n "$WEBSITE_ID" ] && [ -n "$ADMIN_TOKEN" ]; then
    curl -s -o /dev/null -X DELETE -H "Authorization: Bearer $ADMIN_TOKEN" \
        "$BASE_URL/admin/allowed-websites/$WEBSITE_ID" 2>/dev/null && echo "  Dominio $WEBSITE_ID eliminado." || true
fi
if [ -n "$DOC_ID" ] && [ -n "$USER_TOKEN" ]; then
    curl -s -o /dev/null -X DELETE -H "Authorization: Bearer $USER_TOKEN" \
        "$BASE_URL/documents/$DOC_ID" 2>/dev/null && echo "  Documento $DOC_ID eliminado." || true
fi
if [ -n "$USER_TOKEN" ]; then
    curl -s -o /dev/null -X DELETE \
        -H "Authorization: Bearer $USER_TOKEN" \
        "$BASE_URL/users/me" 2>/dev/null && \
        echo "  Usuario $TEST_EMAIL eliminado." || true
fi

# ── RESUMEN ────────────────────────────────────────────────────────────────────

echo ""
echo "==========================================="
echo " RESUMEN: $PASS_COUNT OK  |  $FAIL_COUNT FAIL  |  $SKIP_COUNT SKIP  |  $TOTAL total"
echo "==========================================="

# ── INFORME MARKDOWN ───────────────────────────────────────────────────────────

OUTPUT_PATH="$OUTPUT_DIR/RESULTADO_PRUEBAS_$TIMESTAMP.md"
FECHA_STR=$(date +"%Y-%m-%d %H:%M:%S")
SECTIONS_ORDER=("AUTH" "ESPACIOS CREATIVOS" "DOCUMENTOS Y VERSIONES" "PANEL DE ADMINISTRACION" "SEGURIDAD")

{
    echo "# Informe de Pruebas -- LifeHub (interactivo)"
    echo ""
    echo "**Fecha:** $FECHA_STR  "
    echo "**Modulos:** $SELECTED  "
    echo "**Entorno:** $BASE_URL  "
    echo "**Script:** run-tests-interactive.sh  "
    echo ""
    echo "---"
    echo ""
    echo "## Resultados"

    for sec in "${SECTIONS_ORDER[@]}"; do
        has=false
        for i in "${!ALL_IDS[@]}"; do [ "${ALL_SECTION[$i]}" = "$sec" ] && has=true && break; done
        $has || continue
        echo ""; echo "### $sec"; echo ""
        echo "| ID | Descripcion | Esperado | Real | Resultado |"
        echo "|----|-------------|----------|------|-----------|"
        for i in "${!ALL_IDS[@]}"; do
            [ "${ALL_SECTION[$i]}" != "$sec" ] && continue
            echo "| ${ALL_IDS[$i]} | ${ALL_DESCS[$i]} | ${ALL_EXPECTED[$i]} | ${ALL_ACTUAL[$i]} | ${ALL_STATUS[$i]} |"
        done
    done

    echo ""; echo "---"; echo ""; echo "## Resumen"; echo ""
    echo "| Modulo | Total | PASS | FAIL | SKIP |"
    echo "|--------|-------|------|------|------|"
    for sec in "${SECTIONS_ORDER[@]}"; do
        s_total=0; s_pass=0; s_fail=0; s_skip=0
        for i in "${!ALL_IDS[@]}"; do
            [ "${ALL_SECTION[$i]}" != "$sec" ] && continue
            s_total=$((s_total + 1))
            case "${ALL_STATUS[$i]}" in PASS) s_pass=$((s_pass+1));; FAIL) s_fail=$((s_fail+1));; SKIP) s_skip=$((s_skip+1));; esac
        done
        [ $s_total -eq 0 ] && continue
        echo "| $sec | $s_total | $s_pass | $s_fail | $s_skip |"
    done
    echo "| **TOTAL** | **$TOTAL** | **$PASS_COUNT** | **$FAIL_COUNT** | **$SKIP_COUNT** |"
} > "$OUTPUT_PATH"

echo ""
if [ $FAIL_COUNT -gt 0 ]; then
    echo "Informe generado con $FAIL_COUNT incidencia(s): $OUTPUT_PATH"
else
    echo "Informe generado: $OUTPUT_PATH"
fi

exit $FAIL_COUNT
