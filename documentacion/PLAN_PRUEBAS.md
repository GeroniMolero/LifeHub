# Plan de Pruebas — LifeHub

**Fecha:** 08-05-2026 (última actualización)  
**Versión del proyecto:** master (post-patrones de diseño, validación coherente, scripts robustos)  
**Entorno de pruebas:** Docker dev stack (lifehub-sql-dev + lifehub-backend-dev) + frontend local

---

## Entorno y condiciones previas

- Stack arrancado con `.\start.ps1 local` (Windows) o `./start.sh dev` (Linux)
- Contenedores `lifehub-sql-dev` y `lifehub-backend-dev` en estado **healthy**
- Frontend disponible en `http://localhost:4200`
- Backend disponible en `http://localhost:5000`
- Existe al menos un usuario admin creado por el seeder

---

## Casos de prueba

### CP-01 — Autenticación

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-01-01 | Registro de nuevo usuario | 1. Ir a /register 2. Rellenar formulario con datos válidos 3. Enviar | Redirige a /login. Usuario creado. | `{"success":true,"message":"Registro exitoso"}` | ✅ PASS |
| CP-01-02 | Registro con email duplicado | 1. Intentar registrar un email ya existente | Mensaje de error indicando email en uso | `{"success":false,"message":"El usuario ya existe."}` | ✅ PASS |
| CP-01-03 | Login correcto | 1. Ir a /login 2. Introducir credenciales válidas | Redirige a /home. Token JWT almacenado en localStorage. | `{"success":true,"message":"Login exitoso","token":"eyJ..."}` | ✅ PASS |
| CP-01-04 | Login con contraseña incorrecta | 1. Introducir email válido y contraseña errónea | Mensaje de error. No se accede a la app. | `{"success":false,"message":"Email o contraseña incorrectos"}` | ✅ PASS |
| CP-01-05 | Acceso a ruta protegida sin sesión | 1. Sin estar logueado, navegar a /spaces | Redirige a /login | API devuelve HTTP 401 sin token | ✅ PASS |
| CP-01-06 | Cierre de sesión | 1. Estando logueado, pulsar logout | Redirige a /login. Token eliminado de localStorage. | Verificado en frontend (token eliminado de localStorage) | ✅ PASS |
| CP-01-07 | Login con cuenta pendiente de activación | 1. Registrar nuevo usuario 2. Intentar login inmediatamente sin que el admin active la cuenta | HTTP 401. Mensaje: "Esta cuenta no está activa." | `{"success":false,"message":"Esta cuenta no está activa."}` | ✅ PASS |

---

### CP-02 — Espacios creativos

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-02-01 | Crear espacio privado | 1. Ir a Espacios 2. Crear nuevo espacio con privacidad "privado" | Espacio aparece en la lista. Solo visible para el creador. | API devuelve espacio con `privacy:0`, HTTP 201 | ✅ PASS |
| CP-02-02 | Crear espacio compartido | 1. Crear espacio con privacidad "compartido" | Espacio aparece en la lista. Visible para usuarios con permiso. | Verificado en frontend | ✅ PASS |
| CP-02-03 | Editar espacio | 1. Abrir un espacio existente 2. Editar nombre o descripción 3. Guardar | Cambios persistidos correctamente | HTTP 200 con datos actualizados | ✅ PASS |
| CP-02-04 | Eliminar espacio | 1. Eliminar un espacio existente 2. Confirmar | Espacio desaparece de la lista | HTTP 204 No Content | ✅ PASS |
| CP-02-05 | Compartir espacio con amigo | 1. Abrir espacio 2. Añadir amigo como colaborador | El amigo puede ver el espacio en su cuenta | Verificado en frontend | ✅ PASS |
| CP-02-06 | Control de permisos viewer | 1. Acceder a espacio compartido como viewer | Puede ver el contenido pero no editarlo | Verificado en frontend | ✅ PASS |

---

