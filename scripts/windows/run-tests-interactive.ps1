Param(
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Continue"
$ProjectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not $OutputDir) { $OutputDir = Join-Path $ProjectRoot "documentacion" }

$AdminEmail = $null; $AdminPass = $null
$envFile = Join-Path $ProjectRoot ".env"
if (Test-Path $envFile) {
    Get-Content $envFile | Where-Object { $_ -match '^\s*[^#]' } | ForEach-Object {
        if ($_ -match '^\s*ADMIN_EMAIL\s*=\s*(.+)$')    { $AdminEmail = $Matches[1].Trim() }
        if ($_ -match '^\s*ADMIN_PASSWORD\s*=\s*(.+)$') { $AdminPass  = $Matches[1].Trim() }
    }
}

# ── MENU ──────────────────────────────────────────────────────────────────────

Clear-Host
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "   LifeHub  --  Test Runner Interactivo    " -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

$urlInput = Read-Host "URL de la API  [http://localhost:5000/api]"
$BaseUrl   = if ($urlInput.Trim()) { $urlInput.Trim() } else { "http://localhost:5000/api" }

Write-Host ""
Write-Host "Modulos disponibles:" -ForegroundColor White
Write-Host "  [1] AUTH             (8 tests)"
Write-Host "  [2] ESPACIOS         (5 tests)"
Write-Host "  [3] DOCUMENTOS       (9 tests)"
Write-Host "  [4] COLABORACION     (3 tests)"
Write-Host "  [5] PANEL ADMIN      (6 tests)"
Write-Host "  [6] SEGURIDAD        (2 tests)"
Write-Host "  [A] TODOS"
Write-Host ""
$sel    = (Read-Host "Seleccion  (ej: 1 3  o  A para todos)").Trim().ToUpper()
$parts  = $sel -split '\s+'
$all    = ($sel -eq "" -or $sel -eq "A")
$doAuth  = $all -or $parts -contains "1"
$doSpace = $all -or $parts -contains "2"
$doDoc   = $all -or $parts -contains "3"
$doCol   = $all -or $parts -contains "4"
$doAdmin = $all -or $parts -contains "5"
$doSec   = $all -or $parts -contains "6"

$needsUser  = $doSpace -or $doDoc -or $doCol -or $doAdmin -or $doSec
$needsAdmin = $doCol -or $doAdmin

$selectedNames = @()
if ($doAuth)  { $selectedNames += "AUTH" }
if ($doSpace) { $selectedNames += "ESPACIOS" }
if ($doDoc)   { $selectedNames += "DOCUMENTOS" }
if ($doCol)   { $selectedNames += "COLABORACION" }
if ($doAdmin) { $selectedNames += "ADMIN" }
if ($doSec)   { $selectedNames += "SEGURIDAD" }

Write-Host ""
Write-Host "  Modulos : $($selectedNames -join ', ')" -ForegroundColor Yellow
Write-Host "  Endpoint: $BaseUrl" -ForegroundColor Yellow

# ── VALORES PERSONALIZADOS ─────────────────────────────────────────────────────

$Timestamp = Get-Date -Format "yyyyMMdd_HHmmss"

Write-Host ""
Write-Host "Valores de prueba (Enter para usar el valor por defecto):" -ForegroundColor White
Write-Host ""

$defEmail = "autotest_$Timestamp@lifehub-auto.test"
$defPass  = "AutoTest123!"
$inp = Read-Host "  Email de prueba        [$defEmail]"
$TestEmail = if ($inp.Trim()) { $inp.Trim() } else { $defEmail }
$inp = Read-Host "  Contrasena de prueba   [$defPass]"
$TestPass  = if ($inp.Trim()) { $inp.Trim() } else { $defPass }

$SpaceName = "Espacio AutoTest $Timestamp"
$DocTitle  = "Doc AutoTest $Timestamp"
$DocContent = "# Test`nContenido de prueba."
$AdminDomain = "autotest-$Timestamp.io"

if ($doSpace) {
    $inp = Read-Host "  Nombre del espacio     [$SpaceName]"
    $SpaceName = if ($inp.Trim()) { $inp.Trim() } else { $SpaceName }
}
if ($doDoc) {
    $inp = Read-Host "  Titulo del documento   [$DocTitle]"
    $DocTitle = if ($inp.Trim()) { $inp.Trim() } else { $DocTitle }
    $inp = Read-Host "  Contenido del documento [$DocContent]"
    $DocContent = if ($inp.Trim()) { $inp.Trim() } else { $DocContent }
}
if ($doAdmin) {
    $inp = Read-Host "  Dominio admin          [$AdminDomain]"
    $AdminDomain = if ($inp.Trim()) { $inp.Trim() } else { $AdminDomain }
}

