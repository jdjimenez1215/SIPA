# Verification Report

**Change**: inscripcion-microservice
**Version**: N/A (specs are unversioned deltas)
**Mode**: Standard, STATIC ONLY (Strict TDD disabled; no build, no test, no docker, no ef executed by the verifier)

Important: no scenario can be marked COMPLIANT in the strict sense of the skill (test executed AND passed), because tests were never compiled or run (user rule). Matrix statuses below mean: `COVERED` = implementation located AND a unit test exists that, by static reading, should pass; `IMPL-ONLY` = implementation located, no unit test; `PARTIAL` = test covers only part of the scenario.

---

## Completeness
| Metric | Value |
|--------|-------|
| Tasks total | 45 (1.1-1.5, 2.1-2.12, 3.1-3.13, 4.1-4.6, 5.1-5.6, 6.1-6.8, 7.1-7.3, 8.1) |
| Tasks complete | 45 |
| Tasks incomplete | 0 |

Task 8.1 is corroborated: migration `20260930020943_InitialCreate` exists, is newer than every configuration/seed/entity file (no model drift), and no `xmin`/`RowVersion` appears anywhere in src.

## Build & Tests Execution
**Build**: not executed (user rule). Domain/Application/Infrastructure/Api are known to compile (`dotnet ef migrations add` succeeded).
**Tests**: not executed. 47 `[Fact]` found (6 rule classes 29, Engine 4, PrerequisiteGraph 8, EnrollCommandHandler 6; matches apply-progress).
**Coverage**: not available.

### Static compile review of tests/MsInscripcion.UnitTests (never compiled) - no compile ERROR found
| Risk | Verdict |
|------|---------|
| Moq `ReturnsAsync(new HashSet<int>())` on `Task<IReadOnlySet<int>>`, `List<>` on `Task<IReadOnlyList<>>`, `Dictionary<>` on `Task<IReadOnlyDictionary<>>` | OK. TResult is fixed by the exact bound from the `Setup` return type (`IReturns<TMock, Task<TResult>>`), the concrete value is only a lower bound. |
| `TestData.Materia(...)` method vs `Materia` type | OK. Inside `TestData` every use of `Materia` is in a type context (return type, `new Materia`, generic args, arrays), where member lookup ignores methods. Tests call `TestData.Materia` qualified. |
| `Student(...)` / `Context(...)` / `T(...)` names | OK, no clashes. |
| xunit `using Xunit;` | OK: `<Using Include="Xunit" />` in tests csproj (global). ImplicitUsings on via Directory.Build.props (System.Linq, Threading, Tasks). |
| Collection expressions (`[materia]`, `?? []`, `new PrerequisiteGraph([(1,2)])`, `new EnrollmentRulesEngine([])`) | OK with `LangVersion 12.0`. `new HashSet<int>(approved ?? [])` resolves to `IEnumerable<int>` overload. |
| FluentAssertions 6.12: `.Select(v => v.Code).Should().BeEquivalentTo("a","b")` | OK: `IEnumerable<string>.Should()` is `StringCollectionAssertions`, its `params string[]` overload wins over inherited generic overload (derived-class preference). |
| `BeEquivalentTo` on `(int,string)` ValueTuples | OK (ValueTuple overrides Equals; compared as value). |
| `await act.Should().ThrowAsync<T>()` on `Func<Task<IReadOnlyList<InscripcionDto>>>` | OK, `.Which` available. |
| Expression tree `r.Materia != null`, `i.FechaInscripcion == FixedNow` inside `OnlyContain` | OK. |
| `FixedTimeProvider : TimeProvider` (nested, primary ctor) | OK (.NET 8 BCL). |
| `Options.Create(...)` vs namespace `...Common.Options` | OK: no enclosing namespace named `Options`; `using` of a namespace does not import its name. |
| Package flow: tests reference Domain+Application only; `Microsoft.Extensions.Options` arrives transitively via Application | OK. |
| Nullable flow on `added!` / `added.Should()` (EnrollCommandHandlerTests:98-104) | Only a possible warning, not an error (no TreatWarningsAsErrors). |

Dynamic sanity check (mentally executed) of all 47 tests against the implementation: expectations match the code (e.g. engine test yields exactly the 5 listed violations; handler tests: 3 violations, rollback once, nothing persisted; graph tests match the DFS). No failing test predicted.