### CP-03 — Documentos

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-03-01 | Crear documento | 1. Dentro de un espacio, crear nuevo documento | Documento aparece en la lista | HTTP 201, documento con id creado | ✅ PASS |
| CP-03-02 | Editar con Markdown | 1. Abrir documento 2. Escribir Markdown (negrita, listas, encabezados) | La previsualización renderiza el Markdown correctamente | HTTP 200, contenido Markdown almacenado y renderizado en frontend | ✅ PASS |
| CP-03-03 | Sanitización XSS | 1. Introducir `<script>alert('xss')</script>` en el editor | El script no se ejecuta. Se muestra como texto o se elimina. | Backend elimina el payload XSS antes de persistir (`SanitizeHtml()` en `DocumentsController`). Frontend también sanitiza con `DomSanitizer` antes de renderizar. Protección en doble capa. | ✅ PASS |
| CP-03-04 | Descargar documento | 1. Desde la lista de documentos, pulsar descargar | Se descarga el archivo correctamente | Verificado en frontend | ✅ PASS |
| CP-03-05 | Versionado — crear snapshot | 1. Guardar documento 2. Crear snapshot vía API | Versión almacenada con número de versión | `{"id":1,"versionNumber":1,...}` devuelto por API | ✅ PASS |
| CP-03-06 | Restaurar versión anterior | 1. Editar documento 2. Restaurar snapshot anterior | El contenido vuelve al estado de esa versión | Contenido restaurado verificado en GET posterior | ✅ PASS |
| CP-03-07 | Eliminar documento | 1. Eliminar un documento con confirmación | Documento desaparece de la lista | HTTP 204 No Content | ✅ PASS |
| CP-03-08 | Modo split del editor | 1. Abrir documento 2. Seleccionar pestaña Split | Código y preview se muestran lado a lado | `activeTab: 'split'` implementado en document-detail y space-editor-main. Tab bar con tres opciones, grid 1fr/1fr. | ✅ PASS |

---

### CP-04 — Sistema de amigos

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-04-01 | Buscar usuario | 1. Ir a la sección de amigos 2. Buscar por nombre o email | El usuario aparece en los resultados | Verificado en frontend | ✅ PASS |
| CP-04-02 | Enviar solicitud de amistad | 1. Enviar solicitud a un usuario encontrado | Solicitud queda en estado pendiente | Verificado en frontend | ✅ PASS |
| CP-04-03 | Aceptar solicitud | 1. Desde la otra cuenta, aceptar la solicitud | Ambos usuarios aparecen como amigos | Verificado en frontend | ✅ PASS |
| CP-04-04 | Rechazar solicitud | 1. Rechazar una solicitud entrante | La solicitud desaparece. No se añade el amigo. | Verificado en frontend | ✅ PASS |
| CP-04-05 | Badge de mensajes no leídos | 1. Recibir mensajes de un amigo sin abrir el chat 2. Ir a /social | Cada amigo muestra un contador con los mensajes no leídos | `unreadPerFriend`, `hasUnread()` y `unreadCountForFriend()` implementados. Contador se actualiza en tiempo real vía SignalR y se resetea al abrir el chat. | ✅ PASS |

---

### CP-05 — Perfil

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-05-01 | Actualizar perfil | 1. Ir a perfil 2. Cambiar nombre, descripción o imagen 3. Guardar | Cambios reflejados inmediatamente | Verificado en frontend | ✅ PASS |
| CP-05-02 | Perfil público | 1. Acceder al perfil público de otro usuario | Se muestra nombre, bio, imagen y grid de documentos/espacios visibles | Grid de dos columnas implementado en public-user. Documentos y espacios marcados como visibles se muestran correctamente. | ✅ PASS |
| CP-05-03 | Marcar contenido visible en perfil | 1. Desde el perfil, activar toggle de visibilidad en hasta 3 documentos y 3 espacios | El contenido aparece en el grid del perfil público | `toggleDocumentProfileVisibility()` e `isProfileVisible` implementados. Badge de contador se deshabilita al alcanzar el límite configurado. | ✅ PASS |
| CP-05-04 | Modal de previsualización de documento | 1. Desde el perfil público de un usuario, pulsar sobre un documento | Se abre modal con markdown renderizado y botones Ver y Descargar | `openDocPreview()` y `closeDocPreview()` implementados. Markdown renderizado con `marked` y sanitizado con `DomSanitizer`. | ✅ PASS |

---

---

