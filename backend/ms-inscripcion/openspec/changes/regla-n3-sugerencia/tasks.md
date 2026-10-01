# Tasks: Regla N+3 y endpoint de sugerencia

Spec refs: ER=enrollment-rules, ES=enrollment-suggestion, EM=enrollment-management, AS=available-subjects, DR=deployment-runtime, AE=api-error-handling. Paths relative to `backend/ms-inscripcion/`. No TDD; tests after code. Agents never build/test/commit.

## Batch 1 - Domain (sub-agent)
- [x] 1.1 `Domain/Rules/ErrorCodes.cs`: add `LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO` (AE cap violation)
- [x] 1.2 `Domain/Rules/EnrollmentContext.cs`: `MaxSemesterAhead` -> `MaxExtraSubjects`; add `IsExtra(Materia)` (D5)
- [x] 1.3 Create `Domain/Rules/SemesterWindowRule.cs` (> N+1 -> `SEMESTRE_EXCEDIDO`); delete `SemesterLimitRule.cs` (ER R4)
- [x] 1.4 Create `Domain/Rules/ExtraSubjectsCapRule.cs` (active + prior candidates + evaluated; details `max`,`used`) (ER cap)
- [x] 1.5 `Domain/Rules/EnrollmentRulesEngine.cs`: add `EvaluateCandidate`; `Evaluate` composes it (D4)
- [x] 1.6 Create `Domain/Enums/EstadoSugerencia.cs` and `SuggestionRow`
- [x] 1.7 Create `Domain/Services/EnrollmentSuggestionCalculator.cs` per design algorithm (ES window/priority/cap/slot-passing)

## Batch 2 - Application (sub-agent)
- [x] 2.1 `Application/Common/Options/EnrollmentOptions.cs`: remove `MaxSemestersAhead`, add `MaxNextSemesterSubjects` (default 3, `>= 0` in `Validate()`) (DR config)
- [x] 2.2 Create `Features/Sugerencia/Dtos/SugerenciaDto.cs` (+ `NotificacionDto`; fixture names + id/periodo/motivo) (ES contract)
- [x] 2.3 Create `Features/Sugerencia/Queries/GetSugerenciaQuery.cs` (validator periodo, handler, 404) (ES unknown student)
- [x] 2.4 `Features/Materias/Queries/MateriasDisponiblesQuery.cs`: delegate to calculator, return only `Sugerida` (AS)
- [x] 2.5 `Abstractions/Persistence/IMateriaRepository.cs`: add `GetIdsByCodigosAsync`
- [x] 2.6 `Features/Inscripciones/Commands/EnrollCommand.cs`: `CodigosMaterias`, XOR validator, resolve codes before transaction, cap option in context (EM codigos/both-or-neither)
- [x] 2.7 `Application/DependencyInjection.cs`: register new rules + calculator, drop old rule

## Batch 3 - Infrastructure + Api (sub-agent)
- [x] 3.1 `Infrastructure/.../MateriaRepository.cs`: implement `GetIdsByCodigosAsync`
- [x] 3.2 Rewrite `Persistence/Seed/SeedData.cs`: 2 carreras, 53 Unillanos materias + ADM101, 33 prereqs, cupos (603903=1) (DR seed)
- [x] 3.3 SeedData: horarios (parity rule, overrides 603305/603902/ADM101)
- [x] 3.4 SeedData: students Laura/Mateo/Camila/Sofia/Andres with historial; Andres Activa 603903 (ES scenarios)
- [x] 3.5 `Api/Controllers/EstudiantesController.cs`: `GET {id}/sugerencia`; `[Obsolete]` on materias-disponibles
- [x] 3.6 `Api/Controllers/InscripcionesController.cs`: `EnrollRequest.CodigosMaterias`
- [x] 3.7 `appsettings.json`, `docker-compose.yml`: new key, remove old (DR old key absent)

## Batch 4 - Unit tests (sub-agent)
- [x] 4.1 `Rules/SemesterLimitRuleTests.cs` -> `SemesterWindowRuleTests.cs` (ER N+1/N+2/owed)
- [x] 4.2 Create `Rules/ExtraSubjectsCapRuleTests.cs` (ER within/over/active/N not counted)
- [x] 4.3 Update `Engine/EnrollmentRulesEngineTests.cs`, `Fixtures/TestData.cs` for new context
- [x] 4.4 Create `Services/EnrollmentSuggestionCalculatorTests.cs` (Laura 23, cap, Camila owed, Sofia A3, hidden-but-counted, last semester)
- [x] 4.5 Create `Seed/SeedDataTests.cs` (53 unicos, 33 prereqs, calculator over seed: Laura/Mateo/Camila/Sofia)
- [x] 4.6 Update `Handlers/EnrollCommandHandlerTests.cs` (cap, codigos XOR, unknown code) (EM)
- [x] 4.7 Create `Handlers/GetSugerenciaQueryHandlerTests.cs` (mapping, totalCreditos, 404)

## Batch 5 - Migration (ORCHESTRATOR ONLY)
- [x] 5.1 `DOTNET_ROLL_FORWARD=Major dotnet ef migrations add UnillanosSeed -p src/MsInscripcion.Infrastructure -s src/MsInscripcion.Api -o Persistence/Migrations`
- [x] 5.2 Review diff: only Insert/Update/DeleteData; snapshot ok
- [x] 5.3 `dotnet test`

## Batch 6 - E2E, Postman, README (sub-agent)
- [x] 6.1 Rewrite `scripts/e2e.sh`: new codes/ids, "Sugerencia" section, POST cases from design (EM, AE)
- [x] 6.2 Edit/regenerate `postman/MsInscripcion.postman_collection.json` (script in agent temp dir)
- [x] 6.3 `README.md`: rule, codes, config, seed tables, curl, deprecated endpoint, `down -v`, A1-A5

## Batch 7 - Run & verify (ORCHESTRATOR ONLY)
- [x] 7.1 `docker compose down -v && API_PORT=8081 DB_PORT=5433 docker compose up --build`
- [x] 7.2 Run `scripts/e2e.sh`; run newman (DR first start/restart)

> **Nota B5 (orquestador):** la migración incremental `UnillanosSeed` falló en BD nueva (23503: EF emitió `UpdateData` de inscripciones antes del `InsertData` de materias). Se regeneró una única `InitialCreate` (desvío del ADR D12, justificado: no hay BD productiva y el seed nuevo ya exige `docker compose down -v`). `dotnet test`: 114/114.
