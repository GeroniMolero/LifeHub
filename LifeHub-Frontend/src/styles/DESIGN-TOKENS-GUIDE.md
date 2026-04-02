/* 
  GUÍA DE USO - DESIGN TOKENS Y VARIABLES SCSS
  =============================================
  
  Este archivo documenta cómo utilizar los design tokens definidos en 
  src/styles/design-tokens.scss en los componentes de la aplicación.
  
  Los design tokens centralizan todos los valores de diseño (colores, 
  espaciado, tipografía, etc.) para mantener consistencia visual en 
  toda la aplicación.
*/

// ============================================================
// 1. IMPORTAR LOS DESIGN TOKENS EN TUS COMPONENTES
// ============================================================

// En el archivo SCSS de tu componente, importa los design tokens:

@import 'path/to/styles/design-tokens';

// ============================================================
// 2. UTILIZAR VARIABLES DE COLOR
// ============================================================

// EJEMPLO: Layout Sidebar

.sidebar-nav-item {
  color: $color-text-primary;        // Color de texto primario
  background: $color-background;     // Fondo
  border: 1px solid $color-border;   // Borde

  &:hover {
    background: $color-background-alt;
    color: $color-primary;
  }

  &.active {
    background-color: $color-primary;
    color: $color-white;
  }
}

// Cuando quieras cambiar el azul primario en toda la app, 
// solo necesitas cambiar $color-primary en design-tokens.scss

// ============================================================
// 3. UTILIZAR ESPACIADO (PADDING Y MARGIN)
// ============================================================

// Escala de espaciado: $space-1 (4px) hasta $space-16 (64px)

.header {
  padding: $space-4 $space-6;           // Equivalente a 16px 24px
  margin-bottom: $space-8;              // Equivalente a 32px
}

.card-content {
  padding: $spacing-lg;                 // Alias para layouts grandes
  margin: $spacing-md $spacing-lg;      // Combinación de valores
  gap: $space-3;                        // Para flexbox/grid
}

// ============================================================
// 4. UTILIZAR TIPOGRAFÍA
// ============================================================

.page-title {
  font-size: $font-size-3xl;            // 32px
  font-weight: $font-weight-bold;       // 700
  line-height: $line-height-tight;      // 1.2
  color: $color-text-primary;
}

.card-description {
  font-size: $font-size-sm;             // 14px
  color: $color-text-secondary;         // Gris más claro
  font-weight: $font-weight-normal;
  line-height: $line-height-relaxed;    // 1.75
}

// ============================================================
// 5. UTILIZAR BORDES Y ESQUINAS
// ============================================================

.button-primary {
  border-radius: $border-radius-base;   // 4px
  border: $border-width-base solid $color-primary;
}

.card-large {
  border-radius: $border-radius-lg;     // 8px
  padding: $space-6;
}

.badge {
  border-radius: $border-radius-full;   // 9999px (círculo)
  padding: $space-2 $space-3;
}

// ============================================================
// 6. UTILIZAR SOMBRAS
// ============================================================

.card {
  box-shadow: $shadow-base;             // Sombra suave

  &:hover {
    box-shadow: $shadow-md;             // Sombra más grande en hover
  }
}

.dropdown-menu {
  box-shadow: $shadow-lg;               // Sombra grande
}

// ============================================================
// 7. UTILIZAR TRANSICIONES
// ============================================================

.button {
  transition: background-color $transition-base,
              color $transition-base,
              transform $transition-fast;

  &:hover {
    background-color: $color-primary;
  }
}

.sidebar {
  transition: width $transition-base,
              margin-left $transition-base;
}

// ============================================================
// 8. UTILIZAR MEDIA QUERIES CON BREAKPOINTS
// ============================================================

.responsive-layout {
  display: grid;
  grid-template-columns: 1fr;

  @include respond-to('md') {
    grid-template-columns: 1fr 1fr;
  }

  @include respond-to('lg') {
    grid-template-columns: 1fr 1fr 1fr;
  }
}

// ============================================================
// 9. UTILIZAR MIXINS ÚTILES
// ============================================================

