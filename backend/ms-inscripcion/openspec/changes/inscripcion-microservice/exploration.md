## Exploration: inscripcion-microservice (design options, greenfield)

### Current State
Greenfield. Only `openspec/` and `.atl/` exist. No code, no tests. Stack is fixed by requirements: .NET 8, EF Core + PostgreSQL, MediatR, FluentValidation, Serilog, Swagger, xUnit/FluentAssertions/Moq, Docker. Environment has only .NET SDK/runtime 10. User rules: no builds after changes, no TDD, no commits, no AI attribution.

### Affected Areas (planned layout)
- `src/Domain` — entities, `IEnrollmentRule`, rules, `EnrollmentRulesEngine`, `RuleViolation`, error codes. No package dependencies.
- `src/Application` — MediatR commands/queries, FluentValidation, DTOs, repository/UoW abstractions, `EnrollmentOptions`.
- `src/Infrastructure` — DbContext, EF configs, migration, seed, repositories, seat locking.
- `src/Api` — controllers, exception middleware, ProblemDetails factory, Serilog, Swagger.
- `tests/*.UnitTests` — one test class per rule (success and failure).
- `Directory.Build.props`, `Dockerfile`, `docker-compose.yml`, `README.md`.

### 1. Controllers vs Minimal APIs
| | Pros | Cons |
|---|---|---|
| Controllers | `[ApiController]` gives automatic model-binding errors and ProblemDetails. Filters. Mature Swagger annotations (`ProducesResponseType`). Suits 5 CRUD resources plus 5 custom endpoints. `WebApplicationFactory` works the same either way. | More ceremony. |
| Minimal APIs | Less code, faster startup. | Needs endpoint groups and filters by hand. The API layer gets messy at ~20 endpoints. Swagger metadata is more verbose. |

**Recommendation: Controllers.** Justification for the README: many endpoints across resources, convention-based routing, filters, and a thin controller that only does `mediator.Send`. This makes "no logic in controllers" easy to check.

### 2. Rule validators: Specification vs Strategy
- **Specification** (`IsSatisfiedBy` returning bool): good for composing predicates. It gives no reason or details, so it needs extra work to report violations.
- **Strategy** (`IEnrollmentRule.Evaluate(context, materia)` returning `IEnumerable<RuleViolation>`): each rule carries its own code and message. Rules are injected as `IEnumerable<IEnrollmentRule>` and registered by DI. Adding a rule needs no change to existing code (OCP).

**Recommendation: Strategy (Specification-flavoured, but returns violations).**
```
interface IEnrollmentRule { string Code {get;}  IEnumerable<RuleViolation> Evaluate(EnrollmentContext ctx, Materia candidate); }
record RuleViolation(string Code, Guid MateriaId, string MateriaCodigo, string Message, IReadOnlyDictionary<string,object>? Details);
class EnrollmentRulesEngine { IReadOnlyList<RuleViolation> Evaluate(EnrollmentContext ctx, IEnumerable<Materia> candidates) }
```
- Rules: `CareerMatchRule`, `PrerequisitesRule`, `ScheduleOverlapRule`, `SemesterLimitRule`, `NoDuplicateRule` (covers MATERIA_YA_APROBADA and MATERIA_YA_INSCRITA), `SeatAvailabilityRule`.
- The engine runs every rule for every candidate and never short-circuits. It concatenates all violations. Each violation carries the `MateriaId`, so the caller can report which materia caused it.
- `EnrollmentContext` is loaded once by the handler with a fixed number of queries, to avoid N+1: student, approved materia ids from the history, active enrollments for the period with their schedules, seat counts per materia and period, `MaxSemestersAhead`, and the candidate schedules.
- Overlap inside one request: for candidate i, compare against existing active enrollments plus the other candidates that come earlier in request order. The violation goes on the later materia and names the conflicting materia in `Details` (`conflictsWith`). This is deterministic and avoids duplicate pairs.
- The rules need context data, so they are pure and unit-testable without a database (no Moq needed for the rules).

### 3. Seat-quota concurrency (PostgreSQL)
Key point: **`Materia` is never updated when someone enrolls**, so an `xmin` token on `Materia` never changes and gives no protection. Also, occupancy is per (materia, periodo), while `CuposMaximos` is on the materia.

| Option | How | Pros | Cons |
|---|---|---|---|
| A. xmin token on Materia | Touch the materia row and let `DbUpdateConcurrencyException` fire | Idiomatic EF | Must force an update of the row. Retries under contention. Poor fit: seats are per period. |
| B. `SELECT ... FOR UPDATE` on the materia rows | In the transaction: `SELECT * FROM materias WHERE id = ANY(@ids) ORDER BY id FOR UPDATE` (via `FromSqlInterpolated`), then count active enrollments for the period, then insert | Correct. Serializes per materia. Ordered ids avoid deadlocks. No counter drift. Simple. | Raw SQL. Lock held for the transaction (short). |
| C. Counter table `cupo_periodo(materia_id, periodo, ocupados)` with atomic conditional `UPDATE ... SET ocupados = ocupados + 1 WHERE ocupados < max` (`ExecuteSqlInterpolated`, check rows affected) | Lock-free check, atomic | Fast | Extra table. Counter can drift and must be decremented on cancel. More moving parts. |

