# LifeHub

# <img width="1528" height="867" alt="image" src="https://github.com/user-attachments/assets/2e023cb4-c5f1-4fec-9b99-5e4402b35198" />

**LifeHub** es un portal web completo desarrollado con **Angular** (frontend) y **.NET 8** (backend), diseñado como un entorno centralizado para herramientas útiles del día a día.

## Características Principales

### Sistema de Autenticación
- Registro e inicio de sesión seguros
- Autenticación basada en JWT
- Gestión de permisos y roles
- Persistencia de sesión (localStorage)
- Cierre de sesión con un clic
  
```Ejemplo de respuesta exitosa del servidor tras el login, proporcionando el Bearer Token.```

# <img width="718" height="171" alt="image" src="https://github.com/user-attachments/assets/edf777e1-fb8d-414b-8d3b-5f3651761ca0" />


### Gestión de Amigos

# <img width="1565" height="950" alt="image" src="https://github.com/user-attachments/assets/c68f5efc-3d26-4210-897a-6cd932f3d1a8" />

- Solicitudes de amistad
- Aceptar/rechazar solicitudes
- Lista de amigos


### Espacios creativos

# <img width="1565" height="950" alt="image" src="https://github.com/user-attachments/assets/7a2a1263-cef6-44b4-8c57-9d13ce49ea26" />

- Crear, editar y eliminar espacios.
- Con acceso a editor markdown y recursos multimedia embebidos.
- Posibilidad de espacio colaborativo con tu lista de amigos.

### Gestión de Documentos

# <img width="1565" height="950" alt="image" src="https://github.com/user-attachments/assets/5f80560b-4c61-467b-a726-9f7f17b7b2e7" />

- Crear, editar, eliminar y descargar documentos
- Editor de texto en línea
- Diferentes tipos de documentos (notas, archivos, listas)

### Perfil

# <img width="1565" height="950" alt="image" src="https://github.com/user-attachments/assets/8e6061e8-ea45-4ff7-9c58-bc00c5dba8dc" />

- Mostrar nombre, descripcion, imagen.
- Añadir previsualización de hasta 2 espacios favoritos para mostrar en tu perfil.

### Panel de administrador

# <img width="1565" height="950" alt="image" src="https://github.com/user-attachments/assets/58dc1465-dc9d-47a7-8701-f0177660a0b5" />

- Añadir, eliminar y desactivar dominios permitidos para embebidos.
- Visualizar usuarios (en un futuro acciones)

### Reproductor de Música (en desarrollo)
- Registro de archivos locales
- Metadatos de canciones (artista, álbum, duración)
- Gestión de biblioteca local

### Chat en Tiempo Real (pendiente)
- Mensajería instantánea con SignalR
- Notificaciones de mensajes leídos
- Conversaciones uno a uno

### Recomendaciones (pendiente)
- Recomendar películas, series y libros
- Sistema de calificaciones
- Comentarios en recomendaciones

## Arquitectura

### Backend (.NET 8)
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- SignalR para chat en tiempo real
- JWT para autenticación
- AutoMapper para mapeo de DTOs

### Frontend (Angular 19)
- Standalone Components
- Reactive Forms
- HttpClient con interceptores
- Routing y Guards
- Services para comunicación HTTP

## Documentación técnica

- Frontend (general): `LifeHub-Frontend/README.md`
- Spaces (módulo): `LifeHub-Frontend/src/app/pages/spaces/README.md`
- Explicación de arrastre multimedia (funcional + técnica): `LifeHub-Frontend/docs/ARRASTRE_MULTIMEDIA.md`

## Requisitos

- **.NET 8 SDK** o superior
- **Node.js 20+** y npm
- **SQL Server 2019+** (o LocalDB)
- **Visual Studio Code** o **Visual Studio 2022**

## Inicio Rápido

### Windows

```powershell
.\start.ps1 local            # Recomendado: Docker (backend+db) + frontend local
.\start.ps1 local-noinstall  # Igual que local, pero sin npm ci
.\start.ps1 dev              # Todo el stack dev en Docker
.\start.ps1 prod             # Stack de producción en Docker
```

Para detener todo:
```powershell
.\stop-local.ps1
```

### Linux / macOS

```bash
./start.sh dev    # Stack dev en Docker
./start.sh prod   # Stack de producción en Docker
```