---

## Spec Compliance Matrix

### enrollment-rules
| Requirement | Scenario | Location / Test | Result |
|---|---|---|---|
| Engine collects all | Multiple violations returned | `EnrollmentRulesEngine.cs:8-11`; `EnrollmentRulesEngineTests.Evaluate_MultipleMateriasWithDifferentFailures_...` | COVERED |
| Engine collects all | No violations | `EnrollmentRulesEngineTests.Evaluate_ValidRequest_ReturnsNoViolations` | COVERED |
| R1 | Same career / Other career | `CareerMatchRule.cs:9-16`; `CareerMatchRuleTests` (2) | COVERED |
| R2 | All approved / One missing / No prereqs | `PrerequisitesRule.cs:9-31`; `PrerequisitesRuleTests` | COVERED. "P2 Cursando" only simulated by absence from `approved`; real filter in `StudentRepository.cs:19` is untested (see W5) |
| R3 | Adjacent blocks / Overlap existing / Same request (later only) / Other day | `HorarioMateria.cs:18-21` (half-open), `ScheduleOverlapRule.cs:11-32`, `EnrollmentContext.CandidatesBefore`; `ScheduleOverlapRuleTests`, engine test | COVERED. "Cancelada ignored" is enforced in `InscripcionRepository.cs:29-33`, untested |
| R4 | Within / Beyond / Configured X | `SemesterLimitRule.cs:9-22`; `SemesterLimitRuleTests` (4) | COVERED |
| R5 | Fresh / Approved / Active in period / Retake | `NoDuplicateRule.cs:9-25`; `NoDuplicateRuleTests` (5) | COVERED (Reprobada/Cancelada by construction, see W5) |
| R6 | Available / Full / Cancelled frees seat | `SeatAvailabilityRule.cs:9-22`, counts `InscripcionRepository.cs:17-25` (Activa + periodo only); `SeatAvailabilityRuleTests` | COVERED at rule level; cancelled scenario is tautological (count injected) |

### enrollment-management
| Requirement | Scenario | Location / Test | Result |
|---|---|---|---|
| Atomic enrollment | All valid (3, Activa, 201) | `EnrollCommand.cs:98-114`, `InscripcionesController.cs:22-27`; `Handle_AllRulesSatisfied_...` (2 materias) | COVERED |
| Atomic enrollment | One fails, none enrolled | `EnrollCommand.cs:91-96` (rollback, throw before `AddRange`); `Handle_AnyViolation_ThrowsWithAllViolationsAndPersistsNothing` | COVERED (not with literal CRUCE_HORARIO, equivalent) |
| Atomic enrollment | All violations returned (4 entries with materiaId/materiaCodigo) | `RuleViolation.cs`, middleware `ExceptionHandlingMiddleware.cs:71-78`; handler test (3 entries) | COVERED |
| Request validation | Bad periodo `2026-3` | `EnrollCommandValidator` `EnrollCommand.cs:25-29`, mapped 400 `ExceptionHandlingMiddleware.cs:83-101` | IMPL-ONLY (no validator test) |
| Request validation | Duplicate ids | `EnrollCommand.cs:31-36` + middleware `:85-86` | IMPL-ONLY |
| Request validation | Unknown student (404) | `EnrollCommand.cs:56-58`; `Handle_StudentNotFound_...` | COVERED |
| (lookup) | Unknown materia (404) | `EnrollCommand.cs:66-73`; `Handle_MateriaNotFound_...` | COVERED |
| Seat safety | Concurrent last seat | Lock `MateriaRepository.cs:19-21` BEFORE counts `EnrollCommand.cs:63-80`; second request re-counts after lock and gets CUPO_AGOTADO | IMPL-ONLY (needs integration/manual test) |
| Seat safety | Concurrent duplicate | Serialized by lock; loser sees winner's row and gets `MATERIA_YA_INSCRITA` as a 422 violation (NOT 409, see W2); safety net `UnitOfWork.cs:39-42` + index `InscripcionConfiguration.cs:23-26` | PARTIAL |
| Cancel | Active -> 204 / Cancel again 409 / Unknown 404 | `CancelCommand.cs:25-37`, `InscripcionesController.cs:34-38` | IMPL-ONLY (no test, W4) |
| List | With periodo / Default periodo / Unknown student | `GetInscripcionesQuery.cs:36-46`, `EstudiantesController.cs:20-21` | IMPL-ONLY |

