# Destacados técnicos — LifeHub

Inventario de decisiones de diseño y patrones relevantes del proyecto, útil para
el README público y para la memoria del TFG.

Columna **Destino**: `README` = ya añadir o candidato a añadir en el README público;
`TFG` = más adecuado para la memoria (más detalle, más técnico); `AMBOS` = versión
resumida en README, desarrollo en TFG.

---

## Backend (.NET 8 / ASP.NET Core)

### Autenticación y autorización

| # | Hito | Estado README | Destino |
|---|------|---------------|---------|
| B-01 | JWT validado con `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey` | No mencionado | TFG |
| B-02 | **SignalR autenticado via cookie HttpOnly**: el JWT no viaja en la URL sino en una cookie HttpOnly, evitando que quede expuesto en logs o historial del navegador | No mencionado | AMBOS |
| B-03 | **Política `CanViewAdmin` combinada**: acepta rol `Admin` O claim de permiso específico (`admin.users.view`) — permite delegación granular sin elevar roles completos | No mencionado | AMBOS |
| B-04 | **`EnsureActiveSessionAsync()`**: valida que el usuario sigue existiendo en BD antes de ejecutar operaciones sensibles — revocación de acceso instantánea aunque el token no haya expirado | No mencionado | AMBOS |
| B-05 | Claims personalizados: `admin.users.view`, `documents.view.all` inyectados al usuario admin en el seeder | No mencionado | TFG |
| B-06 | `ClaimsPrincipalExtensions.GetUserId()`: extrae el ID con fallback multi-claim (Sub → NameIdentifier → "sub") | No mencionado | TFG |

### Arquitectura de servicios

| # | Hito | Estado README | Destino |
|---|------|---------------|---------|
| B-07 | **Result pattern `ServiceResult<T>`**: servicios devuelven un resultado tipado (Ok / NotFound / Forbidden / BadRequest / Conflict / Unauthorized) en lugar de excepciones para flujos esperados | En Backend/README.md, no en raíz | AMBOS |
| B-08 | **`ApiControllerBase.ToActionResult()`**: traduce `ServiceResult<T>` al `IActionResult` HTTP correspondiente — formato de error uniforme (`ApiErrorDto` con `code` + `message`) en todos los endpoints | En Backend/README.md, no en raíz | TFG |
| B-09 | **`SpaceAccessPolicy`** (ya en README): centraliza reglas de acceso a espacios y documentos (propietario, editor, viewer). Modelo de permisos transitivo: los permisos de un documento dependen del espacio al que pertenece | Ya en README | — |
| B-10 | **`IHtmlSanitizer` / `HtmlSanitizer`** (ya en README): interfaz inyectable, sustituible en tests sin dependencias externas | Ya en README | — |
| B-11 | **`BusinessRules` via `IOptions<T>`** (ya en README): límites de negocio (MaxDocumentVersions=30, MaxSpacesPerUser=10, etc.) en `appsettings.json`, cargados vía `IOptions<BusinessRules>` e inyectados en servicios | Ya en README | — |

### Persistencia y base de datos

| # | Hito | Estado README | Destino |
|---|------|---------------|---------|
| B-12 | **Audit trail preservado con `SetNull`**: la FK de `ActivityLogs.UserId` usa `DeleteBehavior.SetNull` — los registros de actividad sobreviven a la eliminación del usuario | No mencionado | AMBOS |
| B-13 | **Relaciones con `NoAction`**: Friendships, Messages, SpacePermissions y DocumentVersions usan `DeleteBehavior.NoAction` para evitar deletes en cascada no controlados; la limpieza es explícita en `UserService.CleanupUserRelationsAsync()` | No mencionado | TFG |
| B-14 | **Índices compuestos**: `(RequesterId, ReceiverId)` en Friendships, `(SenderId, ReceiverId)` en Messages, `(CreativeSpaceId, UserId)` en SpacePermissions, `(DocumentId, VersionNumber)` en DocumentVersions, `(UserId, CreatedAt)` en ActivityLogs | No mencionado | TFG |
| B-15 | **JSON embebido para datos flexibles**: `MediaReferencesJson` en `CreativeSpace` y `DocumentPublication` almacena arrays de referencias como JSON string, evitando tablas adicionales para datos variables | No mencionado | TFG |
| B-16 | **Aggregación desnormalizada en valoraciones**: `AverageRating` y `TotalRatings` se almacenan en la entidad `Recommendation` para evitar recálculo costoso en cada lectura | No mencionado | TFG |
| B-17 | Restricciones `HasMaxLength` en EF Core para todas las columnas de texto (ya en README) | Ya en README | — |

### Logging y operaciones transversales

