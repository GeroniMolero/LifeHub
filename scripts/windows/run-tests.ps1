Param(
    [string]$BaseUrl    = "http://localhost:5000/api",
    [string]$OutputDir  = ""
)

$ErrorActionPreference = "Continue"

# --- Configuracion ---

$Timestamp   = Get-Date -Format "yyyyMMdd_HHmmss"
$TestEmail   = "autotest_$Timestamp@lifehub-auto.test"
$TestPass    = "AutoTest123!"

$ProjectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not $OutputDir) { $OutputDir = Join-Path $ProjectRoot "documentacion" }

# Leer credenciales de admin desde .env (nunca hardcodeadas en el script)
$AdminEmail  = $null
$AdminPass   = $null
$envFile     = Join-Path $ProjectRoot ".env"
if (Test-Path $envFile) {
    Get-Content $envFile | Where-Object { $_ -match '^\s*[^#]' } | ForEach-Object {
        if ($_ -match '^\s*ADMIN_EMAIL\s*=\s*(.+)$')    { $AdminEmail = $Matches[1].Trim() }
        if ($_ -match '^\s*ADMIN_PASSWORD\s*=\s*(.+)$') { $AdminPass  = $Matches[1].Trim() }
    }
}
if (-not $AdminEmail -or -not $AdminPass) {
    Write-Host "AVISO: ADMIN_EMAIL o ADMIN_PASSWORD no encontrados en .env -- los tests de admin seran SKIP." -ForegroundColor DarkYellow
}

# Estado entre tests
$script:UserToken      = $null
$script:AdminToken     = $null
$script:SpaceId        = $null
$script:DocId          = $null
$script:VersionId      = $null
$script:WebsiteId      = $null

# Resultados
$Results = [System.Collections.Generic.List[PSCustomObject]]::new()

# --- Helpers ---

function Invoke-ApiTest {
    param(
        [string]$Id,
        [string]$Description,
        [string]$Method,
        [string]$Url,
        [hashtable]$Body       = $null,
        [string]$Token         = $null,
        [int]$ExpectedStatus,
        [string]$Contains      = $null,
        [string]$NotContains   = $null,
        [scriptblock]$OnPass   = $null
    )

    $headers = @{ "Content-Type" = "application/json" }
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }

    $actualStatus = 0
    $responseBody = ""

    try {
        $params = @{
            Method          = $Method
            Uri             = "$BaseUrl$Url"
            Headers         = $headers
            UseBasicParsing = $true
            ErrorAction     = "Stop"
        }
        if ($Body) { $params["Body"] = ($Body | ConvertTo-Json -Compress) }

        $response      = Invoke-WebRequest @params
        $actualStatus  = [int]$response.StatusCode
        $responseBody  = $response.Content
    }
    catch {
        try   { $actualStatus = [int]$_.Exception.Response.StatusCode }
        catch { $actualStatus = 0 }
        try {
            $stream       = $_.Exception.Response.GetResponseStream()
            $reader       = [System.IO.StreamReader]::new($stream)
            $responseBody = $reader.ReadToEnd()
            $reader.Dispose()
        }
        catch { $responseBody = "(sin cuerpo)" }
    }

    $pass = ($actualStatus -eq $ExpectedStatus)
    if ($pass -and $Contains)    { $pass = $responseBody -match [regex]::Escape($Contains) }
    if ($pass -and $NotContains) { $pass = -not ($responseBody -match [regex]::Escape($NotContains)) }

    if ($pass -and $OnPass) { & $OnPass $responseBody }

    $truncated = if ($responseBody.Length -gt 180) { $responseBody.Substring(0,180) + "..." } else { $responseBody }

    $row = [PSCustomObject]@{
        Id             = $Id
        Description    = $Description
        ExpectedStatus = $ExpectedStatus
        ActualStatus   = $actualStatus
        Pass           = $pass
        ResponseSnip   = $truncated
    }
    $Results.Add($row)

    $icon  = if ($pass) { "OK  " } else { "FAIL" }
    $color = if ($pass) { "Green" } else { "Red" }
    Write-Host "  [$icon] $Id - $Description  (esperado $ExpectedStatus, real $actualStatus)" -ForegroundColor $color
}

