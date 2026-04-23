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
| CP-01-01 | Registro de nuevo usuario | 1. Ir a /register 2. Rellenar formulario con datos válidos 3. Enviar | Redirige a /login. Usuario creado. | | |
| CP-01-02 | Registro con email duplicado | 1. Intentar registrar un email ya existente | Mensaje de error indicando email en uso | | |
| CP-01-03 | Login correcto | 1. Ir a /login 2. Introducir credenciales válidas | Redirige a /home. Token JWT almacenado en localStorage. | | |
| CP-01-04 | Login con contraseña incorrecta | 1. Introducir email válido y contraseña errónea | Mensaje de error. No se accede a la app. | | |
| CP-01-05 | Acceso a ruta protegida sin sesión | 1. Sin estar logueado, navegar a /spaces | Redirige a /login | | |
| CP-01-06 | Cierre de sesión | 1. Estando logueado, pulsar logout | Redirige a /login. Token eliminado de localStorage. | | |

---

### CP-02 — Espacios creativos

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-02-01 | Crear espacio privado | 1. Ir a Espacios 2. Crear nuevo espacio con privacidad "privado" | Espacio aparece en la lista. Solo visible para el creador. | | |
| CP-02-02 | Crear espacio compartido | 1. Crear espacio con privacidad "compartido" | Espacio aparece en la lista. Visible para usuarios con permiso. | | |
| CP-02-03 | Editar espacio | 1. Abrir un espacio existente 2. Editar nombre o descripción 3. Guardar | Cambios persistidos correctamente | | |
| CP-02-04 | Eliminar espacio | 1. Eliminar un espacio existente 2. Confirmar | Espacio desaparece de la lista | | |
| CP-02-05 | Compartir espacio con amigo | 1. Abrir espacio 2. Añadir amigo como colaborador | El amigo puede ver el espacio en su cuenta | | |
| CP-02-06 | Control de permisos viewer | 1. Acceder a espacio compartido como viewer | Puede ver el contenido pero no editarlo | | |

---

### CP-03 — Documentos

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-03-01 | Crear documento | 1. Dentro de un espacio, crear nuevo documento | Documento aparece en la lista | | |
| CP-03-02 | Editar con Markdown | 1. Abrir documento 2. Escribir Markdown (negrita, listas, encabezados) | La previsualización renderiza el Markdown correctamente | | |
| CP-03-03 | Sanitización XSS | 1. Introducir `<script>alert('xss')</script>` en el editor | El script no se ejecuta. Se muestra como texto o se elimina. | | |
| CP-03-04 | Descargar documento | 1. Desde la lista de documentos, pulsar descargar | Se descarga el archivo correctamente | | |
| CP-03-05 | Versionado automático | 1. Editar y guardar un documento varias veces | Se generan versiones en el historial | | |
| CP-03-06 | Restaurar versión anterior | 1. Abrir historial de versiones 2. Restaurar una versión previa | El contenido del documento vuelve al estado de esa versión | | |
| CP-03-07 | Eliminar documento | 1. Eliminar un documento con confirmación | Documento desaparece de la lista | | |

---

### CP-04 — Sistema de amigos

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-04-01 | Buscar usuario | 1. Ir a la sección de amigos 2. Buscar por nombre o email | El usuario aparece en los resultados | | |
| CP-04-02 | Enviar solicitud de amistad | 1. Enviar solicitud a un usuario encontrado | Solicitud queda en estado pendiente | | |
| CP-04-03 | Aceptar solicitud | 1. Desde la otra cuenta, aceptar la solicitud | Ambos usuarios aparecen como amigos | | |
| CP-04-04 | Rechazar solicitud | 1. Rechazar una solicitud entrante | La solicitud desaparece. No se añade el amigo. | | |

---

### CP-05 — Perfil

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-05-01 | Actualizar perfil | 1. Ir a perfil 2. Cambiar nombre, descripción o imagen 3. Guardar | Cambios reflejados inmediatamente | | |
| CP-05-02 | Perfil público | 1. Acceder al perfil público de otro usuario | Se muestra nombre, bio, imagen y espacios favoritos | | |
| CP-05-03 | Añadir espacio favorito | 1. Marcar un espacio como favorito desde el perfil | El espacio aparece en la previsualización del perfil | | |

---

### CP-06 — Panel de administración

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-06-01 | Acceso admin con rol admin | 1. Login con cuenta admin 2. Navegar a /admin | Panel accesible | | |
| CP-06-02 | Acceso admin sin rol admin | 1. Login con cuenta normal 2. Navegar a /admin | Acceso denegado o redirige | | |
| CP-06-03 | Añadir dominio permitido | 1. En panel admin, añadir un dominio a la allowlist | Dominio aparece en la lista y los embeds de ese dominio funcionan | | |
| CP-06-04 | Desactivar dominio | 1. Desactivar un dominio de la allowlist | Los embeds de ese dominio dejan de cargarse | | |
| CP-06-05 | Ver listado de usuarios | 1. Ir a la sección de usuarios del panel admin | Lista de usuarios del sistema visible | | |

---

### CP-07 — Recursos embebidos

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-07-01 | Embeber recurso de dominio permitido | 1. En un espacio, añadir URL de YouTube o Spotify | El recurso se carga correctamente | | |
| CP-07-02 | Embeber recurso de dominio no permitido | 1. Añadir URL de un dominio no en la allowlist | El recurso no se carga. Mensaje de error o bloqueo. | | |

---

### CP-08 — Seguridad de sesión

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-08-01 | Expiración de token JWT | 1. Modificar `ExpiresInMinutes` a 1 en appsettings.json 2. Login 3. Esperar 1 minuto 4. Hacer cualquier petición | La app redirige automáticamente a /login | | |

---

### CP-09 — Backup y restauración

| ID | Descripción | Pasos | Resultado esperado | Resultado real | Estado |
|----|-------------|-------|--------------------|----------------|--------|
| CP-09-01 | Crear backup con stack activo | 1. Con stack en marcha, ejecutar `.\backup-db.ps1` | Archivo `.bak` generado en `backups/` con timestamp | Archivo `LifeHub_20260423_181124.bak` generado (738 páginas, 0.063s) | ✅ PASS |
| CP-09-02 | Restaurar base de datos desde backup | 1. Ejecutar `.\restore-db.ps1 -BackupFile <ruta>` | BD restaurada. Datos accesibles tras reinicio del backend. | 738 páginas restauradas correctamente (0.036s) | ✅ PASS |
| CP-09-03 | Backup genera nombre único por ejecución | 1. Ejecutar `.\backup-db.ps1` dos veces seguidas | Dos archivos `.bak` con timestamps distintos en `backups/` | | |

---

## Resumen de resultados

| Módulo | Total casos | Pasados | Fallidos | Pendientes |
|--------|-------------|---------|----------|------------|
| CP-01 Autenticación | 6 | | | |
| CP-02 Espacios creativos | 6 | | | |
| CP-03 Documentos | 7 | | | |
| CP-04 Amigos | 4 | | | |
| CP-05 Perfil | 3 | | | |
| CP-06 Panel admin | 5 | | | |
| CP-07 Embebidos | 2 | | | |
| CP-08 Seguridad sesión | 1 | | | |
| CP-09 Backup y restauración | 3 | 2 | 0 | 1 |
| **TOTAL** | **37** | **2** | **0** | **35** |

---

## Incidencias registradas

| ID | Fecha | Descripción | Estado |
|----|-------|-------------|--------|
| — | — | Sin incidencias registradas hasta el momento | — |
