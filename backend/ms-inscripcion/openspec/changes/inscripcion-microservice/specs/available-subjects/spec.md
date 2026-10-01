# Available Subjects Specification

## Purpose
GET /api/estudiantes/{id}/materias-disponibles lists materias the student can enroll in, using the same rules engine as POST.

## Requirements

### Requirement: Available materias
The system MUST evaluate each candidate (materias of the student's carrera not Aprobada) alone with the same engine and return only those with zero violations. `periodo` is optional and defaults to `Enrollment:CurrentPeriod`. Seat availability is advisory; POST re-checks.

#### Scenario: Only compliant materias
- GIVEN candidates: one clashing with an enrolled materia, one over the semester limit, one valid
- WHEN GET
- THEN only the valid one is returned

#### Scenario: Approved and enrolled excluded
- GIVEN one materia Aprobada and one Activa this periodo
- WHEN GET
- THEN neither is returned

#### Scenario: Full materia excluded
- GIVEN a materia with no seats left
- WHEN GET
- THEN it is not returned

#### Scenario: Unknown student
- GIVEN no such student
- WHEN GET
- THEN 404 ESTUDIANTE_NO_ENCONTRADO
