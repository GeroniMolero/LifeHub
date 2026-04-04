# Guia simple: como se mueve un recurso multimedia al arrastrarlo

Este documento explica, sin tecnicismos, como funciona el movimiento de archivos multimedia (imagenes, videos o embeds) dentro del espacio de trabajo.

## 1. Que ve el usuario

Cuando un usuario:
- hace clic en la barra superior del recurso multimedia,
- mantiene pulsado,
- mueve el mouse,
- y suelta,

el recurso se desplaza por la pantalla.

## 2. Idea principal (explicado facil)

Piensa en una nota adhesiva pegada en una pizarra:
- La nota tiene una posicion (izquierda y arriba).
- Cuando la agarras, el sistema recuerda desde que punto exacto la agarraste.
- Mientras mueves la mano, la nota sigue ese movimiento.
- Cuando sueltas, se queda en ese lugar.

Eso mismo ocurre con cada tarjeta multimedia.

## 3. Que guarda el sistema internamente

Para cada recurso multimedia, la aplicacion guarda:
- posicion horizontal (x),
- posicion vertical (y),
- ancho visual,
- orden de capa (para ponerlo delante de otros).

Con esos datos, la interfaz sabe exactamente donde dibujarlo.

## 4. Flujo en 4 pasos

1. Inicio del arrastre:
- Al pulsar sobre el encabezado del recurso, se activa el modo arrastre.
- Se guarda cual recurso se esta arrastrando.
- Se calcula la distancia entre el cursor y la esquina del recurso para que no "salte".

2. Movimiento:
- Mientras el cursor se mueve, se recalcula la posicion del recurso.
- La nueva posicion se actualiza en tiempo real.

3. Pintado en pantalla:
- La interfaz aplica esas coordenadas al elemento visual.
- Por eso el recurso parece seguir al mouse de forma fluida.

4. Fin del arrastre:
- Al soltar el boton del mouse, se desactiva el modo arrastre.
- El recurso queda en su ultima posicion.

## 5. Ejemplo para una persona no tecnica

Imagina a Laura usando LifeHub:

1. Laura abre su espacio creativo y activa una imagen.
2. Ve que tapa parte del texto, asi que quiere moverla.
3. Laura hace clic en la parte superior de la tarjeta de imagen y la arrastra a la derecha.
4. La tarjeta se mueve con su cursor.
5. Laura suelta el mouse.
6. La imagen queda colocada en su nueva posicion, sin tapar el texto.

Resultado: Laura organiza visualmente su contenido sin saber programar.

## 6. Por que este comportamiento se siente natural

- No hay "saltos" al comenzar a arrastrar.
- El elemento sigue al cursor en tiempo real.
- Al soltar, se detiene inmediatamente.
- Si hay varios elementos, el que arrastras pasa al frente para que no estorbe.

## 7. Traduccion de terminos tecnicos a lenguaje simple

- Arrastrar: mover un elemento manteniendo clic.
- Coordenadas x/y: posicion en horizontal y vertical.
- Capa (zIndex): que elemento se ve por encima de otro.
- Evento de mouse: accion del usuario (pulsar, mover, soltar).

## 8. En que partes del proyecto esta esta logica

- Logica de movimiento principal: `LifeHub-Frontend/src/app/pages/spaces/space-workspace/space-workspace.component.ts`
- Vista del recurso multimedia (donde inicia el arrastre): `LifeHub-Frontend/src/app/pages/spaces/components/space-editor-main/space-editor-main.component.html`
- Logica visual del recurso en editor: `LifeHub-Frontend/src/app/pages/spaces/components/space-editor-main/space-editor-main.component.ts`

## 9. Resumen corto

El sistema recuerda donde esta cada recurso, detecta cuando lo agarras, actualiza su posicion mientras mueves el mouse y lo deja fijo al soltar. Eso permite una experiencia de arrastre intuitiva para cualquier usuario, incluso sin conocimientos tecnicos.

## 10. Explicacion tecnica (para quien quiera ver como funciona por dentro)

### Arquitectura de componentes

- `space-workspace.component.ts` actua como contenedor de estado y orquestador.
- `space-editor-main.component.html` dispara el inicio del arrastre en el encabezado de cada tarjeta visual.
- `space-editor-main.component.ts` calcula estilos de posicion (`left`, `top`, `width`, `zIndex`) usando el estado recibido.

### Estructuras de datos clave

- `visualLayouts: Map<string, VisualMediaLayout>`:
  - clave: id del recurso multimedia.
  - valor: `{ x, y, width, zIndex }`.
- `draggingMedia: { id, offsetX, offsetY } | null`:
  - indica si hay un arrastre activo.
  - guarda el desplazamiento interno del puntero respecto a la tarjeta.
- `activeVisualMediaIds: Set<string>`:
  - define que recursos visuales estan activos en el canvas.

### Flujo tecnico exacto

1. `pointerdown` en `.visual-item-handle`:
	- se emite `startDraggingMedia` con el evento y el `id` del recurso.

2. `startDraggingMedia(event, id)` en el contenedor:
	- calcula `offsetX` y `offsetY` usando el `getBoundingClientRect()` del elemento.
	- guarda `draggingMedia`.
	- llama a `bringVisualToFront(id)` para elevar `zIndex`.

3. `@HostListener('document:pointermove')`:
	- si no hay `draggingMedia`, no hace nada.
	- obtiene el rectangulo de `.main-content-canvas`.
	- actualiza coordenadas del layout:
	  - `x = max(0, clientX - canvasLeft - offsetX)`
	  - `y = max(0, clientY - canvasTop - offsetY)`
	- ese `max(0, ...)` evita salir por arriba/izquierda.

4. Pintado en vista:
	- `[ngStyle]="getVisualStyle(item.id)"` aplica las coordenadas al DOM.
	- al cambiar `x/y`, Angular refresca posicion en pantalla.

5. `@HostListener('document:pointerup')`:
	- pone `draggingMedia = null` y termina el arrastre.

### Por que no "salta" el elemento

Si no se guardara `offsetX/offsetY`, al iniciar el arrastre la esquina superior izquierda del recurso se pegaria al cursor. Con offset, el punto donde el usuario agarra el recurso se mantiene estable durante todo el movimiento.

### Capa visual y superposicion

`bringVisualToFront(id)` incrementa un contador global (`zIndexCounter`) y asigna ese nuevo valor al recurso arrastrado. Asi, el recurso activo siempre queda por encima de los demas.

### Inicializacion de layout

Cuando se agrega o carga un recurso visual, `ensureVisualLayout(reference)` crea una posicion inicial escalonada y un ancho segun el tipo de contenido (embed, imagen, video). Esto evita que todos aparezcan exactamente en el mismo punto.

### Consideraciones y limites actuales

- El limite solo evita posiciones negativas (no salir por arriba/izquierda).
- No hay tope explicito para derecha/abajo del canvas.
- El arrastre depende de `pointer` events, lo cual es adecuado para mouse y pantallas tactiles compatibles.

### Posibles mejoras tecnicas futuras

- Limitar tambien borde derecho e inferior del canvas.
- Persistir posiciones en backend para mantener layout entre sesiones.
- Agregar "snap to grid" para alinear elementos.
- Usar `requestAnimationFrame` para suavizar en cargas muy altas.
