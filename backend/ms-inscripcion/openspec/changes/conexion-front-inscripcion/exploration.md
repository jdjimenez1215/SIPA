# Exploración: conexión del front SIPA con ms-inscripcion

Change: `conexion-front-inscripcion` · Rama: `feature/ms-inscripcion` · Fecha: 2026-10-01 · Modo: hybrid

> Alcance: solo lectura. Se leyó el front completo (`index.html`, `sugerencia-matricula.html`, `assets/js/app.js`, `assets/js/login.js`, `dev-server.py`), `mini-identity-api-dotnet` (Program, AuthController, JwtTokenService, launchSettings, appsettings), `ms-inscripcion` (README, Program, controllers, DTOs, reglas, seed, middleware) y la rama `origin/feature/datos-mock`.

## 1. Estado actual

### 1.1 Cómo quedó conectado el login (commit `88cadd0` "Integra autenticacion API en SIPA")

| Aspecto | Valor real |
|---|---|
| API de auth | `mini-identity-api-dotnet`, .NET, puerto **5132** (http) / 7260 (https), `launchSettings.json` |
| Endpoint | `POST /api/auth/login` body `{ usernameOrEmail, password }` |
| Respuesta | `{ accessToken, tokenType:"Bearer", username, email, roles[] }` (NO devuelve `usuario`/`nombreCompleto`) |
| Base URL en el front | `LOGIN_CONFIG.baseURL = '/api'` (relativa, mismo origen) |
| Cómo llega a 5132 | `dev-server.py` (puerto **5500**) hace proxy de TODO `/api/*` a `API_ORIGIN = http://localhost:5132`. Solo implementa GET y POST (PUT/DELETE/OPTIONS no existen: `SimpleHTTPRequestHandler` responde 501) |
| CORS | No existe en ninguna API (`rg Cors` sin resultados). El proxy lo evita: el navegador siempre habla con el mismo origen (5500) |
| Token | `sessionStorage['sipa.auth.token']`; `sessionStorage['sipa.auth.user']` solo se guarda si la respuesta trae `usuario`/`user` (identity NO lo trae, así que en la práctica no se guarda nada de usuario) |
| Header | `MatriculaApi._headers()` en `app.js`: `Authorization: Bearer <token>` + `Accept: application/json` (+ `Content-Type` si POST) |
| Errores | Credenciales inválidas llegan como **500** (identity no tiene exception handler; lanza `UnauthorizedAccessException`); `login.js` lo detecta con regex sobre el body (`invalid credentials`) |
| JWT | HS256, `Key=THIS_IS_A_DEMO_KEY_CHANGE_IT_123456789`, `Issuer=MiniIdentityApi`, `Audience=MiniIdentityApiUsers`, expira en 1 h. Claims: `sub` (GUID), `unique_name`, `email`, `role[]`. **No hay ningún claim de estudiante** |
| Usuarios | `InMemoryUserRepository` (se pierden al reiniciar). Único seed: `admin` / `Admin123*` con rol Admin. `POST /api/auth/register` es público |
| Guard de sesión | `app.js` NO redirige a login si falta token (cualquiera abre `sugerencia-matricula.html`) |

Nota de arquitectura front: el código NO usa jQuery para lógica; es Vanilla JS con patrón módulo (capa de red / capa de UI / main). jQuery y Bootstrap 3 solo aportan estilos y dropdowns. Hay un `GUIA_BACKEND.md` referenciado en comentarios que NO existe en el repo.

### 1.2 ms-inscripcion hoy

- .NET 8, Clean Architecture, PostgreSQL, Docker (`API_PORT` por defecto **8080**), MediatR + FluentValidation, ProblemDetails.
- **Confirmado: no hay CORS ni autenticación** en `Program.cs` (supuesto 9 del README: "Sin autenticación ni autorización (fuera de alcance)"). Pipeline: Serilog -> `ExceptionHandlingMiddleware` -> `UseStatusCodePages` -> Swagger -> `MapControllers`.
- Endpoints (20): `carreras` CRUD, `materias` CRUD (+horarios, +prerrequisitos), `GET /api/estudiantes/{id}/materias-disponibles?periodo=`, `GET /api/estudiantes/{id}/inscripciones?periodo=`, `POST /api/inscripciones`, `DELETE /api/inscripciones/{id}`.
- **No existe** `GET /api/estudiantes/{id}` (perfil), ni listado de estudiantes, ni endpoint de historial académico, ni endpoint que exponga el período vigente.
- La entidad `Estudiante` solo tiene `Id(int), Nombre, CarreraId, SemestreActual`: **no hay código de estudiante ni vínculo con el usuario de identity**.