### CP-10 — Publicaciones de documentos

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-10-01 | Publicar documento | 1. Abrir documento 2. Abrir modal de publicación 3. Confirmar | Documento queda publicado y accesible en vista pública | Modal `showPublicationModal` implementado. `savePublicationData()` llama a `upsertPublication()` con `isPublic: true`. Mensaje de confirmación: "Documento publicado." | ✅ PASS |
| CP-10-02 | Autor visible en vista pública | 1. Acceder a la vista pública de un documento publicado | Se muestra el nombre del autor junto al contenido | Campo `Author` presente en `DocumentPublicationDto` y en los DTOs de respuesta pública. | ✅ PASS |
| CP-10-03 | Publicar documento ajeno (control de acceso) | 1. Intentar publicar un documento que no es propio vía API | HTTP 403 Forbidden | Cubierto por suite automatizada T-PUB (documento ajeno → 403). 46/46 OK. | ✅ PASS |
| CP-10-04 | Despublicar documento | 1. Despublicar un documento publicado | Documento desaparece de la vista pública | `savePublicationData()` con `isPublic: false`. Mensaje: "Acceso desactivado." Verificado en código y suite T-PUB. | ✅ PASS |

---

### CP-06 — Panel de administración

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-06-01 | Acceso admin con rol admin | 1. Login con cuenta admin 2. Navegar a /admin | Panel accesible | HTTP 200 con token de rol Admin | ✅ PASS |
| CP-06-02 | Acceso admin sin rol admin | 1. Login con cuenta normal 2. Navegar a /admin | Acceso denegado o redirige | HTTP 403 Forbidden con token de rol User | ✅ PASS |
| CP-06-03 | Añadir dominio permitido | 1. En panel admin, añadir un dominio a la allowlist | Dominio aparece en la lista y los embeds de ese dominio funcionan | `{"id":6,"domain":"testsite.com","isActive":true}` | ✅ PASS |
| CP-06-04 | Desactivar dominio | 1. Desactivar un dominio de la allowlist | Los embeds de ese dominio dejan de cargarse | HTTP 200, `isActive` actualizado a false | ✅ PASS |
| CP-06-05 | Ver listado de usuarios | 1. Ir a la sección de usuarios del panel admin | Lista de usuarios del sistema visible | 5 usuarios devueltos correctamente | ✅ PASS |
| CP-06-06 | Nuevo usuario queda inactivo hasta activación | 1. Registrar nuevo usuario 2. Verificar en panel admin que aparece como "Inactivo" | Usuario visible en lista con estado Inactivo | `isActive: false` en respuesta de `GET /admin/users` | ✅ PASS |
| CP-06-07 | Activar usuario e iniciar sesión | 1. Activar usuario desde panel admin 2. El usuario intenta login | Login exitoso tras activación | Toggle devuelve `isActive: true`. Login retorna token. | ✅ PASS |
| CP-06-08 | Desactivar usuario bloquea login | 1. Desactivar usuario activo desde panel admin 2. El usuario intenta login | HTTP 401 con mensaje "Esta cuenta no está activa." | `{"success":false,"message":"Esta cuenta no está activa."}` | ✅ PASS |
| CP-06-09 | Admin edita email de usuario | 1. Editar email de un usuario desde modal admin 2. Usuario hace login con el nuevo email | Login con nuevo email funciona. Email anterior deja de funcionar. | `PUT /admin/users/{id}` devuelve usuario actualizado. Login con nuevo email OK. | ✅ PASS |
| CP-06-10 | Admin resetea contraseña | 1. Desde modal admin, establecer nueva contraseña a un usuario 2. Usuario hace login con la nueva contraseña | Login con nueva contraseña funciona | `POST /admin/users/{id}/set-password` devuelve 204. Login con nueva clave OK. | ✅ PASS |
| CP-06-11 | Admin cambia rol de usuario | 1. Desde modal admin, cambiar rol a Admin 2. Usuario hace login de nuevo | El usuario obtiene el rol Admin en el nuevo token | `PUT /admin/users/{id}/roles` devuelve usuario con rol actualizado. | ✅ PASS |
| CP-06-12 | Ver logs de actividad con filtros | 1. Ir a pestaña Actividad 2. Filtrar por tipo de entidad "Document" y rango de fechas | Se muestran solo los logs del tipo y rango seleccionados con paginación | `GET /admin/activity-logs?entityType=Document` devuelve `{"items":[...],"totalCount":N}` | ✅ PASS |
| CP-06-13 | Lanzar backup desde panel admin | 1. Ir a pestaña Sistema 2. Pulsar "Lanzar backup" | Mensaje de éxito con ruta del archivo .bak generado | `{"message":"Backup completado correctamente.","backupFile":"/var/opt/mssql/backup/LifeHubDB_...bak"}` | ✅ PASS |