| # | Hito | Estado README | Destino |
|---|------|---------------|---------|
| B-18 | **Activity logging fire-and-forget**: los logs de actividad se persisten de forma no bloqueante — un fallo de logging se registra como warning pero no propaga error al flujo principal | No mencionado | AMBOS |
| B-19 | **Cabeceras de seguridad HTTP** (ya en README): `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, cabecera `Server` suprimida vía Kestrel (`AddServerHeader = false`) + nginx (`more_clear_headers Server`) | Ya en README | — |
| B-20 | **Healthcheck en cascada Docker**: el backend no arranca hasta que SQL Server supera un healthcheck real (`SELECT 1` via `sqlcmd`), con 10 reintentos y 60 s de margen de inicio | No mencionado | AMBOS |

---

## Frontend (Angular 19)

### Seguridad y autenticación

| # | Hito | Estado README | Destino |
|---|------|---------------|---------|
| F-01 | **JWT Interceptor con protección anti-loop**: añade `Authorization: Bearer` a todas las peticiones; el flag `isLoggingOut` evita múltiples logouts simultáneos al recibir 401 | No mencionado | TFG |
| F-02 | **XSRF token handling**: `withXsrfConfiguration({ cookieName: 'XSRF-TOKEN', headerName: 'X-XSRF-TOKEN' })` configurado en `app.config.ts` para protección CSRF | No mencionado | AMBOS |
| F-03 | **`GuestGuard`**: protección bidireccional de rutas — usuarios autenticados no pueden acceder a `/login` o `/register`, igual que los no autenticados no acceden a rutas protegidas | No mencionado | README |
| F-04 | `AuthGuard` con `returnUrl`: al redirigir al login conserva la URL original para navegar tras autenticarse | No mencionado | TFG |

### Arquitectura Angular

| # | Hito | Estado README | Destino |
|---|------|---------------|---------|
| F-05 | **Resolver pattern**: los datos del espacio creativo se pre-cargan antes de que el componente se inicialice, eliminando estados de carga intermedios en la navegación | No mencionado | AMBOS |
| F-06 | **`APP_INITIALIZER`** (ya en README): `ConfigService.loadLimits()` ejecutado antes del bootstrap — garantiza que los límites de negocio están disponibles desde el primer render | Ya en README | — |
| F-07 | Standalone Components (ya en README) | Ya en README | — |
| F-08 | **SignalR con `withAutomaticReconnect()`**: el cliente se reconecta automáticamente tras pérdida de conexión sin intervención del usuario | No mencionado | README |

### Experiencia de usuario

| # | Hito | Estado README | Destino |
|---|------|---------------|---------|
| F-09 | **`SafeResourceUrl` con caché**: URLs de embeds saneadas con `DomSanitizer` una sola vez y cacheadas en un `Map<string, SafeResourceUrl>` para evitar re-saneamientos en cada ciclo de render | No mencionado | TFG |
| F-10 | **Atajos de teclado en el editor**: Tab inserta 2 espacios, Ctrl/Cmd+B aplica negrita — soporte cruzado Windows/macOS | No mencionado | README |
| F-11 | **Sistema de toast global** (ya en README): límite de 5 simultáneos, animaciones de entrada/salida, truncado de mensajes largos | Ya en README | — |
| F-12 | **Drag-and-drop multimedia con z-index gestionado**: posiciones visuales mapeadas por ID con contador de z-index propio para solapamiento correcto | No mencionado | TFG |
| F-13 | **`sessionStorage` para estado temporal de medios**: referencias multimedia del espacio almacenadas en sesión (no persisten entre pestañas ni recargas) | No mencionado | TFG |

---

## Infraestructura

| # | Hito | Estado README | Destino |
|---|------|---------------|---------|
| I-01 | **Multi-stage build backend**: imagen SDK para compilar, imagen runtime ASP.NET para producción — imagen final más ligera | No mencionado | TFG |
| I-02 | **Multi-stage build frontend**: Node 20 Alpine para build, Alpine 3.21 + nginx para servir — sin Node.js en producción | No mencionado | TFG |
| I-03 | **Compresión gzip en nginx**: habilitada para CSS, JSON, JS, XML con umbral mínimo de 1 KB | No mencionado | README |
| I-04 | **Proxy inverso nginx**: enruta `/api/` al backend y `/hubs/` con upgrade WebSocket; el frontend nunca expone directamente el puerto del backend | No mencionado | AMBOS |

---

## Resumen — candidatos a añadir al README

Los marcados `README` o `AMBOS` que aún no están:

| ID | Descripción breve |
|----|-------------------|
| B-02 | SignalR autenticado via cookie HttpOnly |
| B-03 | Política `CanViewAdmin` combinada (rol + claim) |
| B-04 | `EnsureActiveSessionAsync()` — revocación de sesión activa |
| B-07 | Result pattern `ServiceResult<T>` (ya en Backend README, falta en raíz) |
| B-12 | Audit trail preservado con `DeleteBehavior.SetNull` |
| B-18 | Activity logging fire-and-forget |
| B-20 | Healthcheck en cascada Docker |
| F-02 | XSRF token handling |
| F-03 | `GuestGuard` — protección bidireccional de rutas |
| F-05 | Resolver pattern — pre-carga de datos en navegación |
| F-08 | SignalR con `withAutomaticReconnect()` |
| F-10 | Atajos de teclado en el editor (Tab, Ctrl+B) |
| I-03 | Compresión gzip en nginx |
| I-04 | Proxy inverso nginx con upgrade WebSocket |