**Recommendation: B** (pessimistic lock on ordered materia rows, `READ COMMITTED`, count after the lock). The atomic unit is one `IDbContextTransaction` (or a single `SaveChanges` after the lock) that inserts all Inscripciones or none.
Safety nets:
- Partial unique index on `inscripciones (estudiante_id, materia_id, periodo_academico) WHERE estado = 'Activa'` (Npgsql `HasFilter`). It backstops duplicates from concurrent requests. Catch `PostgresException` with SqlState 23505 and map it to MATERIA_YA_INSCRITA.
- Map 40001/40P01 to 409 CONFLICTO_CONCURRENCIA (optionally retry once).
- Optional: `xmin` on `Inscripcion` only, so a double cancel is caught. Cancelling an already cancelled enrollment is a plain state check (409 INSCRIPCION_YA_CANCELADA).
- Only `Activa` enrollments count toward seats.

**Npgsql xmin mapping (EF Core 8 / Npgsql 8):** `UseXminAsConcurrencyToken()` is obsolete since Npgsql 7. Current approach:
```csharp
public uint Version { get; set; }               // on the entity
builder.Property(e => e.Version).IsRowVersion(); // maps to system column xmin
```
The legacy equivalent is `.HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken()`. The `IsRowVersion()` form is from memory of the Npgsql docs (not re-verified online). Verify it with a migration diff, since xmin must not appear as a created column in the migration.

