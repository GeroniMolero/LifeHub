# Plan de Actualizacion de LifeHub tomando como referencia LifeHub.pdf

Fecha de analisis: 2026-04-02
Documento de referencia: LifeHub.pdf (27 paginas)

## Estado de ejecucion (actualizado 2026-04-02)

### ✅ Completado en esta sesión de trabajo

**Backend**
- ✅ Centralización de auth/session checks → ApiControllerBase (refactor)
- ✅ Homogeneización de respuestas de error → ApiErrorDto + manejo centralizado
- ✅ Activity logging integrado en todos los controladores (8 controladores)
- ✅ Migración EF Core: DocumentVersions + relaciones CreativeSpace/Documents
- ✅ Endpoints de versionado (snapshot, restore) en DocumentVersionsController
- ✅ Endpoints de permisos completamente implementados en CreativeSpacesController
- ✅ Utilidades transversales: ClaimsPrincipalExtensions, IActivityLogService

**Frontend**
- ✅ Modelos TypeScript: CreativeSpace, DocumentVersion, SpacePrivacy enum
- ✅ Servicios HTTP: CreativeSpaceService, DocumentVersionService
- ✅ Componente SpacesComponent: CRUD completo (crear, listar, editar, eliminar)
- ✅ Integración con API backend (base URL config)
- ✅ Formularios reactivos para espacios con validaciones

**DevOps/Organización**
- ✅ Scripts de startup local: dev-local.ps1, stop-local.ps1
- ✅ Organización de pruebas: test/diagnostics/ con smoke tests
- ✅ .gitignore expandido (25+ patrones) para artefactos transientes
- ✅ 5 commits semánticos creados con claridad de intención

Pendiente inmediato para fidelidad completa al PDF:
- Integración real de DocumentsComponent con DocumentService (eliminar mocks)
- Módulo de historial de versiones (listar snapshots + restaurar)
- UI de compartición/permisos (grant/revoke editor/viewer)
- Panel admin básico (usuarios + logs de actividad)
- Perfil público limitado

## 1) Diagnostico rapido

El proyecto actual tiene dos lineas:
- Legacy/MVP inicial: LifeHub-back-v0 + LifeHub-front-v0
- Implementacion activa: LifeHub-Backend + LifeHub-Frontend

La implementacion activa ya cubre gran parte del nucleo del PDF (autenticacion JWT, espacios creativos, versionado, permisos, logs, Docker), pero existe desalineacion entre:
- Lo implementado en backend
- Lo expuesto en frontend
- Lo documentado en README

Tambien hay modulos fuera del alcance MVP del PDF (chat, amigos, recomendaciones), mientras faltan piezas del MVP formal (panel admin basico y flujo publico claramente definido end-to-end).

## 2) Matriz de alineacion PDF vs estado actual

### 2.1 Requisitos funcionales del PDF

1. Registro/inicio/cierre de sesion
- Estado: Cumplido
- Evidencia: AuthController, frontend auth y guards

2. Creacion de espacios creativos
- Estado: Parcial
- Evidencia: API existe (CreativeSpacesController)
- Brecha: No hay seccion de espacios creativos visible como modulo dedicado en rutas frontend

3. Edicion de documentos Markdown
- Estado: Parcial (mejorado)
- Evidencia: API completa, DocumentService lista, modelos TypeScript definidos
- Brecha: UI aún en modo demo (sin editor WYSIWYG, sin integracion real de datos)

4. Guardado de versiones y restauracion
- Estado: Parcial (mejorado)
- Evidencia: DocumentVersionsController + endpoints (snapshot + restore POST/GET)
- Brecha: Sin módulo frontend visible para historial ni interfaz de restauración

5. Compartir espacios con usuarios de confianza
- Estado: Parcial (mejorado)
- Evidencia: Endpoints de permisos en CreativeSpacesController, ServicioTipado
- Brecha: Sin UX dedicada (formulario share, lista de miembros, grant/revoke permisos)

6. Privacidad (privado/compartido)
- Estado: Parcial (mejorado)
- Evidencia: SpacePrivacy enum, servicios tipados, selectores en UI
- Brecha: Validación de negocio incompleta (post-save sync)

7. Recursos externos embebidos
- Estado: Parcial
- Evidencia: Existe modulo de musica, pero no un modulo transversal de recursos embebidos por espacio/documento

