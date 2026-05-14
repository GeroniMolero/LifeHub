# LifeHub

# <img width="1919" height="1031" alt="image" src="https://github.com/user-attachments/assets/97b07952-b869-4aa3-b467-d0479762fd99" />

**LifeHub** es un portal web completo desarrollado con **Angular** (frontend) y **.NET 8** (backend), diseñado como un entorno centralizado para herramientas útiles del día a día.

Desarrollado como Trabajo de Fin de Grado del ciclo de Desarrollo de Aplicaciones Web (DAW).

## Características Principales

### Sistema de Autenticación
- Registro e inicio de sesión seguros
- Las nuevas cuentas se activan automáticamente al registrarse
- Autenticación basada en JWT
- Gestión de permisos y roles
- Persistencia de sesión (localStorage)
- Cierre de sesión con un clic
  
```Ejemplo de respuesta exitosa del servidor tras el login, proporcionando el Bearer Token.```

# <img width="718" height="171" alt="image" src="https://github.com/user-attachments/assets/edf777e1-fb8d-414b-8d3b-5f3651761ca0" />


### Inicio

# <img width="1919" height="1031" alt="image" src="https://github.com/user-attachments/assets/844e81bc-17dc-45a9-b92c-b27af8a36677" />

- Panel de resumen de actividad: contadores de documentos, espacios, amigos y solicitudes pendientes
- Documentos recientes con acceso directo (últimos 5 modificados)
- Espacios favoritos marcados desde el módulo Espacios
- Resumen social: lista de amigos con acceso rápido al chat

---

### Espacios creativos

# <img width="1919" height="1030" alt="image" src="https://github.com/user-attachments/assets/e0508d71-96bc-48af-8f11-7c9e6ef4ce8d" />

- Crear, editar y eliminar espacios.
- Con acceso a editor markdown y recursos multimedia embebidos.
- Items multimedia posicionables, redimensionables y con control de volumen para vídeos
- Buscador de documentos en la sidebar del espacio
- Posibilidad de espacio colaborativo con tu lista de amigos.
- **Colaboración por roles**: invitados con rol Viewer (solo lectura) o Editor (lectura y escritura)
- Los editores pueden crear y modificar documentos del espacio
- Solo el propietario puede eliminar documentos

---

### Gestión de Documentos

# <img width="1919" height="1033" alt="image" src="https://github.com/user-attachments/assets/a5da7e4a-5336-4f05-95d1-dd1d9bddc086" />

- Crear, editar, eliminar y descargar documentos
- Editor de texto con **modo split** (código y preview en paralelo)
- Diferentes tipos de documentos (notas, archivos, listas)
- **Versionado automático**: cada guardado crea una snapshot de la versión anterior
- Límite de 30 versiones por documento
- Eliminación de versiones individuales (solo propietario)
- Atribución: cada versión muestra quién la creó
- **Publicación de documentos**: modal con control de visibilidad en perfil público, autor visible en la vista pública

---

### Social

# <img width="1919" height="1030" alt="image" src="https://github.com/user-attachments/assets/138ed546-2202-44df-9076-92ce923df17e" />

- Panel de contactos con búsqueda de usuarios y solicitudes de amistad
- Gestión de amigos (enviar/aceptar/rechazar/eliminar) desde el perfil público
- Conversación directa con cualquier amigo (chat en tiempo real con SignalR)
- Badge de mensajes no leídos por amigo en el panel Social
- Feed de actividad de amigos (en desarrollo)

---

### Perfil

# <img width="1919" height="1032" alt="image" src="https://github.com/user-attachments/assets/ffce8171-0dd1-42e7-8d68-28596a2183d8" />

- Mostrar nombre, descripción e imagen.
- Vista pública con grid de dos columnas: documentos publicados y espacios.
- Marcar hasta 3 documentos y 3 espacios como visibles en el perfil.
- Modal de previsualización de documentos con markdown renderizado, botones Ver y Descargar.
- Badges de contador que se deshabilitan al alcanzar los límites configurados.

