<!--
QUICK REFERENCE - DESIGN TOKENS
================================
Cheat sheet rápida de variables SCSS disponibles
Copia este archivo en tu carpeta de bookmarks o impímelo
-->

# 🚀 DESIGN TOKENS - REFERENCIA RÁPIDA

## 📌 Importar en tu Componente

```scss
// Para componentes en src/app/
@import 'src/styles/design-tokens';

// Para componentes anidados (contar niveles hacia src)
// Ejemplo: layout-header.component.scss
@import '../../../../../styles/design-tokens';
```

---

## 🎨 COLORES MÁS USADOS

| Variable | Valor | Uso |
|----------|-------|-----|
| `$color-primary` | #3498db | Botones principal, links destacados |
| `$color-primary-hover` | #2980b9 | Estado hover del primario |
| `$color-success` | #27ae60 | Mensajes de éxito, check |
| `$color-danger` | #e74c3c | Errores, botón delete |
| `$color-warning` | #f39c12 | Advertencias |
| `$color-text-primary` | #333333 | Texto normal |
| `$color-text-secondary` | #666666 | Texto secundario, hint |
| `$color-white` | #ffffff | Fondo blanco, text invert |
| `$color-background` | #f5f5f5 | Fondo general |
| `$color-border` | #ddd | Bordes estándar |

---

## 📏 ESPACIADO MÁS USADO

| Variable | Valor | Uso Común |
|----------|-------|----------|
| `$space-2` | 8px | Espacios muy pequeños |
| `$space-3` | 12px | Pequeños espacios interiores |
| `$space-4` | 16px | Espacios estándar (padding) |
| `$space-6` | 24px | Espacios medianos (margin) |
| `$space-8` | 32px | Espacios grandes |
| `$spacing-sm` | 12px | Alias: padding pequeño |
| `$spacing-md` | 16px | Alias: padding mediano |
| `$spacing-lg` | 24px | Alias: padding grande |

---

## 🔤 TIPOGRAFÍA MÁS USADA

| Variable | Valor |
|----------|-------|
| `$font-size-sm` | 14px |
| `$font-size-base` | 16px |
| `$font-size-lg` | 18px |
| `$font-size-xl` | 20px |
| `$font-size-2xl` | 24px |
| `$font-weight-normal` | 400 |
| `$font-weight-medium` | 500 |
| `$font-weight-bold` | 700 |

---

## 🎭 BORDES Y ESQUINAS

```scss
$border-radius-base: 4px;      // Estándar
$border-radius-md: 6px;        // Mediano
$border-radius-lg: 8px;        // Grande
$border-radius-full: 9999px;   // Circular
```

---

## 💫 SOMBRAS

```scss
$shadow-sm: 0 1px 2px 0 rgba(0, 0, 0, 0.05);    // Muy sutil
$shadow-base: 0 2px 8px rgba(0, 0, 0, 0.1);     // Estándar
$shadow-md: 0 4px 12px rgba(0, 0, 0, 0.15);     // Mediana
$shadow-lg: 0 8px 24px rgba(0, 0, 0, 0.2);      // Grande
```

---

## ⏱️ TRANSICIONES

```scss
$transition-fast: 150ms ease-in-out;   // Muy rápido
$transition-base: 300ms ease-in-out;   // Estándar (RECOMENDADO)
$transition-slow: 500ms ease-in-out;   // Lento
```

---

## 📱 RESPONSIVE BREAKPOINTS

```scss
$breakpoint-md: 960px;    // iPad y superior (PRINCIPALE)
$breakpoint-lg: 1200px;   // Desktop
```

Usar mixin:
```scss
@include respond-to('md') {  // Para tablet/desktop
  // estilos
}

@include respond-to('lg') {  // Para desktop grande
  // estilos
}
```

---

## 🛠️ MIXINS ÚTILES

### Flexbox Centrado
```scss
.centered {
  @include flex-center;   // Centra horizontalmente y verticalmente
}
```

### Truncar Texto (Elipsis)
```scss
.text-truncated {
  @include truncate;      // Añade puntos suspensivos
}
```

### Focus Ring (Accesibilidad)
```scss
input:focus {
  @include focus-ring;    // Outline visible para keyboard users
}
```

---

## 💡 EJEMPLOS RÁPIDOS

### Botón
```scss
.btn {
  padding: $space-2 $space-4;
  border-radius: $border-radius-base;
  font-size: $font-size-base;
  transition: all $transition-base;
}
```

### Card
```scss
.card {
  padding: $spacing-lg;
  border-radius: $border-radius-lg;
  box-shadow: $shadow-base;
  background: $color-white;
}
```

### Título
```scss
h1 {
  font-size: $font-size-2xl;
  font-weight: $font-weight-bold;
  color: $color-text-primary;
}
```

### Texto Secundario
```scss
.subtitle {
  font-size: $font-size-sm;
  color: $color-text-secondary;
  margin-top: $space-3;
}
```

---

## ❌ ERRORES COMUNES

### ❌ Antes (Sin usar tokens)
```scss
.button {
  padding: 8px 16px;
  background: #3498db;
  color: white;
  border-radius: 4px;
}
```

### ✅ Después (Con tokens)
```scss
@import 'src/styles/design-tokens';

.button {
  padding: $space-2 $space-4;
  background: $color-primary;
  color: $color-white;
  border-radius: $border-radius-base;
}
```

---

## 🎯 CUANDO AGREGAR UN NUEVO TOKEN

Hazlo **SOLO SI**:
- ✅ El valor se reutiliza en 2+ lugares
- ✅ Es un valor de diseño consistente
- ✅ Forma parte del sistema visual

No lo hagas si:
- ❌ Es un valor único para un componente
- ❌ Es un cálculo específico
- ❌ Es muy contextual

---

## 🔗 ARCHIVOS IMPORTANTES

| Archivo | Descripción |
|---------|------------|
| `src/styles/design-tokens.scss` | **ARCHIVO PRINCIPAL** - Todas las variables |
| `src/styles.scss` | Estilos globales (importa tokens) |
| `README-DESIGN-TOKENS.md` | Documentación completa |
| `src/styles/DESIGN-TOKENS-GUIDE.md` | Guía detallada con +40 ejemplos |

---

## 📞 PREGUNTAS FRECUENTES

**P: ¿Dónde defino nuevas variables?**
R: En `src/styles/design-tokens.scss`

**P: ¿Cómo importo en un componente anidado?**
R: Cuenta los `../` necesarios para llegar a `src`, luego suma `styles/design-tokens`

**P: ¿Puedo cambiar los valores por componente?**
R: No, los tokens son globales. Si necesitas algo específico, crea una **nueva variable** en design-tokens.scss

**P: ¿Qué pasa si cambio un color primario?**
R: Todos los componentes que usen `$color-primary` se actualizan automáticamente

---

**Actualizado**: Abril 2026
**Última verificación de sintaxis**: Build exitoso ✓
