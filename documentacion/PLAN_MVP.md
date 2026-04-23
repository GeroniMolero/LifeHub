# LifeHub — PLAN_MVP (Intermodular DAW 2025-26)

**Fecha:** 2026-03-20  
**Autor:** Gerónimo Molero Rodríguez (2ºDAW 2025-26)  
**Repositorio:** `Szungg/LifeHub`

## 0) Principios del proyecto (fiel al documento)
LifeHub es una aplicación web que proporciona un **espacio creativo personal** donde los usuarios pueden **escribir en Markdown**, **organizar ideas** y **compartir contenido de forma controlada**, reduciendo la fragmentación de herramientas.

### Stack (según documento)
- **Frontend:** Angular (UI en español)
- **Backend:** .NET Web API REST
- **Base de datos:** SQL Server (en este repo) *(alternativa aceptable: PostgreSQL)*
- **Auth:** JWT
- **Despliegue:** Docker + Docker Compose

### Convenciones de nomenclatura (decisiones cerradas)
- **Código (C# / TypeScript):** inglés
- **Interfaz (UI):** español
- **C#**: clases/props en `PascalCase`, privados en `_camelCase`
- **Angular**: archivos en `kebab-case`, símbolos en `PascalCase` (types) y `camelCase` (vars/func)
- **API**: rutas en inglés, plural, estilo REST  
  Ej: `/api/spaces/...`, `/api/public/...`, `/api/admin/...`

### Entidades del dominio (código)
- `CreativeSpace`
- `Document` (con `description` opcional + `DocumentType` fijo a Markdown en MVP)
- `SpacePermission` (Viewer/Editor)
- `DocumentVersion` (versionado manual)
- `DocumentPublication` (publicación pública + metadatos)
- `ActivityLog`

### Alcance MVP (funcional)
El MVP debe incluir:
- Registro / login / logout.
- Gestión de perfil (mínimo: nombre, bio, foto opcional).
- CRUD de **espacios creativos**.
- CRUD de **documentos Markdown** dentro de espacios.
- **Versionado manual** (crear/listar/restaurar versiones).
- **Privacidad** del espacio: `Private` / `Shared`.
- **Compartición dentro de la app** con usuarios de confianza (permisos Viewer/Editor).
- **Recursos externos embebidos legales**: se modelan como **metadatos de publicación** (sin hosting de ficheros).
- **Perfil público limitado**:
  - listado de documentos publicados
  - vista pública del documento (contenido completo) + metadatos
- **Panel de administración básico**.
- **Logs de actividad** para auditoría básica.
- Despliegue con `docker compose up`.

### Fuera del MVP (según documento)
Debe quedar claro (y oculto del flujo principal):
- Edición colaborativa en tiempo real.
- Sistema completo de mensajería/chat.
- Gestión avanzada de recursos multimedia.
- App móvil.

> Nota: El repositorio puede contener módulos experimentales (chat/amigos/recomendaciones/música),
> pero deben quedar ocultos del menú/routing principal y no formar parte del MVP evaluable.

---

# Planificación (8 semanas) — Roadmap real del repo

## FASE 0 — Alineación del repositorio (1 día)
**Objetivo:** que el proyecto “cuente” lo mismo que el documento.
- [ ] Actualizar README y docs para describir LifeHub como “espacio creativo personal”.
- [ ] Ocultar rutas/menú de módulos fuera de MVP (friends/chat/recommendations/music).
- [ ] Confirmar flujo principal: Login → Espacios → Documentos → Editor.

**Hecho cuando:** un evaluador navega y solo ve lo descrito en el documento.

---

## FASE 1 — CreativeSpaces (Semana 1-2)
### Backend
- [ ] Modelo `CreativeSpace`:
  - `Id`, `OwnerUserId`, `Name`, `Description`, `Privacy (Private/Shared)`, `CreatedAt`, `UpdatedAt`.
- [ ] `DbSet<CreativeSpace>` + migración.
- [ ] `SpacesController` (`/api/spaces`): CRUD.
- [ ] Seguridad: owner-only (Fase 1).

### Frontend
- [ ] Model `CreativeSpace` + `SpaceService`.
- [ ] Página **Espacios**: listar/crear/editar/borrar + “Abrir”.
- [ ] Routing: `/spaces` como landing post-login.

**Hecho cuando:** el usuario crea y gestiona espacios propios.

---

## FASE 2 — Documents dentro de Spaces + Editor Markdown (Semana 2-3)
### Backend
- [ ] `Document` pertenece a `CreativeSpace` con `CreativeSpaceId` FK.
- [ ] `Description` opcional.
- [ ] `DocumentType` existente y preparado para futuro, pero en MVP:
  - **Type fijo a `Markdown`** (selector no visible todavía).
- [ ] Endpoints anidados:
  - `/api/spaces/{spaceId}/documents` CRUD.
- [ ] Autorización (por ahora): owner-only.

### Frontend
- [ ] Rutas:
  - `/spaces/:spaceId/documents`
  - `/spaces/:spaceId/documents/:documentId`
- [ ] Lista de documentos por espacio.
- [ ] Editor Markdown:
  - textarea + preview
  - guardar
  - `type` fijo a Markdown (sin selector visible).

**Hecho cuando:** documentos están organizados por espacios y se edita Markdown.

---

## FASE 3 — Sharing + Permissions (Semana 3-4)
### Backend
- [ ] Modelo `SpacePermission`:
  - `CreativeSpaceId`, `UserId`, `Role (Viewer/Editor)`, `CreatedAt`.
  - Unique `(CreativeSpaceId, UserId)`.
- [ ] Endpoint búsqueda de usuarios (compartir dentro de app):
  - `GET /api/users/search?q=...`
- [ ] Endpoints permisos:
  - `GET/POST/DELETE /api/spaces/{spaceId}/permissions`
- [ ] Regla: al añadir el primer permiso, el espacio pasa automáticamente a `Shared`.
- [ ] Aplicar permisos a documentos:
  - Viewer: lectura
  - Editor: escritura
  - Owner: todo
- [ ] No existe transferencia de propiedad del espacio (MVP).

### Frontend
- [ ] Modal **Compartir**:
  - buscar usuario dentro de la app
  - seleccionar rol Viewer/Editor
  - lista de permisos + revocar
- [ ] Ajuste UX: ocultar/deshabilitar acciones de edición para Viewer.

**Hecho cuando:** se comparte un espacio y se respetan los roles.

---

## FASE 4 — Versionado manual (Semana 4-5)
### Backend
- [ ] Modelo `DocumentVersion` (snapshot completo):
  - `TitleSnapshot`, `DescriptionSnapshot`, `ContentSnapshot`, `TypeSnapshot`,
  - `VersionNumber`, `CreatedAt`, `CreatedByUserId`.
- [ ] Endpoints:
  - `POST /api/documents/{documentId}/versions` (crear)
  - `GET /api/documents/{documentId}/versions` (listar)
  - `POST /api/documents/{documentId}/versions/{versionId}/restore` (restaurar)
- [ ] Autorización:
  - listar: Viewer+
  - crear/restaurar: Editor/Owner

### Frontend
- [ ] Botón **Crear versión**.
- [ ] Panel historial + restaurar (confirmación).

**Hecho cuando:** se crean versiones y se restaura correctamente.

---

## FASE 5 — Perfil público limitado + Publicación con metadatos (Semana 6)
### Requisito clave
Ver documento público completo + “info extra” (p.ej. nombres/referencias de recursos multimedia utilizados) aportada desde un **modal de publicación** y editable desde un **modal de actualización**.

### Backend
- [ ] `Document.IsPublic` + `PublishedAt`.
- [ ] Modelo `DocumentPublication` (1–1 con Document):
  - `PublicTitle?`, `PublicDescription?`
  - `MediaReferencesJson` (**tipo + texto libre**)
  - `ExternalLinksJson` (links legales)
  - `PublishedByUserId`, `CreatedAt`, `UpdatedAt`
- [ ] Endpoints AUTH:
  - `GET /api/spaces/{spaceId}/documents/{documentId}/publication`
  - `PUT /api/spaces/{spaceId}/documents/{documentId}/publication` (upsert + publish/unpublish)
  - **Solo owner** puede publicar/despublicar.
- [ ] Endpoints públicos:
  - `GET /api/public/users/{userId}`
  - `GET /api/public/users/{userId}/documents`
  - `GET /api/public/documents/{documentId}` (contenido completo + metadatos)

### Frontend
- [ ] Modal reutilizable **Publicar/Actualizar publicación**:
  - título/desc públicos opcionales
  - `mediaReferences` lista (tipo+texto libre)
  - `externalLinks` lista
- [ ] Perfil público `/u/:userId`.
- [ ] Vista documento público:
  - Markdown renderizado
  - sección “Recursos utilizados” (mediaReferences + links)

**Hecho cuando:** cualquier visitante puede ver el documento publicado y sus metadatos.

---

## FASE 6 — Admin + ActivityLog + Validación fuerte de enlaces externos (Semana 7)
### Backend
- [ ] Modelo `ActivityLog` + enum `ActivityAction`.
- [ ] `ActivityLogService` para registrar:
  - spaces CRUD
  - docs CRUD
  - sharing (add/remove/update role)
  - version create/restore
  - publication publish/update/unpublish
  - intentos rechazados por enlaces no permitidos
- [ ] Validación estricta de `externalLinks`:
  - allowlist configurable en `appsettings.json`
  - bloquear publicación si hay enlaces no permitidos
  - devolver mensaje claro + lista de enlaces rechazados
  - log `ExternalResourceRejected`
- [ ] Admin endpoints (rol Admin):
  - `GET /api/admin/users`
  - `GET /api/admin/logs`

### Frontend
- [ ] Toast amigable si se bloquea publicación por dominios no permitidos.
- [ ] `/admin` (solo Admin): usuarios + logs.

**Hecho cuando:** admin ve actividad y la publicación aplica allowlist.

---

## FASE 7 — Cierre (Semana 8)
### Checklist end-to-end (manual)
1. [ ] Registro/Login.
2. [ ] Crear space.
3. [ ] Crear doc Markdown y guardar.
4. [ ] Crear versión #1.
5. [ ] Editar doc, crear versión #2.
6. [ ] Restaurar versión #1.
7. [ ] Compartir space con usuario B como Viewer.
8. [ ] B ve pero no edita.
9. [ ] Cambiar a Editor, B edita.
10. [ ] Owner publica doc con modal + mediaReferences + externalLinks permitidos.
11. [ ] Visitar perfil público sin login y ver doc completo + metadatos.
12. [ ] Intentar publicar link no permitido → bloqueo + toast + log.
13. [ ] Admin revisa logs.

### Docker y documentación
- [ ] `docker compose up` funciona en limpio.
- [ ] `docker-compose.dev.yml` (dev híbrido) funciona.
- [ ] Documentar reset DB (si aplica): `docker compose down -v`.
- [ ] Documentar allowlist (dominios permitidos) y cómo editarla.

**Hecho cuando:** un evaluador puede levantar y comprobar el MVP en 5–10 minutos.

---

## Anexo — Notas de implementación
- `DocumentType`: MVP fijo a `Markdown`, pero el modelo/DTO están preparados para añadir selector en el futuro.
- Publicación:
  - `mediaReferences` es texto libre con prefijo de tipo (ej. `video: ...`, `music: ...`).
  - `externalLinks` se valida estrictamente (allowlist).