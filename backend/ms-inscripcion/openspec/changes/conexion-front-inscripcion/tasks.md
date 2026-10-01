# Tasks: Conexión front SIPA ↔ ms-inscripcion

Hecho (sin tareas): merge datos-mock, corrección mock, backend `regla-n3-sugerencia`. Sin TDD. Spec = `frontend-enrollment-integration`.

## Batch F1: Proxy (`dev-server.py`)

- [x] 1.1 Leer `AUTH_ORIGIN` (def. :5132) y `ENROLLMENT_ORIGIN` (def. :8080), quitar `/` final. [Dev proxy routing]
- [x] 1.2 Tabla de ruteo por segmento (`p`, `p/…`, `p?…`): auth|users|roles|demo → identity; resto `/api/*` → enrollment. [Dev proxy routing]
- [x] 1.3 Un `_proxy_api()` para GET/POST/PUT/DELETE/OPTIONS solo `/api/*`; no-API mantiene 405. [Verbs and header]
- [x] 1.4 Reenviar `Accept` (def. json), `Content-Type` solo con body, `Authorization` solo si existe. [Verbs and header]
- [x] 1.5 Pasar status/Content-Type/body upstream intactos (rama `HTTPError`). [Other errors]
- [x] 1.6 `URLError`/`TimeoutError`/`ConnectionError` → 502 problem+json `PROXY_SIN_CONEXION` con origen en `detail`. [Target down]
- [x] 1.7 Imprimir tabla de ruteo al arrancar. [Dev proxy routing]

## Batch F2: `app.js` + `app.css`

- [x] 2.1 `API_CONFIG` (estudianteId 1, periodo 2026-2, endpoints, `useMock:false`, `mockDelayMs`); sin duplicar valores. [API_CONFIG]
- [x] 2.2 `MOTIVO_LABELS` DESPUÉS del bloque `MOCK_SUGERENCIA` (no tocarlo; no escribir ese texto antes). [Real suggestion rendering]
- [x] 2.3 `MatriculaApi._request` → `{ok,status,body}`; rechazo fetch → status 0; JSON solo si content-type json. [Other errors]
- [x] 2.4 `obtenerSugerencia()` y `confirmarMatricula({estudianteId,periodo,materiaIds})`; rama mock (200 clon / 201 ids). [Offline mock mode]
- [x] 2.5 `describirProblema(result)` pura: 0, detail(+errors en 400, +traceId en 500), `Error HTTP n`. [Other errors]
- [x] 2.6 `UIManager.setCargando()` y `_celdaEstado()` (badge + motivo en español, `title`), `data-id`. Vía `textContent`. [Real suggestion rendering]
- [x] 2.7 Filas `Prerrequisito` bloqueadas, no seleccionables; total = `totalCreditos`. [Real suggestion rendering]
- [x] 2.8 `App.cargarSugerencia()`: carga, render, error → alerta danger, botón deshabilitado sin `Sugerida`. [Load error]
- [x] 2.9 `confirmarMatricula()` con guard `enviando`; `materiaIds` solo `Sugerida`; sin `id` → alerta, sin POST. [Confirm payload]
- [x] 2.10 201 → alerta "N asignaturas inscritas" + recarga (mock sin recarga). [Successful enrollment]
- [x] 2.11 422 → `limpiarViolaciones`/`renderViolaciones` por `materiaCodigo` (fallback `materiaId`), alerta global, sin recarga. [Rejection]
- [x] 2.12 400/404/409/5xx/502 → alerta con `describirProblema`, sin badges. [Other errors]
- [x] 2.13 Alertas `fade show` → `fade in`; actualizar comentario de cabecera. [Real suggestion rendering]
- [x] 2.14 `app.css`: `.badge-success/.badge-warning/.badge-danger`, `.motivo`, `.violacion`. [Real suggestion rendering]

## Batch F3: Smoke + README

- [x] 3.1 (Opcional) `scripts/smoke-proxy.sh`: página 200, login admin 200, sugerencia 200, id 999 → 404, POST `codigosMaterias` → 422. [Manual verification checklist]
- [x] 3.2 Sección en `README.md` raíz: levantar front, identity y ms-inscripcion; puertos, env vars, admin/Admin123*. [Manual verification checklist]

## Batch F4: Run & Verify (solo orquestador)

- [x] 4.1 `node src/pruebas/validar-mocks.mjs` → 36/36. [Fixture parity]
- [x] 4.2 `ENROLLMENT_ORIGIN=http://localhost:8081 python dev-server.py`; login admin. [Auth unchanged]
- [x] 4.3 Smoke vía :5500 (3.1 si existe). [Prefix routing]
- [x] 4.4 Navegador: Laura 12 filas, 8 Sugerida, 23 créditos. [Laura loads]
- [x] 4.5 422: cargar página, `curl` inscribir 603601, confirmar. [Violation on a row]
- [x] 4.6 201 en BD limpia. [201]
- [x] 4.7 404 (`estudianteId` 999) y 502 (ms detenido). [Load error / Target down]
- [x] 4.8 `useMock:true` sin red. [Mock render]
