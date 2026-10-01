# Delta for Enrollment Rules

R1, R3, R5, R6 and the engine are UNCHANGED (career match, schedule overlap, duplicates, seat availability). R4 is replaced; a new cap rule is added; R2 is reaffirmed.

## ADDED Requirements

### Requirement: R2 Prerequisites remain strict (unchanged)
`PrerequisitesRule` MUST NOT change: a prerequisite counts only with an `Aprobada` history entry. Co-requested materias, Activa enrollments of the period, `Cursando` and `Reprobada` MUST NOT count.

#### Scenario: Co-requested prerequisite does not count
- GIVEN Laura and candidates [603601, 603702] where 603601 is a prerequisite of 603702
- WHEN evaluated
- THEN PREREQUISITO_NO_CUMPLIDO is reported for 603702 only

#### Scenario: Active prerequisite does not count
- GIVEN Activa enrollment in the prerequisite for this periodo
- WHEN the dependent is evaluated
- THEN PREREQUISITO_NO_CUMPLIDO is reported

### Requirement: Shared cap of owed + N+1 (LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO)
Activa enrollments of the periodo plus candidates, counted over owed subjects (semestre < N) and N+1 subjects, MUST be <= `Enrollment:MaxNextSemesterSubjects` (default 3). Excess MUST report LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO. Semestre N subjects MUST NOT count.

#### Scenario: Within cap
- GIVEN 0 active extras and 3 N+1 candidates
- WHEN evaluated
- THEN no violation

#### Scenario: Over cap
- GIVEN 4 extra candidates (N+1 and/or owed)
- WHEN evaluated
- THEN LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO is reported

#### Scenario: Active enrollments count
- GIVEN 2 Activa extras in the periodo and 2 extra candidates
- WHEN evaluated
- THEN LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO is reported

#### Scenario: Semester N not counted
- GIVEN 5 sem-N candidates and 3 N+1 candidates
- WHEN evaluated
- THEN no violation

## MODIFIED Requirements

### Requirement: R4 Semester window (SEMESTRE_EXCEDIDO)
Materia.Semestre MUST be <= student.SemestreActual + 1. Semestres < N (owed) and N are allowed; the cap rule limits how many extras. There is NO `MaxSemestersAhead` setting.
(Previously: Semestre <= SemestreActual + `Enrollment:MaxSemestersAhead`, default 3)

#### Scenario: N+1 allowed
- GIVEN SemestreActual=6
- WHEN candidate Semestre=7
- THEN no violation

#### Scenario: N+2 rejected
- GIVEN SemestreActual=6
- WHEN candidate Semestre=8
- THEN SEMESTRE_EXCEDIDO is reported

#### Scenario: Owed subject allowed
- GIVEN SemestreActual=6 and an unapproved sem-4 materia
- WHEN evaluated
- THEN no SEMESTRE_EXCEDIDO

## REMOVED Requirements

### Requirement: Configured X for the semester limit
(Reason: `Enrollment:MaxSemestersAhead` and its "Configured X" scenario no longer exist; replaced by the fixed N+1 window and the cap rule)
