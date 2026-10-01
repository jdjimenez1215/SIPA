# Frontend Enrollment Integration Specification

## Purpose
Connects `sugerencia-matricula.html` (dev server on :5500) to ms-inscripcion: proxy routing, real suggestion, confirmation and ProblemDetails presentation. No business rules live in the client. Backend contract: `regla-n3-sugerencia`.

## Requirements

### Requirement: Dev proxy routing
`dev-server.py` MUST route `/api/auth|users|roles|demo` to identity (`AUTH_ORIGIN`, default `http://localhost:5132`) and every other `/api/*` to `ENROLLMENT_ORIGIN` (default `http://localhost:8080`). It MUST support GET, POST, PUT, DELETE and OPTIONS, MUST forward `Authorization`, and MUST answer 502 naming the unreachable origin.

#### Scenario: Prefix routing
- GIVEN `ENROLLMENT_ORIGIN=http://localhost:8081`
- WHEN the browser calls `POST /api/auth/login` and `GET /api/estudiantes/1/sugerencia`
- THEN the first reaches identity and the second reaches :8081

#### Scenario: Verbs and header
- GIVEN a request with `Authorization: Bearer x`
- WHEN it is a PUT, DELETE or OPTIONS to `/api/inscripciones/1`
- THEN it is forwarded with the header and not answered 501

#### Scenario: Target down
- GIVEN ms-inscripcion is stopped
- WHEN `GET /api/estudiantes/1/sugerencia`
- THEN the proxy returns 502 whose message includes the origin

### Requirement: API_CONFIG
`app.js` MUST define `API_CONFIG` with `estudianteId`, `periodo` (`2026-2`), real `endpoints` and `useMock`. Values MUST NOT be duplicated elsewhere.

#### Scenario: Request built from config
- GIVEN `useMock=false`, `estudianteId=1`, `periodo=2026-2`
- WHEN the page loads
- THEN it calls `GET /api/estudiantes/1/sugerencia?periodo=2026-2`

### Requirement: Real suggestion rendering
With `useMock=false` the page MUST render the response: profile (`nombreEstudiante`, `programa`, `semestreActual`), one table row per `materiasSugeridas`, an `estado` badge, and `totalCreditos` as returned. A `Prerrequisito` row MUST be visibly blocked, show its `motivo` as Spanish text (not the raw code), and MUST NOT be selectable.

#### Scenario: Laura loads
- GIVEN Laura (id 1, 2026-2)
- WHEN the page loads
- THEN 12 rows are shown, 8 `Sugerida` and 4 blocked (603702, 603704, 603705, 603706)
- AND total credits shows 23

#### Scenario: Blocked row with motivo
- GIVEN a row `estado="Prerrequisito"` with `motivo="CUPO_AGOTADO"`
- WHEN rendered
- THEN the row is blocked and shows a Spanish explanation

#### Scenario: Load error
- GIVEN 404 ESTUDIANTE_NO_ENCONTRADO
- WHEN the page loads
- THEN the ProblemDetails `detail` is shown in an alert and no table rows appear

### Requirement: Confirm payload
Confirm MUST POST `/api/inscripciones` `{estudianteId, periodo, materiaIds}` containing ONLY the `id` of `Sugerida` rows, using the `id` returned by the API (no code-to-id lookup).

#### Scenario: Only Sugerida ids
- GIVEN Laura's 12 rows
- WHEN the user confirms
- THEN `materiaIds` has exactly the 8 `Sugerida` ids and no blocked ids

### Requirement: Successful enrollment
On 201 the page MUST show a success alert "N asignaturas inscritas" (N = array length) and reload the suggestion.

#### Scenario: 201
- GIVEN the API answers 201 with 3 InscripcionDto
- WHEN confirm completes
- THEN the alert reads "3 asignaturas inscritas" and the suggestion is requested again

### Requirement: Rejection (422, atomic)
On 422 the page MUST group `violations[]` by `materiaCodigo`, show per row a badge and a tooltip with `message`, and show a global alert stating that nothing was enrolled. It MUST NOT reload the table.

#### Scenario: Violation on a row
- GIVEN 422 with `MATERIA_YA_INSCRITA` on 603601 and `PREREQUISITO_NO_CUMPLIDO` on 603702
- WHEN received
- THEN each row shows its badge and tooltip, and the global alert says nothing was enrolled

#### Scenario: Several violations, one row
- GIVEN two violations with the same `materiaCodigo`
- WHEN rendered
- THEN that row shows both messages

### Requirement: Other errors
On 400, 404, 409 and 5xx the page MUST show the ProblemDetails `detail`; for 500 it SHOULD include `traceId`. The network layer MUST keep `status` and the parsed JSON body in the error. A non-JSON body MUST fall back to a generic message.

#### Scenario: 409 and 400
- GIVEN 409 CONFLICTO_CONCURRENCIA, or 400 VALIDACION_FALLIDA
- WHEN confirm completes
- THEN an alert shows the `detail` and no row badges are added

#### Scenario: 502 on confirm
- GIVEN the proxy answers 502 with a non-JSON body
- WHEN confirm completes
- THEN a generic connectivity alert is shown

### Requirement: Offline mock mode
With `useMock=true` the page MUST render `MOCK_SUGERENCIA` without network calls. `MOCK_SUGERENCIA` MUST stay byte-identical (`JSON.stringify`) to `src/data/mocks/sugerencia_mock.json`, and `node src/pruebas/validar-mocks.mjs` MUST pass.

#### Scenario: Mock render
- GIVEN `useMock=true` and no backend
- WHEN the page loads
- THEN the mock table renders and no `/api` request is made

#### Scenario: Fixture parity
- WHEN `validar-mocks.mjs` runs
- THEN group 5 passes

### Requirement: Auth unchanged
Login and token handling (`login.js`, `/api/auth/*` to identity) MUST behave as before.

#### Scenario: Login
- GIVEN valid credentials
- WHEN submitted via the dev server
- THEN identity answers and the token is stored as before

### Requirement: Manual verification checklist
The change MUST ship a manual checklist (no front test framework) covering: Laura load (12 rows, 23 credits), 201, 422 (pre-enroll a subject by curl, confirm), 404 (unknown `estudianteId`), backend down (502), `useMock=true`, login, and `validar-mocks.mjs`.

#### Scenario: Checklist complete
- GIVEN the change is applied
- WHEN each item is executed
- THEN every item passes before archive