## 2. Inventario de pantallas y datos (origen actual)

| Pantalla / feature | Datos que necesita | Origen hoy |
|---|---|---|
| `index.html` Login | usuario, password -> token | **API real** (identity 5132 vía proxy) |
| `index.html` Recuperar contraseña (`/auth/recuperar-password`) | usuario | Endpoint inexistente en identity (falla -> mensaje de servicio no disponible) |
| `index.html` Consultar usuario por documento (`/auth/consultar-usuario`) | documento -> usuario | Endpoint inexistente en identity |
| `sugerencia-matricula.html` Perfil (navbar) | `nombreEstudiante`, `programa`, `semestreActual` | **Hardcoded** `MOCK_SUGERENCIA` (`useMock: true`) |
| Tabla de sugerencia | por fila: `codigo, nombre, creditos, semestre, estado(Sugerida/Prerrequisito), prerrequisitoCumplido` | Hardcoded mock (en `main` con códigos ficticios INF-xxx; en `datos-mock` con códigos reales 603xxx) |
| Total de créditos | `totalCreditos` (solo filas `Sugerida`) | Mock; `app.js` calcula respaldo si falta |
| Botón "Confirmar Matrícula" | POST `{ codigoEstudiante:null, codigosMaterias[] }` -> `{ exito, matriculaId, mensaje }` | Mock (`useMock`) |
| Notificaciones (campana) | `id, titulo, mensaje, fecha, leida` | Mock |
| Reloj header | hora cliente | Cliente |
| Menú "Inicio" / "Mi Historial Académico" | - | Solo anclas `#inicio` / `#historial`; **no existen pantallas** |
| Menú usuario (foto, contraseña, manuales, cerrar sesión) | - | Enlaces a `siau.unillanos.edu.co` (no funcionales aquí); no hay logout local |

Contrato que el front espera (hoy): `GET /api/v1/matricula/sugerencia` y `POST /api/v1/matricula/confirmar` (rutas que **no existen** en ms-inscripcion).

## 3. Rama `origin/feature/datos-mock` (NO mergeada)

- Ramificada de `9f0fa07` (antes de auth). Diff contra su base: **solo** `assets/js/app.js` (MOCK_SUGERENCIA) y los archivos nuevos `src/data/mocks/*` + `src/pruebas/validar-mocks.mjs`. `sugerencia-matricula.html` e `index.html` NO difieren. (Contra `main` el diff muestra también "borrado" del backend/auth y `dev-server.py` solo porque la rama no los tiene.)
- Contenido: `malla_curricular.json` (53 asignaturas reales del plan PIS 603 2018-I de Unillanos, 10 semestres, 165 créditos, códigos de 6 dígitos, `prerrequisitos` = lista de códigos), `estudiante_prueba.json` (Laura Gómez Ríos, `1045123456`, semestreActual 6, 26 aprobadas de sem. 1-5, estados `APROBADO`, nota), `sugerencia_mock.json` (12 filas, `totalCreditos: 26`).
- **Conflicto futuro**: ambas ramas tocan `assets/js/app.js` (mock vs `_headers`/baseURL). El merge será manual pero acotado (distintas zonas del archivo).
- `validar-mocks.mjs` compara `MOCK_SUGERENCIA` de `app.js` con el fixture: **si se elimina/reemplaza el mock embebido, esa prueba (grupo 5) deja de ser válida** y hay que ajustarla.

### 3.1 La "regla N+3" del mock NO es la regla R4 de ms-inscripcion (hallazgo clave)

