# LifeHub Frontend

Frontend de LifeHub desarrollado con Angular 19

## Instalación

```bash
npm install
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
- **Chat en Tiempo Real**: Comunicación instantánea con SignalR
- **Recomendaciones**: Recomienda películas y libros
- **Documentos**: Crea y edita documentos en línea
- **Música**: Registro de archivos de música locales

## Documentación

- Guía de arrastre multimedia en Spaces: `src/app/pages/spaces/EXPLICACION_ARRASTRE_MULTIMEDIA.md`
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