### available-subjects
| Scenario | Location | Result |
|---|---|---|
| Only compliant / Approved+enrolled excluded / Full excluded / Unknown student | `MateriasDisponiblesQuery.cs:42-73` (`Candidates=[m]`, approved filtered, enrolled removed by R5, seats by R6, 404 at :44-46). README result (2,6,7,8) re-derived by hand from the seed: correct | IMPL-ONLY (no handler test) |

### academic-catalog
| Requirement | Scenario | Location / Test | Result |
|---|---|---|---|
| Carrera/Materia CRUD | Create / Duplicate codigo 409 / Delete in use 409 / 404s | `CarreraCommands.cs`, `MateriaCommands.cs`, FK Restrict in configs, `UnitOfWork.cs:45-48` | IMPL-ONLY |
| Horario management | Valid block / Inverted / Overlapping own block | `HorarioCommands.cs:61-77`, `:83-102` (adjacent 10-12 accepted, half-open) | IMPL-ONLY (`HorarioChecks` untested) |
| Prerrequisito management | Valid / Self-reference / Indirect cycle | `PrerequisiteGraph.cs`, `PrerrequisitoCommands.cs:51-79`; `PrerequisiteGraphTests` (8) | COVERED (graph); handler IMPL-ONLY |
| Catalog query | Filtered / No filters | `MateriaRepository.cs:45-57`, `MateriasController.cs:21-22` | IMPL-ONLY |

### api-error-handling
| Requirement | Scenario | Location | Result |
|---|---|---|---|
| ProblemDetails contract | Shape (type,title,status,detail,instance,code,traceId) | `ExceptionHandlingMiddleware.cs:35-40,103-114`; model-state path `Program.cs:33-47` | IMPL-ONLY (framework 404/405/415 not covered, S2) |
| Status mapping | Violations payload 422 / Validation 400 / duplicates 400 / 404 / 409 / 422 / 500 | `:44-61` | IMPL-ONLY; PostgresException 40001/40P01 NOT mapped here (W1) |
| Global middleware | Unhandled exception 500 generic + logged | `:29-30,56-60` | IMPL-ONLY |
| Global middleware | Serialization failure -> 409 CONFLICTO_CONCURRENCIA | Done in `UnitOfWork.Translate` (`:49-52`) and `MateriaRepository.cs:23-26` (lock-time deadlock), not in middleware | PARTIAL (W1) |
| Thin controllers | Controller review | All 6 controllers only build a command/query and `mediator.Send`; only `request.MateriaIds ?? []` null-coalescing | COMPLIANT (by inspection) |

### deployment-runtime
| Requirement | Scenario | Location | Result |
|---|---|---|---|
| Docker composition | Compose up | `Dockerfile` sdk:8.0/aspnet:8.0 non-root `app`, `docker-compose.yml` postgres:16-alpine + healthcheck + `depends_on: service_healthy`, `ASPNETCORE_ENVIRONMENT=Development` so Swagger is on | COMPLIANT (static) |
| Migrate and seed | First start / Restart idempotent | `Program.cs:83-88` `MigrateAsync`; seed via `HasData` in migration (inherently idempotent) | COMPLIANT (static) |
| Configuration | Override X / Default X=3 | `EnrollmentOptions.cs:13`, `appsettings.json:5-8`, compose env keys `Enrollment__MaxSemestersAhead`, `Database__ApplyMigrationsOnStartup`, `ConnectionStrings__Default` all match appsettings keys | COMPLIANT (static) |
| Documentation | README content | Run instructions, curl samples, Mermaid ER + flowchart + sequence, 10 assumptions | COMPLIANT |

**Compliance summary**: 62 spec scenarios; 0 "executed and passed" (nothing was executed). Every scenario has an implementation location; the R1-R6 / engine / graph / enroll-handler scenarios have unit tests (statically expected to pass); catalog, cancel, list, available-subjects, validation, middleware and deployment scenarios are IMPL-ONLY or PARTIAL as marked above.

---

