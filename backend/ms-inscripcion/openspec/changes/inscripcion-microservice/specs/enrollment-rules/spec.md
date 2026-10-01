# Enrollment Rules Specification

## Purpose
Six independent business rules (one Strategy class each) plus an engine that evaluates every rule for every candidate materia and returns ALL violations, each tied to its materia.

## Requirements

### Requirement: Rules engine collects all violations
The engine MUST run every rule for every candidate, MUST NOT short-circuit, and MUST return each violation with code, materiaId, materiaCodigo and a Spanish message.

#### Scenario: Multiple violations returned
- GIVEN candidate A breaks rules 1 and 4, and candidate B breaks rule 6
- WHEN the engine evaluates [A, B]
- THEN it returns 3 violations
- AND each names its materia and code

#### Scenario: No violations
- GIVEN candidates satisfy all rules
- WHEN the engine evaluates them
- THEN the result is empty

### Requirement: R1 Career match (CARRERA_NO_CORRESPONDE)
A materia MUST belong to the student's carrera.

#### Scenario: Same career
- GIVEN student and materia share CarreraId
- WHEN evaluated
- THEN no violation

#### Scenario: Other career
- GIVEN materia CarreraId differs from the student's
- WHEN evaluated
- THEN CARRERA_NO_CORRESPONDE is reported for that materia

### Requirement: R2 Prerequisites (PREREQUISITO_NO_CUMPLIDO)
Every prerequisite MUST have an `Aprobada` history entry. `Cursando` and `Reprobada` MUST NOT count. The violation SHOULD list the missing prerequisites in details.

#### Scenario: All approved
- GIVEN prerequisites P1, P2 are Aprobada
- WHEN evaluated
- THEN no violation

#### Scenario: One missing
- GIVEN P1 Aprobada and P2 Cursando
- WHEN evaluated
- THEN PREREQUISITO_NO_CUMPLIDO is reported, naming P2

#### Scenario: No prerequisites
- GIVEN the materia has none
- WHEN evaluated
- THEN no violation

### Requirement: R3 Schedule overlap (CRUCE_HORARIO)
Two horarios on the same day overlap iff `a.start < b.end && b.start < a.end` (half-open; back-to-back does not overlap). Candidates MUST be checked against the student's Activa enrollments of the same periodo AND against earlier candidates of the same request. The violation MUST go on the later materia and name the conflicting materia in details (`conflictsWith`). Cancelada enrollments MUST be ignored.

#### Scenario: No overlap, adjacent blocks
- GIVEN enrolled Lunes 08:00-10:00
- WHEN candidate is Lunes 10:00-12:00
- THEN no violation

#### Scenario: Overlap with existing enrollment
- GIVEN enrolled Lunes 08:00-10:00
- WHEN candidate is Lunes 09:00-11:00
- THEN CRUCE_HORARIO is reported, conflictsWith = enrolled materia

#### Scenario: Overlap inside the same request
- GIVEN no prior enrollments and request [X Martes 08:00-10:00, Y Martes 09:00-11:00]
- WHEN evaluated
- THEN CRUCE_HORARIO is reported on Y only, conflictsWith = X

#### Scenario: Same time, different day
- GIVEN enrolled Lunes 08:00-10:00 and candidate Martes 08:00-10:00
- WHEN evaluated
- THEN no violation

### Requirement: R4 Semester limit (SEMESTRE_EXCEDIDO)
Materia.Semestre MUST be <= student.SemestreActual + X, where X is `Enrollment:MaxSemestersAhead` (default 3).

#### Scenario: Within limit
- GIVEN SemestreActual=2, X=3
- WHEN candidate Semestre=5
- THEN no violation

#### Scenario: Beyond limit
- GIVEN SemestreActual=2, X=3
- WHEN candidate Semestre=6
- THEN SEMESTRE_EXCEDIDO is reported

#### Scenario: Configured X
- GIVEN X=1 and SemestreActual=2
- WHEN candidate Semestre=4
- THEN SEMESTRE_EXCEDIDO is reported

### Requirement: R5 No duplicates (MATERIA_YA_APROBADA / MATERIA_YA_INSCRITA)
A materia MUST NOT be enrolled if the student has it `Aprobada`, or has an Activa enrollment for it in the same periodo. `Reprobada`, `Cursando` and Cancelada enrollments MUST NOT block.

#### Scenario: Fresh materia
- GIVEN no approved history or active enrollment for it
- WHEN evaluated
- THEN no violation

#### Scenario: Already approved
- GIVEN history Aprobada
- WHEN evaluated
- THEN MATERIA_YA_APROBADA is reported

#### Scenario: Already active in period
- GIVEN Activa enrollment in the same periodo
- WHEN evaluated
- THEN MATERIA_YA_INSCRITA is reported

#### Scenario: Retake or re-enroll
- GIVEN history Reprobada, or a Cancelada enrollment
- WHEN evaluated
- THEN no violation

### Requirement: R6 Seat availability (CUPO_AGOTADO)
Activa enrollments of the periodo MUST be < CuposMaximos. Only Activa counts.

#### Scenario: Seat available
- GIVEN CuposMaximos=30 with 29 Activa
- WHEN evaluated
- THEN no violation

#### Scenario: Full
- GIVEN CuposMaximos=30 with 30 Activa
- WHEN evaluated
- THEN CUPO_AGOTADO is reported

#### Scenario: Cancelled frees a seat
- GIVEN 30 max, 29 Activa and 1 Cancelada
- WHEN evaluated
- THEN no violation
