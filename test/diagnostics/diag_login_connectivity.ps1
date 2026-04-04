param(
  [string]$BaseUrl = 'http://localhost:5000',
  [string]$FrontendOrigin = 'http://localhost:4200',
  [string]$Email = 'admin@lifehub.com',
  [string]$Password = 'Admin123!',
  [string]$BackendContainer = 'lifehub-backend-dev'
)

$ErrorActionPreference = 'Stop'

function Write-Step {
  param([string]$Message)
  Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Write-Ok {
  param([string]$Message)
  Write-Host "[OK] $Message" -ForegroundColor Green
}

function Write-Warn {
  param([string]$Message)
  Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Write-Fail {
  param([string]$Message)
  Write-Host "[FAIL] $Message" -ForegroundColor Red
}

$global:HasError = $false

Write-Step "Comprobando contenedor backend en Docker"
$dockerCheck = (Get-Command docker -ErrorAction SilentlyContinue)
if (-not $dockerCheck) {
  Write-Warn 'Docker no esta disponible en PATH. Se omite validacion de contenedor.'
} else {
  try {
    $containerStatus = docker ps --filter "name=$BackendContainer" --format "{{.Names}}|{{.Status}}|{{.Ports}}"
    if ([string]::IsNullOrWhiteSpace($containerStatus)) {
      Write-Warn "No aparece un contenedor activo llamado '$BackendContainer'."
    } else {
      Write-Ok "Contenedor detectado: $containerStatus"
    }
  } catch {
    Write-Warn "No se pudo consultar Docker: $($_.Exception.Message)"
  }
}

Write-Step "Comprobando endpoint Swagger del backend"
$swaggerUrl = "$BaseUrl/swagger/index.html"
try {
  $swagger = Invoke-WebRequest -Method Get -Uri $swaggerUrl -TimeoutSec 8 -ErrorAction Stop
  $serverHeader = $swagger.Headers['Server']
  Write-Ok "Swagger responde con HTTP $([int]$swagger.StatusCode). Server='$serverHeader'"
  if ($serverHeader -ne 'Kestrel') {
    Write-Warn 'El servidor no parece Kestrel. Revisa si otro proceso responde en el mismo puerto.'
  }
} catch {
  $global:HasError = $true
  Write-Fail "No responde Swagger en $swaggerUrl"
  Write-Host "Sugerencia: docker logs -f $BackendContainer" -ForegroundColor DarkYellow
}

Write-Step "Comprobando CORS preflight para login"
$preflightHeaders = @{
  Origin = $FrontendOrigin
  'Access-Control-Request-Method' = 'POST'
  'Access-Control-Request-Headers' = 'content-type,authorization'
}

try {
  $preflight = Invoke-WebRequest -Method Options -Uri "$BaseUrl/api/auth/login" -Headers $preflightHeaders -TimeoutSec 8 -ErrorAction Stop
  $allowOrigin = $preflight.Headers['Access-Control-Allow-Origin']
  $allowMethods = $preflight.Headers['Access-Control-Allow-Methods']
  Write-Ok "Preflight HTTP $([int]$preflight.StatusCode), Allow-Origin='$allowOrigin', Allow-Methods='$allowMethods'"
  if ($allowOrigin -ne $FrontendOrigin) {
    Write-Warn 'Allow-Origin no coincide con el origin del frontend.'
  }
} catch {
  $global:HasError = $true
  Write-Fail 'Fallo el preflight CORS de /api/auth/login'
}

Write-Step "Comprobando login real"
$loginBody = @{
  email = $Email
  password = $Password
} | ConvertTo-Json -Compress

try {
  $login = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/login" -ContentType 'application/json' -Body $loginBody -TimeoutSec 10 -ErrorAction Stop

  if ($login.success -eq $true -and -not [string]::IsNullOrWhiteSpace($login.token)) {
    Write-Ok "Login correcto para '$Email'"
  } else {
    $global:HasError = $true
    Write-Fail "La API respondio pero sin token valido para '$Email'"
  }
} catch {
  $global:HasError = $true

  if ($_.Exception.Response) {
    $resp = $_.Exception.Response
    $status = [int]$resp.StatusCode
    $bodyText = ''
    try {
      $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
      $bodyText = $reader.ReadToEnd()
    } catch {
      $bodyText = '<sin cuerpo>'
    }

    Write-Fail "Login fallo con HTTP $status"
    Write-Host "Respuesta: $bodyText" -ForegroundColor DarkYellow

    if ($status -eq 401) {
      Write-Warn 'Probable causa: credenciales incorrectas o usuario inexistente.'
    } elseif ($status -eq 415) {
      Write-Warn 'Probable causa: Content-Type incorrecto (debe ser application/json).'
    } elseif ($status -ge 500) {
      Write-Warn 'Probable causa: excepcion del backend. Revisa docker logs.'
    }
  } else {
    Write-Fail "Error de red o disponibilidad: $($_.Exception.Message)"
  }
}

Write-Step 'Resumen'
if ($global:HasError) {
  Write-Fail 'Diagnostico final: hay al menos un fallo. Revisa los avisos anteriores.'
  Write-Host "Comando util: docker logs --tail 200 $BackendContainer" -ForegroundColor DarkYellow
  exit 1
} else {
  Write-Ok 'Diagnostico final: backend, CORS y login operativos.'
  exit 0
}
