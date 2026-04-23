# LifeHub Backend

Backend de LifeHub desarrollado con .NET 8

## Instalación

### Requisitos Previos

- .NET 8 SDK
- SQL Server (localdb o instalación completa)

### Configuración

1. Restaura los paquetes NuGet:

```bash
dotnet restore
```

2. Copia `.env.example` a `.env` en la raíz del proyecto y rellena los valores reales (`DB_PASSWORD`, `JWT_KEY`). Las credenciales se inyectan como variables de entorno y no deben escribirse en `appsettings.json`.

3. Las migraciones de base de datos se aplican automáticamente al arrancar la aplicación. No es necesario ejecutar `dotnet ef database update` manualmente.

## Ejecución

```bash
dotnet run
```

La API estará disponible en `http://localhost:5000` (o el puerto configurado).

## Características

- **Autenticación JWT**: Sistema de autenticación basado en tokens
- **Gestión de Usuarios**: Registro, login y perfil
- **Amigos**: Sistema de solicitudes y gestión de amigos
- **Documentos**: CRUD de documentos en línea con versionado
- **Mensajes**: Infraestructura SignalR y endpoints HTTP (sin frontend implementado)
- **Recomendaciones**: API con sistema de valoraciones (sin frontend implementado)
- **Música**: Gestión de metadatos de archivos locales (sin frontend implementado)

## Estructura del Proyecto

```
Controllers/       # Endpoints de la API
Data/             # DbContext y seeds
DTOs/             # Data Transfer Objects
Models/           # Entidades de negocio
Hubs/             # SignalR hubs
Utilidades/       # Configuraciones y helpers
Migrations/       # Migraciones de EF Core
```

## Endpoints Principales

### Autenticación
- `POST /api/auth/register` - Registro
- `POST /api/auth/login` - Login

### Usuarios
- `GET /api/users/{id}` - Obtener usuario
- `GET /api/users/me` - Usuario actual
- `PUT /api/users/me` - Actualizar perfil
- `POST /api/users/change-password` - Cambiar contraseña

### Amigos
- `GET /api/friendships` - Listar solicitudes
- `GET /api/friendships/accepted` - Amigos aceptados
- `POST /api/friendships` - Enviar solicitud
- `PUT /api/friendships/{id}` - Responder solicitud
- `DELETE /api/friendships/{id}` - Eliminar amistad

### Mensajes
- `GET /api/messages/conversation/{userId}` - Conversación
- `POST /api/messages` - Enviar mensaje
- `PUT /api/messages/{id}/mark-read` - Marcar como leído
- `GET /api/messages/unread` - Contar no leídos

### Recomendaciones
- `GET /api/recommendations` - Listar todas
- `GET /api/recommendations/{id}` - Obtener una
- `POST /api/recommendations` - Crear
- `PUT /api/recommendations/{id}` - Actualizar
- `DELETE /api/recommendations/{id}` - Eliminar
- `POST /api/recommendations/{id}/rate` - Calificar

### Documentos
- `GET /api/documents` - Listar
- `GET /api/documents/{id}` - Obtener
- `POST /api/documents` - Crear
- `PUT /api/documents/{id}` - Actualizar
- `DELETE /api/documents/{id}` - Eliminar

### Música
- `GET /api/musicfiles` - Listar
- `GET /api/musicfiles/{id}` - Obtener
- `POST /api/musicfiles` - Crear
- `PUT /api/musicfiles/{id}` - Actualizar
- `DELETE /api/musicfiles/{id}` - Eliminar

## SignalR Hub

`/hubs/chat` - Chat en tiempo real
- Métodos: `SendMessageAsync`, `MarkMessageAsReadAsync`
- Eventos: `ReceiveMessage`, `MessageRead`, `Error`

## Dependencias Principales

- Microsoft.AspNetCore.Identity.EntityFrameworkCore 8.0.0
- Microsoft.AspNetCore.Authentication.JwtBearer 8.0.0
- Microsoft.EntityFrameworkCore.SqlServer 8.0.0
- Microsoft.AspNetCore.SignalR 1.1.0
- AutoMapper.Extensions.Microsoft.DependencyInjection 12.0.1
- Swashbuckle.AspNetCore 6.6.2
