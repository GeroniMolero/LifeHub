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
- **Sistema de Amigos**: Solicitudes de amistad y gestión de amigos
- **Espacios creativos**: CRUD de espacios con editor Markdown y recursos embebidos
- **Documentos**: Crea, edita y descarga documentos en línea
- **Chat en Tiempo Real**: En desarrollo (infraestructura backend lista, sin interfaz)
- **Recomendaciones**: En desarrollo (API backend lista, sin interfaz)
- **Música**: En desarrollo (API backend lista, sin interfaz)

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
- @microsoft/signalr 8.0

## Compilación para Producción

```bash
npm run build
```

El resultado se genera en la carpeta `dist/`.
