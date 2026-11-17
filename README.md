# SIPA - Proyecto Parcial

[![CI](https://github.com/jdjimenez1215/SIPA/actions/workflows/ci.yml/badge.svg)](https://github.com/jdjimenez1215/SIPA/actions/workflows/ci.yml)

Sistema de **sugerencia e inscripción de materias** para Ingeniería de Sistemas (Unillanos). Tiene un front web, una API de identidad (login JWT), el microservicio de inscripción y un asistente de IA, que aplica la regla N+3 (semestre N más hasta 3 materias extra), **prioriza las materias más antiguas**, evita cruces de horario, muestra los **cupos disponibles** y respeta un **tope de 18 créditos** (20 si el promedio es 4.0 o más). Cada materia se puede marcar o desmarcar antes de confirmar.

## Documentación

### Diagramas fuente: https://drive.google.com/drive/folders/1TX8xGdMn67DkWf8xb1m4oZKuO1uS4yuQ?usp=drive_link

| Documento | Contenido |
|---|---|
| [Cómo correr el sistema](#cómo-correr-el-sistema-completo) (abajo) | Levantar las 3 piezas: puertos, variables y credenciales |
| [docs/pruebas.md](docs/pruebas.md) | Cómo correr las suites de prueba y la prueba manual en el navegador |
| [docs/despliegue-azure.md](docs/despliegue-azure.md) | Pipeline, Container Apps, secretos y variables de producción |
| [GUIA_BACKEND.md](GUIA_BACKEND.md) | Contrato front ↔ API |
| [backend/ms-asistente/README.md](backend/ms-asistente/README.md) | Asistente de IA: endpoint, proveedores y fallback |
| [docs/arquitectura-y-ajustes.md](docs/arquitectura-y-ajustes.md) | Cómo está construido (diagramas), la regla N+3 y los ajustes y bugs corregidos |
| [backend/ms-inscripcion/README.md](backend/ms-inscripcion/README.md) | Detalle del microservicio: endpoints, configuración, seed y ejemplos curl |
| [backend/mini-identity-api-dotnet/README.md](backend/mini-identity-api-dotnet/README.md) | API de identidad |

### Arranque rápido con Docker (recomendado)

Levanta las **cinco piezas** (PostgreSQL, ms-inscripción, identity, asistente y front) con un solo comando:

```bash
# 1. Clave de firma JWT compartida (obligatoria; no está en el repo)
export JWT_KEY="$(openssl rand -base64 48)"

# 2. Stack completo
docker compose up -d --build

# 3. Ver que todo quedó sano
docker compose ps
```

Abrí **http://localhost:5500** y entrá con:

| Cuenta | Contraseña | Alcance |
|---|---|---|
| `admin` | `Admin123*` | Administra usuarios y cualquier estudiante |
| `laura` | `Laura123*` | Solo sus datos (estudiante 1); puede usar el chat de IA |

- Puertos: front `5500`, identity `5132`, API `8080`, asistente `8083`, PostgreSQL `5432` (todos en `127.0.0.1`).
- Si venís de una versión anterior, la primera vez usá `docker compose down -v` para crear el rol de BD `sipa_app` y la base `identidad`.
- Health checks: `http://localhost:8080/ready`, `http://localhost:5132/health`.
- Detener: `docker compose down`. Reiniciar datos demo: `docker compose down -v`.
- Si un puerto está ocupado: `API_PORT=8081`, `DB_PORT=5433`, `IDENTITY_PORT=5133`, `FRONT_PORT=5501`.

> También podés guardar las variables en `SIPA/.env` (`JWT_KEY=...`, y opcionalmente `GROQ_API_KEY=...`). Ese archivo está en `.gitignore`. Sin clave de IA, el asistente usa el modo de reglas.

### Arranque rápido sin Docker (Git Bash)

```bash
# 0. Clave de firma JWT compartida (obligatoria; no está en el repo)
export JWT_KEY="$(openssl rand -base64 48)"

# 1. Backend de inscripción + PostgreSQL
cd backend/ms-inscripcion && API_PORT=8081 DB_PORT=5433 docker compose up --build -d && cd ../..

# 2. Identity (otra terminal, con la MISMA JWT_KEY)
export JWT_KEY="<la misma clave del paso 0>"
dotnet run --project backend/mini-identity-api-dotnet/src/MiniIdentityApi.Api --launch-profile http

# 3. Front + proxy (otra terminal)
ENROLLMENT_ORIGIN=http://localhost:8081 python dev-server.py
```

En **PowerShell** generá la clave así (equivale a `openssl rand -base64 48`):

```powershell
$b = New-Object byte[] 48
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
$env:JWT_KEY = [Convert]::ToBase64String($b)
```

> `ms-inscripcion` valida los JWT que emite `mini-identity-api`: ambos leen `Jwt:Key` de la
> variable `JWT_KEY` (o `Jwt__Key`). Sin ella el backend no arranca, y en `Production` se rechaza
> la clave de desarrollo. Mínimo 32 bytes.
> Todas las rutas `/api/*` salvo `/api/auth/*` exigen `Authorization: Bearer <accessToken>`.

## Cómo correr el sistema completo

El front (HTML/JS estático) se sirve con `dev-server.py`, que además actúa de **proxy mismo-origen** hacia los dos backends (sin CORS).

### Prerrequisitos

**Con Docker (recomendado):**

- Docker + Docker Compose v2 (nada más: los Dockerfiles ya incluyen .NET y Python)

**Sin Docker (desarrollo local):**

- Docker + Docker Compose (solo para PostgreSQL + ms-inscripcion)
- .NET SDK 10 (mini-identity-api)
- Python 3 (dev-server del front)
- Node.js 22 (tests del front y `validar-mocks.mjs`)

### 1) ms-inscripcion (docker compose)

```bash
# Clave de firma compartida con MiniIdentityApi (no se commitea)
export JWT_KEY="$(openssl rand -base64 48)"

cd backend/ms-inscripcion
docker compose up -d --build
# si 8080 está ocupado:                API_PORT=8081
# si un PostgreSQL nativo usa 5432:    DB_PORT=5433
API_PORT=8081 DB_PORT=5433 docker compose up -d --build
```

> En Windows es común que un PostgreSQL nativo ocupe el 5432 (usar `DB_PORT=5433`) y que el 8080 esté tomado (usar `API_PORT=8081`). En PowerShell: `$env:JWT_KEY="<clave>"; $env:API_PORT="8081"; $env:DB_PORT="5433"; docker compose up -d --build`.
> Reiniciar los datos de demo: `docker compose down -v`.
> `db` y `api` se publican solo en `127.0.0.1`: no quedan expuestos a la red local.

### 2) Identity (mini-identity-api, .NET)

```bash
export JWT_KEY="<la misma clave que usa ms-inscripcion>"
dotnet run --project backend/mini-identity-api-dotnet/src/MiniIdentityApi.Api --launch-profile http
```

Escucha en `http://localhost:5132`. Usuario: `admin` / `Admin123*`. Sin `JWT_KEY` usa la clave
de `appsettings.Development.json` (solo válida en `Development`; `ms-inscripcion` en Docker corre
en `Production` y la rechaza, así que exportá la misma clave en ambas terminales).

### 3) Front + proxy

```bash
ENROLLMENT_ORIGIN=http://localhost:8081 python dev-server.py
```

PowerShell:

```powershell
$env:ENROLLMENT_ORIGIN="http://localhost:8081"; python dev-server.py
```

Abrir <http://localhost:5500>. Variables: `AUTH_ORIGIN` (def. `http://localhost:5132`), `ENROLLMENT_ORIGIN` (def. `http://localhost:8080`), `PORT` (def. 5500). Al arrancar imprime la tabla de ruteo.

### Ruteo del proxy

| Ruta (`/api/...`) | Destino | Variable |
|---|---|---|
| `/api/auth`, `/api/users`, `/api/roles`, `/api/demo` | identity | `AUTH_ORIGIN` |
| cualquier otro `/api/*` (`/api/estudiantes/...`, `/api/inscripciones`) | ms-inscripcion | `ENROLLMENT_ORIGIN` |

Si el destino está caído, el proxy responde `502 application/problem+json` (`PROXY_SIN_CONEXION`) con el origen en `detail`.

### Seguridad y alcance de las cuentas

- **Registro solo para Admin**: `POST /api/auth/register` exige rol `Admin`, contraseña fuerte (8+, mayúscula, minúscula, dígito y símbolo) y email válido. El Admin puede vincular la cuenta a un estudiante con `estudianteId`.
- **Sin IDOR**: cada usuario recibe un claim `estudianteId`; `ms-inscripcion` solo permite leer/inscribir su propio estudiante (`403` si intenta otro). `Admin` puede operar sobre cualquiera. Cancelar una inscripción por id es solo para `Admin`.
- **Fuerza bruta**: el login permite 5 intentos por minuto por IP; después responde `429`.
- **Errores**: 400/401/403/404/409 con `application/problem+json` y `code`.
- **Persistencia**: usuarios y roles se guardan en PostgreSQL (base `identidad`) cuando hay `ConnectionStrings__Default`; sin connection string (tests o demo local) usa memoria.
- **BD**: la API usa el rol `sipa_app`, no el superusuario `postgres`.
- **Recuperación de contraseña y consulta de usuario**: deshabilitadas a propósito; los enlaces muestran “no disponible” y no llaman a la red.
- **IA**: `ms-asistente` lee el `estudianteId` solo del JWT y reenvía el token a ms-inscripción; nunca inscribe. La API key (`GROQ_API_KEY` o `ANTHROPIC_API_KEY`) es un secreto de entorno y el fallback de reglas se activa si falla.
- **Producción/Azure**: `SWAGGER_ENABLED=false` por defecto; `JWT_KEY`, `POSTGRES_PASSWORD` y `DB_APP_PASSWORD` se leen de variables de entorno (ver `.env.example`). `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` ya está configurado para el ingress. Ver [docs/despliegue-azure.md](docs/despliegue-azure.md).
- **Identity con migraciones EF**: al cambiar el modelo ya no se usa `EnsureCreated`; el arranque ejecuta `MigrateAsync`. En local, tras cambios de esquema hacé `docker compose down -v`.
- **Front**: `dev-server.py` es para desarrollo. Para producción usá un reverse proxy que replique `/api/*` (ver la misma guía).

### Configuración del front (`API_CONFIG` en `assets/js/app.js`)

- `estudianteId: 1` → Laura Gómez Ríos (datos semilla)
- `periodo: '2026-2'`
- `useMock: false` → backend real; `true` → demo offline con el mock (sin red)

### Qué deberías ver

Tras iniciar sesión, `sugerencia-matricula.html` muestra a Laura con **12 filas**: **6 Sugeridas** (verde, 18 créditos), **2 bloqueadas por el límite de créditos** (603701 y 603703) y **4 bloqueadas por prerrequisito**. Abajo aparece la tabla de **materias inscritas** del período.

El límite es **18 créditos**, o **20** cuando el promedio histórico es **4.0 o más**. La sugerencia evalúa primero las materias adeudadas más antiguas y descarta las que se cruzan en horario.

Cada fila muestra sus **cupos disponibles / máximos** y una casilla de selección (marcada por defecto; las bloqueadas no se pueden marcar). Al desmarcar una materia bajan los créditos seleccionados y solo se inscriben las que queden marcadas.

En **Materias inscritas** cada fila tiene un botón **Cancelar** (el estudiante puede cancelar las suyas; Admin cualquiera), que libera el cupo y devuelve la materia a la sugerencia.

Al final hay un **Asistente de matrícula (IA)**: entrá como `laura`, preguntá qué inscribir o por qué una materia está bloqueada. El asistente **solo propone** y pre-marca casillas; la inscripción la confirmás vos y pasa por el motor de reglas. Con `GROQ_API_KEY` usa Groq (`openai/gpt-oss-20b` por defecto); sin clave funciona en **modo reglas** (`fallback=true`). La cuenta Admin no puede usarlo (no tiene `estudianteId`).

### Verificación

```bash
# Tests unitarios (147 ms + 30 identity + 5 asistente), sin Docker
dotnet test backend/ms-inscripcion/tests/MsInscripcion.UnitTests
dotnet test backend/mini-identity-api-dotnet/src/MiniIdentityApi.Tests
dotnet test backend/ms-asistente/tests/MsAsistente.Tests

# Tests del front (jsdom) y del mock
npm ci
npm test
npm run test:mock

# E2E del backend (159 checks; requiere el stack arriba y BD limpia)
PORT=8080 bash backend/ms-inscripcion/scripts/e2e.sh

# Smoke vía el proxy (solo lectura/rechazo; no escribe en la BD)
bash scripts/smoke-proxy.sh              # BASE=http://localhost:5500 por defecto (7 checks)

# Paridad del mock con el fixture (esperado 36/36)
node src/pruebas/validar-mocks.mjs
```

Checklist manual:

1. `validar-mocks.mjs` → 36/36.
2. Login `admin` / `Admin123*` funciona por :5500.
3. Tabla: 12 filas, 6 Sugeridas, 2 bloqueadas por créditos, 4 por prerrequisito, total 18.
4. 422: cargar la página, inscribir `603601` por `curl` (`POST /api/inscripciones`), confirmar en la UI → `MATERIA_YA_INSCRITA` en esa fila, alerta visible, sin recarga.
5. 201 sobre BD limpia → alerta de éxito, la recarga deja solo las bloqueadas y el botón deshabilitado (reiniciar con `docker compose down -v`).
6. `estudianteId: 999` → alerta 404; detener ms-inscripcion → alerta 502 con el origen.
7. `useMock: true` → demo offline intacta.
