$ErrorActionPreference = 'Stop'
$base = 'http://localhost:5000'

$adminLogin = Invoke-RestMethod -Method Post -Uri "$base/api/auth/login" -ContentType 'application/json' -Body '{"email":"admin@lifehub.com","password":"Admin123!"}'
$juanLogin = Invoke-RestMethod -Method Post -Uri "$base/api/auth/login" -ContentType 'application/json' -Body '{"email":"juan@lifehub.com","password":"Test123!"}'

$adminToken = $adminLogin.token
$juanToken = $juanLogin.token
$juanId = $juanLogin.user.id

$headersAdmin = @{ Authorization = "Bearer $adminToken" }
$headersJuan = @{ Authorization = "Bearer $juanToken" }

$stamp = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$spaceBody = @{
  name = "Diag Space $stamp"
  description = "diag"
  privacy = 0
  isPublicProfileVisible = $false
} | ConvertTo-Json -Compress
$space = Invoke-RestMethod -Method Post -Uri "$base/api/creativespaces" -Headers $headersAdmin -ContentType 'application/json' -Body $spaceBody
$spaceId = $space.id

$docBody = @{
  title = "Diag Doc $stamp"
  description = "diag"
  content = "base"
  type = 0
  creativeSpaceId = $spaceId
} | ConvertTo-Json -Compress
$doc = Invoke-RestMethod -Method Post -Uri "$base/api/documents" -Headers $headersAdmin -ContentType 'application/json' -Body $docBody
$docId = $doc.id

$shareBody = @{
  userId = $juanId
  permissionLevel = 0
} | ConvertTo-Json -Compress
Invoke-RestMethod -Method Post -Uri "$base/api/creativespaces/$spaceId/permissions" -Headers $headersAdmin -ContentType 'application/json' -Body $shareBody | Out-Null

"spaceId=$spaceId docId=$docId docCreativeSpaceId=$($doc.creativeSpaceId) juanId=$juanId"
$perms = Invoke-RestMethod -Method Get -Uri "$base/api/creativespaces/$spaceId/permissions" -Headers $headersAdmin
"permissions=" + ($perms | ConvertTo-Json -Compress)

try {
  $resp = Invoke-WebRequest -Method Get -Uri "$base/api/documentversions/document/$docId" -Headers $headersJuan -ErrorAction Stop
  "viewerStatus=$($resp.StatusCode)"
  $resp.Content
} catch {
  $status = $_.Exception.Response.StatusCode.value__
  $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
  $body = $reader.ReadToEnd()
  "viewerStatus=$status"
  $body
}