| | Mock del front (`datos-mock/README.md`) | ms-inscripcion |
|---|---|---|
| Significado | Se cursa el semestre N **completo** + **máximo 3 asignaturas** del semestre N+1 | `Enrollment:MaxSemestersAhead=3`: puede inscribir materias hasta el semestre `SemestreActual + 3` (sin límite de cantidad) |
| Ejemplo (N=6) | Sem. 6 (6 materias) + 3 de sem. 7; las otras 3 de sem. 7 salen como `Prerrequisito` | Permitiría sem. 6, 7, 8 y 9 sin tope de cantidad |
| Prerrequisitos | Se cumplen también con materias **sugeridas en el mismo período** (603702 requiere 603601, que no está aprobada pero se sugiere) | Solo cuenta `Aprobada` en historial (`PrerequisitesRule`; supuesto 1: `Cursando` no vale) -> 603702 se **rechazaría** con `PREREQUISITO_NO_CUMPLIDO` |
| Orden/ranking | Algoritmo secuencial por orden de malla; consume cupo N+1 | No existe ranking: `materias-disponibles` evalúa cada materia SOLA |
| Filas bloqueadas | Devuelve filas `estado: "Prerrequisito"` (para mostrar pendientes) | Solo devuelve las **elegibles**; las que fallan simplemente no aparecen y no dice por qué |
| Significado de `semestreActual=6` | El estudiante *entra* a 6 (aprobó 1-5) | Dato plano de `Estudiante.SemestreActual` |

Conclusión: **no hay equivalencia directa**. Hay que decidir si el negocio es "N+3 semestres" (backend actual) o "N + 3 materias de N+1" (mock/UI/título). Es una decisión humana (pregunta abierta Q1).

## 4. Tabla de mapeo front -> ms-inscripcion

### 4.1 Sugerencia de matrícula <-> `GET /api/estudiantes/{id}/materias-disponibles`

| Campo del front (fila) | Campo ms-inscripcion (`MateriaDto`) | Compatibilidad |
|---|---|---|
| `codigo` | `codigo` | Exacto (string). **Pero los valores difieren**: seed `MAT101` vs mock `603601` |
| `nombre` | `nombre` | Exacto |
| `creditos` | `creditos` | Exacto |
| `semestre` | `semestre` | Exacto |
| `estado` (`Sugerida`/`Prerrequisito`) | - | **Falta**. Todo lo devuelto es elegible; se puede fijar `'Sugerida'` en un adaptador, pero no hay filas `Prerrequisito` |
| `prerrequisitoCumplido` | - (hay `prerrequisitoIds[]`) | **Falta** como booleano; derivable solo para elegibles (siempre `true`) |
| `totalCreditos` | - | **Falta**; calculable en el front (ya existe el fallback con `console.warn`) |
| `nombreEstudiante` | - | **Falta**: no hay `GET /api/estudiantes/{id}` |
| `programa` | `carreraId` en cada materia + `GET /api/carreras/{id}` | Derivable con 2 llamadas, pero necesita conocer `carreraId` (viene de una materia) |
| `semestreActual` | - | **Falta** (no se expone) |
| `notificaciones[]` | - | **Falta** (ms-inscripcion no tiene el concepto) |
| (extra, no usado por UI) | `id`, `carreraId`, `cuposMaximos`, `horarios[]`, `prerrequisitoIds[]` | Sobran; `id` es **imprescindible** para el POST |
| Parámetro `periodo` | query `?periodo=AAAA-N` (default `Enrollment:CurrentPeriod`) | El front no maneja período; no hay endpoint para leer el vigente |

### 4.2 Confirmar matrícula <-> `POST /api/inscripciones`

| Front (`confirmarMatricula`) | ms-inscripcion (`EnrollRequest`) | Compatibilidad |
|---|---|---|
| `codigoEstudiante: null` ("lo resuelve el token") | `estudianteId: int` | **Distinto**: el backend exige id numérico en el body; el token no lleva ese dato |
| (no existe) | `periodo: "AAAA-N"` (obligatorio, regex `^\d{4}-[12]$`) | **Falta** en el front |
| `codigosMaterias: string[]` (códigos) | `materiaIds: int[]` | **Renombre + conversión**: hay que mapear código -> `id` (viene en `MateriaDto`; guardarlo en cada fila, p. ej. `data-id`) |
| Respuesta esperada `{ exito, matriculaId, mensaje }` | `201` + `InscripcionDto[]` (`id, estudianteId, materiaId, periodoAcademico, estado, fechaInscripcion, materia{...}`) | **Distinto**: no hay `exito`/`matriculaId`/`mensaje`; el front debe sintetizar el mensaje (p. ej. "N asignaturas inscritas") |
| Errores: `throw new Error('HTTP n')` sin body | `400/404/409/422` ProblemDetails (`code`, `violations[]`, `errors{}`) | **Distinto**: la capa de red actual descarta el body; debe parsearlo |

