# SIPA - Proyecto Parcial

Sistema de **sugerencia e inscripción de materias** para Ingeniería de Sistemas (Unillanos). Tiene un front web, una API de identidad (login JWT) y el microservicio de inscripción, que aplica la regla N+3: todo el semestre N más hasta 3 materias extra.

## Documentación

| Documento | Contenido |
|---|---|
| [Cómo correr el sistema](#cómo-correr-el-sistema-completo) (abajo) | Levantar las 3 piezas: puertos, variables y credenciales |
| [docs/pruebas.md](docs/pruebas.md) | Cómo correr las 5 suites de prueba y la prueba manual en el navegador |
| [docs/arquitectura-y-ajustes.md](docs/arquitectura-y-ajustes.md) | Cómo está construido (diagramas), la regla N+3 y los ajustes y bugs corregidos |
| [backend/ms-inscripcion/README.md](backend/ms-inscripcion/README.md) | Detalle del microservicio: endpoints, configuración, seed y ejemplos curl |
| [backend/mini-identity-api-dotnet/README.md](backend/mini-identity-api-dotnet/README.md) | API de identidad |

### Arranque rápido (Git Bash)

```bash
# 1. Backend de inscripción + PostgreSQL
cd backend/ms-inscripcion && API_PORT=8081 DB_PORT=5433 docker compose up --build -d && cd ../..

# 2. Identity (otra terminal)
dotnet run --project backend/mini-identity-api-dotnet/src/MiniIdentityApi.Api --launch-profile http

# 3. Front + proxy (otra terminal)
ENROLLMENT_ORIGIN=http://localhost:8081 python dev-server.py
```

Abrí **http://localhost:5500** y entrá con `admin` / `Admin123*`.

## Cómo correr el sistema completo

El front (HTML/JS estático) se sirve con `dev-server.py`, que además actúa de **proxy mismo-origen** hacia los dos backends (sin CORS).

### Prerrequisitos

- Docker + Docker Compose (ms-inscripcion y su PostgreSQL)
- .NET SDK (mini-identity-api)
- Python 3 (dev-server del front)
- Node.js (solo para `validar-mocks.mjs`)

### 1) ms-inscripcion (docker compose)

```bash
cd backend/ms-inscripcion
docker compose up -d --build
# si 8080 está ocupado:                API_PORT=8081
# si un PostgreSQL nativo usa 5432:    DB_PORT=5433
API_PORT=8081 DB_PORT=5433 docker compose up -d --build
```

> En Windows es común que un PostgreSQL nativo ocupe el 5432 (usar `DB_PORT=5433`) y que el 8080 esté tomado (usar `API_PORT=8081`). En PowerShell: `$env:API_PORT="8081"; $env:DB_PORT="5433"; docker compose up -d --build`.
> Reiniciar los datos de demo: `docker compose down -v`.

### 2) Identity (mini-identity-api, .NET)

```bash
dotnet run --project backend/mini-identity-api-dotnet/src/MiniIdentityApi.Api --launch-profile http
```

Escucha en `http://localhost:5132`. Usuario: `admin` / `Admin123*`.

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

### Configuración del front (`API_CONFIG` en `assets/js/app.js`)

- `estudianteId: 1` → Laura Gómez Ríos (datos semilla)
- `periodo: '2026-2'`
- `useMock: false` → backend real; `true` → demo offline con el mock (sin red)

### Qué deberías ver

Tras iniciar sesión, `sugerencia-matricula.html` muestra a Laura con **12 filas**: **8 Sugeridas** (verde), **4 bloqueadas** por prerrequisito (amarillo, "Prerrequisito no aprobado") y **23 créditos** de total.

### Verificación

```bash
# Smoke vía el proxy (solo lectura/rechazo; no escribe en la BD)
bash scripts/smoke-proxy.sh              # BASE=http://localhost:5500 por defecto

# Paridad del mock con el fixture (esperado 36/36)
node src/pruebas/validar-mocks.mjs
```

Checklist manual:

1. `validar-mocks.mjs` → 36/36.
2. Login `admin` / `Admin123*` funciona por :5500.
3. Tabla: 12 filas, 8 Sugeridas, 4 prerrequisito, total 23.
4. 422: cargar la página, inscribir `603601` por `curl` (`POST /api/inscripciones`), confirmar en la UI → `MATERIA_YA_INSCRITA` en esa fila, alerta visible, sin recarga.
5. 201 sobre BD limpia → alerta de éxito, la recarga deja solo las bloqueadas y el botón deshabilitado (reiniciar con `docker compose down -v`).
6. `estudianteId: 999` → alerta 404; detener ms-inscripcion → alerta 502 con el origen.
7. `useMock: true` → demo offline intacta.