Write-Host ""

$script:UserToken  = $null
$script:AdminToken = $null
$script:SpaceId    = $null
$script:DocId      = $null
$script:VersionId  = $null
$script:WebsiteId  = $null

$Results = [System.Collections.Generic.List[PSCustomObject]]::new()

function Invoke-ApiTest {
    param(
        [string]$Id, [string]$Description,
        [string]$Method, [string]$Url,
        [hashtable]$Body = $null, [string]$Token = $null,
        [int]$ExpectedStatus,
        [string]$Contains = $null, [string]$NotContains = $null,
        [scriptblock]$OnPass = $null
    )
    $headers = @{ "Content-Type" = "application/json" }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }
    $actualStatus = 0; $responseBody = ""
    try {
        $params = @{ Method=$Method; Uri="$BaseUrl$Url"; Headers=$headers; UseBasicParsing=$true; ErrorAction="Stop" }
        if ($Body) { $params["Body"] = ($Body | ConvertTo-Json -Compress) }
        $response     = Invoke-WebRequest @params
        $actualStatus = [int]$response.StatusCode
        $responseBody = $response.Content
    } catch {
        try   { $actualStatus = [int]$_.Exception.Response.StatusCode } catch { $actualStatus = 0 }
        try {
            $stream = $_.Exception.Response.GetResponseStream()
            $reader = [System.IO.StreamReader]::new($stream)
            $responseBody = $reader.ReadToEnd(); $reader.Dispose()
        } catch { $responseBody = "(sin cuerpo)" }
    }
    $pass = ($actualStatus -eq $ExpectedStatus)
    if ($pass -and $Contains)    { $pass = $responseBody -match [regex]::Escape($Contains) }
    if ($pass -and $NotContains) { $pass = -not ($responseBody -match [regex]::Escape($NotContains)) }
    if ($pass -and $OnPass) { & $OnPass $responseBody }
    $truncated = if ($responseBody.Length -gt 180) { $responseBody.Substring(0,180) + "..." } else { $responseBody }
    $Results.Add([PSCustomObject]@{ Id=$Id; Description=$Description; ExpectedStatus=$ExpectedStatus; ActualStatus=$actualStatus; Pass=$pass; ResponseSnip=$truncated })
    $icon  = if ($pass) { "OK  " } else { "FAIL" }
    $color = if ($pass) { "Green" } else { "Red" }
    Write-Host "  [$icon] $Id - $Description  (esperado $ExpectedStatus, real $actualStatus)" -ForegroundColor $color
}

function Skip-Test {
    param([string]$Id, [string]$Description, [string]$Reason)
    $Results.Add([PSCustomObject]@{ Id=$Id; Description=$Description; ExpectedStatus="-"; ActualStatus="SKIP"; Pass=$null; ResponseSnip=$Reason })
    Write-Host "  [SKIP] $Id - $Description ($Reason)" -ForegroundColor DarkYellow
}

function Section { param([string]$Name) Write-Host ""; Write-Host "--- $Name ---" -ForegroundColor Cyan }

# ── SETUP SILENCIOSO DE TOKENS (si se necesitan sin haber seleccionado AUTH) ──

if ($needsUser -and -not $doAuth) {
    Write-Host "  [setup] Registrando usuario de prueba..." -ForegroundColor DarkGray
    try {
        Invoke-WebRequest -Method POST -Uri "$BaseUrl/auth/register" -UseBasicParsing -ErrorAction SilentlyContinue `
            -Headers @{ "Content-Type"="application/json" } `
            -Body (@{ email=$TestEmail; fullName="Test AutoScript"; password=$TestPass; confirmPassword=$TestPass } | ConvertTo-Json -Compress) | Out-Null
    } catch {}
    try {
        $r = Invoke-WebRequest -Method POST -Uri "$BaseUrl/auth/login" -UseBasicParsing -ErrorAction Stop `
            -Headers @{ "Content-Type"="application/json" } `
            -Body (@{ email=$TestEmail; password=$TestPass } | ConvertTo-Json -Compress)
        $script:UserToken = ($r.Content | ConvertFrom-Json).token
        Write-Host "  [setup] Token de usuario obtenido." -ForegroundColor DarkGray
    } catch { Write-Host "  [setup] No se pudo obtener token de usuario." -ForegroundColor DarkYellow }
}