### 4.3 Malla curricular <-> `GET /api/materias`

`GET /api/materias?carreraId=&semestre=` devuelve el catálogo completo (`MateriaDto`: `id, codigo, nombre, creditos, carreraId, semestre, cuposMaximos, horarios[], prerrequisitoIds[]`). Contra `malla_curricular.json` (`codigo, nombre, creditos, semestre, prerrequisitos[]` como **códigos**):

| Mock | ms | Compatibilidad |
|---|---|---|
| `codigo/nombre/creditos/semestre` | idem | Exacto |
| `prerrequisitos: string[]` (códigos) | `prerrequisitoIds: int[]` (ids) | Renombre + tipo (para pasar a código hay que cruzar con el catálogo) |
| - | `cuposMaximos`, `horarios[]`, `carreraId` | El mock no los tiene -> **al sembrar desde la malla hay que inventar cupos y horarios** |
| `programa` (nivel raíz) | `Carrera.nombre` | Equivalente, pero ms-inscripcion lo modela como entidad |

No hay pantalla de malla en el front (no se usa hoy): solo sería útil para una vista nueva.

### 4.4 Historial académico <-> `HistorialAcademico`

- **No hay ningún endpoint que exponga el historial** (confirmado: ningún controller lo publica; solo se usa internamente en `GetApprovedMateriaIdsAsync`). **BLOQUEANTE** para la pantalla "Mi Historial Académico" (hoy solo un ancla).
- Mapeo si se crea: `estudiante_prueba.historialAcademico[]` `{codigo, estado:"APROBADO", nota}` vs `HistorialAcademico` `{materiaId, estado: Aprobada|Reprobada|Cursando, nota?, periodo}`. Diferencias: `codigo` -> `materiaId` (o incluir `materiaCodigo` en el DTO), `APROBADO` -> `Aprobada` (enum texto distinto), falta `periodo` en el mock.
- Además `estudiante_prueba` trae `codigoEstudiante` (cédula `1045123456`), `nombreEstudiante`, `programa`, `semestreActual`: la entidad `Estudiante` no tiene `codigoEstudiante`.

### 4.5 Contrato de errores (ProblemDetails) -> render por materia

Forma real (README, `ExceptionHandlingMiddleware`):

```
422 application/problem+json
{ type, title, status, detail, instance, code:"INSCRIPCION_RECHAZADA", traceId,
  violations:[ { code, materiaId, materiaCodigo, message, details } ] }
```

Cómo debería renderizar el front (el contrato ya es suficiente, no hace falta cambiar backend):
- `violations[]` se agrupa por `materiaCodigo` (una materia puede tener varias) y se pinta en la fila de la tabla (`tr[data-codigo]`): badge rojo + `message` (ya viene en español). Casos con `details` útiles: `CRUCE_HORARIO` (`conflictsWith`), `PREREQUISITO_NO_CUMPLIDO` (`missing[]`), `CUPO_AGOTADO` (`cuposMaximos`, `inscritos`), `SEMESTRE_EXCEDIDO`.
- Resumen global en `#alert-container` ("No se inscribió ninguna: N materias con problemas") porque la operación es atómica (todo o nada).
- Otros: `400 VALIDACION_FALLIDA` (`errors{campo:[msg]}`) y `MATERIA_DUPLICADA_EN_SOLICITUD` -> alerta genérica; `404 ESTUDIANTE_NO_ENCONTRADO/MATERIA_NO_ENCONTRADA` -> alerta; `409 CONFLICTO_CONCURRENCIA` -> "reintente"; `409 MATERIA_YA_INSCRITA` (red de seguridad) -> alerta; `500 ERROR_INTERNO` -> mensaje con `traceId`.
- Cambio necesario en la capa de red: el error debe conservar `status` y el JSON del body (hoy `app.js` lanza `Error` con solo el status; `login.js` guarda `error.body` como texto: reutilizar ese patrón y hacer `JSON.parse`).
- Tras un 422 se recomienda NO recargar la tabla (que el usuario vea lo que falló); tras 201, recargar `materias-disponibles` (las ya inscritas desaparecerán por R5).

## 5. Brechas / bloqueantes con enfoque recomendado