## Correctness: the 6 business rules and cross-cutting checks
| Item | Status | Evidence |
|---|---|---|
| Half-open overlap `a.s < b.e && b.s < a.e`, same day | OK | `HorarioMateria.cs:18-21` |
| N+X limit `Semestre <= SemestreActual + X`, X from options | OK | `SemesterLimitRule.cs:9-10`, `EnrollCommand.cs:85` |
| Only `Aprobada` counts (prereqs and duplicates) | OK | `StudentRepository.cs:19` |
| Cancelada ignored (overlap, duplicate, seats) | OK | `InscripcionRepository.cs:17-21,29-33` all filter `Estado == Activa` |
| Seats counted only Activa in the periodo, across all students | OK | `InscripcionRepository.cs:17-25` |
| All violations, no short-circuit | OK | `EnrollmentRulesEngine.cs:8-11` (SelectMany over candidates x rules); handler throws one exception holding the full list |
| Atomicity | OK | single `SaveChangesAsync` inside one transaction; rollback on violation/missing/exception (`await using` disposes = rollback) |
| FOR UPDATE ordering before seat count | OK | `EnrollCommand.cs:63-64` lock, then `:78-80` approved/enrollments/counts; ids sorted asc (`:63`), SQL `ORDER BY id FOR UPDATE` (`MateriaRepository.cs:20`); Sort is below LockRows so locks are taken in id order |
| No business logic in controllers | OK | see thin controllers row |
| Domain zero package refs | OK | `MsInscripcion.Domain.csproj` has no ItemGroup |
| Application has no EF/Npgsql/AspNetCore refs | OK | csproj + `rg` over Application/Domain: no matches |
| Code in English, messages in Spanish | OK | identifiers English (domain nouns Spanish per requirement model); all user-facing strings Spanish |
| Seed vs README examples | OK | Re-derived by hand: `[1,2,6,4,9,10,11,12]` -> exactly 7 violations (MAT101, MAT201/CRUCE with FIS101, ETI101, ALG201, EDD301, ING601, ADM101); `[6,7,8]` -> 201 (no clashes, prereqs met); `[5]` -> CRUCE with ETI101; materias-disponibles -> ids 2,6,7,8; cancel + ALG201 flow valid |
| Migration vs configurations | OK | Tables/columns snake_case, string enums, checks `ck_horario_materia_rango` / `ck_prerrequisitos_no_self`, partial unique `ux_inscripciones_activa` (`estado = 'Activa'`), FK Restrict except Cascade horarios/prerrequisitos(materia_id), identity start 1000, no xmin; snapshot consistent |

## Coherence (Design)
| Decision | Followed? | Notes |
|---|---|---|
| ADR1 Controllers | Yes | |
| ADR2 Rules engine in Domain | Yes | |
| ADR3 Repos + UoW | Yes | |
| ADR4 Pessimistic lock + partial unique index | Yes, with deviation | design.md:82 says `FromSql(SELECT * ...)`; implemented as scalar `SqlQuery<int>` lock then a normal Include load (avoids composing over FOR UPDATE). Valid improvement. |
| ADR5-11 (int ids, HasData, CPM, RollForward, snake_case, HH:mm converter, custom middleware) | Yes | |
| ADR12 race duplicate -> 409 | Deviated in practice | see W2 |
| Config keys | Doc typo | design.md:133-134 says `MaxSemesterAhead`; spec and code use `MaxSemestersAhead` |

---

## Issues Found

**CRITICAL**: None.