---

### CP-07 — Recursos embebidos

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-07-01 | Embeber recurso de dominio permitido | 1. En un espacio, añadir URL de YouTube o Spotify | El recurso se carga correctamente | Verificado en frontend con dominio en allowlist | ✅ PASS |
| CP-07-02 | Embeber recurso de dominio no permitido | 1. Añadir URL de un dominio no en la allowlist | El recurso no se carga. Mensaje de error o bloqueo. | Verificado en frontend — embed bloqueado para dominio no registrado | ✅ PASS |

---

### CP-08 — Seguridad de sesión

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-08-01 | Expiración de token JWT | 1. Modificar `ExpiresInMinutes` a 1 en appsettings.json 2. Login 3. Esperar 1 minuto 4. Hacer cualquier petición | La app redirige automáticamente a /login | Token inválido devuelve HTTP 401. Interceptor del frontend captura 401 y redirige a /login automáticamente. | ✅ PASS |

---

### CP-09 — Backup y restauración

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-09-01 | Crear backup con stack activo | 1. Con stack en marcha, ejecutar `.\scripts\windows\backup-db.ps1` | Archivo `.bak` generado en `backups/` con timestamp | Archivo `LifeHub_20260423_181124.bak` generado (738 páginas, 0.063s) | ✅ PASS |
| CP-09-02 | Restaurar base de datos desde backup | 1. Ejecutar `.\scripts\windows\restore-db.ps1 -BackupFile <ruta>` | BD restaurada. Datos accesibles tras reinicio del backend. | 738 páginas restauradas correctamente (0.036s) | ✅ PASS |
| CP-09-03 | Backup genera nombre único por ejecución | 1. Ejecutar `.\scripts\windows\backup-db.ps1` dos veces seguidas | Dos archivos `.bak` con timestamps distintos en `backups/` | `LifeHub_20260423_181124.bak` y `LifeHub_20260423_182959.bak` | ✅ PASS |

---

## Resumen de resultados

### Pruebas manuales (09-05-2026)

| Módulo | Total casos | Pasados | Fallidos | Pendientes |
|--------|-------------|---------|----------|------------|
| CP-01 Autenticación | 7 | 7 | 0 | 0 |
| CP-02 Espacios creativos | 6 | 6 | 0 | 0 |
| CP-03 Documentos | 8 | 8 | 0 | 0 |
| CP-04 Amigos | 5 | 5 | 0 | 0 |
| CP-05 Perfil | 4 | 4 | 0 | 0 |
| CP-06 Panel admin | 13 | 13 | 0 | 0 |
| CP-07 Embebidos | 2 | 2 | 0 | 0 |
| CP-08 Seguridad sesión | 1 | 1 | 0 | 0 |
| CP-09 Backup y restauración | 3 | 3 | 0 | 0 |
| CP-10 Publicaciones | 4 | 4 | 0 | 0 |
| **TOTAL** | **53** | **53** | **0** | **0** |

### Pruebas automatizadas — suite `run-tests.ps1` (09-05-2026)

| Bloque | Tests | OK | FAIL | SKIP |
|--------|-------|----|------|------|
| AUTH | 9 | 9 | 0 | 0 |
| Espacios creativos | 5 | 5 | 0 | 0 |
| Documentos y versiones | 9 | 9 | 0 | 0 |
| Colaboración en espacios | 3 | 3 | 0 | 0 |
| Publicaciones | 11 | 11 | 0 | 0 |
| Panel de administración | 19 | 19 | 0 | 0 |
| Seguridad | 4 | 4 | 0 | 0 |
| **TOTAL** | **60** | **60** | **0** | **0** |

### Tests unitarios frontend — Jasmine (09-05-2026)

| Archivo | Tests | OK |
|---------|-------|----|
| `auth.service.spec.ts` | 17 | 17 |
| `admin.service.spec.ts` | 13 | 13 |
| `config.service.spec.ts` | 3 | 3 |
| `space-workspace.component.spec.ts` | 5 | 5 |
| **TOTAL** | **38** | **38** |

---

## Incidencias registradas