### G1. CORS en ms-inscripcion
| Enfoque | Pros | Contras | Esfuerzo |
|---|---|---|---|
| A. Reutilizar el proxy de `dev-server.py` (ruteo por prefijo: `/api/auth|users|roles|demo` -> 5132; resto -> 8080) | Sin tocar el backend; mismo patrón que auth; cero preflight | Solo sirve en dev; el proxy hoy solo hace GET/POST (hay que agregar DELETE/PUT/OPTIONS si se quiere cancelar inscripción); hay que mantener el ruteo | S |
| B. `AddCors` en ms-inscripcion (origen configurable `Cors:AllowedOrigins`, p. ej. `http://localhost:5500`) | Funciona sin proxy (front estático directo a 8080, Docker, prod) | Hay que tocar `Program.cs`, configurar en compose; hay que repetirlo en identity si se usa directo | S |
| C. Ambos | Robusto | Dos mecanismos | S |

Recomendado: **A para el primer corte** (consistente con login) y **B como mejora** (P2) para no depender del proxy.

Gotcha: hoy el proxy manda TODO `/api/*` a 5132. `/api/materias` o `/api/estudiantes/...` caerían en identity y devolverían 404. Es el primer cambio obligatorio.

### G2. Autenticación: ¿ms-inscripcion valida el JWT de identity?
| Enfoque | Pros | Contras | Esfuerzo |
|---|---|---|---|
| A. Sin auth en ms (estado actual), el front solo envía el header | Cero cambios; desbloquea el demo | Cualquiera con `estudianteId` inscribe a otro; contradice el sentido del login | S (0) |
| B. `AddJwtBearer` en ms con mismo `Issuer/Audience/Key` (config por env: `Jwt__Key`, etc.) + `[Authorize]` en controllers de estudiantes/inscripciones | Seguridad real, patrón idéntico a identity (copiar bloque de `Program.cs`), tokens ya emitidos | Clave simétrica compartida en dos servicios (aceptable en demo, riesgosa en prod); hay que actualizar Postman/e2e.sh (73 chequeos) con token; CRUD de catálogo debería exigir rol `Admin` | M |
| C. Gateway/API gateway que valida | Centraliza | Sobre-ingeniería para el alcance | L |

Recomendado: **B**, pero **después** del primer corte funcional (P2), con `[Authorize]` en inscripciones/consultas de estudiante y `[Authorize(Roles="Admin")]` en CRUD de catálogo. Para B hay un detalle: autorización por recurso (que el `estudianteId` pedido sea del usuario del token) exige resolver G3.

### G3. Mapeo usuario logueado -> `estudianteId`
Lo que hay: JWT con `sub` (GUID), `unique_name`, `email`, `role`; `Estudiante` sin código ni usuario. **Nada usable hoy.**

| Enfoque | Pros | Contras | Esfuerzo |
|---|---|---|---|
| A. Config del front (`API_CONFIG.estudianteId = 1`, o query `?estudianteId=`) para el primer corte | Inmediato, desbloquea la integración | Hack de demo; no es seguridad ni multiusuario | S |
| B. Convención: `username` de identity = cédula/código; ms agrega `Estudiante.Codigo` (columna única, migración) y `GET /api/estudiantes/by-codigo/{codigo}` (o `/me` leyendo `unique_name` del JWT) | Sin tocar identity; natural para Unillanos (cédula `1045123456`); `/me` evita IDOR | Requiere migración + endpoint + acordar convención de username; registro de usuarios debe usar la cédula | M |
| C. Identity emite claim `estudianteId`/`codigo` (User con campo extra, `RegisterRequest` con campo) | Un solo lugar de verdad en el token; ms no consulta a nadie | Toca identity (otro "equipo"/servicio), en memoria; acopla identity al dominio académico | M |
| D. Tabla de vinculación `usuario_sub -> estudiante_id` en ms | Desacoplado | Más piezas; hay que poblarla | M |

Recomendado: **A en P1, B como evolución (P2)** (username = código de estudiante; `GET /api/estudiantes/me`). Evita tocar identity.

