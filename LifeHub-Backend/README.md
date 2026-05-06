# LifeHub Backend

Backend de LifeHub desarrollado con .NET 8 y ASP.NET Core Web API.

## Requisitos

- .NET 8 SDK (solo si desarrollas fuera de Docker)
- Docker Desktop (recomendado — gestiona backend y base de datos)

## Configuración

1. Copia `.env.example` a `.env` (desarrollo) o `.env.production` (servidor de prod) en la raíz del repositorio y rellena los valores reales. Las credenciales se inyectan como variables de entorno y no deben escribirse en `appsettings.json`.

2. Las migraciones de base de datos se aplican automáticamente al arrancar. No es necesario ejecutar `dotnet ef database update` manualmente.

## Ejecución

```bash
# Con Docker (recomendado)
docker compose -f docker-compose.dev.yml up -d mssql backend

# Local (requiere SQL Server accesible)
dotnet run
```

La API estará disponible en `http://localhost:5000`.  
Swagger UI: `http://localhost:5000/swagger`

## Estructura del proyecto

```
Controllers/    Endpoints de la API — delegan en servicios, no contienen lógica de negocio
Services/       Capa de negocio — un módulo por dominio (interfaz + implementación)
DTOs/           Objetos de transferencia de datos (entrada y salida)
Models/         Entidades de base de datos (EF Core)
Data/           DbContext y seeds
Migrations/     Migraciones de EF Core
Hubs/           SignalR hubs (chat en tiempo real)
Utilidades/     Patrones transversales — ver sección Arquitectura
```

## Arquitectura y patrones de diseño

### Capa de servicios

Cada dominio tiene su propio módulo bajo `Services/[Dominio]/` con una interfaz y su implementación:

| Módulo | Interfaz |
|--------|----------|
| Documentos | `IDocumentService` |
| Espacios creativos | `ICreativeSpacesService` |
| Versiones de documentos | `IDocumentVersionService` |
| Publicaciones | `IDocumentPublicationsService` |
| Amigos | `IFriendshipsService` |
| Mensajes | `IMessagesService` |
| Música | `IMusicFilesService` |
| Recomendaciones | `IRecommendationsService` |
| Usuarios | `IUserService` |
| Registro de actividad | `IActivityLogService` |

Los controladores inyectan la interfaz y delegan toda la lógica al servicio. No contienen queries ni reglas de negocio.

### Result pattern — `ServiceResult<T>`

Los servicios devuelven `ServiceResult<T>` en lugar de lanzar excepciones para flujos de negocio esperados. El tipo encapsula el estado de la operación y el valor o mensaje de error:

```csharp
ServiceResult<T>.Ok(value)
ServiceResult<T>.NotFound("Documento no encontrado.")
ServiceResult<T>.Forbidden("Sin permiso para editar.")
ServiceResult<T>.BadRequest("Límite de versiones alcanzado.")
ServiceResult<T>.Conflict("El nombre ya existe.")
```

### Base controller — `ApiControllerBase`

Todos los controladores heredan de `ApiControllerBase`, que traduce un `ServiceResult<T>` al `IActionResult` HTTP correspondiente mediante `ToActionResult()`. Centraliza la generación de respuestas de error con un formato uniforme (`ApiErrorDto` con `code` y `message`).

### Policy pattern — `SpaceAccessPolicy`

Clase estática en `Utilidades/` que centraliza todas las reglas de acceso a espacios y documentos. Antes estas comprobaciones estaban duplicadas en varios controladores y servicios.

```csharp
SpaceAccessPolicy.CanAccess(space, userId)      // propietario o cualquier colaborador
SpaceAccessPolicy.CanEdit(space, userId)         // propietario o colaborador con rol Editor
SpaceAccessPolicy.CanAccessDocument(doc, userId)
SpaceAccessPolicy.CanEditDocument(doc, userId)
```

### Strategy pattern — `IHtmlSanitizer`

