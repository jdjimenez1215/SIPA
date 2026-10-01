# Tasks: Inscripcion Microservice

Rules: NO TDD (tests after code), never build/test except task 8.1, do not commit. Code in English, messages in Spanish. Refs = spec capability.

## Batch 1: Skeleton
- [x] 1.1 Create `Directory.Build.props` (net8.0, Nullable, ImplicitUsings, RollForward=Major) [deployment-runtime]
- [x] 1.2 Create `Directory.Packages.props` with all pins from design (CPM) [deployment-runtime]
- [x] 1.3 Create `.config/dotnet-tools.json` (dotnet-ef 8.0.11), `.gitignore`, `.dockerignore`
- [x] 1.4 Create 4 `src/*/*.csproj` + `tests/MsInscripcion.UnitTests/*.csproj` with references per design
- [x] 1.5 Create `MsInscripcion.sln` including all 5 projects

## Batch 2: Domain
- [x] 2.1 `Entities/`: Carrera, Materia, HorarioMateria, Prerrequisito, Estudiante, HistorialAcademico, Inscripcion [academic-catalog]
- [x] 2.2 `Enums/`: EstadoHistorial, EstadoInscripcion, DiaSemana
- [x] 2.3 `Rules/`: IEnrollmentRule, RuleViolation, EnrollmentContext, ErrorCodes [enrollment-rules]
- [x] 2.4 `Rules/EnrollmentRulesEngine.cs`, no short-circuit [Rules engine collects all violations]
- [x] 2.5 `CareerMatchRule` [R1]
- [x] 2.6 `PrerequisitesRule` [R2]
- [x] 2.7 `ScheduleOverlapRule`, half-open, `CandidatesBefore`, `conflictsWith` [R3]
- [x] 2.8 `SemesterLimitRule` [R4]
- [x] 2.9 `NoDuplicateRule` (YA_APROBADA/YA_INSCRITA) [R5]
- [x] 2.10 `SeatAvailabilityRule` [R6]
- [x] 2.11 `Services/PrerequisiteGraph.cs` cycle detection [Prerrequisito: self-reference, indirect cycle]
- [x] 2.12 `Exceptions/DomainException.cs` (code) [api-error-handling]

## Batch 3: Application
- [x] 3.1 `Common/Exceptions/`: NotFound, Conflict, BusinessRuleViolation [Status mapping]
- [x] 3.2 `Common/Options/EnrollmentOptions.cs` [Configuration]
- [x] 3.3 `Abstractions/Persistence/`: repositories, `IUnitOfWork`, `ITransaction`
- [x] 3.4 `Common/Behaviors/ValidationBehavior.cs` [Validation error]
- [x] 3.5 `Features/Inscripciones`: EnrollCommand+Validator (periodo regex, duplicate ids)+Handler (lock, engine, all-or-none) [Atomic enrollment; Request validation]
- [x] 3.6 `Features/Inscripciones`: CancelCommand+Handler [Cancel]
- [x] 3.7 `Features/Inscripciones`: GetInscripcionesQuery+Handler, default periodo [List enrollments]
- [x] 3.8 `Features/Materias`: MateriasDisponiblesQuery (`Candidates=[m]`) [available-subjects]
- [x] 3.9 `Features/Carreras`: CRUD commands/queries/DTOs/validators [Carrera CRUD]
- [x] 3.10 `Features/Materias`: CRUD + filtered GET [Materia CRUD; Catalog query]
- [x] 3.11 `Features/Horarios`: commands/DTOs/validators, invalid range, own overlap [Horario management]
- [x] 3.12 `Features/Prerrequisitos`: commands using PrerequisiteGraph [Prerrequisito management]
- [x] 3.13 `DependencyInjection.cs` (MediatR, validators, engine, 6 rules, options validation)