if ($needsAdmin -and -not $doAuth -and $AdminEmail -and $AdminPass) {
    try {
        $r = Invoke-WebRequest -Method POST -Uri "$BaseUrl/auth/login" -UseBasicParsing -ErrorAction Stop `
            -Headers @{ "Content-Type"="application/json" } `
            -Body (@{ email=$AdminEmail; password=$AdminPass } | ConvertTo-Json -Compress)
        $script:AdminToken = ($r.Content | ConvertFrom-Json).token
        Write-Host "  [setup] Token de admin obtenido." -ForegroundColor DarkGray
    } catch { Write-Host "  [setup] No se pudo obtener token de admin." -ForegroundColor DarkYellow }
}

# ── BLOQUE 1: AUTH ─────────────────────────────────────────────────────────────

if ($doAuth) {
    Section "AUTH"
    Invoke-ApiTest -Id "T-AUTH-01" -Description "Registro nuevo usuario" `
        -Method POST -Url "/auth/register" -ExpectedStatus 200 -Contains '"success":true' `
        -Body @{ email=$TestEmail; fullName="Test AutoScript"; password=$TestPass; confirmPassword=$TestPass }
    Invoke-ApiTest -Id "T-AUTH-02" -Description "Registro email duplicado" `
        -Method POST -Url "/auth/register" -ExpectedStatus 400 `
        -Body @{ email=$TestEmail; fullName="Test AutoScript"; password=$TestPass; confirmPassword=$TestPass }
    Invoke-ApiTest -Id "T-AUTH-03" -Description "Registro email con formato invalido" `
        -Method POST -Url "/auth/register" -ExpectedStatus 400 `
        -Body @{ email="esto-no-es-email"; fullName="X"; password=$TestPass; confirmPassword=$TestPass }
    Invoke-ApiTest -Id "T-AUTH-04" -Description "Login correcto - obtener token" `
        -Method POST -Url "/auth/login" -ExpectedStatus 200 -Contains '"success":true' `
        -Body @{ email=$TestEmail; password=$TestPass } `
        -OnPass { param($b); $script:UserToken = ($b | ConvertFrom-Json).token }
    Invoke-ApiTest -Id "T-AUTH-05" -Description "Login contrasena incorrecta" `
        -Method POST -Url "/auth/login" -ExpectedStatus 401 `
        -Body @{ email=$TestEmail; password="WrongPass999!" }
    Invoke-ApiTest -Id "T-AUTH-06" -Description "Ruta protegida sin token -> 401" `
        -Method GET -Url "/creativespaces" -ExpectedStatus 401
    Invoke-ApiTest -Id "T-AUTH-07" -Description "Ruta protegida con token invalido -> 401" `
        -Method GET -Url "/creativespaces" -ExpectedStatus 401 -Token "este.token.esinvalido"
    if ($AdminEmail -and $AdminPass) {
        Invoke-ApiTest -Id "T-AUTH-08" -Description "Login admin" `
            -Method POST -Url "/auth/login" -ExpectedStatus 200 -Contains '"success":true' `
            -Body @{ email=$AdminEmail; password=$AdminPass } `
            -OnPass { param($b); $script:AdminToken = ($b | ConvertFrom-Json).token }
    } else {
        Skip-Test -Id "T-AUTH-08" -Description "Login admin" -Reason "ADMIN_EMAIL/ADMIN_PASSWORD no definidos en .env"
    }
}

# ── BLOQUE 2: ESPACIOS ─────────────────────────────────────────────────────────