---

### Panel de administrador

# <img width="1919" height="1032" alt="image" src="https://github.com/user-attachments/assets/0b0bb482-d7c6-421b-948e-d33b6a066998" />

- Gestión de usuarios: activación de cuentas, edición, cambio de rol y eliminación
- Gestión de dominios permitidos para embebidos
- Registro de actividad con filtros y paginación
- Copias de seguridad bajo demanda

---

### Reproductor de Música
- Reproducción de archivos de audio locales en el sidebar de espacios creativos (disponibles durante la sesión)
- Soporte de Spotify vía embed para música persistente sin necesidad de subir archivos

### Chat en Tiempo Real
- Conversaciones uno a uno accesibles desde el módulo Social
- Mensajería instantánea con SignalR (WebSockets)
- Notificaciones de mensajes leídos (en desarrollo)

### Recomendaciones (pendiente)
- Recomendar películas, series y libros
- Sistema de calificaciones
- Comentarios en recomendaciones

## Arquitectura

### Backend (.NET 8)
- ASP.NET Core Web API con arquitectura de capas (Controllers → Services → Data)
- Entity Framework Core + SQL Server con migraciones automáticas al arrancar
- SignalR para chat en tiempo real con autenticación integrada y reconexión automática
- JWT para autenticación — sesiones validadas en cada petición, no solo al hacer login
- AutoMapper para mapeo de DTOs
- **SpaceAccessPolicy**: política centralizada de acceso a espacios y documentos
- **IHtmlSanitizer / HtmlSanitizer**: sanitización de contenido HTML inyectable y sustituible en tests
- **BusinessRules**: límites de negocio configurables en `appsettings.json` y expuestos al frontend via `GET /api/config/limits`
- **Result pattern (`ServiceResult<T>`)**: los servicios devuelven resultados tipados en lugar de lanzar excepciones, con mapeo automático a códigos HTTP en el controlador base
- **DiscordNotificationService**: notificación al administrador vía webhook de Discord cuando se registra un nuevo usuario; se omite silenciosamente si `DISCORD_WEBHOOK_URL` no está configurada
- Historial de actividad preservado incluso al eliminar cuentas de usuario
- Validación de entrada en dos capas: Data Annotations en DTOs y restricciones de longitud en base de datos
- Cabeceras de seguridad HTTP (`X-Content-Type-Options`, `X-Frame-Options`) y cabecera `Server` suprimida

### Frontend (Angular 19)
- Standalone Components
- Reactive Forms con validación cliente
- HttpClient con interceptor JWT y gestión automática de sesión expirada
- Protección de rutas bidireccional: los usuarios no autenticados no acceden a la app y los autenticados no pueden volver a login/registro
- Datos pre-cargados antes de renderizar pantallas (Resolver pattern) — sin estados de carga intermedios en la navegación
- SignalR con reconexión automática transparente al usuario
- Sistema global de notificaciones toast (éxito, error, info) con animaciones, límite de 5 simultáneos y truncado de mensajes largos
- `APP_INITIALIZER` para cargar los límites de negocio desde el backend antes del primer render

## Documentación técnica

- Frontend (general): `LifeHub-Frontend/README.md`
- Spaces (módulo): `LifeHub-Frontend/src/app/pages/spaces/README.md`
- Explicación de arrastre multimedia (funcional + técnica): `LifeHub-Frontend/docs/ARRASTRE_MULTIMEDIA.md`

## Variables de entorno

Copia la plantilla y rellena los valores reales antes de arrancar:

```bash
cp .env.example .env          # desarrollo local
cp .env.example .env.production  # servidor de producción
```

`.env.example` contiene todas las variables con descripción. Los archivos `.env` y `.env.production` nunca se suben al repositorio.

