# Proposal: Conexión front SIPA ↔ ms-inscripcion

## Intent

La pantalla `sugerencia-matricula.html` funciona solo con `MOCK_SUGERENCIA` y llama a rutas inexistentes (`/api/v1/matricula/*`); el proxy manda todo `/api/*` a identity. Objetivo: primer flujo de punta a punta: ver la sugerencia real y confirmar la matrícula (o entender por qué se rechazó).

## Scope

### In Scope
- `dev-server.py`: ruteo por prefijo (`/api/auth|users|roles|demo` → identity `AUTH_ORIGIN`, default `:5132`; resto de `/api/*` → `ENROLLMENT_ORIGIN`, default `http://localhost:8080`, por env var porque `API_PORT` puede cambiar); `do_PUT/do_DELETE/do_OPTIONS`; reenvío de `Authorization`; mensaje 502 con el origen real.
- `app.js`: `API_CONFIG` con `estudianteId`, `periodo` y endpoints reales; `useMock=false` → `GET /api/estudiantes/{id}/sugerencia?periodo=`.
- Se mantiene `useMock` como modo offline: el `MOCK_SUGERENCIA` queda idéntico al fixture (`validar-mocks.mjs` grupo 5 hace `JSON.stringify ===`), así que no se le agregan campos.
- Confirmar: `POST /api/inscripciones` `{estudianteId, periodo, materiaIds}` con las filas `Sugerida`, usando el `id` que devuelve la sugerencia (el front no traduce código → id). 201 → alerta de éxito ("N asignaturas inscritas") y recarga. 422 → `violations[]` agrupadas por `materiaCodigo`, con badge y tooltip por fila (`message` en español) y una alerta global (todo o nada). 400/404/409/5xx → `detail` del ProblemDetails (+`traceId` si es 500).
- Capa de red: el error conserva `status` y el body JSON (hoy se descarta).

### Out of Scope
- "Mis inscripciones" + cancelar: P2, tiene UI nueva; el proxy ya queda listo para DELETE.
- CORS en ms-inscripcion: el proxy lo hace innecesario.
- JWT en ms, vínculo usuario ↔ estudiante, `/estudiantes/me`, guard de sesión, 500 de identity, historial y notificaciones reales.

## Capabilities

### New Capabilities
- `frontend-enrollment-integration`: proxy de desarrollo, carga de la sugerencia real, confirmación y presentación de errores ProblemDetails.

### Modified Capabilities
- None (el contrato de backend lo define `regla-n3-sugerencia`).

## Approach

El adaptador va en `MatriculaApi`; `UIManager` solo agrega `renderViolations`/`showProblem`. No hay reglas de negocio en el cliente. `totalCreditos` viene del backend (el fallback ya existe).

## Affected Areas

| Area | Impact | Description |
|------|--------|-------------|
| `dev-server.py` | Modified | ruteo, verbos, env vars |
| `assets/js/app.js` | Modified | config, red, errores, render de violaciones |
| `sugerencia-matricula.html` | Modified (mínimo) | estilos/tooltip opcionales |
| `assets/js/login.js` | Ninguno | — |

## Risks

| Risk | Likelihood | Mitigation |
|------|------------|------------|
| La sugerencia no trae `id` por fila | Med | Exigir `id` (campo aditivo) en `regla-n3-sugerencia`. Plan B: `GET /api/materias` y mapear código → id |
| Conflicto de merge con `feature/datos-mock` en `app.js` | High | Mergear `datos-mock` a esta rama ANTES de aplicar; resolver tomando su bloque `MOCK_SUGERENCIA` y conservando `_headers`, `baseURL:''` y `login.js` nuestros (su diff de auth es solo antigüedad de la rama) |
| IDOR por `estudianteId` en config | High | Aceptado solo para la demo |
| `periodo` hardcodeado se desincroniza | Med | Valor único en `API_CONFIG`; endpoint de período en un change posterior |

## Rollback Plan

- Contingencia inmediata: `API_CONFIG.useMock=true` restaura la demo offline sin backend.
- Total: `git revert` del commit del change (solo `dev-server.py` + `app.js`; no hay cambios de datos ni de backend). El login no se ve afectado porque `/api/auth` sigue yendo a identity.

## Dependencies

- **`regla-n3-sugerencia` se implementa primero**: `GET /api/estudiantes/{id}/sugerencia?periodo=` con la forma de `sugerencia_mock.json` + `id` por fila, regla N + máx. 3 de N+1, códigos 603xxx y seed con Laura.
- Mergear `feature/datos-mock` antes del apply.

## Verification (sin TDD, sin framework)

- Checklist manual: carga con Laura (12 filas, 26 créditos); 201; 422 (inscribir antes una materia por curl y luego confirmar desde la UI → `MATERIA_YA_INSCRITA` en esa fila); 404 (estudianteId inexistente); backend apagado → 502; `useMock=true` sigue funcionando; login sigue funcionando.
- Smoke opcional `scripts/smoke-proxy.sh` (curl a `:5500` para auth y para enrollment) y `node src/pruebas/validar-mocks.mjs` en verde.

## Open Questions (supuestos por defecto)

- Período: `API_CONFIG.periodo = '2026-2'`.
- ¿Conflicto parcial al confirmar? No puede pasar: el POST es atómico (o se inscribe todo o nada).
- 500 de identity con credenciales inválidas: fuera de alcance (el parche por regex se mantiene).
- `estudianteId` de Laura: el id que asigne el seed de `regla-n3-sugerencia`.

## Success Criteria

- [ ] Con `useMock=false`, la tabla muestra la sugerencia real de Laura vía `:5500`.
- [ ] Confirmar inscribe (201) y un rechazo muestra cada violación en su fila.
- [ ] Login y modo mock sin regresiones; `validar-mocks.mjs` grupo 5 en verde.