if ($doSpace) {
    Section "ESPACIOS CREATIVOS"
    if (-not $script:UserToken) {
        Skip-Test -Id "T-SPACE-*" -Description "Todos los tests de espacios" -Reason "UserToken no disponible"
    } else {
        Invoke-ApiTest -Id "T-SPACE-01" -Description "Crear espacio OK" `
            -Method POST -Url "/creativespaces" -ExpectedStatus 201 -Token $script:UserToken `
            -Body @{ name=$SpaceName; description=""; privacy=0; isPublicProfileVisible=$false } `
            -OnPass { param($b); $script:SpaceId = ($b | ConvertFrom-Json).id }
        Invoke-ApiTest -Id "T-SPACE-02" -Description "Crear espacio sin nombre -> error" `
            -Method POST -Url "/creativespaces" -ExpectedStatus 400 -Token $script:UserToken `
            -Body @{ name=""; description=""; privacy=0; isPublicProfileVisible=$false }
        if ($script:SpaceId) {
            Invoke-ApiTest -Id "T-SPACE-03" -Description "Editar espacio OK" `
                -Method PUT -Url "/creativespaces/$($script:SpaceId)" -ExpectedStatus 200 -Token $script:UserToken `
                -Body @{ name="$SpaceName (editado)"; description="Editado"; privacy=0; isPublicProfileVisible=$false }
            Invoke-ApiTest -Id "T-SPACE-04" -Description "Editar espacio de otro usuario -> 404" `
                -Method PUT -Url "/creativespaces/99999" -ExpectedStatus 404 -Token $script:UserToken `
                -Body @{ name="X"; description=""; privacy=0; isPublicProfileVisible=$false }
        } else {
            Skip-Test -Id "T-SPACE-03" -Description "Editar espacio" -Reason "SpaceId no disponible"
            Skip-Test -Id "T-SPACE-04" -Description "Editar espacio ajeno" -Reason "SpaceId no disponible"
        }
        Invoke-ApiTest -Id "T-SPACE-05" -Description "Acceso a espacios autenticado -> 200" `
            -Method GET -Url "/creativespaces" -ExpectedStatus 200 -Token $script:UserToken
    }
}

# ── BLOQUE 3: DOCUMENTOS ───────────────────────────────────────────────────────

