# Editor Markdown en Spaces

## Objetivo

Este documento explica como funciona el editor de texto Markdown usado en la vista de espacios creativos del frontend.

## Archivos principales

- `src/app/pages/spaces/space-workspace/space-workspace.component.ts`
- `src/app/pages/spaces/space-workspace/space-workspace.component.html`
- `src/app/pages/spaces/components/space-editor-main/space-editor-main.component.ts`
- `src/app/pages/spaces/components/space-editor-main/space-editor-main.component.html`
- `src/app/pages/spaces/components/space-editor-main/space-editor-main.component.scss`
- `test/unit/spaces/space-workspace.component.spec.ts`

## Arquitectura general

El editor Markdown se reparte entre dos componentes:

### 1. `SpaceWorkspaceComponent`

Es el componente contenedor y concentra la logica de negocio del documento.

Responsabilidades principales:

- Cargar y seleccionar documentos del espacio.
- Mantener el formulario reactivo `editDocumentForm`.
- Controlar la pestaña activa: `code` o `preview`.
- Convertir Markdown a HTML.
- Sanitizar el HTML final antes de renderizarlo.
- Guardar cambios en backend mediante `DocumentService`.

### 2. `SpaceEditorMainComponent`

Es el componente visual del editor.

Responsabilidades principales:

- Mostrar campos de titulo y descripcion.
- Mostrar el `textarea` cuando la pestaña activa es `code`.
- Mostrar el preview HTML cuando la pestaña activa es `preview`.
- Emitir eventos al componente padre (`setActiveTab`, `saveDocument`, `deleteDocument`).

En este diseño, el componente hijo no interpreta Markdown. Solo renderiza lo que ya le entrega el componente padre en `renderedPreview`.

## Flujo del contenido

### Entrada de texto

El contenido editable vive en el control reactivo:

- `editDocumentForm.get('content')`

Ese contenido se edita en el `textarea` del componente `SpaceEditorMainComponent`.

### Cuándo se recalcula el preview

El HTML de preview se recalcula en `updateRenderedPreview()`.

Actualmente se invoca en estos momentos:

- Al seleccionar un documento con `selectDocument()`.
- Al cambiar a la pestaña `preview` con `setActiveTab('preview')`.
- Después de guardar un documento con `saveDocument()`.

Esto significa que el sistema **no hace preview en tiempo real mientras el usuario escribe**. El refresh del preview ocurre al entrar en la pestaña de preview o después de guardar.

## Pipeline de renderizado

El flujo es este:

1. El usuario escribe Markdown en el `textarea`.
2. `SpaceWorkspaceComponent` toma el contenido del formulario.
3. `renderMarkdownToHtml(markdown)` transforma el texto usando `marked`.
4. Se aplican postprocesos de seguridad y compatibilidad.
5. Angular sanitiza el HTML final con `DomSanitizer.sanitize(SecurityContext.HTML, html)`.
6. El resultado se entrega al hijo como `renderedPreview`.
7. El hijo lo inserta con `[innerHTML]` dentro de `.rendered-markdown`.

## Parser Markdown

El parser actual es la libreria `marked`.

Configuracion usada:

- `gfm: true`: activa sintaxis GitHub Flavored Markdown.
- `breaks: true`: convierte saltos de linea simples en `<br>` dentro de parrafos.
- `renderer: this.markdownRenderer`: usa un renderer personalizado para endurecer la seguridad.

## Medidas de seguridad

El editor aplica varias capas defensivas.

### 1. Escape de HTML crudo desde Markdown

En el constructor se crea un renderer de `marked`:

- `private readonly markdownRenderer = new marked.Renderer();`

Y se redefine el callback `html`:

- `this.markdownRenderer.html = ({ text }) => this.escapeHtml(text);`

Con eso, bloques o tags HTML escritos directamente en Markdown no se renderizan como HTML ejecutable. Se convierten en texto escapado.