### G4. Seed: ficticio ING-SIS vs malla real Unillanos
- Seed actual (`SeedData.cs`, `HasData` en migración `20260930020943_InitialCreate`): 2 carreras, 12 materias (`MAT101`…), 2 estudiantes (Ana sem. 2, Carlos), historial, horarios, 2 inscripciones. Códigos y nombres **no coinciden** con `603xxx`.
- Proponer sembrar desde `malla_curricular.json` + `estudiante_prueba.json`: 1 carrera `Ingeniería de Sistemas` (código p. ej. `603`), 53 materias (código 6 dígitos, nombre, créditos, semestre), prerrequisitos (traducir códigos -> ids), estudiante Laura Gómez (semestre 6, 26 aprobadas, notas).
- Datos que hay que **inventar** (el mock no los trae): `cuposMaximos` (p. ej. 30), `horarios` (sin horarios no hay cruces: aceptable al inicio, pero R3 queda sin ejercitarse), período del historial (`2026-1`).
- Mecánica: `HasData` exige nueva migración y **recrear datos** (`docker compose down -v`); `README` y `e2e.sh`/Postman asumen ids y códigos actuales (MAT101, etc.) -> **hay que decidir si el seed Unillanos reemplaza o convive con el seed de pruebas**. Opción: seed por entorno/flag (`Database:SeedProfile = Demo|Unillanos`) con un seeder en runtime idempotente en lugar de `HasData`; más flexible, pero rompe la decisión #7 del README ("semilla con HasData") y es más trabajo.
- Si el seed pasa a Unillanos, la regla N+3 del mock sigue sin reproducirse (ver 3.1): con Laura (sem. 6, N+3 semestres) `materias-disponibles` devolvería materias de sem. 6 a 9 elegibles, no 12 filas.
- Esfuerzo: M (script/generador de seed + migración + ajustar docs/tests).

### G5. URLs/puertos configurables
Hoy: front 5500, identity 5132 (7260 https), ms 8080 (`API_PORT`), Postgres 5432. `app.js` usa `baseURL:''` y rutas `/api/v1/matricula/...` (inexistentes). Recomendado:
- `API_CONFIG` con `baseURL: ''` (mismo origen) y `endpoints` reales (`materiasDisponibles: id => /api/estudiantes/${id}/materias-disponibles`, `inscripciones: '/api/inscripciones'`).
- `dev-server.py` con ruteo por prefijo y orígenes por variable de entorno (`AUTH_ORIGIN`, `ENROLLMENT_ORIGIN`, defaults 5132/8080) para que `API_PORT=8081` no obligue a editar código.
- Mantener un solo flag: `useMock` (permite seguir demostrando sin backend; ver Q6).

### G6. Contrato del front vs backend (rutas y semántica)
El front fue escrito "contract-first" con `/api/v1/matricula/sugerencia|confirmar`. Opciones:
| Enfoque | Pros | Contras | Esfuerzo |
|---|---|---|---|
| A. Adaptador en el front (`MatriculaApi` traduce `materias-disponibles` -> forma de sugerencia y `POST /api/inscripciones`) | No toca backend; UIManager y HTML casi intactos; mantiene el fixture como contrato interno de UI | Lógica de adaptación (códigos->ids, totalCreditos, estado fijo) vive en el cliente; faltan perfil y filas bloqueadas | M |
| B. Endpoint BFF/agregado en ms-inscripcion `GET /api/estudiantes/{id}/sugerencia-matricula` con la forma exacta del mock (incluye perfil, `totalCreditos`, filas bloqueadas con motivo) | El front casi no cambia; permite implementar la regla "N + 3 materias de N+1" server-side; resuelve perfil y bloqueadas | Mucho más backend (nueva regla, nuevo DTO, tests); hay que resolver Q1 antes | L |
| C. Híbrido: A ahora + endpoint de perfil `GET /api/estudiantes/{id}` (S) y B más adelante | Entrega incremental | Dos fases | M + S |

Recomendado: **C**.

## 6. Candidatos de conexión priorizados