if ($doDoc) {
    Section "DOCUMENTOS Y VERSIONES"
    if (-not $script:UserToken) {
        Skip-Test -Id "T-DOC-*" -Description "Todos los tests de documentos" -Reason "UserToken no disponible"
    } else {
        Invoke-ApiTest -Id "T-DOC-01" -Description "Crear documento OK" `
            -Method POST -Url "/documents" -ExpectedStatus 201 -Token $script:UserToken `
            -Body @{ title=$DocTitle; content=$DocContent; description="" } `
            -OnPass { param($b); $script:DocId = ($b | ConvertFrom-Json).id }
        Invoke-ApiTest -Id "T-DOC-02" -Description "Crear documento sin titulo -> error" `
            -Method POST -Url "/documents" -ExpectedStatus 400 -Token $script:UserToken `
            -Body @{ title=""; content="x"; description="" }
        if ($script:DocId) {
            Invoke-ApiTest -Id "T-DOC-03" -Description "Editar documento OK" `
                -Method PUT -Url "/documents/$($script:DocId)" -ExpectedStatus 200 -Token $script:UserToken `
                -Body @{ title=$DocTitle; content="$DocContent (editado)"; description=""; creativeSpaceId=$null }
            Invoke-ApiTest -Id "T-DOC-04" -Description "XSS sanitizado en backend" `
                -Method PUT -Url "/documents/$($script:DocId)" -ExpectedStatus 200 -NotContains "<script>" -Token $script:UserToken `
                -Body @{ title=$DocTitle; content="<script>alert(xss)</script>"; description=""; creativeSpaceId=$null }
            Invoke-ApiTest -Id "T-DOC-05" -Description "Crear snapshot de version" `
                -Method POST -Url "/documentversions/document/$($script:DocId)/snapshot" -ExpectedStatus 201 -Token $script:UserToken `
                -Body @{ comment="snapshot-autotest" } `
                -OnPass { param($b); $script:VersionId = ($b | ConvertFrom-Json).id }
            Invoke-ApiTest -Id "T-DOC-06" -Description "Listar versiones del documento" `
                -Method GET -Url "/documentversions/document/$($script:DocId)" -ExpectedStatus 200 -Token $script:UserToken
            # T-DOC-07: necesita un documento que pertenezca a OTRO usuario (admin).
            # Crear uno temporalmente con el token admin y eliminarlo tras el test.
            $script:AdminDocId = $null
            if ($script:AdminToken) {
                try {
                    $adminDocBody = @{ title="Doc Admin T-DOC-07 $Timestamp"; content="doc temporal para test acceso ajeno"; description="" } | ConvertTo-Json -Compress
                    $adminDocResp = Invoke-WebRequest -Method POST -Uri "$BaseUrl/documents" `
                        -Headers @{ "Content-Type"="application/json"; "Authorization"="Bearer $script:AdminToken" } `
                        -Body $adminDocBody -UseBasicParsing -ErrorAction Stop
                    $script:AdminDocId = ($adminDocResp.Content | ConvertFrom-Json).id
                } catch { }
            }

            if ($script:AdminDocId) {
                Invoke-ApiTest -Id "T-DOC-07" -Description "Snapshot de documento ajeno -> 403" `
                    -Method POST -Url "/documentversions/document/$($script:AdminDocId)/snapshot" -ExpectedStatus 403 -Token $script:UserToken `
                    -Body @{ comment="intruso" }

                # Cleanup: eliminar el documento temporal del admin
                try {
                    Invoke-WebRequest -Method DELETE -Uri "$BaseUrl/documents/$($script:AdminDocId)" `
                        -Headers @{ "Authorization"="Bearer $script:AdminToken" } `
                        -UseBasicParsing -ErrorAction SilentlyContinue | Out-Null
                } catch { }
            } else {
                Skip-Test -Id "T-DOC-07" -Description "Snapshot de documento ajeno -> 403" `
                    -Reason "AdminToken no disponible; configurar ADMIN_EMAIL/ADMIN_PASSWORD en .env"
            }
            if ($script:VersionId) {
                Invoke-ApiTest -Id "T-DOC-08" -Description "Restaurar version anterior" `
                    -Method POST -Url "/documentversions/$($script:VersionId)/restore" -ExpectedStatus 200 -Token $script:UserToken `
                    -Body @{}
            } else {
                Skip-Test -Id "T-DOC-08" -Description "Restaurar version" -Reason "VersionId no disponible"
            }
            Invoke-ApiTest -Id "T-DOC-09" -Description "Eliminar documento OK" `
                -Method DELETE -Url "/documents/$($script:DocId)" -ExpectedStatus 204 -Token $script:UserToken
        } else {
            "T-DOC-03","T-DOC-04","T-DOC-05","T-DOC-06","T-DOC-07","T-DOC-08","T-DOC-09" | ForEach-Object {
                Skip-Test -Id $_ -Description "Test de documento" -Reason "DocId no disponible"
            }
        }
    }
}

# ── BLOQUE 4: COLABORACION ────────────────────────────────────────────────────

if ($doCol) {
    Section "COLABORACION EN ESPACIOS COMPARTIDOS"
    if (-not ($script:AdminToken -and $script:UserToken)) {
        Skip-Test -Id "T-COL-01" -Description "Editor puede editar documento ajeno" -Reason "AdminToken o UserToken no disponibles"
        Skip-Test -Id "T-COL-02" -Description "Editor no puede borrar documento ajeno" -Reason "AdminToken o UserToken no disponibles"
        Skip-Test -Id "T-COL-03" -Description "Viewer no puede editar documento ajeno" -Reason "AdminToken o UserToken no disponibles"
    } else {
        $script:UserId = $null
        try {
            $meResp = Invoke-WebRequest -Method GET -Uri "$BaseUrl/users/me" `
                -Headers @{ "Authorization"="Bearer $script:UserToken" } `
                -UseBasicParsing -ErrorAction Stop
            $script:UserId = ($meResp.Content | ConvertFrom-Json).id
        } catch { }

        if (-not $script:UserId) {
            Skip-Test -Id "T-COL-01" -Description "Editor puede editar documento ajeno" -Reason "No se pudo obtener UserId"
            Skip-Test -Id "T-COL-02" -Description "Editor no puede borrar documento ajeno" -Reason "No se pudo obtener UserId"
            Skip-Test -Id "T-COL-03" -Description "Viewer no puede editar documento ajeno" -Reason "No se pudo obtener UserId"
        } else {
            $script:ColSpaceId = $null; $script:ColDocId = $null
            $colTs = Get-Date -Format "yyyyMMdd_HHmmss"
            try {
                $r = Invoke-WebRequest -Method POST -Uri "$BaseUrl/creativespaces" -UseBasicParsing -ErrorAction Stop `
                    -Headers @{ "Content-Type"="application/json"; "Authorization"="Bearer $script:AdminToken" } `
                    -Body (@{ name="Col-Test-$colTs"; description="temp"; privacy=0 } | ConvertTo-Json -Compress)
                $script:ColSpaceId = ($r.Content | ConvertFrom-Json).id
            } catch { }

            if ($script:ColSpaceId) {
                try {
                    $r = Invoke-WebRequest -Method POST -Uri "$BaseUrl/documents" -UseBasicParsing -ErrorAction Stop `
                        -Headers @{ "Content-Type"="application/json"; "Authorization"="Bearer $script:AdminToken" } `
                        -Body (@{ title="Doc-Col-$colTs"; content="contenido original"; description=""; creativeSpaceId=$script:ColSpaceId } | ConvertTo-Json -Compress)
                    $script:ColDocId = ($r.Content | ConvertFrom-Json).id
                } catch { }
            }

            if ($script:ColSpaceId -and $script:ColDocId) {
                try {
                    Invoke-WebRequest -Method POST -Uri "$BaseUrl/creativespaces/$($script:ColSpaceId)/permissions" -UseBasicParsing -ErrorAction SilentlyContinue `
                        -Headers @{ "Content-Type"="application/json"; "Authorization"="Bearer $script:AdminToken" } `
                        -Body (@{ userId=$script:UserId; permissionLevel=1 } | ConvertTo-Json -Compress) | Out-Null
                } catch { }

                Invoke-ApiTest -Id "T-COL-01" -Description "Editor del espacio puede editar documento ajeno -> 200" `
                    -Method PUT -Url "/documents/$($script:ColDocId)" -ExpectedStatus 200 -Token $script:UserToken `
                    -Body @{ title="Doc-Col-$colTs"; content="editado por colaborador"; description="" }

                Invoke-ApiTest -Id "T-COL-02" -Description "Editor del espacio no puede borrar documento ajeno -> 403" `
                    -Method DELETE -Url "/documents/$($script:ColDocId)" -ExpectedStatus 403 -Token $script:UserToken

                try {
                    Invoke-WebRequest -Method POST -Uri "$BaseUrl/creativespaces/$($script:ColSpaceId)/permissions" -UseBasicParsing -ErrorAction SilentlyContinue `
                        -Headers @{ "Content-Type"="application/json"; "Authorization"="Bearer $script:AdminToken" } `
                        -Body (@{ userId=$script:UserId; permissionLevel=0 } | ConvertTo-Json -Compress) | Out-Null
                } catch { }

                Invoke-ApiTest -Id "T-COL-03" -Description "Viewer del espacio no puede editar documento ajeno -> 403" `
                    -Method PUT -Url "/documents/$($script:ColDocId)" -ExpectedStatus 403 -Token $script:UserToken `
                    -Body @{ title="Doc-Col-$colTs"; content="intento de edicion viewer"; description="" }
            } else {
                Skip-Test -Id "T-COL-01" -Description "Editor puede editar documento ajeno" -Reason "No se pudo crear espacio/documento temporal"
                Skip-Test -Id "T-COL-02" -Description "Editor no puede borrar documento ajeno" -Reason "No se pudo crear espacio/documento temporal"
                Skip-Test -Id "T-COL-03" -Description "Viewer no puede editar documento ajeno" -Reason "No se pudo crear espacio/documento temporal"
            }

            if ($script:ColDocId) {
                try {
                    Invoke-WebRequest -Method DELETE -Uri "$BaseUrl/documents/$($script:ColDocId)" `
                        -Headers @{ "Authorization"="Bearer $script:AdminToken" } `
                        -UseBasicParsing -ErrorAction SilentlyContinue | Out-Null
                } catch { }
            }
            if ($script:ColSpaceId) {
                try {
                    Invoke-WebRequest -Method DELETE -Uri "$BaseUrl/creativespaces/$($script:ColSpaceId)" `
                        -Headers @{ "Authorization"="Bearer $script:AdminToken" } `
                        -UseBasicParsing -ErrorAction SilentlyContinue | Out-Null
                } catch { }
            }
        }
    }
}

