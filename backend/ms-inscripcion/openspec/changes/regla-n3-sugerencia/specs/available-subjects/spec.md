# Delta for Available Subjects

## MODIFIED Requirements

### Requirement: Available materias (deprecated)
GET /api/estudiantes/{id}/materias-disponibles MUST return exactly the `Sugerida` rows of the same calculator as `sugerencia` (as MateriaDto), for the same student and periodo. It MUST be marked deprecated (Swagger/README) in favour of `sugerencia`. `periodo` is optional and defaults to `Enrollment:CurrentPeriod`.
(Previously: each non-Aprobada candidate was evaluated alone with the engine and returned if it had zero violations)

#### Scenario: Equals Sugerida rows
- GIVEN Laura
- WHEN GET materias-disponibles and GET sugerencia
- THEN the ids returned equal the ids of the `Sugerida` rows (603601-603606, 603701, 603703)

#### Scenario: Approved and enrolled excluded
- GIVEN one materia Aprobada and one Activa this periodo
- WHEN GET
- THEN neither is returned

#### Scenario: Cap and prerequisites applied
- GIVEN the helper student (sem 7, sem 1-6 Aprobada)
- WHEN GET
- THEN only 3 sem-8 materias (603801-603803) are returned, none of 603804, 603805, 603806

#### Scenario: Unknown student
- GIVEN no such student
- WHEN GET
- THEN 404 ESTUDIANTE_NO_ENCONTRADO