En Linux el frontend se levanta manualmente en una segunda terminal:
```bash
cd LifeHub-Frontend
npm ci --legacy-peer-deps
npm start
```

### Si prefieres el flujo manual de siempre (2 terminales)

**Terminal 1 - Backend + Database en Docker:**
```powershell
docker compose -f docker-compose.dev.yml up -d mssql backend
```

**Terminal 2 - Frontend Local:**
```powershell
cd LifeHub-Frontend
npm ci --legacy-peer-deps
npm start
```

---

**Acceso:**
- Frontend: http://localhost:4200
- Backend: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- SQL Server: localhost:1433

## Entorno Nuevo (Otro Ordenador)

Flujo recomendado para un clon nuevo del repositorio:

```powershell
git clone <repo>
cd LifeHub
.\start.ps1 local
```

No hace falta instalar .NET/SQL Server localmente para ejecutar backend+BD en Docker.

### Si existe un volumen Docker antiguo con esquema desalineado

Solo si detectas errores de esquema al arrancar (por ejemplo columnas faltantes),
resetea la base de datos local Docker y vuelve a iniciar:

```powershell
docker compose -f docker-compose.dev.yml down -v
.\start.ps1 local
```

Esto borra solo datos locales de desarrollo en Docker (`sql_data`).

## Copias de seguridad

### Windows
```powershell
# Crear backup
.\backup-db.ps1

# Restaurar
.\restore-db.ps1 -BackupFile .\backups\LifeHub_20260423_143000.bak
```

### Linux / macOS
```bash
# Crear backup
./backup-db.sh

# Restaurar
./restore-db.sh ./backups/LifeHub_20260423_143000.bak
```

Los backups se guardan en la carpeta `backups/` con timestamp. Requiere el stack de desarrollo en marcha.

## Estructura del Proyecto

```
LifeHub/
├── LifeHub-Backend/          # API REST .NET 8
│   ├── Controllers/          # Endpoints
│   ├── Models/               # Entidades de BD
│   ├── DTOs/                 # Data Transfer Objects
│   ├── Services/             # Lógica de negocio
│   ├── Data/                 # DbContext y Migrations
│   └── Hubs/                 # SignalR Hubs
├── LifeHub-Frontend/         # Aplicación Angular
│   ├── src/
│   │   ├── app/
│   │   │   ├── models/       # Interfaces
│   │   │   ├── services/     # HTTP Services
│   │   │   ├── pages/        # Componentes de página
│   │   │   ├── guards/       # Auth Guards
│   │   │   └── interceptors/ # HTTP Interceptors
│   │   ├── assets/           # Archivos estáticos
│   │   └── styles.scss       # Estilos globales
│   └── angular.json          # Configuración Angular
└── docker-compose.yml        # Orquestación Docker

```

## Seguridad

- Autenticación basada en JWT
- Hash de contraseñas con Identity
- Validación en servidor y cliente
- Protección de rutas con Guards
- No almacenamiento de contenido protegido por derechos de autor

### Sesión y expiración de token JWT

- El frontend persiste el token en `localStorage` para mantener sesión entre cierres del navegador.
- Cuando el token expira o es inválido, el backend responde `401 Unauthorized`.
- El interceptor JWT del frontend captura `401`, ejecuta logout y redirige automáticamente a `/login`.
- Este cierre de sesión ocurre en la siguiente petición HTTP al backend tras la expiración del token.
- La duración del token se configura en `LifeHub-Backend/appsettings.json`, en `Jwt:ExpiresInMinutes` (valor actual: `600`).

## Notas Importantes

- **No almacenamos archivos multimedia** - Los archivos de música y documentos se almacenan en el dispositivo del usuario
- **Enlaces externos** - Integramos contenido mediante enlaces oficiales de servicios como YouTube, Spotify, etc.
- **GDPR Compliant** - Respetamos la privacidad del usuario

## Contribución

1. Fork el proyecto
2. Crea una rama (`git checkout -b feature/AmazingFeature`)
3. Commit cambios (`git commit -m 'Add AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## Licencia

Este proyecto está bajo la Licencia MIT - ver el archivo LICENSE para detalles.

## Contacto

Para soporte y preguntas: gemordz@gmail.com

---

**LifeHub** - Tu portal personal para herramientas útiles del día a día
