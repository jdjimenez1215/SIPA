# Delta for Deployment Runtime

Docker composition and Documentation requirements are UNCHANGED (README MUST also document the DB recreation and the deprecation of materias-disponibles).

## MODIFIED Requirements

### Requirement: Migrate and seed on startup
The API MUST apply migrations at startup and seed idempotently the Unillanos data: carrera Ingenieria de Sistemas with the 53 materias and prerrequisitos of `malla_curricular.json`; Laura (`estudiante_prueba.json`, sem 6, 26 Aprobada); a minimal second carrera (for R1); invented but deterministic `cuposMaximos`, horarios without clashes between consecutive semestres, and auxiliary students including the helper student (sem 7, sem 1-6 Aprobada) where the cap of 3 is reached. A new migration MUST be provided.
(Previously: fictitious ING-SIS/ADM seed with MAT-style materias)

#### Scenario: First start
- GIVEN an empty database
- WHEN the API starts
- THEN the schema and Unillanos seed exist (53 materias, Laura, helper student)

#### Scenario: Restart
- GIVEN an already seeded database
- WHEN the API restarts
- THEN no duplicate seed rows are created

#### Scenario: Laura suggestion from seed
- GIVEN the seeded database
- WHEN GET Laura sugerencia
- THEN totalCreditos is 23

### Requirement: Configuration
Settings MUST include the connection string, `Enrollment:MaxNextSemesterSubjects` (default 3) and `Enrollment:CurrentPeriod`, overridable by environment variables. `Enrollment:MaxSemestersAhead` MUST NOT exist in code, appsettings or docker-compose.
(Previously: `Enrollment:MaxSemestersAhead` default 3)

#### Scenario: Override cap
- GIVEN env `Enrollment__MaxNextSemesterSubjects=2`
- WHEN 3 N+1 materias are requested
- THEN LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO is reported

#### Scenario: Default cap
- GIVEN no override
- WHEN the API reads options
- THEN the cap is 3

#### Scenario: Old key absent
- GIVEN the repository
- WHEN searched for `MaxSemestersAhead`
- THEN there are no matches

## REMOVED Requirements

### Requirement: Override X (MaxSemestersAhead)
(Reason: setting removed; see Configuration)