| Pri | Candidato | Valor para el usuario | Esfuerzo | Dependencias | Archivos que cambian |
|---|---|---|---|---|---|
| **P1** | **Corte vertical "Sugerencia real + Confirmar con violaciones"**: `useMock=false`; `materias-disponibles` -> tabla; total de créditos; `POST /api/inscripciones`; render de `violations[]` por fila + alerta global; `estudianteId` y `periodo` por config; proxy con ruteo por prefijo | Primer flujo de punta a punta: el estudiante ve materias reales elegibles y se matricula (o entiende por qué no) | **M** | Q2/Q3 (estudianteId y período por config en este corte); Postgres+API arriba (`docker compose up`) | Front: `assets/js/app.js` (API_CONFIG, `MatriculaApi`, `UIManager.renderViolaciones`, mapeo codigo->id), `sugerencia-matricula.html` (data-id en filas se hace por JS; solo cambia si se agrega contenedor de violaciones), `dev-server.py` (ruteo por prefijo, soporte DELETE/PUT/OPTIONS opcional). Backend: ninguno obligatorio |
| P2 | Perfil del estudiante: `GET /api/estudiantes/{id}` (nombre, carrera, semestreActual) -> navbar | Se ve el nombre/programa reales en vez de hardcode | S | P1 | Backend: nuevo `EstudiantesController` action + `GetEstudianteQuery` + DTO + tests; Front: `app.js` (`renderPerfil`) |
| P2 | Mis inscripciones + cancelar: `GET /api/estudiantes/{id}/inscripciones` y `DELETE /api/inscripciones/{id}` | El estudiante ve lo ya matriculado y libera cupo; tras 201 refleja el resultado | M | P1; proxy con DELETE | Front: `app.js` (+ sección/tabla nueva en `sugerencia-matricula.html`), `dev-server.py` (do_DELETE) |
| P2 | CORS configurable en ms-inscripcion | Permite front sin proxy (otros hosts/Docker) | S | - | Backend: `Program.cs`, `appsettings*.json`, `docker-compose.yml` |
| P2 | JWT en ms-inscripcion (mismo issuer/audience/key) + `/api/estudiantes/me` (username = código) + migración `Estudiante.Codigo` | Seguridad real y multiusuario sin config manual del id | M-L | G3 decidida (Q3/Q4); G2 | Backend: `Program.cs`, csproj (JwtBearer), controllers, migración, seed, `scripts/e2e.sh`, Postman, README; Front: `app.js` (quitar estudianteId de config, manejo 401 -> login), guard de sesión |
| P2 | Seed Unillanos (malla + Laura) | Datos coherentes con lo que ven los usuarios/mocks | M | Q5 (reemplazar o convivir) | Backend: `SeedData.cs`/seeder, nueva migración, README, tests/e2e/Postman |
| P3 | Alinear la regla N+3 del mock (N completo + máx. 3 de N+1, prerrequisitos con co-sugeridas) con el backend + endpoint `sugerencia-matricula` | UX exacto al mock aprobado por negocio (incluye filas "Prerrequisito") | L | Q1 | Backend: Domain (nueva regla/servicio), Application (query), Api (endpoint), tests; Front: casi nada |
| P3 | Historial académico: endpoint `GET /api/estudiantes/{id}/historial` + pantalla `#historial` | Habilita el ítem de menú que hoy no hace nada | M | Perfil (P2) | Backend: query+DTO+controller; Front: nueva página/JS (`historial.html`, `assets/js/historial.js`), menú |
| P3 | Notificaciones reales | La campana deja de ser mock | M-L | Servicio inexistente | Fuera de ms-inscripcion (nuevo servicio) |
| P3 | Guard de sesión + logout + manejo 401/expiración (1 h) | Evita ver la pantalla sin login; sesión coherente | S | - | Front: `app.js`/`login.js` |

**Recomendación: implementar primero P1.** Es el único corte que no requiere cambios de backend, demuestra el flujo completo con el microservicio real, expone temprano los desajustes de contrato (ids, período, violaciones) y no bloquea las decisiones de seguridad/seed.

## 7. Riesgos

- **Semántica N+3 distinta** (3.1): si P1 se presenta como "Regla N+3" el usuario verá materias de sem. N+3 sin límite de 3 y sin filas `Prerrequisito`; puede percibirse como bug. Mitigar renombrando temporalmente el texto o resolviendo Q1.
- **El mock embebido es contrato testeado** (`validar-mocks.mjs` grupo 5): al quitarlo en P1 esa prueba falla; coordinar con el dueño de `datos-mock` (merge de `app.js`).
- **Proxy actual rompe todo no-auth** (`/api/*` -> 5132) y no soporta DELETE/PUT.
- **`estudianteId` en el body sin auth** (IDOR) mientras no se resuelva G2/G3.
- **Seed con `HasData`**: cambiar datos requiere migración y recrear BD; `e2e.sh` (73 checks) y Postman (65 requests) dependen del seed actual.
- **Período**: el front tendría `'2026-2'` hardcodeado; si `Enrollment:CurrentPeriod` cambia, se desincroniza (no hay endpoint que lo exponga).
- **Errores de identity** llegan como 500 (sin handler): frágil; el front ya parchea por regex.
- **Códigos duplicados de convención**: ms usa códigos `MAT101`; mock `603601`; el payload del front envía códigos mientras el backend exige ids.