**WARNING**
- W1. `src/MsInscripcion.Api/Middleware/ExceptionHandlingMiddleware.cs:44-61` - spec "Global exception middleware" says PostgreSQL 40001/40P01 MUST map to 409 CONFLICTO_CONCURRENCIA in the middleware. Middleware has no `PostgresException`/`DbUpdateException` case; the mapping lives only in `UnitOfWork.cs:49-52` (SaveChanges) and `MateriaRepository.cs:23-26` (lock step). Any other DB call raising these states would return 500. Functionally covered on the real paths; spec text not literally met.
- W2. Concurrent duplicate outcome contradicts docs. `EnrollCommand.cs:63-80`: the lock precedes the duplicate read, so the losing request (READ COMMITTED, new snapshot per statement) sees the winner's row and gets a 422 `INSCRIPCION_RECHAZADA` with violation `MATERIA_YA_INSCRITA`, not a 409. The 409 path (`UnitOfWork.cs:39-42`) is a defensive net that is practically unreachable. `README.md:115,156,442` (assumption 7), design ADR 12 and the api-error-handling status table state 409. The enrollment-management scenario itself ("fails with MATERIA_YA_INSCRITA") is satisfied. Fix the docs or the flow.
- W3. `src/MsInscripcion.Api/Program.cs:72-73` - `UseSerilogRequestLogging` is registered AFTER (inside) `ExceptionHandlingMiddleware`. Serilog's request logger logs any exception that passes through it as status 500 / Error, so every handled 404/409/422/400 raised as an exception is logged as a 500 error. Register Serilog request logging BEFORE the exception middleware.
- W4. Unit-testable spec scenarios without any test: `EnrollCommandValidator` (bad periodo, duplicate ids, empty materiaIds), `CancelCommandHandler` (204 / already cancelled / not found), `GetInscripcionesQueryHandler`, `MateriasDisponiblesQueryHandler` (4 scenarios), `HorarioChecks` (3 scenarios), add-prerequisite handler, `ExceptionHandlingMiddleware` mapping. Requirement 6 (tests per rule) is met; the rest of the spec is only IMPL-ONLY.
- W5. Rule-level tests for "Cursando/Reprobada do not count", "Cancelada ignored" and "Cancelled frees a seat" are tautological (the test injects the already-filtered approved set / counts). The real filters are in `StudentRepository.cs:19`, `InscripcionRepository.cs:17-21,29-33` with no test (would need integration or repository tests over EF InMemory/Testcontainers).
- W6. `src/MsInscripcion.Infrastructure/Persistence/Seed/SeedData.cs:19-20` - seed has 2 carreras (ADM added to demo R1). Original requirement 5 and spec deployment-runtime say "1 carrera". Justified in design.md and README, but the spec text was not updated. Reconcile (update spec) or accept explicitly.
- W7. Process risk: tests project has never been compiled or run, and the runtime behaviour of `SELECT ... FOR UPDATE` wrapped by EF as a subquery, concurrency scenarios and Docker start-up are unverified. No defect found by static review; recommend one authorised `dotnet test` and one manual two-session concurrency check with `docker compose up` before archive.

**SUGGESTION**
- S1. Per-student race: two simultaneous POSTs by the same student for DIFFERENT overlapping materias take disjoint locks (`MateriaRepository.cs:19-21`) and both can pass R3, persisting a schedule clash. Also lock the `estudiantes` row (`SELECT ... FROM estudiantes WHERE id = @id FOR UPDATE`) before the materia locks (fixed order: student then materias).
- S2. `Program.cs:30-48, 81` - framework-generated errors (unmatched route 404, 405, 415, some model-binding messages) do not carry `code`/`traceId` in every case, and binding messages in `errors` are English (the Spanish `JsonException` message from `TimeOnlyHHmmConverter.cs:19` is replaced by MVC's generic text). Consider `AddProblemDetails` + `UseStatusCodePages` customisation.
- S3. `ExceptionHandlingMiddleware.cs:97` - `errors` keys are PascalCase property names while the JSON contract is camelCase; consider camel-casing the keys.
- S4. `HorarioRepository.cs:15` - `OrderBy(h => h.DiaSemana)` sorts the string column alphabetically in SQL (Jueves, Lunes, ...), while DTO mappers sort by enum order in memory. Cosmetic inconsistency.
- S5. `MateriaQueries.cs:20,23` - `RuleFor(x => x.CarreraId!.Value)` works because of `.When`, but is fragile; prefer `RuleFor(x => x.CarreraId).GreaterThan(0).When(...)`.
- S6. `design.md:133-134` uses `MaxSemesterAhead` (typo vs `MaxSemestersAhead`); `design.md:82` describes `FromSql(SELECT *)` instead of the implemented two-step lock. Sync the design file before archive. Also `UnitOfWork.cs:42` says "período" while all other messages say "periodo".
- S7. Swagger is only enabled in Development and compose forces Development (`docker-compose.yml:59`); consider a dedicated `Swagger:Enabled` flag so Production-like containers can still expose it.

---

## Verdict
**PASS WITH WARNINGS** (0 CRITICAL, 7 WARNING, 7 SUGGESTION)

All 45 tasks are done, every spec requirement has an implementation, the six rules and the atomicity/lock ordering are logically correct, layering restrictions hold, seed/README/migration/Docker are mutually consistent, and no compile error was found in the never-compiled test project. Remaining risk is unexecuted behaviour (W7) and a few spec/doc mismatches (W1, W2, W3, W6).