8. Perfiles publicos limitados
- Estado: Parcial
- Evidencia: Existen campos de perfil y bandera IsPublicProfileVisible en espacio
- Brecha: Falta endpoint/route publica claramente definida para consumo anonimo con contenido limitado

9. Panel de administracion basico
- Estado: No cumplido (funcionalmente)
- Evidencia: Roles y seed de Admin existen
- Brecha: No hay endpoints admin dedicados ni pagina admin en frontend

10. Registro de actividad
- Estado: Parcial
- Evidencia: ActivityLog se crea en operaciones de espacios/versiones
- Brecha: Cobertura incompleta en todos los modulos y sin vista admin/auditoria

### 2.2 Requisitos no funcionales del PDF

1. JWT, REST, BD relacional, Docker/Compose
- Estado: Cumplido

2. Validacion de datos y control de errores
- Estado: Parcial
- Brecha: Homogeneizar validaciones DTO, codigos de error y respuestas estandar

3. Interfaz web accesible y coherente con MVP
- Estado: Parcial
- Brecha: Falta cohesion funcional alrededor del dominio principal (espacios/documentos/versiones)

## 3) Riesgo principal detectado

El mayor riesgo no es tecnico, sino de enfoque: el producto implementado se ha ampliado en funcionalidades secundarias antes de cerrar completamente el eje del MVP del PDF (espacios creativos + Markdown + versionado + comparticion + admin basico).

## 4) Plan de actualizacion recomendado (por fases)

## Fase A - Cierre del nucleo MVP del PDF (alta prioridad)

Objetivo: dejar completamente operativo el flujo principal definido en el documento.

Backend
- Completar y estandarizar endpoints de espacios/documentos/versiones/permisos.
- Asegurar que toda operacion clave registre ActivityLog.
- Crear endpoints publicos limitados para perfil/espacios visibles.

Frontend
- Integrar la pagina de documentos con API real (eliminar mocks).
- Crear modulo de espacios creativos (listado, alta, edicion, privacidad).
- Crear vista de historial de versiones (listar snapshots, restaurar).
- Implementar comparticion de espacios (alta/baja permisos, nivel editor/visor).

## Fase B - Panel de administracion basico (obligatorio segun PDF)

Backend
- Endpoints protegidos por rol Admin: usuarios, actividad, recursos.
- Politicas de autorizacion por rol.

Frontend
- Route/admin con guard de rol.
- Vistas minimas: listado usuarios, logs de actividad, acciones basicas de moderacion.

## Fase C - Perfil publico limitado y recursos embebidos

Backend
- Endpoint anonimo para perfil publico limitado.
- Filtros estrictos de contenido publico.

Frontend
- Ruta publica de perfil y seccion de contenido visible.
- Visualizacion de recursos embebidos permitidos (enlaces legales).

## Fase D - Calidad y cierre de proyecto

- Pruebas backend (al menos smoke tests de controladores clave).
- Pruebas frontend de rutas criticas.
- Actualizacion de documentacion para reflejar realidad funcional.
- Congelar alcance MVP y mover extras (chat/amigos/recomendaciones) a roadmap posterior si hace falta.

## 5) Backlog priorizado (sprint sugerido)

Sprint 1 (inmediato)
- Integrar DocumentsComponent con DocumentService (CRUD real)
- Crear pagina CreativeSpaces y su servicio
- Crear pagina DocumentVersions (historial + restaurar)

Sprint 2
- Comparticion/permisos en UI
- Endpoints/UX de perfil publico

Sprint 3
- Panel admin basico (usuarios + logs)
- Hardening de validaciones y errores

## 6) Criterios de aceptacion de actualizacion

- El flujo end-to-end "crear espacio -> crear documento Markdown -> guardar version -> restaurar version -> compartir" funciona desde UI.
- Existe panel admin minimo usable por rol Admin.
- Existe ruta publica limitada de perfil/contenido.
- Logs auditables para operaciones clave.
- README principal describe exactamente las capacidades reales del sistema.

## 7) Siguiente ejecucion recomendada

Orden de implementacion sugerido:
1) Documents + Versiones en frontend (porque backend ya esta avanzado)
2) Spaces + permisos en frontend
3) Admin endpoints + admin UI
4) Perfil publico limitado
5) Actualizacion final de documentacion y pruebas