Interfaz inyectable para sanitización de HTML antes de persistir contenido. La implementación `HtmlSanitizer` elimina etiquetas `<script>`, atributos de evento (`on*=`) y URIs `javascript:`. Al ser una interfaz, es sustituible en tests sin dependencias externas.

### Constantes de negocio — `BusinessRules`

Clase estática con las constantes compartidas entre servicios (por ejemplo, el límite de 30 versiones por documento). Evita magic numbers dispersos por el código.

### Extension methods — `ClaimsPrincipalExtensions`

Extiende `ClaimsPrincipal` con `GetUserId()` para extraer el identificador de usuario del token JWT de forma consistente en toda la aplicación.

## Validación

- **DTOs de entrada**: Data Annotations (`[Required]`, `[MaxLength]`, `[StringLength]`, etc.)
- **Base de datos**: restricciones `HasMaxLength` en EF Core para todas las columnas de texto
- **Frontend**: Reactive Forms con validación cliente (Angular)

## Seguridad

- Autenticación JWT — token generado en login, validado en cada petición
- Hash de contraseñas con ASP.NET Core Identity
- Sanitización XSS en backend antes de persistir contenido HTML
- Autorización por roles (`Admin`) y por política de acceso a espacios

## Endpoints principales

### Autenticación
- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/logout`

### Usuarios
- `GET /api/users/{id}`
- `GET /api/users/me`
- `PUT /api/users/me`
- `POST /api/users/change-password`

### Amigos
- `GET /api/friendships` — solicitudes pendientes
- `GET /api/friendships/accepted` — amigos aceptados
- `POST /api/friendships` — enviar solicitud
- `PUT /api/friendships/{id}` — aceptar/rechazar
- `DELETE /api/friendships/{id}` — eliminar

### Espacios creativos
- `GET /api/creativespaces`
- `POST /api/creativespaces`
- `PUT /api/creativespaces/{id}`
- `DELETE /api/creativespaces/{id}`
- `POST /api/creativespaces/{id}/invite`
- `DELETE /api/creativespaces/{id}/permissions/{userId}`

### Documentos
- `GET /api/documents`
- `GET /api/documents/{id}`
- `POST /api/documents`
- `PUT /api/documents/{id}`
- `DELETE /api/documents/{id}`

### Versiones de documentos
- `GET /api/documentversions/{documentId}`
- `DELETE /api/documentversions/{id}`

### Mensajes
- `GET /api/messages/conversation/{userId}`
- `POST /api/messages`
- `PUT /api/messages/{id}/mark-read`
- `GET /api/messages/unread`

### Recomendaciones
- `GET /api/recommendations`
- `POST /api/recommendations`
- `PUT /api/recommendations/{id}`
- `DELETE /api/recommendations/{id}`
- `POST /api/recommendations/{id}/rate`

### Música
- `GET /api/musicfiles`
- `POST /api/musicfiles`
- `PUT /api/musicfiles/{id}`
- `DELETE /api/musicfiles/{id}`

### Administración
- `GET /api/allowedwebsites`
- `POST /api/allowedwebsites`
- `DELETE /api/allowedwebsites/{id}`

## SignalR

`/hubs/chat` — chat en tiempo real  
Métodos del cliente: `SendMessageAsync`, `MarkMessageAsReadAsync`  
Eventos del servidor: `ReceiveMessage`, `MessageRead`, `Error`

## Dependencias principales

- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 8.0.0
- `Microsoft.AspNetCore.Authentication.JwtBearer` 8.0.0
- `Microsoft.EntityFrameworkCore.SqlServer` 8.0.0
- `Microsoft.AspNetCore.SignalR` 1.1.0
- `AutoMapper` 15.1.1 — DI integrado desde v13 (`AddAutoMapper(cfg => cfg.AddProfile<T>())`)
- `Swashbuckle.AspNetCore` 6.6.2