function Skip-Test {
    param([string]$Id, [string]$Description, [string]$Reason)
    $row = [PSCustomObject]@{
        Id = $Id; Description = $Description
        ExpectedStatus = "-"; ActualStatus = "SKIP"; Pass = $null; ResponseSnip = $Reason
    }
    $Results.Add($row)
    Write-Host "  [SKIP] $Id - $Description ($Reason)" -ForegroundColor DarkYellow
}

function Section { param([string]$Name)
    Write-Host ""
    Write-Host "--- $Name ---" -ForegroundColor Cyan
}

# --- BLOQUE 1: Autenticacion ---

Section "AUTH"

Invoke-ApiTest -Id "T-AUTH-01" -Description "Registro nuevo usuario" `
    -Method POST -Url "/auth/register" -ExpectedStatus 200 `
    -Contains '"success":true' `
    -Body @{ email=$TestEmail; fullName="Test AutoScript"; password=$TestPass; confirmPassword=$TestPass }

Invoke-ApiTest -Id "T-AUTH-02" -Description "Registro email duplicado" `
    -Method POST -Url "/auth/register" -ExpectedStatus 400 `
    -Body @{ email=$TestEmail; fullName="Test AutoScript"; password=$TestPass; confirmPassword=$TestPass }

Invoke-ApiTest -Id "T-AUTH-03" -Description "Registro email con formato invalido" `
    -Method POST -Url "/auth/register" -ExpectedStatus 400 `
    -Body @{ email="esto-no-es-email"; fullName="X"; password=$TestPass; confirmPassword=$TestPass }

Invoke-ApiTest -Id "T-AUTH-04" -Description "Login correcto - obtener token" `
    -Method POST -Url "/auth/login" -ExpectedStatus 200 `
    -Contains '"success":true' `
    -Body @{ email=$TestEmail; password=$TestPass } `
    -OnPass {
        param($body)
        $obj = $body | ConvertFrom-Json
        $script:UserToken = $obj.token
    }

Invoke-ApiTest -Id "T-AUTH-05" -Description "Login contrasena incorrecta" `
    -Method POST -Url "/auth/login" -ExpectedStatus 401 `
    -Body @{ email=$TestEmail; password="WrongPass999!" }

Invoke-ApiTest -Id "T-AUTH-06" -Description "Ruta protegida sin token -> 401" `
    -Method GET -Url "/creativespaces" -ExpectedStatus 401

Invoke-ApiTest -Id "T-AUTH-07" -Description "Ruta protegida con token invalido -> 401" `
    -Method GET -Url "/creativespaces" -ExpectedStatus 401 `
    -Token "este.token.esinvalido"

if ($AdminEmail -and $AdminPass) {
    Invoke-ApiTest -Id "T-AUTH-08" -Description "Login admin (setup para tests admin)" `
        -Method POST -Url "/auth/login" -ExpectedStatus 200 `
        -Contains '"success":true' `
        -Body @{ email=$AdminEmail; password=$AdminPass } `
        -OnPass {
            param($body)
            $obj = $body | ConvertFrom-Json
            $script:AdminToken = $obj.token
        }
} else {
    Skip-Test -Id "T-AUTH-08" -Description "Login admin" -Reason "ADMIN_EMAIL/ADMIN_PASSWORD no definidos en .env"
}

# --- BLOQUE 2: Espacios creativos ---

Section "ESPACIOS CREATIVOS"