| Variable | Descripción |
|----------|-------------|
| `DB_PASSWORD` | Contraseña del usuario SA de SQL Server |
| `JWT_KEY` | Clave secreta para firmar tokens JWT (mín. 32 chars) |
| `DB_NAME` | Nombre de la base de datos (`LifeHubDB`) |
| `SQL_CONTAINER` | Nombre del contenedor SQL (`lifehub-sql-dev` / `lifehub-sqlserver`) |
| `SQLCMD_PATH` | Ruta a sqlcmd dentro del contenedor |
| `SQLCMD_OPTS` | Opciones extra para sqlcmd (vacío en dev, `-C` en prod) |
| `BACKEND_CONTAINER` | Nombre del contenedor backend |
| `ADMIN_EMAIL` / `ADMIN_PASSWORD` | Credenciales para los scripts de test |
| `DOMAIN` | Dominio para HTTPS en producción (ej. `lifehubapp.duckdns.org`). Requerido para que nginx genere el bloque SSL con Let's Encrypt. |
| `DISCORD_WEBHOOK_URL` | URL del webhook de Discord para notificaciones de nuevos registros. Opcional — si no se define, las notificaciones se desactivan. |

## Requisitos

- **Docker Desktop** (recomendado — gestiona backend y base de datos sin instalación local)
- **Node.js 20+ LTS** y npm (para el frontend local)
- **.NET 8 SDK** — solo si desarrollas el backend fuera de Docker
- **Visual Studio Code** o **Visual Studio 2022** (opcional)

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
npm ci
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
npm ci
npm start
```

---

**Acceso:**
- Frontend: http://localhost:4200
- Backend: http://localhost:5000
- Swagger: http://localhost:5000/swagger
- SQL Server: localhost:1433

> **Credenciales de administrador:** el DataSeeder crea el usuario admin con las credenciales definidas en `ADMIN_EMAIL`/`ADMIN_PASSWORD` del `.env`. Si no están definidas, el admin no se crea (se muestra un aviso en el log del backend). Las cuentas de usuario normal se crean directamente desde el registro.

## Entorno Nuevo (Otro Ordenador)

Flujo recomendado para un clon nuevo del repositorio:

```powershell
git clone <repo>
cd LifeHub
copy .env.example .env        # Edita .env con tus valores reales (DB_PASSWORD, JWT_KEY, etc.)
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

Los scripts leen la configuración del `.env` por defecto. Usa `-EnvFile` / `-e` para apuntar a otro archivo (p. ej. `.env.production`).

### Windows
```powershell
# Crear backup (dev local)
.\scripts\windows\backup-db.ps1

# Restaurar
.\scripts\windows\restore-db.ps1 -BackupFile .\backups\LifeHubDB_20260423_143000.bak
```

### Linux / macOS
```bash
# Crear backup (dev local)
./scripts/linux/backup-db.sh

# Restaurar
./scripts/linux/restore-db.sh ./backups/LifeHubDB_20260423_143000.bak
```


Los backups se guardan en `backups/` con el formato `<DB_NAME>_<timestamp>.bak`.

## Pruebas

Estado verificado a fecha **2026-05-12**:

- Resultado integración API: **69/69 PASS**, **0 FAIL**, **0 SKIP**.
- Suite unitaria backend: **160 tests** (xUnit).
- Suite unitaria frontend: **8 spec files · 64 tests** (Jasmine/Karma).

### Tests unitarios (sin servidor)

La suite unitaria cubre **160 casos** de backend y **64 tests** de frontend (8 archivos spec):

| Capa | Herramienta | Cobertura |
|------|-------------|-----------|
| Backend — 9 servicios | xUnit + EF Core InMemory | AllowedWebsite, CreativeSpace, Document, DocumentPublication, DocumentVersion, Friendship, Message, MusicFile, Recommendation, User |
| Frontend — servicios | Jasmine / Karma | AdminService, AuthService, ConfigService, SpaceWorkspaceComponent |
| Frontend — guards | Jasmine / Karma | AuthGuard, GuestGuard, AdminGuard |
| Frontend — interceptor | Jasmine / Karma | JwtInterceptor (cabecera Authorization, manejo de 401) |

