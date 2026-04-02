# 🎨 GUÍA DE ESTILOS - DESIGN TOKENS

Este documento describe cómo utilizar el sistema de **design tokens** (tokens de diseño) establecidos en el proyecto LifeHub Frontend para mantener une consistencia visual en toda la aplicación.

## 📋 Contenidos

- [Descripción General](#descripción-general)
- [Estructura de Archivos](#estructura-de-archivos)
- [Tokens Disponibles](#tokens-disponibles)
- [Cómo Importar en tus Componentes](#cómo-importar-en-tus-componentes)
- [Ejemplos Prácticos](#ejemplos-prácticos)
- [Mejores Prácticas](#mejores-prácticas)

---

## 📌 Descripción General

Un **design token** es un valor reutilizable (color, espaciado, tipografía, etc.) que se define una única vez y se usa en toda la aplicación. En lugar de escribir `#3498db` en cada componente, utilizamos `$color-primary`, que es más legible y más fácil de mantener.

### Ventajas

✅ **Consistencia Visual**: Todos los colores, espacios y estilos son uniformes
✅ **Mantenibilidad**: Cambiar un color globalmente toma un segundo
✅ **Escalabilidad**: Fácil de agregar nuevos tokens
✅ **Documentación**: El código es autodocumentado y legible
✅ **Reutilización**: Evita duplicación de valores
✅ **Temas**: Permite cambiar temas fácilmente en el futuro

---

## 📂 Estructura de Archivos

```
LifeHub-Frontend/src/
├── styles/
│   ├── design-tokens.scss          ← Archivo principal con todas las variables
│   ├── DESIGN-TOKENS-GUIDE.md      ← Ejemplos de uso
│   └── ...
├── styles.scss                      ← Estilos globales (importa design-tokens)
└── app/
    ├── layouts/
    │   └── main-layout/
    │       └── components/
    │           ├── layout-header/
    │           │   └── layout-header.component.scss     ← Usar design-tokens
    │           └── layout-sidebar/
    │               └── layout-sidebar.component.scss    ← Usar design-tokens
    └── ... otros componentes
```

---

## 🎯 Tokens Disponibles

### 1. **Colores (Color Palette)**

#### Primarios
```scss
$color-primary: #3498db;          // Azul principal
$color-primary-hover: #2980b9;    // Azul más oscuro (hover)
$color-primary-dark: #1e5f9e;     // Azul oscuro
$color-primary-light: #5dade2;    // Azul claro
```

#### De Estado
```scss
$color-success: #27ae60;           // Verde (éxito)
$color-warning: #f39c12;           // Naranja (advertencia)
$color-danger: #e74c3c;            // Rojo (error)
$color-info: #3498db;              // Información
```

#### Neutrales
```scss
$color-text-primary: #333333;      // Texto principal
$color-text-secondary: #666666;    // Texto secundario
$color-background: #f5f5f5;        // Fondo
$color-border: #ddd;               // Bordes
$color-white: #ffffff;             // Blanco
$color-black: #000000;             // Negro
```

### 2. **Tipografía (Typography)**

#### Font Family
```scss
$font-family-base: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, ...
$font-family-mono: 'Courier New', Courier, monospace
```

#### Tamaños
```scss
$font-size-xs: 0.75rem;      // 12px
$font-size-sm: 0.875rem;     // 14px
$font-size-base: 1rem;       // 16px
$font-size-lg: 1.125rem;     // 18px
$font-size-xl: 1.25rem;      // 20px
$font-size-2xl: 1.5rem;      // 24px
$font-size-3xl: 2rem;        // 32px
```

#### Pesos
```scss
$font-weight-light: 300;
$font-weight-normal: 400;
$font-weight-medium: 500;
$font-weight-semibold: 600;
$font-weight-bold: 700;
```

### 3. **Espaciado (Spacing)**

Escala de 4px base:
```scss
$space-1: 0.25rem;   // 4px
$space-2: 0.5rem;    // 8px
$space-3: 0.75rem;   // 12px
$space-4: 1rem;      // 16px
$space-5: 1.25rem;   // 20px
$space-6: 1.5rem;    // 24px
$space-8: 2rem;      // 32px
$space-10: 2.5rem;   // 40px
$space-12: 3rem;     // 48px
$space-16: 4rem;     // 64px
```

**Aliases comunes:**
```scss
$spacing-xs: $space-2;      // 8px
$spacing-sm: $space-3;      // 12px
$spacing-md: $space-4;      // 16px
$spacing-lg: $space-6;      // 24px
$spacing-xl: $space-8;      // 32px
```

### 4. **Bordes (Border Radius)**

```scss
$border-radius-sm: 2px;
$border-radius-base: 4px;
$border-radius-md: 6px;
$border-radius-lg: 8px;
$border-radius-xl: 12px;
$border-radius-2xl: 16px;
$border-radius-full: 9999px;      // Circular
```

### 5. **Sombras (Shadows)**

```scss
$shadow-sm: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
$shadow-base: 0 2px 8px rgba(0, 0, 0, 0.1);
$shadow-md: 0 4px 12px rgba(0, 0, 0, 0.15);
$shadow-lg: 0 8px 24px rgba(0, 0, 0, 0.2);
$shadow-xl: 0 12px 48px rgba(0, 0, 0, 0.25);
```

### 6. **Transiciones (Transitions)**

```scss
$transition-fast: 150ms ease-in-out;
$transition-base: 300ms ease-in-out;
$transition-slow: 500ms ease-in-out;
```

### 7. **Breakpoints Responsive**

```scss
$breakpoint-sm: 640px;      // Small
$breakpoint-md: 960px;      // Medium
$breakpoint-lg: 1200px;     // Large
$breakpoint-xl: 1440px;     // Extra Large
```

### 8. **Z-Index (Capas de apilamiento)**

```scss
$z-index-dropdown: 1000;
$z-index-sticky: 1020;
$z-index-fixed: 1030;
$z-index-modal: 1050;
$z-index-tooltip: 1070;
```

---

## 💻 Cómo Importar en tus Componentes

### Para componentes en `src/app/`

**Ejemplo: `src/app/components/card/card.component.scss`**

```scss
// Importar design tokens
@import 'src/styles/design-tokens';

// Luego usar las variables
.card {
  background: $color-white;
  border-radius: $border-radius-lg;
  box-shadow: $shadow-base;
  padding: $spacing-lg;
  margin-bottom: $space-4;
  color: $color-text-primary;
  font-family: $font-family-base;
  font-size: $font-size-base;
}
```

### Para componentes anidados

**Ejemplo: `src/app/layouts/main-layout/components/layout-header/layout-header.component.scss`**

Contar los niveles hacia arriba:
- layout-header/ → `../`
- components/ → `../` (total 2)
- main-layout/ → `../` (total 3)
- layouts/ → `../` (total 4)
- app/ → `../` (total 5)
- src/ → Luego `styles/design-tokens`

```scss
@import '../../../../../styles/design-tokens';
```

---

## 📖 Ejemplos Prácticos

### Ejemplo 1: Botón Primario

```scss
@import 'src/styles/design-tokens';

.btn-primary {
  background-color: $color-primary;
  color: $color-white;
  padding: $space-2 $space-4;
  border-radius: $border-radius-base;
  border: none;
  cursor: pointer;
  font-size: $font-size-base;
  font-weight: $font-weight-medium;
  transition: background-color $transition-base;

  &:hover {
    background-color: $color-primary-hover;
  }

  &:active {
    background-color: $color-primary-dark;
  }
}
```

### Ejemplo 2: Card de Contenido

```scss
@import 'src/styles/design-tokens';

.content-card {
  background: $color-white;
  border-radius: $border-radius-lg;
  box-shadow: $shadow-base;
  padding: $spacing-lg;
  margin-bottom: $spacing-md;
  border: 1px solid $color-border-light;
  transition: all $transition-base;

  &:hover {
    box-shadow: $shadow-md;
    border-color: $color-primary;
  }

  .card-title {
    font-size: $font-size-2xl;
    font-weight: $font-weight-bold;
    color: $color-text-primary;
    margin: 0 0 $space-3 0;
  }

  .card-description {
    color: $color-text-secondary;
    font-size: $font-size-sm;
    line-height: 1.5;
  }
}
```

### Ejemplo 3: Layout Responsivo

```scss
@import 'src/styles/design-tokens';

.grid-layout {
  display: grid;
  grid-template-columns: 1fr;
  gap: $spacing-lg;

  @include respond-to('md') {
    grid-template-columns: 1fr 1fr;
  }

  @include respond-to('lg') {
    grid-template-columns: repeat(3, 1fr);
  }
}
```

### Ejemplo 4: Alerta de Éxito

```scss
@import 'src/styles/design-tokens';

.alert-success {
  background-color: $color-success-light;
  color: $color-success-text;
  padding: $space-4;
  border-radius: $border-radius-base;
  border-left: 4px solid $color-success;
  display: flex;
  align-items: center;
  gap: $space-3;
}
```

---

## 🔧 Mixins Disponibles

### Responsive Media Queries

```scss
@include respond-to('sm') { }    // 640px+
@include respond-to('md') { }    // 960px+
@include respond-to('lg') { }    // 1200px+
@include respond-to('xl') { }    // 1440px+
```

### Flexbox Centrado

```scss
.centered {
  @include flex-center;      // Flexbox con align y justify center
}
```

### Truncar Texto

```scss
.truncated {
  @include truncate;         // text-overflow: ellipsis + white-space: nowrap
}
```

### Focus Ring (Accesibilidad)

```scss
.button:focus {
  @include focus-ring;       // Outline para accesibilidad
}
```

### Reset de Lista

```scss
.navigation {
  @include list-reset;       // list-style: none + padding/margin reset
}
```

---

## ✅ Mejores Prácticas

### ✓ DO - Hacer

```scss
// ✓ Usar variables
.header {
  padding: $spacing-lg;
  color: $color-text-primary;
  background: $color-white;
}
```

### ✗ DON'T - No Hacer

```scss
// ✗ No hardcodear valores
.header {
  padding: 24px;
  color: #333333;
  background: #ffffff;
}
```

### Modificar Valores Globales

Si necesitas cambiar el color azul primario en toda la aplicación, solo edita una línea en `design-tokens.scss`:

```scss
// src/styles/design-tokens.scss
$color-primary: #3498db;  // Cambiar aquí
```

Todos los componentes que usen `$color-primary` se actualizarán automáticamente.

### Agregar Nuevos Tokens

Cuando necesites un nuevo valor, primero agrégalo a `design-tokens.scss`:

```scss
// src/styles/design-tokens.scss
$color-secondary: #95a5a6;
$spacing-new: 2rem;
```

Luego úsarlo en tus componentes:

```scss
.selector {
  background: $color-secondary;
  margin: $spacing-new;
}
```

---

## 📝 Referencias

- File de configuración: [`src/styles/design-tokens.scss`](./src/styles/design-tokens.scss)
- Guía detallada: [`src/styles/DESIGN-TOKENS-GUIDE.md`](./src/styles/DESIGN-TOKENS-GUIDE.md)
- Estilos globales: [`src/styles.scss`](./src/styles.scss)

---

## 🎓 Recursos

- [SASS/SCSS Official Docs](https://sass-lang.com/)
- [Design Systems Documentation](https://www.designsystems.com/)
- [Material Design System](https://material.io/design)
- [Tailwind CSS tokens](https://tailwindcss.com/docs/customization/configuration)

---

**Última actualización**: Abril 2026

> **⭐ Mantener actualizado**: Revisa `design-tokens.scss` regularmente y agrega nuevos tokens según sea necesario para mantener el sistema de diseño coherente.