// Mixin para flexbox centrado
.modal-center {
  @include flex-center;
  min-height: 100vh;
}

// Mixin para truncar texto
.text-truncated {
  @include truncate;
}

// Mixin para resetear listas
.navigation {
  @include list-reset;
}

// Mixin para enfoque accesible
.button:focus {
  @include focus-ring;
}

// ============================================================
// 10. UTILIZAR Z-INDEX
// ============================================================

.dropdown {
  z-index: $z-index-dropdown;         // 1000
}

.modal-backdrop {
  z-index: $z-index-modal-backdrop;   // 1040
}

.modal {
  z-index: $z-index-modal;            // 1050
}

.tooltip {
  z-index: $z-index-tooltip;          // 1070
}

// ============================================================
// 11. UTILIZAR DIMENSIONES PREDEFINIDAS
// ============================================================

.app-sidebar {
  width: $sidebar-width-desktop;      // 240px
  
  @include respond-to('md') {
    width: $sidebar-width-mobile;     // 280px en mobile
  }
}

.app-header {
  height: $header-height;             // 60px
}

// ============================================================
// EJEMPLO COMPLETO: COMPONENTE PERSONALIZADO
// ============================================================

// Archivo: src/app/components/custom-card/custom-card.component.scss

@import 'path/to/styles/design-tokens';

.custom-card {
  background: $color-white;
  border-radius: $border-radius-lg;
  box-shadow: $shadow-base;
  padding: $spacing-lg;
  border: $border-width-base solid $color-border-light;
  transition: all $transition-base;

  &:hover {
    box-shadow: $shadow-md;
    border-color: $color-primary;
  }

  .card-header {
    margin-bottom: $spacing-md;
    padding-bottom: $space-4;
    border-bottom: $border-width-base solid $color-divider;

    h2 {
      font-size: $font-size-2xl;
      font-weight: $font-weight-bold;
      color: $color-text-primary;
      margin: 0;
    }
  }

  .card-content {
    margin-bottom: $spacing-md;

    p {
      color: $color-text-secondary;
      font-size: $font-size-sm;
      line-height: $line-height-relaxed;
      margin: 0 0 $space-3 0;

      &:last-child {
        margin-bottom: 0;
      }
    }
  }

  .card-footer {
    display: flex;
    gap: $space-3;
    padding-top: $space-4;

    button {
      flex: 1;
      padding: $space-2 $space-4;
      border-radius: $border-radius-base;
      border: none;
      cursor: pointer;
      font-weight: $font-weight-medium;
      transition: all $transition-fast;

      &.btn-primary {
        background-color: $color-primary;
        color: $color-white;

        &:hover {
          background-color: $color-primary-hover;
        }
      }

      &.btn-secondary {
        background-color: $color-secondary;
        color: $color-white;

        &:hover {
          background-color: $color-secondary-hover;
        }
      }
    }
  }

  // Responsive
  @include respond-to('md') {
    padding: $spacing-xl;

    .card-header h2 {
      font-size: $font-size-3xl;
    }
  }
}

// ============================================================
// VENTAJAS DE USAR DESIGN TOKENS
// ============================================================

/*
  ✓ Consistencia: Todos los colores y espacios son uniformes
  ✓ Mantenibilidad: Cambiar un color globalmente es trivial
  ✓ Escalabilidad: Fácil de extender con nuevas variables
  ✓ Reutilización: Evita duplicación de valores
  ✓ Documentación: Código más legible y autodocumentado
  ✓ Temas: Permite cambiar temas fácilmente en el futuro
  ✓ Accesibilidad: Contrasts y proporciones consistentes
  ✓ Performance: Reduce tamaño del CSS compilado
*/

// ============================================================
// PRÓXIMOS PASOS
// ============================================================

/*
  1. Importa los design tokens en TODOS tus componentes SCSS
  2. Reemplaza valores hardcodeados con variables correspondientes
  3. Cuando necesites un nuevo color/valor, defínelo primero en design-tokens.scss
  4. Revisa design-tokens.scss regularmente para mantenerlo actualizado
  5. Considera agregar nuevas variables según sea necesario
*/