**Windows:**
```powershell
.\scripts\windows\run-unit-tests.ps1
```

**Linux / macOS:**
```bash
./scripts/linux/run-unit-tests.sh
```

No requiere servidor ni base de datos — se ejecutan en local en segundos.

---

### Tests de integración E2E (requieren servidor)

La suite cubre **69 casos de prueba** sobre la API REST del backend, agrupados en ocho módulos:

| Módulo | Casos | Qué se verifica |
|--------|-------|------------------|
| AUTH | 10 | Registro, login, activación de usuario de pruebas, validación de campos y contraseña, tokens inválidos |
| DOCS | 10 | CRUD de documentos, versionado, control de acceso, shape paginada del listado |
| SPACES | 5 | CRUD de espacios creativos, validación de nombre |
| COL | 3 | Permisos de colaboración (Viewer vs Editor), acceso a documentos compartidos |
| PUBLICATIONS | 11 | Flujo completo de publicaciones, casos negativos (sin token, documento ajeno, tras despublicar) |
| ADMIN | 21 | Acceso por rol, gestión de dominios, usuarios, backup, logs de actividad, eliminación con relaciones activas, shape paginada |
| MENSAJES | 3 | Conversación paginada, control de autenticación, envío de mensajes |
| SEGURIDAD | 6 | Acceso sin token, token manipulado, cabeceras de seguridad HTTP, IDOR |

Los tests crean un usuario temporal propio y lo eliminan al finalizar — la base de datos queda limpia. Los tests de administrador requieren credenciales de admin en el `.env`.

### Prerequisitos (tests E2E)

- El backend debe estar en marcha (`http://localhost:5000` en dev, `https://lifehubapp.duckdns.org/api` en producción)
- Las variables `ADMIN_EMAIL` y `ADMIN_PASSWORD` en el `.env` de la raíz (sin ellas, los tests de admin se omiten con SKIP)

### Ejecución automatizada (genera informe Markdown)

**Windows:**
```powershell
.\scripts\windows\run-tests.ps1
# Con backend en otra URL:
.\scripts\windows\run-tests.ps1 -BaseUrl http://localhost:5001/api
```

**Linux / macOS:**
```bash
./scripts/linux/run-tests.sh
# Con backend en otra URL:
./scripts/linux/run-tests.sh http://localhost:5001/api
```

El informe se guarda en `documentacion/RESULTADO_PRUEBAS_<timestamp>.md` (archivo ignorado por git).

### Cobertura actual y plan de ampliación

Actualmente se valida bien la lógica de negocio backend, los flujos críticos de API y la capa de seguridad del frontend (guards e interceptor).

Objetivo de incremento de cobertura (siguiente iteración):

1. **Frontend unitario — componentes (prioridad alta):** ampliar specs en componentes clave de `pages/social`, `pages/profile` y `pages/spaces`.
2. **Frontend integración (prioridad alta):** empezar a poblar `LifeHub-Frontend/test/integration` con flujos de login, creación/edición de espacios y publicación/despublicación de documentos.
3. **Frontend E2E (prioridad media):** añadir escenarios de humo en `LifeHub-Frontend/test/e2e` para navegación principal y control de permisos.
4. **Métrica de cobertura (prioridad media):** publicar porcentaje de líneas/ramas frontend en CI con umbral mínimo (por ejemplo, 60% inicial y subida progresiva por sprint).

### Ejecución interactiva (resultados en tiempo real en la terminal)

**Windows:**
```powershell
.\scripts\windows\run-tests-interactive.ps1
```

**Linux / macOS:**
```bash
./scripts/linux/run-tests-interactive.sh
```

---

## Estructura del Proyecto

