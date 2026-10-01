# Enrollment Management Specification

## Purpose
Create, cancel and list enrollments. Creation is atomic and seat-safe under concurrency.

## Requirements

### Requirement: Atomic enrollment (POST /api/inscripciones)
Body `{estudianteId, periodo, materiaIds[]}`. The system MUST run all six rules and persist all enrollments as Activa (201) only if there are zero violations; otherwise it MUST persist none and return 422 with ALL violations.

#### Scenario: All valid
- GIVEN 3 valid materias
- WHEN POST
- THEN 201 and 3 Activa enrollments exist

#### Scenario: One fails, none enrolled
- GIVEN 3 materias, one with CRUCE_HORARIO
- WHEN POST
- THEN 422 and zero new enrollments exist

#### Scenario: All violations returned
- GIVEN materia A breaks R1 and R4, materia B breaks R3 and R6
- WHEN POST
- THEN 422 `violations` has 4 entries, each with its materiaId and materiaCodigo

### Requirement: Request validation and lookups
`periodo` MUST match `^\d{4}-[12]$`; `materiaIds` MUST be non-empty. Duplicated ids MUST yield 400 MATERIA_DUPLICADA_EN_SOLICITUD. Unknown student or materia MUST yield 404.

#### Scenario: Bad periodo
- GIVEN periodo "2026-3"
- WHEN POST
- THEN 400 VALIDACION_FALLIDA

#### Scenario: Duplicate ids
- GIVEN materiaIds [M1, M1]
- WHEN POST
- THEN 400 MATERIA_DUPLICADA_EN_SOLICITUD

#### Scenario: Unknown student
- GIVEN estudianteId not in DB
- WHEN POST
- THEN 404 ESTUDIANTE_NO_ENCONTRADO

### Requirement: Seat safety under concurrency
The system MUST NOT let Activa enrollments of a materia in a periodo exceed CuposMaximos, even with concurrent requests. A concurrent duplicate Activa enrollment MUST be rejected as MATERIA_YA_INSCRITA.

#### Scenario: Concurrent last seat
- GIVEN one seat left and two students POST for it simultaneously
- WHEN both are processed
- THEN exactly one gets 201 and the other 422 CUPO_AGOTADO

#### Scenario: Concurrent duplicate
- GIVEN the same student sends the same POST twice at once
- WHEN both are processed
- THEN one succeeds and the other fails with MATERIA_YA_INSCRITA

### Requirement: Cancel (DELETE /api/inscripciones/{id})
The system MUST soft-cancel (Estado=Cancelada), return 204, and free the seat.

#### Scenario: Cancel active
- GIVEN an Activa enrollment
- WHEN DELETE
- THEN 204 and Estado=Cancelada

#### Scenario: Cancel again
- GIVEN a Cancelada enrollment
- WHEN DELETE
- THEN 409 INSCRIPCION_YA_CANCELADA

#### Scenario: Unknown id
- GIVEN no such enrollment
- WHEN DELETE
- THEN 404 INSCRIPCION_NO_ENCONTRADA

### Requirement: List enrollments (GET /api/estudiantes/{id}/inscripciones?periodo=)
The system MUST return the student's Activa enrollments with materia and horarios; `periodo` defaults to `Enrollment:CurrentPeriod`.

#### Scenario: With periodo
- GIVEN Activa enrollments in 2026-1 and 2026-2
- WHEN GET ?periodo=2026-1
- THEN only 2026-1 items are returned, with horarios

#### Scenario: Default periodo
- GIVEN CurrentPeriod=2026-2
- WHEN GET without periodo
- THEN 2026-2 items are returned

#### Scenario: Unknown student
- GIVEN no such student
- WHEN GET
- THEN 404 ESTUDIANTE_NO_ENCONTRADO
