# Cómo probar SIPA

Hay **cinco suites**. Cada una valida una capa distinta, así que conviene conocerlas todas:

| Suite | Qué valida | Necesita | Resultado esperado |
|---|---|---|---|
| [Tests unitarios](#1-tests-unitarios) | Reglas de negocio y casos de uso, sin base de datos | .NET SDK | **114/114** |
| [E2E del backend](#2-e2e-del-backend-e2esh) | Los 21 endpoints de ms-inscripcion contra la API real | Docker | **145/145** |
| [Colección Postman](#3-colección-postman--newman) | Lo mismo que el E2E, para correr en Postman o CI | Docker + Postman o Node | **299/299** asserts |
| [Smoke del proxy](#4-smoke-del-proxy) | Que el front llega a **las dos** APIs a través del proxy | Todo levantado | **5/5** |
| [Validación del mock](#5-validación-del-mock) | Que el mock del front respeta la regla N+3 | Node | **36/36** |

Y al final, una [prueba manual en el navegador](#6-prueba-manual-en-el-navegador).

> Los comandos están para **Git Bash**. Si en tu máquina el 8080 o el 5432 están ocupados, usá `API_PORT=8081` y `DB_PORT=5433` como en los ejemplos.

---

## Antes de empezar: base limpia

El E2E, Postman y la prueba manual **modifican datos**. Cada corrida necesita la semilla recién cargada:

```bash
cd backend/ms-inscripcion
API_PORT=8081 DB_PORT=5433 docker compose down -v      # borra la BD
API_PORT=8081 DB_PORT=5433 docker compose up --build -d # recrea BD + migración + seed
curl -s -o /dev/null -w '%{http_code}\n' http://localhost:8081/api/carreras   # esperá 200
```

---

## 1. Tests unitarios

No necesitan base de datos ni Docker.

```bash
cd backend/ms-inscripcion
dotnet test
```

Cubren cada regla (caso válido y caso de falla), el motor que junta todas las violaciones, el cálculo de la sugerencia (tope de 3, adeudadas, materias bloqueadas que no ocupan lugar), los datos semilla y los handlers con Moq.

> Los proyectos apuntan a `net8.0`, pero `RollForward=Major` permite correrlos con el runtime de .NET 10.

---

## 2. E2E del backend (`e2e.sh`)

Llama a la API real con `curl` y verifica códigos HTTP y contenido. Incluye **dos pruebas de concurrencia**: dos pedidos simultáneos del mismo estudiante con materias que se cruzan, y dos estudiantes peleando el último cupo.

```bash
# con la base limpia (ver arriba)
cd backend/ms-inscripcion
PORT=8081 bash scripts/e2e.sh
```

Termina con `TOTAL: 145 passed, 0 failed`. Si falla algo, cada línea `FAIL` muestra el endpoint, lo esperado y el cuerpo recibido.

---

## 3. Colección Postman / Newman

Archivos en `backend/ms-inscripcion/postman/`:

- `MsInscripcion.postman_collection.json`: 98 requests en 11 carpetas
- `MsInscripcion.local.postman_environment.json`: variable `host`

**En Postman:**
1. Importá los dos archivos.
2. Elegí el environment **MsInscripcion - local** y poné `host` = `http://localhost:8081`.
3. Con la base limpia, corré la colección **completa y en orden** desde el *Collection Runner*.

**Por consola (Newman):**

```bash
cd backend/ms-inscripcion
npx newman run postman/MsInscripcion.postman_collection.json \
  -e postman/MsInscripcion.local.postman_environment.json \
  --env-var host=http://localhost:8081
```

Esperado: `assertions 299 | failed 0`.

> El orden importa: los requests crean ids que usan los siguientes, y las pruebas de concurrencia lanzan dos pedidos en paralelo desde el script de tests.

---

## 4. Smoke del proxy

Prueba el camino **real** del navegador: todo pasa por `:5500`. **No escribe** en la base.

Requiere los tres servicios levantados (ver [README](../README.md#cómo-correr-el-sistema-completo)):

```bash
bash scripts/smoke-proxy.sh                         # por defecto http://localhost:5500
BASE=http://localhost:5500 bash scripts/smoke-proxy.sh
```

| Chequeo | Esperado |
|---|---|
| `index.html` | 200 |
| Login (va a identity) | 200 con `accessToken` |
| Sugerencia de Laura (va a ms-inscripcion) | 200 con `totalCreditos` e `id` |
| POST que viola un prerrequisito | 422 `INSCRIPCION_RECHAZADA` |
| Estudiante inexistente | 404 ProblemDetails |

Si ves `502 PROXY_SIN_CONEXION`, el proxy no llega a esa API: revisá que esté levantada y que `ENROLLMENT_ORIGIN` apunte al puerto correcto.

---

## 5. Validación del mock

El front tiene un modo *offline* (`useMock: true`). Este script verifica que el mock y el fixture `src/data/mocks/sugerencia_mock.json` respeten la regla N+3 y sean idénticos.

```bash
node src/pruebas/validar-mocks.mjs
```

Esperado: `TODO OK: 36/36 comprobaciones.`

> Si editás `assets/js/app.js`, **no toques el bloque `MOCK_SUGERENCIA`**: el script lo extrae del archivo y lo compara carácter por carácter.

---

## 6. Prueba manual en el navegador

Con la base limpia y los tres servicios arriba:

| # | Paso | Qué tenés que ver |
|---|---|---|
| 1 | Abrí http://localhost:5500 e ingresá con `admin` / `Admin123*` | Entrás al panel |
| 2 | Abrí **Sugerencia de matrícula** | Laura, **23 créditos**, 8 materias *Sugerida* y 4 bloqueadas con su motivo en español |
| 3 | Tocá **Confirmar** | Alerta verde "8 asignaturas inscritas (2026-2)". La tabla se recarga sin esas materias |
| 4 | Resetea la base. Cargá la página **y después** inscribí 603601 por fuera:<br>`curl -X POST http://localhost:8081/api/inscripciones -H 'Content-Type: application/json' -d '{"estudianteId":1,"periodo":"2026-2","codigosMaterias":["603601"]}'`<br>Ahora tocá **Confirmar** | La fila 603601 se marca en rojo con el mensaje de la API (`MATERIA_YA_INSCRITA`) y aparece la alerta "No se inscribió ninguna asignatura". La inscripción es **todo o nada** |
| 5 | Pará ms-inscripcion (`docker compose stop api`) y recargá | Alerta con el error 502 del proxy |
| 6 | Poné `useMock: true` en `assets/js/app.js` y recargá | La tabla muestra el mock (también 23 créditos), sin backend |
| 7 | Abrí http://localhost:5500/index.html en una ventana privada y probá una clave incorrecta | No entra. Identity responde 500 en vez de 401: es una limitación conocida. El enlace "Cerrar Sesión" del menú apunta al sistema de Unillanos, no cierra la sesión local |

### Otros estudiantes para probar la regla

Cambiá `estudianteId` en `API_CONFIG` (`assets/js/app.js`):

| Id | Estudiante | Qué demuestra |
|---|---|---|
| 1 | Laura (sem 6) | Prerrequisitos estrictos: 23 créditos |
| 2 | Mateo (sem 7) | **Tope de 3**: 603804 y 603806 quedan bloqueadas por límite |
| 3 | Camila (sem 7) | Una materia **adeudada** (603305) ocupa uno de los 3 lugares |
| 4 | Sofía (sem 8) | Materias con cruce o sin cupo **no gastan lugar** |
| 5 | Andrés (sem 9) | Tiene el único cupo de 603903 |