```
LifeHub/
├── LifeHub-Backend/          # API REST .NET 8
│   ├── Controllers/          # Endpoints
│   ├── Models/               # Entidades de BD
│   ├── DTOs/                 # Data Transfer Objects
│   ├── Services/             # Lógica de negocio
│   ├── Data/                 # DbContext y Migrations
│   ├── Utilidades/           # Patrones transversales (SpaceAccessPolicy, IHtmlSanitizer, BusinessRules)
│   └── Hubs/                 # SignalR Hubs
├── LifeHub-Frontend/         # Aplicación Angular
│   ├── src/
│   │   ├── app/
│   │   │   ├── models/       # Interfaces
│   │   │   ├── services/     # HTTP Services (incluye ToastService)
│   │   │   ├── components/   # Componentes reutilizables (ToastContainer, ...)
│   │   │   ├── pages/        # Componentes de página
│   │   │   ├── guards/       # Auth Guards
│   │   │   └── interceptors/ # HTTP Interceptors
│   │   ├── assets/           # Archivos estáticos
│   │   └── styles.scss       # Estilos globales
│   └── angular.json          # Configuración Angular
├── documentacion/            # Plan de pruebas y documentación del proyecto
├── LifeHub-Backend.Tests/    # Tests unitarios backend (xUnit)
│   ├── Helpers/              # TestHelpers: contexto InMemory, UserManager, AutoMapper
│   └── Services/             # ~160 tests sobre los 9 servicios de negocio
├── scripts/
│   ├── windows/              # Scripts PowerShell (Windows)
│   │   ├── backup-db.ps1
│   │   ├── restore-db.ps1
│   │   ├── run-unit-tests.ps1        # Tests unitarios (sin servidor)
│   │   ├── run-tests.ps1             # Suite E2E automatizada → genera informe .md
│   │   └── run-tests-interactive.ps1 # Resultados en tiempo real en terminal
│   └── linux/                # Scripts Bash (Linux / macOS)
│       ├── backup-db.sh
│       ├── restore-db.sh
│       ├── run-unit-tests.sh
│       ├── run-tests.sh
│       └── run-tests-interactive.sh
├── docker-compose.dev.yml    # Stack de desarrollo (backend watch + SQL Edge)
├── docker-compose.yml        # Stack de producción
└── .env.example              # Plantilla de variables de entorno
```

## Seguridad

- Autenticación basada en JWT
- Hash de contraseñas con Identity
- **Complejidad de contraseña**: mínimo 10 caracteres, mayúscula, minúscula, dígito y carácter especial — validado en el frontend (Angular Reactive Forms) y en el backend (ASP.NET Identity con `RequiredLength = 10`). Los mensajes de error del servidor se devuelven en español mediante `SpanishIdentityErrorDescriber`
- Validación en servidor (Data Annotations en DTOs) y cliente (Angular Reactive Forms)
- Restricciones de longitud en base de datos (EF Core `HasMaxLength`)
- Sanitización XSS en backend: `IHtmlSanitizer` sanitiza el contenido HTML antes de persistir
- **HTTPS en producción**: certificado Let's Encrypt gestionado con certbot y renovación automática. Todas las peticiones HTTP son redirigidas a HTTPS mediante nginx
- Cabeceras HTTP de seguridad (`X-Content-Type-Options`, `X-Frame-Options`) y cabecera `Server` suprimida
- Rate limiting en endpoints de autenticación para mitigar ataques de fuerza bruta
- Protección de rutas con Guards
- No almacenamiento de contenido protegido por derechos de autor

### Sesión y expiración de token JWT

- El frontend persiste el token en `localStorage` para mantener sesión entre cierres del navegador.
- Cuando el token expira o es inválido, el backend responde `401 Unauthorized`.
- El interceptor JWT del frontend captura `401`, ejecuta logout y redirige automáticamente a `/login`.
- Este cierre de sesión ocurre en la siguiente petición HTTP al backend tras la expiración del token.
- La duración del token se configura en `LifeHub-Backend/appsettings.json`, en `Jwt:ExpiresInMinutes`.

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

Todos los derechos reservados al autor — ver el archivo LICENSE para detalles.

## Contacto

Para soporte y preguntas: gemordz@gmail.com

---

**LifeHub** - Tu portal personal para herramientas útiles del día a día