# ── BLOQUE 5: ADMIN ────────────────────────────────────────────────────────────

if ($doAdmin) {
    Section "PANEL DE ADMINISTRACION"
    Invoke-ApiTest -Id "T-ADMIN-01" -Description "Acceso admin sin token -> 401" `
        -Method GET -Url "/admin/allowed-websites" -ExpectedStatus 401
    if ($script:UserToken) {
        Invoke-ApiTest -Id "T-ADMIN-02" -Description "Acceso admin con rol User -> 403" `
            -Method GET -Url "/admin/allowed-websites" -ExpectedStatus 403 -Token $script:UserToken
    } else {
        Skip-Test -Id "T-ADMIN-02" -Description "Acceso admin con rol User" -Reason "UserToken no disponible"
    }
    if ($script:AdminToken) {
        Invoke-ApiTest -Id "T-ADMIN-03" -Description "Acceso admin con rol Admin -> 200" `
            -Method GET -Url "/admin/allowed-websites" -ExpectedStatus 200 -Token $script:AdminToken
        Invoke-ApiTest -Id "T-ADMIN-04" -Description "Anadir dominio permitido" `
            -Method POST -Url "/admin/allowed-websites" -ExpectedStatus 201 -Token $script:AdminToken `
            -Body @{ domain=$AdminDomain; isActive=$true } `
            -OnPass { param($b); $script:WebsiteId = ($b | ConvertFrom-Json).id }
        if ($script:WebsiteId) {
            Invoke-ApiTest -Id "T-ADMIN-05" -Description "Desactivar dominio" `
                -Method PUT -Url "/admin/allowed-websites/$($script:WebsiteId)" -ExpectedStatus 200 -Token $script:AdminToken `
                -Body @{ domain=$AdminDomain; isActive=$false }
        } else {
            Skip-Test -Id "T-ADMIN-05" -Description "Desactivar dominio" -Reason "WebsiteId no disponible"
        }
        Invoke-ApiTest -Id "T-ADMIN-06" -Description "Listar usuarios (admin)" `
            -Method GET -Url "/users" -ExpectedStatus 200 -Token $script:AdminToken
    } else {
        "T-ADMIN-03","T-ADMIN-04","T-ADMIN-05","T-ADMIN-06" | ForEach-Object {
            Skip-Test -Id $_ -Description "Test admin" -Reason "AdminToken no disponible"
        }
    }
}

# ── BLOQUE 5: SEGURIDAD ────────────────────────────────────────────────────────

if ($doSec) {
    Section "SEGURIDAD"
    Invoke-ApiTest -Id "T-SEC-01" -Description "Token expirado/invalido -> 401" `
        -Method GET -Url "/creativespaces" -ExpectedStatus 401 `
        -Token "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE2MDAwMDAwMDB9.invalido"
    if ($script:UserToken -and $script:AdminToken) {
        Invoke-ApiTest -Id "T-SEC-02" -Description "Token User en endpoint Admin -> 403" `
            -Method GET -Url "/admin/allowed-websites" -ExpectedStatus 403 -Token $script:UserToken
    } else {
        Skip-Test -Id "T-SEC-02" -Description "Token User en endpoint Admin" -Reason "Tokens no disponibles"
    }
}