| ID | Fecha | Descripción | Estado |
|----|-------|-------------|--------|
| INC-01 | 23-04-2026 | La ruta de versiones de documentos es `/api/documentversions/document/{id}` en lugar del patrón REST esperado `/api/documents/{id}/versions`. El endpoint funciona correctamente pero el naming es inconsistente con el resto de la API. No tiene impacto funcional. | Abierta — pendiente de valorar refactorización |
| INC-02 | 23-04-2026 | Validación de entrada ausente en dos endpoints de creación de recursos: aceptaban campos obligatorios vacíos sin devolver error. Detectado durante la ejecución de la suite de pruebas automatizada. Corregido añadiendo validación a nivel de DTO. | Resuelta |
| INC-03 | 02-05-2026 20:24 | Test `T-DOC-07` (Snapshot de documento ajeno -> 403) fallaba: esperado HTTP 403, obtenido 404. El script usaba el ID hardcodeado `1` asumiendo que existía y pertenecía a otro usuario; el backend devuelve 404 antes de comprobar permisos si el documento no existe. Corregido: el test ahora crea un documento temporal con el token admin, intenta el snapshot con el token de usuario (obtiene 403 correctamente) y lo elimina al finalizar. Resultado tras el fix: **30/30 PASS**. | Resuelta |
| INC-04 | 02-05-2026 21:25 | Test `T-AUTH-08` (Login admin (setup para tests admin)) fallaba: esperado HTTP 200, obtenido 400 en `run-tests.sh` bajo Git Bash/Windows. Causa raíz: parseo de `.env` en scripts Bash no eliminaba `CRLF` (`\r`) y el valor de `ADMIN_PASSWORD` se enviaba con carácter extra. Corregido normalizando `ADMIN_EMAIL/ADMIN_PASSWORD` (trim + eliminación de `\r`) en los runners Linux. Resultado tras el fix: **30/30 PASS**. | Resuelta |
| INC-05 | 02-05-2026 | Vulnerabilidad XSS en backend: los endpoints de creación y actualización de documentos almacenaban el contenido sin sanitizar. Un atacante con acceso directo a la API podía persistir payloads maliciosos independientemente de la validación del frontend. Corregido implementando `SanitizeHtml()` en `DocumentsController.cs` (elimina bloques `<script>`, atributos `on\w+=` y URIs `javascript:`). Test `T-DOC-04` corregido en los cuatro runners: ahora verifica que el payload XSS es eliminado por el backend (usa `NotContains` en lugar de `Contains`). | Resuelta |
| INC-06 | 02-05-2026 | Inyección JSON en `run-tests-interactive.sh`: el script construía los cuerpos JSON interpolando directamente los valores del usuario sin escapar, por lo que un valor con `"` cerraba prematuramente la cadena y alteraba la estructura de la petición. Corregido implementando `json_escape()` que escapa `\`, `"`, tabuladores y saltos de línea antes de la interpolación. | Resuelta |
| INC-07 | 04-05-2026 | Tests T-COL fallaban durante la limpieza: la restricción de FK impedía eliminar el espacio temporal si el documento creado en el test no se borraba antes. Corregido añadiendo la eliminación explícita del documento antes de la del espacio en el flujo de teardown de los scripts. Resultado: **33/33 PASS**. | Resuelta |
| INC-08 | 06-05-2026 | Backend inaccesible al arrancar: la migración `AddColumnLengthConstraints` crasheaba en cada inicio con el error "Column name 'IsPublic' in table 'Documents' is specified more than once". Causa raíz: la migración había aplicado parcialmente `AddColumn IsPublic/PublishedAt` y creado las tablas `AllowedWebsites` y `DocumentPublications` en una ejecución anterior, pero la transacción no quedó registrada en `__EFMigrationsHistory` (Azure SQL Edge no revirtió el DDL en la excepción). Todos los tests fallaban con HTTP 0 (connection refused). Corregido haciendo idempotentes las cuatro operaciones problemáticas mediante SQL condicional (`IF COL_LENGTH IS NULL` / `IF NOT EXISTS`). Resultado: migración aplicada correctamente, **33/33 PASS**. | Resuelta |
| INC-09 | 12-05-2026 10:04 | Test `T-SEC-04` (Cabecera Server no revela tecnologia) fallo: esperado HTTP 200, obtenido 200. Detectado automaticamente por `run-tests.ps1`. | Abierta |
