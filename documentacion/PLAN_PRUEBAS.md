# Plan de Pruebas — LifeHub

**Fecha:** 23-04-2026  
**Versión del proyecto:** master (post-limpieza de seguridad)  
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
| CP-03-03 | Sanitización XSS | 1. Introducir `<script>alert('xss')</script>` en el editor | El script no se ejecuta. Se muestra como texto o se elimina. | Backend almacena el contenido raw. Frontend sanitiza con DOMPurify antes de renderizar — script no se ejecuta. | ✅ PASS |
| CP-03-04 | Descargar documento | 1. Desde la lista de documentos, pulsar descargar | Se descarga el archivo correctamente | Verificado en frontend | ✅ PASS |
| CP-03-05 | Versionado — crear snapshot | 1. Guardar documento 2. Crear snapshot vía API | Versión almacenada con número de versión | `{"id":1,"versionNumber":1,...}` devuelto por API | ✅ PASS |
| CP-03-06 | Restaurar versión anterior | 1. Editar documento 2. Restaurar snapshot anterior | El contenido vuelve al estado de esa versión | Contenido restaurado verificado en GET posterior | ✅ PASS |
| CP-03-07 | Eliminar documento | 1. Eliminar un documento con confirmación | Documento desaparece de la lista | HTTP 204 No Content | ✅ PASS |

---

### CP-04 — Sistema de amigos

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-04-01 | Buscar usuario | 1. Ir a la sección de amigos 2. Buscar por nombre o email | El usuario aparece en los resultados | Verificado en frontend | ✅ PASS |
| CP-04-02 | Enviar solicitud de amistad | 1. Enviar solicitud a un usuario encontrado | Solicitud queda en estado pendiente | Verificado en frontend | ✅ PASS |
| CP-04-03 | Aceptar solicitud | 1. Desde la otra cuenta, aceptar la solicitud | Ambos usuarios aparecen como amigos | Verificado en frontend | ✅ PASS |
| CP-04-04 | Rechazar solicitud | 1. Rechazar una solicitud entrante | La solicitud desaparece. No se añade el amigo. | Verificado en frontend | ✅ PASS |

---

### CP-05 — Perfil

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-05-01 | Actualizar perfil | 1. Ir a perfil 2. Cambiar nombre, descripción o imagen 3. Guardar | Cambios reflejados inmediatamente | Verificado en frontend | ✅ PASS |
| CP-05-02 | Perfil público | 1. Acceder al perfil público de otro usuario | Se muestra nombre, bio, imagen y espacios favoritos | Verificado en frontend | ✅ PASS |
| CP-05-03 | Añadir espacio favorito | 1. Marcar un espacio como favorito desde el perfil | El espacio aparece en la previsualización del perfil | Verificado en frontend | ✅ PASS |

---

### CP-06 — Panel de administración

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-06-01 | Acceso admin con rol admin | 1. Login con cuenta admin 2. Navegar a /admin | Panel accesible | HTTP 200 con token de rol Admin | ✅ PASS |
| CP-06-02 | Acceso admin sin rol admin | 1. Login con cuenta normal 2. Navegar a /admin | Acceso denegado o redirige | HTTP 403 Forbidden con token de rol User | ✅ PASS |
| CP-06-03 | Añadir dominio permitido | 1. En panel admin, añadir un dominio a la allowlist | Dominio aparece en la lista y los embeds de ese dominio funcionan | `{"id":6,"domain":"testsite.com","isActive":true}` | ✅ PASS |
| CP-06-04 | Desactivar dominio | 1. Desactivar un dominio de la allowlist | Los embeds de ese dominio dejan de cargarse | HTTP 200, `isActive` actualizado a false | ✅ PASS |
| CP-06-05 | Ver listado de usuarios | 1. Ir a la sección de usuarios del panel admin | Lista de usuarios del sistema visible | 5 usuarios devueltos correctamente | ✅ PASS |

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
| CP-09-01 | Crear backup con stack activo | 1. Con stack en marcha, ejecutar `.\backup-db.ps1` | Archivo `.bak` generado en `backups/` con timestamp | Archivo `LifeHub_20260423_181124.bak` generado (738 páginas, 0.063s) | ✅ PASS |
| CP-09-02 | Restaurar base de datos desde backup | 1. Ejecutar `.\restore-db.ps1 -BackupFile <ruta>` | BD restaurada. Datos accesibles tras reinicio del backend. | 738 páginas restauradas correctamente (0.036s) | ✅ PASS |
| CP-09-03 | Backup genera nombre único por ejecución | 1. Ejecutar `.\backup-db.ps1` dos veces seguidas | Dos archivos `.bak` con timestamps distintos en `backups/` | `LifeHub_20260423_181124.bak` y `LifeHub_20260423_182959.bak` | ✅ PASS |

---

## Resumen de resultados

| Módulo | Total casos | Pasados | Fallidos | Pendientes |
|--------|-------------|---------|----------|------------|
| CP-01 Autenticación | 6 | 6 | 0 | 0 |
| CP-02 Espacios creativos | 6 | 6 | 0 | 0 |
| CP-03 Documentos | 7 | 7 | 0 | 0 |
| CP-04 Amigos | 4 | 4 | 0 | 0 |
| CP-05 Perfil | 3 | 3 | 0 | 0 |
| CP-06 Panel admin | 5 | 5 | 0 | 0 |
| CP-07 Embebidos | 2 | 2 | 0 | 0 |
| CP-08 Seguridad sesión | 1 | 1 | 0 | 0 |
| CP-09 Backup y restauración | 3 | 3 | 0 | 0 |
| **TOTAL** | **37** | **37** | **0** | **0** |

---

## Incidencias registradas

| ID | Fecha | Descripción | Estado |
|----|-------|-------------|--------|
| — | — | Sin incidencias registradas hasta el momento | — |
