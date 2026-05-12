# LifeHub Frontend

Frontend de LifeHub desarrollado con Angular 19

## Instalación

```bash
npm ci --legacy-peer-deps
```

## Desarrollo

```bash
npm start
```

Abre `http://localhost:4200/` en tu navegador.

## Características

- **Autenticación**: Registro e inicio de sesión con JWT
- **Perfil de Usuario**: Gestión de perfil y contraseña
- **Social**: Panel de contactos, gestión de amigos y chat en tiempo real con SignalR
- **Espacios creativos**: CRUD de espacios con editor Markdown y recursos embebidos
- **Documentos**: Crea, edita y descarga documentos en línea
- **Recomendaciones**: En desarrollo (API backend lista, sin interfaz)
- **Música**: En desarrollo (API backend lista, sin interfaz)

## Arquitectura y patrones

### Seguridad y sesión

- **JWT Interceptor**: añade el token de autorización a todas las peticiones HTTP de forma transparente; gestiona la expiración de sesión y redirige al login automáticamente evitando bucles de logout
- **Protección de rutas bidireccional**:
  - `AuthGuard`: impide el acceso a rutas protegidas sin sesión activa; conserva la URL destino para redirigir tras el login (`returnUrl`)
  - `GuestGuard`: impide que un usuario ya autenticado acceda a `/login` o `/register`
  - `AdminGuard`: restringe el panel de administración a usuarios con rol o permiso adecuado
- **XSRF**: configurado con `withXsrfConfiguration` para transmitir el token CSRF en cabecera en las peticiones que lo requieran

### Carga de datos y navegación

- **Resolver pattern**: los datos necesarios para una pantalla se pre-cargan antes de que el componente se inicialice, eliminando estados de carga intermedios en la navegación
- **`APP_INITIALIZER`**: `ConfigService.loadLimits()` se ejecuta antes del bootstrap de la aplicación, garantizando que los límites de negocio del backend están disponibles desde el primer render

### Tiempo real

- **SignalR con `withAutomaticReconnect()`**: el cliente se reconecta automáticamente tras una pérdida de conexión sin intervención del usuario
- El servicio de chat se conecta al autenticarse y se desconecta al cerrar sesión, sin dejar conexiones abiertas

### Editor de documentos

- **Atajos de teclado**: Tab inserta dos espacios, Ctrl/Cmd+B aplica negrita — compatible con Windows y macOS
- **Modo split**: código Markdown y previsualización renderizada lado a lado

### Embeds y multimedia

- **`SafeResourceUrl` con caché**: las URLs de recursos embebidos se sanean con `DomSanitizer` una única vez y se cachean en un `Map` para evitar re-saneamientos en cada ciclo de render
- **`sessionStorage` para estado temporal**: las referencias multimedia de un espacio se guardan en sesión (no persisten entre pestañas ni recargas de página)

## Documentación

- Guía del editor Markdown en Spaces: `docs/MARKDOWN_EDITOR.md`
- Guía de arrastre multimedia en Spaces: `docs/ARRASTRE_MULTIMEDIA.md`
- Estructura de tests frontend: `test/README.md`
- Guía de design tokens: `README-DESIGN-TOKENS.md`

## Estructura del Proyecto

```
src/
├── app/
│   ├── models/        # Interfaces y tipos
│   ├── services/      # Servicios HTTP y lógica
│   ├── guards/        # Guards de autenticación
│   ├── interceptors/  # Interceptores HTTP
│   ├── pages/         # Páginas principales
│   └── app.*          # Componente raíz
├── assets/            # Archivos estáticos
└── styles.scss        # Estilos globales
```

## Dependencias

- Angular 19
- TypeScript 5.7
- RxJS 7.8
- @microsoft/signalr 10.0

## Compilación para Producción

```bash
npm run build
```

El resultado se genera en la carpeta `dist/`.
