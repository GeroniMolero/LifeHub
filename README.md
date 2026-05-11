# LifeHub

# <img width="1528" height="867" alt="image" src="https://github.com/user-attachments/assets/2e023cb4-c5f1-4fec-9b99-5e4402b35198" />

**LifeHub** es un portal web completo desarrollado con **Angular** (frontend) y **.NET 8** (backend), diseñado como un entorno centralizado para herramientas útiles del día a día.

Desarrollado como Trabajo de Fin de Grado del ciclo de Desarrollo de Aplicaciones Web (DAW).

## Características Principales

### Sistema de Autenticación
- Registro e inicio de sesión seguros
- Autenticación basada en JWT
- Gestión de permisos y roles
- Persistencia de sesión (localStorage)
- Cierre de sesión con un clic
  
```Ejemplo de respuesta exitosa del servidor tras el login, proporcionando el Bearer Token.```

# <img width="718" height="171" alt="image" src="https://github.com/user-attachments/assets/edf777e1-fb8d-414b-8d3b-5f3651761ca0" />


### Social

# <img width="1565" height="950" alt="image" src="https://github.com/user-attachments/assets/c68f5efc-3d26-4210-897a-6cd932f3d1a8" />

- Panel de contactos con búsqueda de usuarios y solicitudes de amistad
- Gestión de amigos (enviar/aceptar/rechazar/eliminar) desde el perfil público
- Conversación directa con cualquier amigo (chat en tiempo real con SignalR)
- Badge de mensajes no leídos por amigo en el panel Social
- Feed de actividad de amigos (en desarrollo)


### Espacios creativos

# <img width="1565" height="950" alt="image" src="https://github.com/user-attachments/assets/7a2a1263-cef6-44b4-8c57-9d13ce49ea26" />

- Crear, editar y eliminar espacios.
- Con acceso a editor markdown y recursos multimedia embebidos.
- Posibilidad de espacio colaborativo con tu lista de amigos.
- **Colaboración por roles**: invitados con rol Viewer (solo lectura) o Editor (lectura y escritura)
- Los editores pueden crear y modificar documentos del espacio
- Solo el propietario puede eliminar documentos

### Gestión de Documentos

# <img width="1565" height="950" alt="image" src="https://github.com/user-attachments/assets/5f80560b-4c61-467b-a726-9f7f17b7b2e7" />

- Crear, editar, eliminar y descargar documentos
- Editor de texto con **modo split** (código y preview en paralelo)
- Diferentes tipos de documentos (notas, archivos, listas)
- **Versionado automático**: cada guardado crea una snapshot de la versión anterior
- Límite de 30 versiones por documento
- Eliminación de versiones individuales (solo propietario)
- Atribución: cada versión muestra quién la creó
- **Publicación de documentos**: modal con control de visibilidad en perfil público, autor visible en la vista pública

### Perfil

# <img width="1565" height="950" alt="image" src="https://github.com/user-attachments/assets/8e6061e8-ea45-4ff7-9c58-bc00c5dba8dc" />

- Mostrar nombre, descripción e imagen.
- Vista pública con grid de dos columnas: documentos publicados y espacios.
- Marcar hasta 3 documentos y 3 espacios como visibles en el perfil.
- Modal de previsualización de documentos con markdown renderizado, botones Ver y Descargar.
- Badges de contador que se deshabilitan al alcanzar los límites configurados.

### Panel de administrador

# <img width="1565" height="950" alt="image" src="https://github.com/user-attachments/assets/58dc1465-dc9d-47a7-8701-f0177660a0b5" />

- Añadir, eliminar y desactivar dominios permitidos para embebidos.
- Visualizar usuarios (en un futuro acciones)

### Reproductor de Música (en desarrollo)
- Registro de archivos locales
- Metadatos de canciones (artista, álbum, duración)
- Gestión de biblioteca local

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
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- SignalR para chat en tiempo real
- JWT para autenticación
- AutoMapper para mapeo de DTOs
- **SpaceAccessPolicy**: política centralizada de acceso a espacios y documentos
- **IHtmlSanitizer / HtmlSanitizer**: sanitización de HTML inyectable (sin dependencias externas)
- **BusinessRules**: límites de negocio centralizados en `appsettings.json` y expuestos via `GET /api/config/limits`
- Data Annotations en todos los DTOs de entrada + restricciones `HasMaxLength` en EF Core
- Cabeceras de seguridad HTTP (`X-Content-Type-Options`, `X-Frame-Options`) y cabecera `Server` suprimida

