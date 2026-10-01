# Proposal: Inscripcion Microservice (greenfield)

## Intent
A .NET 8 service for course enrollment. It enforces 6 business rules atomically, reports ALL violations for each materia, and keeps seat quotas safe under concurrency.

## Scope
### In Scope
- Clean Architecture solution (Domain/Application/Infrastructure/Api) and unit tests
- EF Core entities, configurations, DbContext, initial migration, seed data (with clashing horarios)
- Enrollment endpoints and catalog CRUD
- One Strategy class per rule, each with success and failure tests
- ProblemDetails, global middleware, Serilog, Swagger
- Dockerfile, docker-compose, README (Mermaid diagrams, assumptions)

### Out of Scope
- Authentication; a periods entity or check that a period is open
- PostgreSQL integration tests (deferred)
- Running builds or tests (user rule)

## Capabilities
### New Capabilities
- `enrollment-rules`: the 6 rule strategies and an engine that collects ALL violations
- `enrollment-management`: atomic POST, soft-cancel DELETE, GET by student/period, seat locking
- `available-subjects`: materias-disponibles, using the same engine
- `academic-catalog`: CRUD for carreras, materias, horarios, prerrequisitos (cycle check); GET /materias with filters
- `api-error-handling`: RFC 7807, error codes, status mapping, middleware
- `deployment-runtime`: Docker/compose, migrate on startup, seed data, configuration

### Modified Capabilities
None

## Approach
- Thin Controllers (`mediator.Send`); MediatR 12.x; FluentValidation via `ValidationBehavior`
- Strategy `IEnrollmentRule` → violations; no short-circuit; preloaded `EnrollmentContext`
- Seats: in one transaction, `FOR UPDATE` on materia rows ordered by id, count Activa rows, then insert; partial unique index on Activa rows (23505 → MATERIA_YA_INSCRITA)
- Periodo is a `YYYY-N` string; defaults to `Enrollment:CurrentPeriod`
- Horarios are half-open `[start, end)` intervals
- Package pins: MediatR 12.x, FluentAssertions 6.x, Moq 4.20.72, EF/Npgsql 8.0.x

## Decision: Initial Migration (RESOLVED)
- The user approved ONE exception to the no-build rule: run `dotnet ef migrations add InitialCreate` once, at the end of apply (`DOTNET_ROLL_FORWARD=Major`, local tool manifest with dotnet-ef 8.x). No other builds or tests.

## Affected Areas
| Area | Impact |
|------|--------|
| `src/Domain`, `src/Application`, `src/Infrastructure`, `src/Api` | New |
| `tests/*.UnitTests` | New |
| `Directory.Build.props`, `Dockerfile`, `docker-compose.yml`, `README.md` | New |

## Risks
| Risk | Likelihood | Mitigation |
|------|------------|------------|
| Code cannot be compiled or tested here | High | Static verification; the user builds |
| The one-time `dotnet ef` build fails because of compile errors | Med | Report the compile errors verbatim to the user and stop; do NOT retry or loop builds |
| `dotnet-ef` 8 on runtime 10 | Med | `DOTNET_ROLL_FORWARD=Major` |
| Locking not tested without PostgreSQL | Med | compose manual test; unique index as backstop |
| materias-disponibles is advisory for seats | Low | POST re-checks under the lock |

## Rollback Plan
No commits yet. Delete `src/`, `tests/` and the root files; run `docker compose down -v`.

## Dependencies
- SDK 10 plus the net8 targeting pack (NuGet), Docker, PostgreSQL 16 image

## Success Criteria
- [ ] Each of the 6 rules is its own class with success and failure tests
- [ ] POST is atomic; 422 lists ALL violations, each with its materia
- [ ] Concurrent POSTs never exceed CuposMaximos
- [ ] `docker compose up` gives a migrated, seeded API with Swagger
- [ ] Controllers have no business logic; the README has assumptions and Mermaid diagrams