### 4. `materias-disponibles` reuses the same rules
- Handler: load the student, the `EnrollmentContext` for the period, and candidates (materias of the student's carrera, not in the approved set).
- For each candidate, call `engine.Evaluate(ctx, [candidate])` on its own. It is available if the result has zero violations. This is the same engine and rule classes as POST, so there is no duplicated logic.
- The `SeatAvailabilityRule` here is a soft check (count-based, no lock), so availability is advisory.
- Cross-request overlap does not apply, since each candidate is evaluated alone against the already enrolled materias.

### 5. Schedule-overlap model
`HorarioMateria { DayOfWeek Dia; TimeOnly HoraInicio; TimeOnly HoraFin }`. Npgsql maps `TimeOnly` to `time`. Store `DayOfWeek` as int (or as a string via a converter, for readability).
Overlap on the same day: `a.Start < b.End && b.Start < a.End`. Intervals are half-open `[start, end)`, so back-to-back blocks (10:00-12:00 and 12:00-14:00) do NOT overlap.
Invariants: `HoraInicio < HoraFin`, no midnight crossing. Validate these in the horario CRUD. Blocks of the same materia must not overlap each other. JSON format: `"HH:mm"`; needs a `TimeOnly` converter on .NET 8 System.Text.Json (it is not native on net8; add a converter or a DTO string).

### 6. "Periodo activo"
No period entity exists in the model. **Assumption:**
- `periodo` is a string `YYYY-N` with N in {1,2} (regex `^\d{4}-[12]$`).
- POST takes it from the body (required, format validated). Rule 5 and rule 3 use that periodo.
- GET `/inscripciones?periodo=` and GET `/materias-disponibles?periodo=` take it as an optional query parameter and default to `Enrollment:CurrentPeriod` in appsettings (e.g. `2026-2`).
- No "period is open" validation, and no periods table. Document it in the README.
- `Cursando` in the history does not count as approved (only `Aprobada` does). `Reprobada` allows a retake. `Cancelada` enrollments do not count toward duplicates, schedule or seats. A re-enrollment after cancelling is allowed.
- History may hold several rows per materia (Reprobada, then Aprobada), so use a surrogate `Id` or the key (EstudianteId, MateriaId, Periodo).
- Prerequisite CRUD must reject self-reference and cycles (PRERREQUISITO_CICLICO).

### 7. Package versions (net8.0)
| Package | Pin |
|---|---|
| MediatR | **12.5.0** (12.x, Apache-2.0). MediatR 13+ moved to a commercial licence with a licence key, so do not upgrade. |
| FluentValidation + FluentValidation.DependencyInjectionExtensions | 11.11.x (avoid the auto-validation pipeline, which is deprecated). Use a MediatR `ValidationBehavior`. |
| Serilog.AspNetCore | 8.0.x |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.x (8.0.11) |
| Microsoft.EntityFrameworkCore.Design | 8.0.x (`PrivateAssets=all`) |
| Swashbuckle.AspNetCore | 6.9.0 (stable on net8) |
| xunit 2.9.x, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk | current 17.x |
| **FluentAssertions** | **6.12.2**. v8+ needs a paid licence for commercial use, so pin 6.x. |
| Moq | 4.20.72 (4.20.0 and 4.20.1 had the SponsorLink issue). |
| Microsoft.AspNetCore.Mvc.Testing | 8.0.x (optional integration tests) |

Licence facts are from memory and were not re-verified online. Re-check them when writing the `.csproj` files.

### 8. Environment gotchas
- Put `<RollForward>Major</RollForward>` in `Directory.Build.props`. It only affects executables and test hosts, which is what is needed to run tests on runtime 10. SDK 10 can build net8.0 targets, but it needs the net8 targeting pack from NuGet (internet needed).
- `dotnet-ef` 8.x is a net8 tool and will fail on a machine with only runtime 10. Set `DOTNET_ROLL_FORWARD=Major` when running it (via a local tool manifest, `dotnet tool install dotnet-ef --version 8.0.x`).
- **Conflict with the user rule "never build".** `dotnet ef migrations add` builds the project. Options: (1) hand-write the migration and the `ModelSnapshot` (error-prone), (2) ask the user to allow this one build, (3) generate it once and leave the snapshot to the user. Recommend (2), and if refused, (1) with a clear warning. The app should call `Database.Migrate()` on startup in the docker-compose setup. Seed data goes in via `HasData` or an idempotent seeder.
- Docker: `mcr.microsoft.com/dotnet/sdk:8.0` and `aspnet:8.0`. The RollForward setting is harmless there.
- Unit tests for the rules need no database. Integration tests that use the lock need real PostgreSQL (EF InMemory and SQLite have no `FOR UPDATE`).

### 9. Error codes and ProblemDetails
Business (POST /inscripciones): CARRERA_NO_CORRESPONDE, PREREQUISITO_NO_CUMPLIDO, CRUCE_HORARIO, SEMESTRE_EXCEDIDO, MATERIA_YA_APROBADA, MATERIA_YA_INSCRITA, CUPO_AGOTADO.
Request/lookup: VALIDACION_FALLIDA (400), MATERIA_DUPLICADA_EN_SOLICITUD (400), ESTUDIANTE_NO_ENCONTRADO / MATERIA_NO_ENCONTRADA / INSCRIPCION_NO_ENCONTRADA / CARRERA_NO_ENCONTRADA (404), INSCRIPCION_YA_CANCELADA (409), CONFLICTO_CONCURRENCIA (409), ERROR_INTERNO (500).
CRUD: CODIGO_DUPLICADO (409), PRERREQUISITO_CICLICO (422), HORARIO_INVALIDO (422), ENTIDAD_EN_USO (409).

HTTP: 422 for rule violations, 400 for input validation, 404, 409 as above.
```json
{
  "type": "https://httpstatuses.io/422",
  "title": "La inscripción no cumple las reglas de negocio",
  "status": 422,
  "detail": "Se encontraron 2 violaciones.",
  "instance": "/api/inscripciones",
  "code": "INSCRIPCION_RECHAZADA",
  "traceId": "...",
  "violations": [
    { "code": "CRUCE_HORARIO", "materiaId": "...", "materiaCodigo": "MAT201",
      "message": "MAT201 se cruza con FIS101 (Lunes 08:00-10:00).",
      "details": { "conflictsWith": "FIS101" } }
  ]
}
```
For validation errors (400), use the `errors` dictionary from `ValidationProblemDetails` plus `code: VALIDACION_FALLIDA`. The global middleware maps domain exceptions (`BusinessRuleViolationException` carrying the violation list, `NotFoundException`, `ConflictException`) and logs unknown exceptions as 500 without leaking details. DELETE returns 204, and cancellation is a soft state change (`Estado = Cancelada`).

### Recommendation (summary)
Controllers. Strategy rules with a violation-collecting engine over a preloaded context. Seats via `SELECT ... FOR UPDATE` on ordered materia rows plus a partial unique index. Period from the request, defaulting to config. Pin MediatR 12.5.0, FluentAssertions 6.12.2, Moq 4.20.72. Half-open intervals.

### Risks
- The migration step needs a build (conflicts with the user's no-build rule). Ask the user or hand-write the migration.
- `dotnet-ef` 8 on runtime 10 needs `DOTNET_ROLL_FORWARD=Major`.
- Cannot compile or run tests in this workflow, so the code must be written carefully (static verification only).
- The `IsRowVersion()` and licence facts were not verified online.
- The pessimistic lock path cannot be tested without PostgreSQL (Testcontainers or docker-compose).
- Advisory `materias-disponibles` may show a materia as available that then hits CUPO_AGOTADO on POST.

### Ready for Proposal
Yes. Tell the user the assumptions above (period handling, half-open intervals, Cursando not counted as approved, 422 for violations, migration/build conflict) and ask whether one build is allowed to generate the migration.