### Frontend (Angular 19)
- Standalone Components
- Reactive Forms
- HttpClient con interceptores
- Routing y Guards
- Services para comunicación HTTP
- Sistema global de notificaciones toast (éxito, error, info) con animaciones de entrada/salida, límite de 5 simultáneos y truncado de mensajes largos
- `APP_INITIALIZER` para cargar los límites de negocio desde el backend al arrancar (`ConfigService`)

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
# Crear backup (dev)
.\scripts\windows\backup-db.ps1

# Crear backup (prod, en la misma máquina)
.\scripts\windows\backup-db.ps1 -EnvFile .env.production

# Restaurar
.\scripts\windows\restore-db.ps1 -BackupFile .\backups\LifeHubDB_20260423_143000.bak
.\scripts\windows\restore-db.ps1 -BackupFile .\backups\LifeHubDB_20260423_143000.bak -EnvFile .env.production
```

### Linux / macOS
```bash
# Crear backup (dev)
./scripts/linux/backup-db.sh

# Crear backup (prod)
./scripts/linux/backup-db.sh -e .env.production

# Restaurar
./scripts/linux/restore-db.sh ./backups/LifeHubDB_20260423_143000.bak
./scripts/linux/restore-db.sh -e .env.production ./backups/LifeHubDB_20260423_143000.bak
```

Los backups se guardan en `backups/` con el formato `<DB_NAME>_<timestamp>.bak`.

## Pruebas

Estado verificado a fecha **2026-05-10**:

- Última ejecución de integración registrada: `documentacion/RESULTADO_PRUEBAS_20260510_195344.md`.
- Resultado integración API: **60/60 PASS**, **0 FAIL**, **0 SKIP**.
- Suite unitaria backend: **160 tests** (xUnit).
- Suite unitaria frontend: **4 specs** (Jasmine/Karma).

### Tests unitarios (sin servidor)

La suite unitaria cubre **160 casos** de backend y **4 specs** de frontend:

| Capa | Herramienta | Cobertura |
|------|-------------|-----------|
| Backend — 9 servicios | xUnit + EF Core InMemory | AllowedWebsite, CreativeSpace, Document, DocumentPublication, DocumentVersion, Friendship, Message, MusicFile, Recommendation, User |
| Frontend | Jasmine / Karma | AdminService, AuthService, ConfigService, SpaceWorkspaceComponent |

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

La suite cubre **60 casos de prueba** sobre la API REST del backend, agrupados en siete módulos:

| Módulo | Casos | Qué se verifica |
|--------|-------|------------------|
| AUTH | 9 | Registro, login, activación de usuario de pruebas, validación de campos, tokens inválidos |
| DOCS | 9 | CRUD de documentos, versionado, control de acceso |
| SPACES | 5 | CRUD de espacios creativos, validación de nombre |
| COL | 3 | Permisos de colaboración (Viewer vs Editor), acceso a documentos compartidos |
| PUBLICATIONS | 11 | Flujo completo de publicaciones, casos negativos (sin token, documento ajeno, tras despublicar) |
| ADMIN | 19 | Acceso por rol, gestión de dominios, usuarios, backup y logs de actividad |
| SEGURIDAD | 4 | Acceso sin token, token manipulado, cabeceras de seguridad HTTP |

Los tests crean un usuario temporal propio y lo eliminan al finalizar — la base de datos queda limpia. Los tests de administrador requieren credenciales de admin en el `.env`.

### Prerequisitos (tests E2E)

- El backend debe estar en marcha (`http://localhost:5000`)
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

Actualmente se valida bien la lógica de negocio backend y los flujos críticos de API. La cobertura frontend todavía está concentrada en servicios y seguridad de renderizado Markdown.

Objetivo de incremento de cobertura (siguiente iteración):

1. **Frontend unitario (prioridad alta):** ampliar specs en `guards`, `interceptors` y componentes clave de `pages/social`, `pages/profile` y `pages/spaces`.
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
- Validación en servidor (Data Annotations en DTOs) y cliente (Angular Reactive Forms)
- Restricciones de longitud en base de datos (EF Core `HasMaxLength`)
- Sanitización XSS en backend: `IHtmlSanitizer` elimina `<script>`, atributos `on*=` y URIs `javascript:` antes de persistir
- Cabeceras HTTP de seguridad (`X-Content-Type-Options`, `X-Frame-Options`) y cabecera `Server` suprimida
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

Todos los derechos reservados al autor — ver el archivo LICENSE para detalles.

## Contacto

Para soporte y preguntas: gemordz@gmail.com

---

**LifeHub** - Tu portal personal para herramientas útiles del día a día