if (-not $script:UserToken) {
    Skip-Test -Id "T-SPACE-*" -Description "Todos los tests de espacios" -Reason "UserToken no disponible (T-AUTH-04 fallo)"
}
else {

    Invoke-ApiTest -Id "T-SPACE-01" -Description "Crear espacio OK" `
        -Method POST -Url "/creativespaces" -ExpectedStatus 201 `
        -Token $script:UserToken `
        -Body @{ name="Espacio AutoTest $Timestamp"; description=""; privacy=0; isPublicProfileVisible=$false } `
        -OnPass {
            param($body)
            $obj = $body | ConvertFrom-Json
            $script:SpaceId = $obj.id
        }

    Invoke-ApiTest -Id "T-SPACE-02" -Description "Crear espacio sin nombre -> error" `
        -Method POST -Url "/creativespaces" -ExpectedStatus 400 `
        -Token $script:UserToken `
        -Body @{ name=""; description=""; privacy=0; isPublicProfileVisible=$false }

    if ($script:SpaceId) {
        Invoke-ApiTest -Id "T-SPACE-03" -Description "Editar espacio OK" `
            -Method PUT -Url "/creativespaces/$($script:SpaceId)" -ExpectedStatus 200 `
            -Token $script:UserToken `
            -Body @{ name="Espacio Editado $Timestamp"; description="Editado"; privacy=0; isPublicProfileVisible=$false }

        Invoke-ApiTest -Id "T-SPACE-04" -Description "Editar espacio de otro usuario -> 404" `
            -Method PUT -Url "/creativespaces/99999" -ExpectedStatus 404 `
            -Token $script:UserToken `
            -Body @{ name="X"; description=""; privacy=0; isPublicProfileVisible=$false }
    }
    else {
        Skip-Test -Id "T-SPACE-03" -Description "Editar espacio" -Reason "SpaceId no disponible"
        Skip-Test -Id "T-SPACE-04" -Description "Editar espacio ajeno" -Reason "SpaceId no disponible"
    }

    Invoke-ApiTest -Id "T-SPACE-05" -Description "Acceso a espacios autenticado -> 200" `
        -Method GET -Url "/creativespaces" -ExpectedStatus 200 `
        -Token $script:UserToken
}

# --- BLOQUE 3: Documentos y versiones ---

Section "DOCUMENTOS Y VERSIONES"

if (-not $script:UserToken) {
    Skip-Test -Id "T-DOC-*" -Description "Todos los tests de documentos" -Reason "UserToken no disponible"
}
else {

    Invoke-ApiTest -Id "T-DOC-01" -Description "Crear documento OK" `
        -Method POST -Url "/documents" -ExpectedStatus 201 `
        -Token $script:UserToken `
        -Body @{ title="Doc AutoTest $Timestamp"; content="# Test\nContenido inicial."; description="" } `
        -OnPass {
            param($body)
            $obj = $body | ConvertFrom-Json
            $script:DocId = $obj.id
        }

    Invoke-ApiTest -Id "T-DOC-02" -Description "Crear documento sin titulo -> error" `
        -Method POST -Url "/documents" -ExpectedStatus 400 `
        -Token $script:UserToken `
        -Body @{ title=""; content="x"; description="" }

    if ($script:DocId) {
        Invoke-ApiTest -Id "T-DOC-03" -Description "Editar documento OK" `
            -Method PUT -Url "/documents/$($script:DocId)" -ExpectedStatus 200 `
            -Token $script:UserToken `
            -Body @{ title="Doc AutoTest $Timestamp"; content="# Test\nContenido editado."; description=""; creativeSpaceId=$null }

        $xssContent = "<script>alert(xss)</script>"
        Invoke-ApiTest -Id "T-DOC-04" -Description "Contenido XSS almacenado (backend no sanitiza)" `
            -Method PUT -Url "/documents/$($script:DocId)" -ExpectedStatus 200 `
            -Contains "<script>" `
            -Token $script:UserToken `
            -Body @{ title="Doc AutoTest $Timestamp"; content=$xssContent; description=""; creativeSpaceId=$null }

        Invoke-ApiTest -Id "T-DOC-05" -Description "Crear snapshot de version" `
            -Method POST -Url "/documentversions/document/$($script:DocId)/snapshot" -ExpectedStatus 201 `
            -Token $script:UserToken `
            -Body @{ comment="snapshot-autotest" } `
            -OnPass {
                param($body)
                $obj = $body | ConvertFrom-Json
                $script:VersionId = $obj.id
            }

        Invoke-ApiTest -Id "T-DOC-06" -Description "Listar versiones del documento" `
            -Method GET -Url "/documentversions/document/$($script:DocId)" -ExpectedStatus 200 `
            -Token $script:UserToken

        Invoke-ApiTest -Id "T-DOC-07" -Description "Snapshot de documento ajeno -> 403" `
            -Method POST -Url "/documentversions/document/1/snapshot" -ExpectedStatus 403 `
            -Token $script:UserToken `
            -Body @{ comment="intruso" }

        if ($script:VersionId) {
            Invoke-ApiTest -Id "T-DOC-08" -Description "Restaurar version anterior" `
                -Method POST -Url "/documentversions/$($script:VersionId)/restore" -ExpectedStatus 200 `
                -Token $script:UserToken `
                -Body @{}
        }
        else {
            Skip-Test -Id "T-DOC-08" -Description "Restaurar version" -Reason "VersionId no disponible"
        }

        Invoke-ApiTest -Id "T-DOC-09" -Description "Eliminar documento OK" `
            -Method DELETE -Url "/documents/$($script:DocId)" -ExpectedStatus 204 `
            -Token $script:UserToken
    }
    else {
        "T-DOC-03","T-DOC-04","T-DOC-05","T-DOC-06","T-DOC-07","T-DOC-08","T-DOC-09" | ForEach-Object {
            Skip-Test -Id $_ -Description "Test de documento" -Reason "DocId no disponible"
        }
    }
}

# --- BLOQUE 4: Panel de administracion ---

Section "PANEL DE ADMINISTRACION"

Invoke-ApiTest -Id "T-ADMIN-01" -Description "Acceso admin sin token -> 401" `
    -Method GET -Url "/admin/allowed-websites" -ExpectedStatus 401

if ($script:UserToken) {
    Invoke-ApiTest -Id "T-ADMIN-02" -Description "Acceso admin con rol User -> 403" `
        -Method GET -Url "/admin/allowed-websites" -ExpectedStatus 403 `
        -Token $script:UserToken
}
else {
    Skip-Test -Id "T-ADMIN-02" -Description "Acceso admin con rol User" -Reason "UserToken no disponible"
}

if ($script:AdminToken) {
    Invoke-ApiTest -Id "T-ADMIN-03" -Description "Acceso admin con rol Admin -> 200" `
        -Method GET -Url "/admin/allowed-websites" -ExpectedStatus 200 `
        -Token $script:AdminToken

    Invoke-ApiTest -Id "T-ADMIN-04" -Description "Anadir dominio permitido" `
        -Method POST -Url "/admin/allowed-websites" -ExpectedStatus 201 `
        -Token $script:AdminToken `
        -Body @{ domain="autotest-$Timestamp.io"; isActive=$true } `
        -OnPass {
            param($body)
            $obj = $body | ConvertFrom-Json
            $script:WebsiteId = $obj.id
        }

    if ($script:WebsiteId) {
        Invoke-ApiTest -Id "T-ADMIN-05" -Description "Desactivar dominio" `
            -Method PUT -Url "/admin/allowed-websites/$($script:WebsiteId)" -ExpectedStatus 200 `
            -Token $script:AdminToken `
            -Body @{ domain="autotest-$Timestamp.io"; isActive=$false }
    }
    else {
        Skip-Test -Id "T-ADMIN-05" -Description "Desactivar dominio" -Reason "WebsiteId no disponible"
    }

    Invoke-ApiTest -Id "T-ADMIN-06" -Description "Listar usuarios (admin)" `
        -Method GET -Url "/users" -ExpectedStatus 200 `
        -Token $script:AdminToken
}
else {
    "T-ADMIN-03","T-ADMIN-04","T-ADMIN-05","T-ADMIN-06" | ForEach-Object {
        Skip-Test -Id $_ -Description "Test admin" -Reason "AdminToken no disponible"
    }
}

# --- BLOQUE 5: Seguridad adicional ---

Section "SEGURIDAD"

Invoke-ApiTest -Id "T-SEC-01" -Description "Token expirado/invalido -> 401" `
    -Method GET -Url "/creativespaces" -ExpectedStatus 401 `
    -Token "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE2MDAwMDAwMDB9.invalido"

if ($script:UserToken -and $script:AdminToken) {
    Invoke-ApiTest -Id "T-SEC-02" -Description "Token User en endpoint Admin -> 403" `
        -Method GET -Url "/admin/allowed-websites" -ExpectedStatus 403 `
        -Token $script:UserToken
}
else {
    Skip-Test -Id "T-SEC-02" -Description "Token User en endpoint Admin" -Reason "Tokens no disponibles"
}

# --- BLOQUE 6: Limpieza ---

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

Write-Host "  Nota: el usuario $TestEmail no tiene endpoint de borrado -- eliminacion manual si se desea." -ForegroundColor DarkGray

# --- BLOQUE 7: Resumen y generacion del informe ---

$passed  = ($Results | Where-Object { $_.Pass -eq $true  } | Measure-Object).Count
$failed  = ($Results | Where-Object { $_.Pass -eq $false } | Measure-Object).Count
$skipped = ($Results | Where-Object { $null -eq $_.Pass  } | Measure-Object).Count
$total   = $Results.Count

Write-Host ""
Write-Host "===========================================" -ForegroundColor White
Write-Host " RESUMEN: $passed OK  |  $failed FAIL  |  $skipped SKIP  |  $total total" -ForegroundColor White
Write-Host "===========================================" -ForegroundColor White

# Generar markdown
$groups = [ordered]@{
    "Autenticacion"           = "T-AUTH"
    "Espacios Creativos"      = "T-SPACE"
    "Documentos y Versiones"  = "T-DOC"
    "Panel de Administracion" = "T-ADMIN"
    "Seguridad"               = "T-SEC"
}

$sb = [System.Text.StringBuilder]::new()

[void]$sb.AppendLine("# Informe de Pruebas Automaticas -- LifeHub")
[void]$sb.AppendLine("")
$fechaStr = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
[void]$sb.AppendLine("**Fecha:** $fechaStr  ")
[void]$sb.AppendLine("**Entorno:** $BaseUrl  ")
[void]$sb.AppendLine("**Usuario de prueba:** $TestEmail  ")
[void]$sb.AppendLine("**Script:** run-tests.ps1  ")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("---")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## Resultados por modulo")

foreach ($section in $groups.GetEnumerator()) {
    $sectionResults = $Results | Where-Object { $_.Id -like "$($section.Value)*" }
    if (-not $sectionResults) { continue }

    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("### $($section.Key)")
    [void]$sb.AppendLine("")
    [void]$sb.AppendLine("| ID | Descripcion | Esperado | Real | Resultado |")
    [void]$sb.AppendLine("|----|-------------|----------|------|-----------|")

    foreach ($r in $sectionResults) {
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
    $sr   = @($Results | Where-Object { $_.Id -like "$($section.Value)*" })
    $sp   = ($sr | Where-Object { $_.Pass -eq $true  } | Measure-Object).Count
    $sf   = ($sr | Where-Object { $_.Pass -eq $false } | Measure-Object).Count
    $ss   = ($sr | Where-Object { $null -eq $_.Pass  } | Measure-Object).Count
    if ($sr.Count -eq 0) { continue }
    [void]$sb.AppendLine("| $($section.Key) | $($sr.Count) | $sp | $sf | $ss |")
}
[void]$sb.AppendLine("| **TOTAL** | **$total** | **$passed** | **$failed** | **$skipped** |")

[void]$sb.AppendLine("")
[void]$sb.AppendLine("---")
[void]$sb.AppendLine("")
[void]$sb.AppendLine("## Incidencias")
[void]$sb.AppendLine("")

$failures = $Results | Where-Object { $_.Pass -eq $false }
if ($failures) {
    [void]$sb.AppendLine("| ID | Descripcion | Esperado | Real | Respuesta |")
    [void]$sb.AppendLine("|----|-------------|----------|------|-----------|")
    foreach ($f in $failures) {
        $snip = $f.ResponseSnip -replace "\|", "\|"
        [void]$sb.AppendLine("| $($f.Id) | $($f.Description) | $($f.ExpectedStatus) | $($f.ActualStatus) | $snip |")
    }
}
else {
    [void]$sb.AppendLine("Sin incidencias. Todos los tests ejecutados han resultado PASS.")
}

$outputPath = Join-Path $OutputDir "RESULTADO_PRUEBAS_$Timestamp.md"
[System.IO.File]::WriteAllText($outputPath, $sb.ToString(), [System.Text.Encoding]::UTF8)

Write-Host ""
if ($failed -gt 0) {
    Write-Host "Informe generado con $failed incidencia(s): $outputPath" -ForegroundColor Yellow
} else {
    Write-Host "Informe generado: $outputPath" -ForegroundColor Green
}

# --- Actualizar PLAN_PRUEBAS.md con nuevas incidencias ---

if ($failures) {
    $planPath = Join-Path $OutputDir "PLAN_PRUEBAS.md"
    if (Test-Path $planPath) {
        $planLines = [System.IO.File]::ReadAllLines($planPath, [System.Text.Encoding]::UTF8)

        # Buscar el ultimo indice INC-XX y el separador de la tabla de incidencias
        $lastIncIdx = -1
        $incSepIdx  = -1
        $inIncSection = $false
        for ($i = 0; $i -lt $planLines.Count; $i++) {
            if ($planLines[$i] -match '^##\s+Incidencias') { $inIncSection = $true }
            if ($inIncSection) {
                if ($planLines[$i] -match '^\|[-| ]+\|') { $incSepIdx = $i }
                if ($planLines[$i] -match '^\|\s*INC-\d+')  { $lastIncIdx = $i }
            }
        }

        $insertAfterIdx = if ($lastIncIdx -ge 0) { $lastIncIdx } elseif ($incSepIdx -ge 0) { $incSepIdx } else { -1 }

        if ($insertAfterIdx -ge 0) {
            # Determinar el siguiente numero de incidencia
            $incMatches = [regex]::Matches(($planLines -join "`n"), '\|\s*INC-(\d+)')
            $lastIncNum = 0
            foreach ($m in $incMatches) {
                $num = [int]$m.Groups[1].Value
                if ($num -gt $lastIncNum) { $lastIncNum = $num }
            }

            $today = (Get-Date).ToString("dd-MM-yyyy HH:mm")
            $insertLines = [System.Collections.Generic.List[string]]::new()
            foreach ($f in $failures) {
                $lastIncNum++
                $incId  = "INC-{0:D2}" -f $lastIncNum
                $desc   = "Test ``$($f.Id)`` ($($f.Description)) fallo: esperado HTTP $($f.ExpectedStatus), obtenido $($f.ActualStatus). Detectado automaticamente por ``run-tests.ps1``."
                $insertLines.Add("| $incId | $today | $desc | Abierta |")
            }

            $newLines = [System.Collections.Generic.List[string]]::new()
            for ($i = 0; $i -lt $planLines.Count; $i++) {
                $newLines.Add($planLines[$i])
                if ($i -eq $insertAfterIdx) {
                    foreach ($line in $insertLines) { $newLines.Add($line) }
                }
            }

            [System.IO.File]::WriteAllLines($planPath, $newLines, [System.Text.Encoding]::UTF8)
            Write-Host "$($failures.Count) incidencia(s) registrada(s) en PLAN_PRUEBAS.md" -ForegroundColor Yellow
        }
        else {
            Write-Host "No se encontro la tabla de incidencias en PLAN_PRUEBAS.md -- nada actualizado." -ForegroundColor DarkYellow
        }
    }
    else {
        Write-Host "PLAN_PRUEBAS.md no encontrado en $OutputDir -- nada actualizado." -ForegroundColor DarkYellow
    }
}

exit $(if ($failed -gt 0) { 1 } else { 0 })