## 8. Preguntas abiertas (requieren decisión humana)

1. **Q1 Regla N+3**: ¿cuál es la regla de negocio correcta? (a) N+3 semestres (`MaxSemestersAhead=3`, backend actual), (b) N completo + máx. 3 materias de N+1 con prerrequisitos que aceptan co-sugeridas (mock). Define si P3 (endpoint de sugerencia) existe y cómo se rotula la pantalla.
2. **Q2 Contrato de la pantalla**: ¿el front debe aceptar el contrato de ms-inscripcion tal cual (adaptador en cliente, enfoque A/C) o se pide a backend un endpoint con la forma del mock (enfoque B)? ¿Se muestran filas bloqueadas con motivo o solo las elegibles?
3. **Q3 estudianteId**: ¿hardcode/config en el primer corte (Ana id=1 o Laura tras seed) o se invierte desde ya en el vínculo usuario -> estudiante?
4. **Q4 Vínculo usuario-estudiante** (si se invierte): ¿convención `username = código/cédula` + `Estudiante.Codigo` en ms (recomendado), claim en el JWT de identity, o tabla de vinculación? ¿Quién es dueño de modificar identity (`mini-identity-api-dotnet`)?
5. **Q5 Seed**: ¿el seed Unillanos reemplaza al fictício o conviven (flag/perfil)? ¿De dónde salen cupos y horarios? ¿Se acepta regenerar BD y reescribir README/e2e/Postman?
6. **Q6 Mock**: ¿se mantiene `useMock` como modo offline/demo o se elimina el `MOCK_SUGERENCIA` (impacta `datos-mock` y `validar-mocks.mjs`)? ¿Cuándo se mergea `feature/datos-mock`?
7. **Q7 Auth en ms**: ¿JWT con clave compartida en esta entrega o se pospone? ¿Roles: CRUD de catálogo solo `Admin`?
8. **Q8 CORS/despliegue**: ¿el front seguirá servido por `dev-server.py` con proxy o se desplegará estático separado (requiere CORS)? ¿Se unifican puertos/orígenes por variables de entorno?
9. **Q9 Período**: ¿el front lo toma de config, o se expone un endpoint (`GET /api/periodo-actual` / incluirlo en el perfil)?
10. **Q10 Alcance de pantallas**: ¿Historial, Inicio y Notificaciones entran en este change o quedan fuera? ¿`GUIA_BACKEND.md` (referenciado pero ausente) existe en otro lugar y fija el contrato?
11. **Q11 Errores de login**: ¿se corrige identity para devolver 401 en credenciales inválidas (hoy 500)? Hay un hack en el front que dependería de ese texto.

## 9. Resumen para propuesta

Listo para proposal: **Sí**, acotando el change a P1 (con la decisión de Q2/Q3 resuelta como "adaptador en el cliente + estudianteId/periodo por config") y dejando Q1, Q4, Q5, Q7 como changes posteriores.

## 10. Decisiones tomadas (2026-10-01)

- **Q1 Regla N+3 → se adopta la del mock (front).** Semestre N completo + máximo 3 materias de N+1; los prerrequisitos pueden cumplirse con materias co-sugeridas en la misma matrícula. El backend debe cambiar su regla (hoy `SemestreActual + MaxSemestersAhead` sin tope y solo prerrequisitos `Aprobada`). Impacta Domain (SemesterLimitRule, PrerequisitesRule), materias-disponibles/sugerencia, unit tests, e2e.sh y Postman. Pasa a ser parte del alcance (antes P3).
- **Q3 estudianteId → fijo en configuración del front** (`API_CONFIG`) junto con el periodo, para el primer corte. El vínculo usuario ↔ estudiante (JWT + `/estudiantes/me`) queda para un change posterior; riesgo IDOR aceptado solo para la demo.
- Pendientes: Q2 (adaptador cliente vs endpoint con forma del mock — con Q1 adoptado, un endpoint `sugerencia` en backend gana peso), Q4–Q11.
- **Q1 (precisión, 2026-10-01): prerrequisitos estrictos** — solo cuentan las materias Aprobada; se descarta la co-sugerencia del mock. Laura: 8 Sugeridas, 4 bloqueadas, totalCreditos 23; el fixture del mock debe corregirse.
