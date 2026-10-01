# Enrollment Suggestion Specification

## Purpose
`GET /api/estudiantes/{id}/sugerencia?periodo=` returns the student's full candidate window with a per-row state, computed by one calculator that reuses the rules engine. Shared by `materias-disponibles` and POST.

## Requirements

### Requirement: Response contract
The response MUST be `{nombreEstudiante, programa, semestreActual, totalCreditos, materiasSugeridas[], notificaciones[]}`; `notificaciones` MUST be `[]`. Each row MUST be `{id, codigo, nombre, creditos, semestre, estado, prerrequisitoCumplido}` where `id` (int) is the materia id usable directly in POST `materiaIds`. `periodo` (root) and `motivo` (row, the rule code when `estado="Prerrequisito"`) MAY be present. `estado` is `Sugerida` or `Prerrequisito`. `totalCreditos` MUST sum only `Sugerida` rows. `periodo` defaults to `Enrollment:CurrentPeriod`.

#### Scenario: Laura (semestre 6)
- GIVEN Laura (sem 6, 26 Aprobada) and the Unillanos seed
- WHEN GET sugerencia
- THEN 12 rows in malla order, each with an `id`
- AND 603601-603606 are `Sugerida`, `prerrequisitoCumplido=true` (18 cr)
- AND 603701 and 603703 are `Sugerida`
- AND 603702, 603704, 603705, 603706 are `Prerrequisito` with `prerrequisitoCumplido=false`
- AND `totalCreditos` = 23

#### Scenario: Unknown student and default periodo
- GIVEN no such student
- WHEN GET
- THEN 404 ESTUDIANTE_NO_ENCONTRADO
- AND for a known student without `periodo`, the calculation uses `Enrollment:CurrentPeriod`

### Requirement: Window, priority and order
Rows MUST be: owed subjects (semestre < N, not Aprobada, including Reprobada), then all of semestre N, then eligible N+1; each group ordered by ascending `codigo`. Owed subjects are PRIORITY, NOT mandatory. Prerequisites are strict (only `Aprobada`).

#### Scenario: Owed subject shares the cap (seed: Camila, sem 7, sem 1-6 Aprobada except 603305 Reprobada)
- GIVEN Camila owes 603305 and has 603801, 603802, 603803, 603804, 603806 eligible in N+1
- WHEN GET sugerencia
- THEN 603305 is `Sugerida` (owed, priority over N+1)
- AND only 603801 and 603802 are `Sugerida` from N+1
- AND 603803, 603804, 603806 are `Prerrequisito` with `prerrequisitoCumplido=true` (cap)
- AND 603805 is `Prerrequisito` with `prerrequisitoCumplido=false` (needs 603701)

#### Scenario: Last semester
- GIVEN N is the last semestre of the carrera
- WHEN GET sugerencia
- THEN no N+1 rows are returned

### Requirement: Shared cap of 3 (owed + N+1)
Owed and N+1 subjects together MUST NOT exceed `Enrollment:MaxNextSemesterSubjects` (3) `Sugerida` rows, counting already-Activa enrollments of the period. Eligible rows beyond the cap MUST be `Prerrequisito` with `prerrequisitoCumplido=true`. Semestre N rows are not capped.

#### Scenario: Cap reached (helper student, sem 7, sem 1-6 Aprobada)
- GIVEN sem 8 eligible 603801, 603802, 603803, 603804, 603806
- WHEN GET sugerencia
- THEN all sem 7 rows are `Sugerida`
- AND 603801, 603802, 603803 are `Sugerida`
- AND 603804 and 603806 are `Prerrequisito` with `prerrequisitoCumplido=true`
- AND 603805 is `Prerrequisito` with `prerrequisitoCumplido=false` (needs 603701)

### Requirement: Other rules in the suggestion
A row failing another rule (seats R6, schedule R3, duplicates) MUST NOT consume a cap slot, MUST be `Prerrequisito` with its `motivo`, and the slot MUST pass to the next eligible row. Schedule clashes MUST be evaluated against higher-priority rows already `Sugerida`.

#### Scenario: Full N+1 subject does not consume a slot
- GIVEN 4 eligible N+1 subjects and the first has no seats
- WHEN GET sugerencia
- THEN the first is `Prerrequisito` with `motivo=CUPO_AGOTADO`
- AND the next 3 by `codigo` are `Sugerida`

#### Scenario: Clash with higher priority row
- GIVEN an N+1 subject overlapping a `Sugerida` sem-N subject
- WHEN GET sugerencia
- THEN it is `Prerrequisito` with `motivo=CRUCE_HORARIO` and consumes no slot

#### Scenario: Blocked rows pass the slot (seed: Sofía, sem 8, sem 1-7 Aprobada)
- GIVEN 603902 clashes with 603801 (sem N) and 603903 has no seats (its only seat taken by Andrés)
- WHEN GET sugerencia
- THEN 603801–603806 are `Sugerida`
- AND 603901 is `Prerrequisito` with `prerrequisitoCumplido=false` (needs 603801)
- AND 603902 is `Prerrequisito` with `motivo=CRUCE_HORARIO`; 603903 with `motivo=CUPO_AGOTADO`
- AND 603904 and 603905 are BOTH `Sugerida` (if blocked rows consumed slots, 603905 would be capped)

### Requirement: Rows not listed
The sugerencia MUST NOT list subjects already `Aprobada`, subjects already `Activa` for the student in the period, subjects of other carreras, or subjects above N+1. Already-`Activa` owed/N+1 enrollments MUST still count toward the cap and occupy the schedule.

#### Scenario: Active enrollment is hidden but counts
- GIVEN Mateo (sem 7) already has 603801 `Activa` in the period
- WHEN GET sugerencia
- THEN 603801 is not listed
- AND only 603802 and 603803 are `Sugerida` from N+1 (603801 uses one of the 3 slots)