# ── LIMPIEZA ───────────────────────────────────────────────────────────────────

Section "LIMPIEZA"
if ($script:SpaceId -and $script:UserToken) {
    try {
        Invoke-WebRequest -Method DELETE -Uri "$BaseUrl/creativespaces/$($script:SpaceId)" `
            -Headers @{ Authorization="Bearer $($script:UserToken)" } -UseBasicParsing -ErrorAction SilentlyContinue | Out-Null
        Write-Host "  Espacio $($script:SpaceId) eliminado." -ForegroundColor DarkGray
    } catch {}
}
if ($script:WebsiteId -and $script:AdminToken) {
    try {
        Invoke-WebRequest -Method DELETE -Uri "$BaseUrl/admin/allowed-websites/$($script:WebsiteId)" `
            -Headers @{ Authorization="Bearer $($script:AdminToken)" } -UseBasicParsing -ErrorAction SilentlyContinue | Out-Null
        Write-Host "  Dominio $($script:WebsiteId) eliminado." -ForegroundColor DarkGray
    } catch {}
}
if ($script:DocId -and $script:UserToken) {
    try {
        Invoke-WebRequest -Method DELETE -Uri "$BaseUrl/documents/$($script:DocId)" `
            -Headers @{ Authorization="Bearer $($script:UserToken)" } -UseBasicParsing -ErrorAction SilentlyContinue | Out-Null
        Write-Host "  Documento $($script:DocId) eliminado." -ForegroundColor DarkGray
    } catch {}
}
if ($script:UserToken) {
    try {
        Invoke-WebRequest -Method DELETE -Uri "$BaseUrl/users/me" `
            -Headers @{ Authorization="Bearer $($script:UserToken)" } -UseBasicParsing -ErrorAction SilentlyContinue | Out-Null
        Write-Host "  Usuario $TestEmail eliminado." -ForegroundColor DarkGray
    } catch {}
}