Ejemplo:

```md
<script>alert(1)</script>
```

Se convierte en una salida escapada tipo:

```html
&lt;script&gt;alert(1)&lt;/script&gt;
```

### 2. Neutralizacion de links `javascript:`

Despues del parseo se aplica un reemplazo defensivo sobre los enlaces generados:

- Todo `href="javascript:..."` se sustituye por `href="#"`.

Esto evita que un markdown como el siguiente genere un enlace ejecutable:

```md
[click](javascript:alert(1))
```

### 3. Sanitizacion final con Angular

Aunque ya existe filtrado previo, el HTML final pasa ademas por:

- `this.sanitizer.sanitize(SecurityContext.HTML, html)`

Esta es la ultima barrera antes de llegar a `[innerHTML]`.

## Compatibilidad especial con task lists

`marked` genera task lists GFM usando inputs checkbox HTML.

Ejemplo conceptual del HTML intermedio:

```html
<li><input checked="" disabled="" type="checkbox"> Tarea</li>
```

Angular sanitiza esos inputs y puede eliminar elementos que no interesan para el preview. Para mantener una representacion estable y visualmente clara, el sistema hace un postproceso y reemplaza los checkboxes por simbolos:

- Tarea completada: `☑`
- Tarea pendiente: `☐`

Ademas añade:

- clase `task-item`
- span `task-box`

Eso permite estilizar la lista correctamente en SCSS.

## Estilos del preview

Los estilos del HTML renderizado viven en:

- `space-editor-main.component.scss`

Como el contenido entra por `[innerHTML]`, los estilos usan:

- `:host ::ng-deep .rendered-markdown`

Esto asegura que headings, listas, blockquotes, tablas, `pre`, `code` y task lists se vean correctamente aunque el HTML sea dinamico.

Elementos cubiertos por estilos:

- `h1` a `h6`
- `p`
- `ul`, `ol`, `li`
- `blockquote`
- `pre`, `code`
- `a`
- `hr`
- `table`, `th`, `td`
- `.task-item`
- `.task-box`

## Relacion entre edicion y preview

El editor tiene dos modos visibles:

- `Code`: muestra el `textarea` editable.
- `Preview`: muestra el HTML renderizado.

No existe un modo de edicion dividida tipo side-by-side en este momento. El usuario alterna entre pestañas.

## Persistencia

El documento se guarda con `saveDocument()`.

Flujo de guardado:

1. Se toma el valor del formulario reactivo.
2. Se construye `UpdateDocumentRequest`.
3. Se llama a `documentService.updateDocument(...)`.
4. Al responder el backend, se actualiza el documento seleccionado.
5. Se vuelve a generar el preview con `updateRenderedPreview()`.

## Tests actuales

Existe una spec en:

- `test/unit/spaces/space-workspace.component.spec.ts`

Casos cubiertos:

- Escape de HTML crudo.
- Neutralizacion de links `javascript:`.
- Render de bloques fenced code.
- Render de task lists checked y unchecked.

## Limitaciones actuales

A dia de hoy el editor tiene estas limitaciones conocidas:

- No hay preview en vivo mientras se escribe.
- No hay toolbar de formato Markdown.
- No hay resaltado de sintaxis en el `textarea` de edicion.
- No hay soporte de side-by-side editor/preview.
- Existe un metodo `looksLikeHtml()` en `SpaceWorkspaceComponent`, pero ya no participa en el flujo actual del preview.

## Resumen tecnico

El comportamiento actual es deliberadamente simple:

- Edicion en `textarea`.
- Parseo Markdown con `marked`.
- Endurecimiento de seguridad en el renderer y en postproceso.
- Sanitizacion final con Angular.
- Render HTML estilizado en la vista preview.

Esto ofrece un preview Markdown razonablemente completo, visualmente consistente y con protecciones basicas contra inyeccion de HTML o URLs peligrosas.