## Batch 4: Infrastructure
- [x] 4.1 `InscripcionDbContext` with snake_case naming [ADR 9]
- [x] 4.2 `Configurations/`: entities, string enums, checks, partial unique `ux_inscripciones_activa`, FK behaviors; no xmin
- [x] 4.3 `Repositories/`: `LockByIdsAsync` (`FOR UPDATE` ORDER BY id), counts, approved ids [Seat safety]
- [x] 4.4 `UnitOfWork` with transaction; map 23505/23503/40P01/40001 to Conflict [Concurrent duplicate/last seat]
- [x] 4.5 `Seed/SeedData.cs` via `HasData` (12 materias, 2 students, history, enrollments) [First start]
- [x] 4.6 `DependencyInjection.cs` (Npgsql, repos, UoW)

## Batch 5: Api
- [x] 5.1 `Json/TimeOnlyHHmmConverter.cs` [ADR 10]
- [x] 5.2 `Middleware/ExceptionHandlingMiddleware.cs`, full mapping table [ProblemDetails; Unhandled/Serialization failure]
- [x] 5.3 `Controllers/InscripcionesController` + `EstudiantesController` (only `mediator.Send`) [Thin controllers]
- [x] 5.4 `Controllers/`: Carreras, Materias, Horarios, Prerrequisitos [academic-catalog]
- [x] 5.5 `Program.cs`: Serilog, Swagger `MapType<TimeOnly>`, middleware, Database:ApplyMigrationsOnStartup [Migrate on startup]
- [x] 5.6 `appsettings.json` and `appsettings.Development.json` [Default X]

## Batch 6: Unit tests
- [x] 6.1 `Fixtures/` builders (Materia, Estudiante, Context)
- [x] 6.2 `Rules/CareerMatchRuleTests`, `PrerequisitesRuleTests` (success+failure) [R1, R2]
- [x] 6.3 `Rules/ScheduleOverlapRuleTests`: adjacent, existing, in-request, other day [R3]
- [x] 6.4 `Rules/SemesterLimitRuleTests` (incl. configured X), `NoDuplicateRuleTests` [R4, R5]
- [x] 6.5 `Rules/SeatAvailabilityRuleTests` (incl. cancelled) [R6]
- [x] 6.6 `Engine/EnrollmentRulesEngineTests`: all violations, none [Rules engine]
- [x] 6.7 `Services/PrerequisiteGraphTests` [cycle scenarios]
- [x] 6.8 `EnrollHandlerTests` with Moq: rollback on violations, commit on success [Atomic enrollment]

## Batch 7: Docker + README
- [x] 7.1 `Dockerfile` multi-stage, non-root [Compose up]
- [x] 7.2 `docker-compose.yml`: db healthcheck, api depends_on healthy [Compose up]
- [x] 7.3 `README.md`: Mermaid ER + sequence, structure, curl examples (`[6,7,8]`, `[1,2,6,4,9,10,11,12]`), assumptions [README content]

## Batch 8: Migration (ONLY allowed build)
- [x] 8.1 Run once `DOTNET_ROLL_FORWARD=Major dotnet ef migrations add InitialCreate -p src/MsInscripcion.Infrastructure -s src/MsInscripcion.Api`; verify no `xmin`. On failure report errors verbatim and STOP.

## Fix batch (post-verify)
- [x] F1 W3: `UseSerilogRequestLogging` registered before `ExceptionHandlingMiddleware` (Program.cs)
- [x] F2 S1 (concurrency bug): per-student lock (`IStudentRepository.LockByIdAsync`; EnrollCommandHandler locks student then materias; tests, design and README updated)
- [x] F3 W2: docs state concurrent duplicate = 422 under lock, 409 (23505) = defensive backstop (README, design ADR 12, api-error-handling spec)
- [x] F4 W1: api-error-handling spec says Infrastructure translates 40001/40P01 to ConflictException
- [x] F5 W6: deployment-runtime spec seed mentions second carrera ADM
- [x] F6 S3: validation `errors` keys camelCase (middleware)
- [x] F7 S4: HorarioRepository orders DiaSemana in memory
- [x] F8 S5: MateriaQueries validators use `RuleFor(x => x.CarreraId/Semestre).GreaterThan(0).When(HasValue)`
- [x] F9 S6: design.md MaxSemestersAhead naming + two-step lock; UnitOfWork "periodo"
- [ ] Not done by decision: W4, W5, S2, S7 (left for the user)