# ── RESUMEN ────────────────────────────────────────────────────────────────────

$passed  = ($Results | Where-Object { $_.Pass -eq $true  } | Measure-Object).Count
$failed  = ($Results | Where-Object { $_.Pass -eq $false } | Measure-Object).Count
$skipped = ($Results | Where-Object { $null -eq $_.Pass  } | Measure-Object).Count
$total   = $Results.Count

Write-Host ""
Write-Host "===========================================" -ForegroundColor White
Write-Host " RESUMEN: $passed OK  |  $failed FAIL  |  $skipped SKIP  |  $total total" -ForegroundColor White
Write-Host "===========================================" -ForegroundColor White

# ── INFORME MARKDOWN ───────────────────────────────────────────────────────────

$groups = [ordered]@{
    "Autenticacion"                = "T-AUTH"
    "Espacios Creativos"           = "T-SPACE"
    "Documentos y Versiones"       = "T-DOC"
    "Colaboracion en espacios"     = "T-COL"
    "Panel de Administracion"      = "T-ADMIN"
    "Seguridad"                    = "T-SEC"
}
$sb = [System.Text.StringBuilder]::new()
[void]$sb.AppendLine("# Informe de Pruebas -- LifeHub (interactivo)")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("**Fecha:** $((Get-Date).ToString('yyyy-MM-dd HH:mm:ss'))  ")
[void]$sb.AppendLine("**Modulos:** $($selectedNames -join ', ')  ")
[void]$sb.AppendLine("**Entorno:** $BaseUrl  ")
[void]$sb.AppendLine("**Script:** run-tests-interactive.ps1  ")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("---")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## Resultados")

foreach ($section in $groups.GetEnumerator()) {
    $sr = $Results | Where-Object { $_.Id -like "$($section.Value)*" }
    if (-not $sr) { continue }
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("### $($section.Key)")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("| ID | Descripcion | Esperado | Real | Resultado |")
    [void]$sb.AppendLine("|----|-------------|----------|------|-----------|")
    foreach ($r in $sr) {
        $estado = if ($null -eq $r.Pass) { "SKIP" } elseif ($r.Pass) { "PASS" } else { "FAIL" }
        [void]$sb.AppendLine("| $($r.Id) | $($r.Description) | $($r.ExpectedStatus) | $($r.ActualStatus) | $estado |")
    }
}

[void]$sb.AppendLine("")
[void]$sb.AppendLine("---")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## Resumen")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("| Modulo | Total | PASS | FAIL | SKIP |")
[void]$sb.AppendLine("|--------|-------|------|------|------|")
foreach ($section in $groups.GetEnumerator()) {
    $sr = @($Results | Where-Object { $_.Id -like "$($section.Value)*" })
    if ($sr.Count -eq 0) { continue }
    $sp = ($sr | Where-Object { $_.Pass -eq $true  } | Measure-Object).Count
    $sf = ($sr | Where-Object { $_.Pass -eq $false } | Measure-Object).Count
    $ss = ($sr | Where-Object { $null -eq $_.Pass  } | Measure-Object).Count
    [void]$sb.AppendLine("| $($section.Key) | $($sr.Count) | $sp | $sf | $ss |")
}
[void]$sb.AppendLine("| **TOTAL** | **$total** | **$passed** | **$failed** | **$skipped** |")

$outputPath = Join-Path $OutputDir "RESULTADO_PRUEBAS_$Timestamp.md"
[System.IO.File]::WriteAllText($outputPath, $sb.ToString(), [System.Text.Encoding]::UTF8)
Write-Host ""
if ($failed -gt 0) {
    Write-Host "Informe generado con $failed incidencia(s): $outputPath" -ForegroundColor Yellow
} else {
    Write-Host "Informe generado: $outputPath" -ForegroundColor Green
}

exit $(if ($failed -gt 0) { 1 } else { 0 })
