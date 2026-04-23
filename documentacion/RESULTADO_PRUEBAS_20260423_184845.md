# Informe de Pruebas Automaticas -- LifeHub

**Fecha:** 2026-04-23 18:48:47  
**Entorno:** http://localhost:5000/api  
**Usuario de prueba:** autotest_20260423_184845@lifehub-auto.test  
**Script:** run-tests.ps1  

---

## Resultados por modulo

### Autenticacion

| ID | Descripcion | Esperado | Real | Resultado |
|----|-------------|----------|------|-----------|
| T-AUTH-01 | Registro nuevo usuario | 200 | 200 | PASS |
| T-AUTH-02 | Registro email duplicado | 400 | 400 | PASS |
| T-AUTH-03 | Registro email con formato invalido | 400 | 400 | PASS |
| T-AUTH-04 | Login correcto - obtener token | 200 | 200 | PASS |
| T-AUTH-05 | Login contrasena incorrecta | 401 | 401 | PASS |
| T-AUTH-06 | Ruta protegida sin token -> 401 | 401 | 401 | PASS |
| T-AUTH-07 | Ruta protegida con token invalido -> 401 | 401 | 401 | PASS |
| T-AUTH-08 | Login admin (setup para tests admin) | 200 | 200 | PASS |

### Espacios Creativos

| ID | Descripcion | Esperado | Real | Resultado |
|----|-------------|----------|------|-----------|
| T-SPACE-01 | Crear espacio OK | 201 | 201 | PASS |
| T-SPACE-02 | Crear espacio sin nombre -> error | 400 | 400 | PASS |
| T-SPACE-03 | Editar espacio OK | 200 | 200 | PASS |
| T-SPACE-04 | Editar espacio de otro usuario -> 404 | 404 | 404 | PASS |
| T-SPACE-05 | Acceso a espacios autenticado -> 200 | 200 | 200 | PASS |

### Documentos y Versiones

| ID | Descripcion | Esperado | Real | Resultado |
|----|-------------|----------|------|-----------|
| T-DOC-01 | Crear documento OK | 201 | 201 | PASS |
| T-DOC-02 | Crear documento sin titulo -> error | 400 | 400 | PASS |
| T-DOC-03 | Editar documento OK | 200 | 200 | PASS |
| T-DOC-04 | Contenido XSS almacenado (backend no sanitiza) | 200 | 200 | PASS |
| T-DOC-05 | Crear snapshot de version | 201 | 201 | PASS |
| T-DOC-06 | Listar versiones del documento | 200 | 200 | PASS |
| T-DOC-07 | Snapshot de documento ajeno -> 403 | 403 | 403 | PASS |
| T-DOC-08 | Restaurar version anterior | 200 | 200 | PASS |
| T-DOC-09 | Eliminar documento OK | 204 | 204 | PASS |

### Panel de Administracion

| ID | Descripcion | Esperado | Real | Resultado |
|----|-------------|----------|------|-----------|
| T-ADMIN-01 | Acceso admin sin token -> 401 | 401 | 401 | PASS |
| T-ADMIN-02 | Acceso admin con rol User -> 403 | 403 | 403 | PASS |
| T-ADMIN-03 | Acceso admin con rol Admin -> 200 | 200 | 200 | PASS |
| T-ADMIN-04 | Anadir dominio permitido | 201 | 201 | PASS |
| T-ADMIN-05 | Desactivar dominio | 200 | 200 | PASS |
| T-ADMIN-06 | Listar usuarios (admin) | 200 | 200 | PASS |

### Seguridad

| ID | Descripcion | Esperado | Real | Resultado |
|----|-------------|----------|------|-----------|
| T-SEC-01 | Token expirado/invalido -> 401 | 401 | 401 | PASS |
| T-SEC-02 | Token User en endpoint Admin -> 403 | 403 | 403 | PASS |

---

## Resumen

| Modulo | Total | PASS | FAIL | SKIP |
|--------|-------|------|------|------|
| Autenticacion | 8 | 8 | 0 | 0 |
| Espacios Creativos | 5 | 5 | 0 | 0 |
| Documentos y Versiones | 9 | 9 | 0 | 0 |
| Panel de Administracion | 6 | 6 | 0 | 0 |
| Seguridad | 2 | 2 | 0 | 0 |
| **TOTAL** | **30** | **30** | **0** | **0** |

---

## Incidencias

Sin incidencias. Todos los tests ejecutados han resultado PASS.
