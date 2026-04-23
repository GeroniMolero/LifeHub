#!/bin/bash

BASE_URL="${1:-http://localhost:5000/api}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
OUTPUT_DIR="${2:-$PROJECT_ROOT/documentacion}"

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
TEST_EMAIL="autotest_${TIMESTAMP}@lifehub-auto.test"
TEST_PASS="AutoTest123!"

# --- Leer credenciales de admin desde .env ---

ADMIN_EMAIL=""
ADMIN_PASS=""

if [ -f "$PROJECT_ROOT/.env" ]; then
    while IFS= read -r line; do
        [[ "$line" =~ ^[[:space:]]*# ]] && continue
        [[ "$line" =~ ^[[:space:]]*$ ]] && continue
        key="${line%%=*}"
        val="${line#*=}"
        key="${key#"${key%%[![:space:]]*}"}"
        key="${key%"${key##*[![:space:]]}"}"
        case "$key" in
            ADMIN_EMAIL)    ADMIN_EMAIL="$val" ;;
            ADMIN_PASSWORD) ADMIN_PASS="$val" ;;
        esac
    done < "$PROJECT_ROOT/.env"
fi

if [ -z "$ADMIN_EMAIL" ] || [ -z "$ADMIN_PASS" ]; then
    echo "AVISO: ADMIN_EMAIL o ADMIN_PASSWORD no encontrados en .env -- los tests de admin seran SKIP."
fi

# --- Estado entre tests ---

USER_TOKEN=""
ADMIN_TOKEN=""
SPACE_ID=""
DOC_ID=""
VERSION_ID=""
WEBSITE_ID=""

# --- Resultados ---

PASS_COUNT=0
FAIL_COUNT=0
SKIP_COUNT=0
TOTAL=0

ALL_IDS=()
ALL_DESCS=()
ALL_EXPECTED=()
ALL_ACTUAL=()
ALL_STATUS=()
ALL_SECTION=()

FAIL_IDS=()
FAIL_DESCS=()
FAIL_EXPECTED=()
FAIL_ACTUAL=()

CURRENT_SECTION=""

# --- Helpers ---

section() {
    CURRENT_SECTION="$1"
    echo ""
    echo "--- $1 ---"
}

json_val() {
    local key="$1"
    local json="$2"
    local val
    # String value: "key":"value"
    val=$(echo "$json" | grep -o "\"$key\":\"[^\"]*\"" | sed "s/\"$key\":\"//;s/\"$//")
    # Numeric value: "key":123
    [ -z "$val" ] && val=$(echo "$json" | grep -o "\"$key\":[0-9]*" | sed "s/\"$key\"://")
    echo "$val"
}

invoke_api_test() {
    local id="$1"
    local desc="$2"
    local method="$3"
    local url="$4"
    local body="${5:-}"
    local token="${6:-}"
    local expected="$7"
    local contains="${8:-}"

    local curl_args=(-s -o /tmp/lh_resp.txt -w "%{http_code}" -X "$method"
                     -H "Content-Type: application/json")
    [ -n "$token" ] && curl_args+=(-H "Authorization: Bearer $token")
    [ -n "$body" ]  && curl_args+=(-d "$body")

    local actual
    actual=$(curl "${curl_args[@]}" "${BASE_URL}${url}" 2>/dev/null) || actual="0"
    local response_body
    response_body=$(cat /tmp/lh_resp.txt 2>/dev/null)

    local pass=true
    [ "$actual" != "$expected" ] && pass=false
    if $pass && [ -n "$contains" ]; then
        echo "$response_body" | grep -qF "$contains" || pass=false
    fi

    ALL_IDS+=("$id")
    ALL_DESCS+=("$desc")
    ALL_EXPECTED+=("$expected")
    ALL_ACTUAL+=("$actual")
    ALL_SECTION+=("$CURRENT_SECTION")
    TOTAL=$((TOTAL + 1))

    if $pass; then
        ALL_STATUS+=("PASS")
        PASS_COUNT=$((PASS_COUNT + 1))
        echo "  [OK  ] $id - $desc  (esperado $expected, real $actual)"
        cp /tmp/lh_resp.txt /tmp/lh_last_resp.txt 2>/dev/null
        return 0
    else
        ALL_STATUS+=("FAIL")
        FAIL_COUNT=$((FAIL_COUNT + 1))
        FAIL_IDS+=("$id")
        FAIL_DESCS+=("$desc")
        FAIL_EXPECTED+=("$expected")
        FAIL_ACTUAL+=("$actual")
        echo "  [FAIL] $id - $desc  (esperado $expected, real $actual)"
        return 1
    fi
}

skip_test() {
    local id="$1"
    local desc="$2"
    local reason="$3"
    ALL_IDS+=("$id")
    ALL_DESCS+=("$desc")
    ALL_EXPECTED+=("-")
    ALL_ACTUAL+=("SKIP")
    ALL_STATUS+=("SKIP")
    ALL_SECTION+=("$CURRENT_SECTION")
    TOTAL=$((TOTAL + 1))
    SKIP_COUNT=$((SKIP_COUNT + 1))
    echo "  [SKIP] $id - $desc ($reason)"
}

# --- BLOQUE 1: AUTH ---

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
    "{\"email\":\"$TEST_EMAIL\",\"password\":\"$TEST_PASS\"}" \
    "" "200" '"success":true'; then
    USER_TOKEN=$(json_val "token" "$(cat /tmp/lh_last_resp.txt)")
fi

invoke_api_test "T-AUTH-05" "Login contrasena incorrecta" POST /auth/login \
    "{\"email\":\"$TEST_EMAIL\",\"password\":\"WrongPass999!\"}" \
    "" "401" || true

invoke_api_test "T-AUTH-06" "Ruta protegida sin token -> 401" GET /creativespaces \
    "" "" "401" || true

invoke_api_test "T-AUTH-07" "Ruta protegida con token invalido -> 401" GET /creativespaces \
    "" "este.token.esinvalido" "401" || true

if [ -n "$ADMIN_EMAIL" ] && [ -n "$ADMIN_PASS" ]; then
    if invoke_api_test "T-AUTH-08" "Login admin (setup para tests admin)" POST /auth/login \
        "{\"email\":\"$ADMIN_EMAIL\",\"password\":\"$ADMIN_PASS\"}" \
        "" "200" '"success":true'; then
        ADMIN_TOKEN=$(json_val "token" "$(cat /tmp/lh_last_resp.txt)")
    fi
else
    skip_test "T-AUTH-08" "Login admin" "ADMIN_EMAIL/ADMIN_PASSWORD no definidos en .env"
fi

# --- BLOQUE 2: ESPACIOS CREATIVOS ---

section "ESPACIOS CREATIVOS"

if [ -z "$USER_TOKEN" ]; then
    skip_test "T-SPACE-*" "Todos los tests de espacios" "UserToken no disponible (T-AUTH-04 fallo)"
else
    if invoke_api_test "T-SPACE-01" "Crear espacio OK" POST /creativespaces \
        "{\"name\":\"Espacio AutoTest $TIMESTAMP\",\"description\":\"\",\"privacy\":0,\"isPublicProfileVisible\":false}" \
        "$USER_TOKEN" "201"; then
        SPACE_ID=$(json_val "id" "$(cat /tmp/lh_last_resp.txt)")
    fi

    invoke_api_test "T-SPACE-02" "Crear espacio sin nombre -> error" POST /creativespaces \
        "{\"name\":\"\",\"description\":\"\",\"privacy\":0,\"isPublicProfileVisible\":false}" \
        "$USER_TOKEN" "400" || true

    if [ -n "$SPACE_ID" ]; then
        invoke_api_test "T-SPACE-03" "Editar espacio OK" PUT "/creativespaces/$SPACE_ID" \
            "{\"name\":\"Espacio Editado $TIMESTAMP\",\"description\":\"Editado\",\"privacy\":0,\"isPublicProfileVisible\":false}" \
            "$USER_TOKEN" "200" || true

        invoke_api_test "T-SPACE-04" "Editar espacio de otro usuario -> 404" PUT "/creativespaces/99999" \
            "{\"name\":\"X\",\"description\":\"\",\"privacy\":0,\"isPublicProfileVisible\":false}" \
            "$USER_TOKEN" "404" || true
    else
        skip_test "T-SPACE-03" "Editar espacio" "SpaceId no disponible"
        skip_test "T-SPACE-04" "Editar espacio ajeno" "SpaceId no disponible"
    fi

    invoke_api_test "T-SPACE-05" "Acceso a espacios autenticado -> 200" GET /creativespaces \
        "" "$USER_TOKEN" "200" || true
fi

# --- BLOQUE 3: DOCUMENTOS Y VERSIONES ---

section "DOCUMENTOS Y VERSIONES"

if [ -z "$USER_TOKEN" ]; then
    skip_test "T-DOC-*" "Todos los tests de documentos" "UserToken no disponible"
else
    if invoke_api_test "T-DOC-01" "Crear documento OK" POST /documents \
        "{\"title\":\"Doc AutoTest $TIMESTAMP\",\"content\":\"# Test\nContenido inicial.\",\"description\":\"\"}" \
        "$USER_TOKEN" "201"; then
        DOC_ID=$(json_val "id" "$(cat /tmp/lh_last_resp.txt)")
    fi

    invoke_api_test "T-DOC-02" "Crear documento sin titulo -> error" POST /documents \
        "{\"title\":\"\",\"content\":\"x\",\"description\":\"\"}" \
        "$USER_TOKEN" "400" || true

    if [ -n "$DOC_ID" ]; then
        invoke_api_test "T-DOC-03" "Editar documento OK" PUT "/documents/$DOC_ID" \
            "{\"title\":\"Doc AutoTest $TIMESTAMP\",\"content\":\"# Test\nContenido editado.\",\"description\":\"\",\"creativeSpaceId\":null}" \
            "$USER_TOKEN" "200" || true

        invoke_api_test "T-DOC-04" "Contenido XSS almacenado (backend no sanitiza)" PUT "/documents/$DOC_ID" \
            "{\"title\":\"Doc AutoTest $TIMESTAMP\",\"content\":\"<script>alert(xss)</script>\",\"description\":\"\",\"creativeSpaceId\":null}" \
            "$USER_TOKEN" "200" "<script>" || true

        if invoke_api_test "T-DOC-05" "Crear snapshot de version" POST "/documentversions/document/$DOC_ID/snapshot" \
            "{\"comment\":\"snapshot-autotest\"}" \
            "$USER_TOKEN" "201"; then
            VERSION_ID=$(json_val "id" "$(cat /tmp/lh_last_resp.txt)")
        fi

        invoke_api_test "T-DOC-06" "Listar versiones del documento" GET "/documentversions/document/$DOC_ID" \
            "" "$USER_TOKEN" "200" || true

        invoke_api_test "T-DOC-07" "Snapshot de documento ajeno -> 403" POST "/documentversions/document/1/snapshot" \
            "{\"comment\":\"intruso\"}" \
            "$USER_TOKEN" "403" || true

        if [ -n "$VERSION_ID" ]; then
            invoke_api_test "T-DOC-08" "Restaurar version anterior" POST "/documentversions/$VERSION_ID/restore" \
                "{}" "$USER_TOKEN" "200" || true
        else
            skip_test "T-DOC-08" "Restaurar version" "VersionId no disponible"
        fi

        invoke_api_test "T-DOC-09" "Eliminar documento OK" DELETE "/documents/$DOC_ID" \
            "" "$USER_TOKEN" "204" || true
    else
        for tid in T-DOC-03 T-DOC-04 T-DOC-05 T-DOC-06 T-DOC-07 T-DOC-08 T-DOC-09; do
            skip_test "$tid" "Test de documento" "DocId no disponible"
        done
    fi
fi

# --- BLOQUE 4: PANEL DE ADMINISTRACION ---

section "PANEL DE ADMINISTRACION"

invoke_api_test "T-ADMIN-01" "Acceso admin sin token -> 401" GET /admin/allowed-websites \
    "" "" "401" || true

if [ -n "$USER_TOKEN" ]; then
    invoke_api_test "T-ADMIN-02" "Acceso admin con rol User -> 403" GET /admin/allowed-websites \
        "" "$USER_TOKEN" "403" || true
else
    skip_test "T-ADMIN-02" "Acceso admin con rol User" "UserToken no disponible"
fi

if [ -n "$ADMIN_TOKEN" ]; then
    invoke_api_test "T-ADMIN-03" "Acceso admin con rol Admin -> 200" GET /admin/allowed-websites \
        "" "$ADMIN_TOKEN" "200" || true

    if invoke_api_test "T-ADMIN-04" "Anadir dominio permitido" POST /admin/allowed-websites \
        "{\"domain\":\"autotest-$TIMESTAMP.io\",\"isActive\":true}" \
        "$ADMIN_TOKEN" "201"; then
        WEBSITE_ID=$(json_val "id" "$(cat /tmp/lh_last_resp.txt)")
    fi

    if [ -n "$WEBSITE_ID" ]; then
        invoke_api_test "T-ADMIN-05" "Desactivar dominio" PUT "/admin/allowed-websites/$WEBSITE_ID" \
            "{\"domain\":\"autotest-$TIMESTAMP.io\",\"isActive\":false}" \
            "$ADMIN_TOKEN" "200" || true
    else
        skip_test "T-ADMIN-05" "Desactivar dominio" "WebsiteId no disponible"
    fi

    invoke_api_test "T-ADMIN-06" "Listar usuarios (admin)" GET /users \
        "" "$ADMIN_TOKEN" "200" || true
else
    for tid in T-ADMIN-03 T-ADMIN-04 T-ADMIN-05 T-ADMIN-06; do
        skip_test "$tid" "Test admin" "AdminToken no disponible"
    done
fi

# --- BLOQUE 5: SEGURIDAD ---

section "SEGURIDAD"

invoke_api_test "T-SEC-01" "Token expirado/invalido -> 401" GET /creativespaces \
    "" "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE2MDAwMDAwMDB9.invalido" "401" || true

if [ -n "$USER_TOKEN" ] && [ -n "$ADMIN_TOKEN" ]; then
    invoke_api_test "T-SEC-02" "Token User en endpoint Admin -> 403" GET /admin/allowed-websites \
        "" "$USER_TOKEN" "403" || true
else
    skip_test "T-SEC-02" "Token User en endpoint Admin" "Tokens no disponibles"
fi

# --- BLOQUE 6: LIMPIEZA ---

section "LIMPIEZA"

if [ -n "$SPACE_ID" ] && [ -n "$USER_TOKEN" ]; then
    curl -s -o /dev/null -X DELETE \
        -H "Authorization: Bearer $USER_TOKEN" \
        "$BASE_URL/creativespaces/$SPACE_ID" 2>/dev/null && \
        echo "  Espacio $SPACE_ID eliminado." || true
fi

if [ -n "$WEBSITE_ID" ] && [ -n "$ADMIN_TOKEN" ]; then
    curl -s -o /dev/null -X DELETE \
        -H "Authorization: Bearer $ADMIN_TOKEN" \
        "$BASE_URL/admin/allowed-websites/$WEBSITE_ID" 2>/dev/null && \
        echo "  Dominio $WEBSITE_ID eliminado." || true
fi

if [ -n "$DOC_ID" ] && [ -n "$USER_TOKEN" ]; then
    curl -s -o /dev/null -X DELETE \
        -H "Authorization: Bearer $USER_TOKEN" \
        "$BASE_URL/documents/$DOC_ID" 2>/dev/null && \
        echo "  Documento $DOC_ID eliminado." || true
fi

if [ -n "$USER_TOKEN" ]; then
    curl -s -o /dev/null -X DELETE \
        -H "Authorization: Bearer $USER_TOKEN" \
        "$BASE_URL/users/me" 2>/dev/null && \
        echo "  Usuario $TEST_EMAIL eliminado." || true
fi

# --- BLOQUE 7: RESUMEN ---

echo ""
echo "==========================================="
echo " RESUMEN: $PASS_COUNT OK  |  $FAIL_COUNT FAIL  |  $SKIP_COUNT SKIP  |  $TOTAL total"
echo "==========================================="

# --- BLOQUE 8: GENERAR INFORME MARKDOWN ---

OUTPUT_PATH="$OUTPUT_DIR/RESULTADO_PRUEBAS_$TIMESTAMP.md"
FECHA_STR=$(date +"%Y-%m-%d %H:%M:%S")
SECTIONS_ORDER=("AUTH" "ESPACIOS CREATIVOS" "DOCUMENTOS Y VERSIONES" "PANEL DE ADMINISTRACION" "SEGURIDAD")

{
    echo "# Informe de Pruebas Automaticas -- LifeHub"
    echo ""
    echo "**Fecha:** $FECHA_STR  "
    echo "**Entorno:** $BASE_URL  "
    echo "**Usuario de prueba:** $TEST_EMAIL  "
    echo "**Script:** run-tests.sh  "
    echo ""
    echo "---"
    echo ""
    echo "## Resultados por modulo"

    for sec in "${SECTIONS_ORDER[@]}"; do
        has_entries=false
        for i in "${!ALL_IDS[@]}"; do
            [ "${ALL_SECTION[$i]}" = "$sec" ] && has_entries=true && break
        done
        $has_entries || continue

        echo ""
        echo "### $sec"
        echo ""
        echo "| ID | Descripcion | Esperado | Real | Resultado |"
        echo "|----|-------------|----------|------|-----------|"
        for i in "${!ALL_IDS[@]}"; do
            [ "${ALL_SECTION[$i]}" != "$sec" ] && continue
            echo "| ${ALL_IDS[$i]} | ${ALL_DESCS[$i]} | ${ALL_EXPECTED[$i]} | ${ALL_ACTUAL[$i]} | ${ALL_STATUS[$i]} |"
        done
    done

    echo ""
    echo "---"
    echo ""
    echo "## Resumen"
    echo ""
    echo "| Modulo | Total | PASS | FAIL | SKIP |"
    echo "|--------|-------|------|------|------|"

    for sec in "${SECTIONS_ORDER[@]}"; do
        s_total=0; s_pass=0; s_fail=0; s_skip=0
        for i in "${!ALL_IDS[@]}"; do
            [ "${ALL_SECTION[$i]}" != "$sec" ] && continue
            s_total=$((s_total + 1))
            case "${ALL_STATUS[$i]}" in
                PASS) s_pass=$((s_pass + 1)) ;;
                FAIL) s_fail=$((s_fail + 1)) ;;
                SKIP) s_skip=$((s_skip + 1)) ;;
            esac
        done
        [ $s_total -eq 0 ] && continue
        echo "| $sec | $s_total | $s_pass | $s_fail | $s_skip |"
    done
    echo "| **TOTAL** | **$TOTAL** | **$PASS_COUNT** | **$FAIL_COUNT** | **$SKIP_COUNT** |"

    echo ""
    echo "---"
    echo ""
    echo "## Incidencias"
    echo ""

    if [ ${#FAIL_IDS[@]} -gt 0 ]; then
        echo "| ID | Descripcion | Esperado | Real |"
        echo "|----|-------------|----------|------|"
        for i in "${!FAIL_IDS[@]}"; do
            echo "| ${FAIL_IDS[$i]} | ${FAIL_DESCS[$i]} | ${FAIL_EXPECTED[$i]} | ${FAIL_ACTUAL[$i]} |"
        done
    else
        echo "Sin incidencias. Todos los tests ejecutados han resultado PASS."
    fi
} > "$OUTPUT_PATH"

echo ""
if [ $FAIL_COUNT -gt 0 ]; then
    echo "Informe generado con $FAIL_COUNT incidencia(s): $OUTPUT_PATH"
else
    echo "Informe generado: $OUTPUT_PATH"
fi

# --- BLOQUE 9: ACTUALIZAR PLAN_PRUEBAS.md ---

if [ ${#FAIL_IDS[@]} -gt 0 ]; then
    PLAN_PATH="$OUTPUT_DIR/PLAN_PRUEBAS.md"
    if [ -f "$PLAN_PATH" ]; then
        LAST_INC_NUM=0
        while IFS= read -r line; do
            if [[ "$line" =~ \|[[:space:]]*INC-([0-9]+) ]]; then
                num=$((10#${BASH_REMATCH[1]}))
                [ $num -gt $LAST_INC_NUM ] && LAST_INC_NUM=$num
            fi
        done < "$PLAN_PATH"

        INSERT_AFTER=-1
        SEP_LINE=-1
        IN_INC=false
        LINE_NUM=0
        while IFS= read -r line; do
            LINE_NUM=$((LINE_NUM + 1))
            [[ "$line" =~ ^##[[:space:]]+Incidencias ]] && IN_INC=true
            if $IN_INC; then
                [[ "$line" =~ ^\|[-|[:space:]]+\| ]] && SEP_LINE=$LINE_NUM
                [[ "$line" =~ ^\|[[:space:]]*INC- ]] && INSERT_AFTER=$LINE_NUM
            fi
        done < "$PLAN_PATH"
        [ $INSERT_AFTER -eq -1 ] && INSERT_AFTER=$SEP_LINE

        if [ $INSERT_AFTER -ge 0 ]; then
            TODAY=$(date +"%d-%m-%Y %H:%M")
            TMP_INSERT=$(mktemp)
            for i in "${!FAIL_IDS[@]}"; do
                LAST_INC_NUM=$((LAST_INC_NUM + 1))
                printf -v INC_ID "INC-%02d" $LAST_INC_NUM
                echo "| $INC_ID | $TODAY | Test \`${FAIL_IDS[$i]}\` (${FAIL_DESCS[$i]}) fallo: esperado HTTP ${FAIL_EXPECTED[$i]}, obtenido ${FAIL_ACTUAL[$i]}. Detectado automaticamente por \`run-tests.sh\`. | Abierta |" >> "$TMP_INSERT"
            done

            TMP_PLAN=$(mktemp)
            awk -v n="$INSERT_AFTER" -v f="$TMP_INSERT" '
            NR == n { print; while ((getline line < f) > 0) print line; next }
            { print }
            ' "$PLAN_PATH" > "$TMP_PLAN"
            rm "$TMP_INSERT"
            mv "$TMP_PLAN" "$PLAN_PATH"
            echo "${#FAIL_IDS[@]} incidencia(s) registrada(s) en PLAN_PRUEBAS.md"
        else
            echo "No se encontro la tabla de incidencias en PLAN_PRUEBAS.md -- nada actualizado."
        fi
    else
        echo "PLAN_PRUEBAS.md no encontrado en $OUTPUT_DIR -- nada actualizado."
    fi
fi

exit $FAIL_COUNT
