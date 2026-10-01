# Proposal: Regla N+3 (N completo + máx. 3 de N+1) y endpoint de sugerencia

Change: `regla-n3-sugerencia` · Rama: `feature/ms-inscripcion` · Modo: hybrid · Origen: `conexion-front-inscripcion/exploration.md` §3.1 y §10 (Q1 = ventana del mock con **prerrequisitos estrictos**; Q2 = endpoint backend; Q5 = seed Unillanos).

## Intent

El backend implementa "N+3" como *hasta el semestre N+3 sin tope* (`MaxSemestersAhead`). El negocio define: **semestre N completo + máximo 3 materias de N+1, cursados en el mismo período**. Decisión del usuario (2026-10-01): *"Si una materia tiene un prerrequisito no se puede inscribir hasta que el prerrequisito sea aprobado"* → los prerrequisitos son **estrictos** (solo `Aprobada`), igual que la regla 2 original; se **descarta la co-sugerencia** del mock. Además el front necesita filas bloqueadas con estado, que solo el backend puede calcular.

## Scope

### In Scope
1. **R4 reemplazada**: ventana permitida = semestres `< N` (adeudadas) + `N` + `N+1`; `> N+1` → `SEMESTRE_EXCEDIDO`. Nueva regla (Strategy aparte) de **tope N+1**: activas N+1 del período + candidatas N+1 ≤ `Enrollment:MaxNextSemesterSubjects` (default 3); exceso → nuevo código `LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO`. **Se elimina `MaxSemestersAhead`** (opciones, validación, appsettings, compose): su semántica deja de existir; mantenerla confundiría.
2. **R2 sin cambios**: `PrerequisitesRule` se queda como hoy; prerrequisito cumplido **solo** con `Aprobada` en historial. Ni co-solicitadas en el request, ni inscripciones `Activa` del período, ni `Cursando`/`Reprobada` cuentan.
3. **Nuevo `GET /api/estudiantes/{id}/sugerencia?periodo=`** con la forma de `sugerencia_mock.json`: `nombreEstudiante, programa, semestreActual, totalCreditos, materiasSugeridas[{codigo,nombre,creditos,semestre,estado,prerrequisitoCumplido}], notificaciones[]` (`notificaciones` = `[]`; no hay servicio). **Contrato (requisito de `conexion-front-inscripcion`)**: cada fila DEBE incluir además **`id` (int, id de la materia)**, porque el front envía `materiaIds` al POST y no mapeará codigo→id. Es aditivo: el mock offline del front sigue idéntico al fixture (`validar-mocks.mjs` grupo 5). Aditivos opcionales: `periodo` (raíz) y `motivo` por fila (código de regla cuando `estado="Prerrequisito"`).
   - Cálculo secuencial reutilizando el `EnrollmentRulesEngine`: cada fila `Sugerida` se acumula al contexto (cruces de horario, tope N+1); los prerrequisitos se evalúan solo contra aprobadas.
   - Filas que fallan prerrequisito → `estado="Prerrequisito"`, `prerrequisitoCumplido=false`. Filas N+1 con prerrequisito cumplido pero fuera del tope → `estado="Prerrequisito"`, `prerrequisitoCumplido=true`. Ambas se muestran, no se omiten.
   - `totalCreditos` = suma de filas `Sugerida`.
4. **`materias-disponibles` se mantiene** y delega en el mismo calculador (devuelve solo las `Sugerida` como `MateriaDto`). Motivo: una sola fuente de verdad, y `conexion-front-inscripcion` P1, e2e y Postman la usan. Se marca *deprecated* en Swagger/README a favor de `sugerencia`.
5. **`POST /api/inscripciones`** aplica las mismas reglas (ventana + tope N+1 contando activas del período + prerrequisitos estrictos). `materiaIds` sigue siendo el contrato principal; **`codigosMaterias: string[]`** se acepta como alternativa excluyente de conveniencia.
6. **Seed Unillanos** reemplaza el ficticio: carrera Ingeniería de Sistemas, 53 materias y prerrequisitos de `malla_curricular.json`, Laura (`estudiante_prueba.json`, sem. 6, 26 `Aprobada`). **Inventados** (el mock no los trae): `cuposMaximos` (p. ej. 30), horarios sin cruces entre semestres consecutivos, período del historial, una segunda carrera mínima (R1) y estudiantes auxiliares, entre ellos **uno donde el tope de 3 sí se alcance** (Laura solo tiene 2 N+1 elegibles). Candidato: estudiante en sem. 7 con sem. 1–6 aprobadas → sem. 8 elegibles 603801, 603802, 603803, 603804, 603806 (5) → 3 `Sugerida` + 2 bloqueadas por tope; 603805 bloqueada por prerrequisito (603701). Nueva migración + recreación de BD.
7. Reescritura de unit tests por regla, `scripts/e2e.sh`, colección Postman y README.

### Out of Scope
- Cambios de front y corrección del fixture del mock (change `conexion-front-inscripcion` / autor del mock), JWT/auth, vínculo usuario↔estudiante (`Estudiante.Codigo`), notificaciones reales, elección del estudiante sobre cuáles 3 de N+1.

## Capabilities

### New Capabilities
- `enrollment-suggestion`: cálculo y endpoint `GET /api/estudiantes/{id}/sugerencia` (filas, estados, `prerrequisitoCumplido`, `totalCreditos`, orden, `id` por fila).

### Modified Capabilities
- `enrollment-rules`: R4 (ventana < N / N / N+1) y nueva regla de tope N+1. R2 **no cambia** (sigue estricta).
- `available-subjects`: deriva del calculador (solo `Sugerida`); deprecated.
- `enrollment-management`: `codigosMaterias` en POST; tope N+1 contando activas del período.
- `deployment-runtime`: seed Unillanos (+ estudiante donde el tope se alcanza), config `MaxNextSemesterSubjects`, eliminación de `MaxSemestersAhead`.

## Approach

Domain: `SemesterWindowRule` (reemplaza `SemesterLimitRule`), `NextSemesterCapRule` (nueva), `PrerequisitesRule` sin cambios; `EnrollmentContext` cambia `MaxSemesterAhead` por `MaxNextSemesterSubjects`. Servicio de dominio puro `EnrollmentSuggestionCalculator` (orden: adeudadas < N, luego N, luego N+1, cada grupo por `codigo` ascendente). Application: `GetSugerenciaQuery` + DTO; `MateriasDisponiblesQuery` y `EnrollCommand` usan lo mismo. Controllers sin lógica.

## Supuestos por defecto (a validar con negocio)

| # | Pregunta | Supuesto |
|---|---|---|
| A1 | ¿Cuáles 3 de N+1? | Las elegibles en orden ascendente de `codigo` (como el mock) |
| A2 | Materias adeudadas de semestres < N | **DECIDIDO (usuario, 2026-10-01):** tienen **prioridad** (se sugieren antes que N+1) pero **no son obligatorias** (el POST no exige incluirlas) y **SÍ consumen** los cupos del tope: el tope de 3 es compartido entre adeudadas + N+1. Ej.: debe 1 de sem 4 → sugerencia = sem N completo + esa adeudada + 2 de N+1; si al confirmar la omite, puede llevar 3 de N+1. `NextSemesterCapRule` cuenta adeudadas + N+1 (activas en el período + candidatas). |
| A3 | Cruce/cupo siguen aplicando | **Validado como recomendación (opción A):** sí aplican; en la **sugerencia**, una fila (adeudada o N+1) que falla otra regla NO consume cupo, se muestra bloqueada con su `motivo` y el cupo pasa a la siguiente elegible. El cruce se evalúa contra las filas ya sugeridas con mayor prioridad (N y adeudadas primero). En el **POST** no hay "pasar al siguiente": la materia inválida produce su violación y la solicitud se rechaza completa (atómica). |
| A4 | N = último semestre | Sin filas N+1 |
| A5 | `Reprobada` de semestre < N | Cuenta como adeudada (A2: prioridad, no obligatoria, consume cupo del tope); se puede reinscribir |

El antiguo A6 (cancelar un prerrequisito co-inscrito) **se elimina**: con prerrequisitos estrictos una materia nunca depende de otra inscrita en el mismo período, así que cancelar una inscripción no puede dejar huérfana a otra.

La nota previa sobre el README del mock ("603605 está aprobada") **queda sustituida**: con la regla estricta 603704 se bloquea por prerrequisito; el problema ahora es todo el fixture (ver Dependencies).

## Affected Areas

| Área | Impacto |
|---|---|
| `src/MsInscripcion.Domain/Rules/*` (SemesterLimitRule, EnrollmentContext, ErrorCodes, NextSemesterCapRule, calculador) | Modified/New/Removed |
| `src/MsInscripcion.Application/Features/Materias/Queries/MateriasDisponiblesQuery.cs`, `Features/Inscripciones/Commands/EnrollCommand.cs`, nuevo `Features/Sugerencia/*`, `Common/Options/EnrollmentOptions.cs` | Modified/New |
| `src/MsInscripcion.Api` (controller estudiantes, `appsettings.json`), `docker-compose.yml` | Modified |
| `src/MsInscripcion.Infrastructure/Persistence/Seed/SeedData.cs`, `Migrations/*` | Modified/New |
| `tests/MsInscripcion.UnitTests`, `scripts/e2e.sh`, `postman/*`, `README.md` | Rewritten |

## Risks

| Riesgo | Prob. | Mitigación |
|---|---|---|
| Cambio de seed rompe BDs existentes (ids/FK) | Alta | `docker compose down -v`; documentado en README |
| e2e (73 checks)/Postman atados a MAT101 | Alta | Reescritura incluida en el alcance |
| Fixture del mock (26 créditos) contradice la regla acordada | Alta | Dependencia externa explícita; la aceptación usa el fixture corregido |
| Supuestos A1–A5 incorrectos | Media | Tabla explícita; cada uno aislado en una regla/orden configurable |
| `materias-disponibles` cambia semántica (acumulativa por tope) | Media | Documentado; consumidores pasan a `sugerencia` |
| Con Laura el tope nunca se ejercita | Media | Estudiante auxiliar de seed donde el tope se alcanza |

## Rollback Plan

Revertir los commits del change en `feature/ms-inscripcion` (vuelven `SemesterLimitRule`, `MaxSemestersAhead` y el seed ficticio), eliminar la nueva migración y recrear la BD (`docker compose down -v && docker compose up`). El endpoint `sugerencia` es aditivo; ningún consumidor productivo depende aún de él.

## Dependencies

- Fixtures de `origin/feature/datos-mock` (malla y Laura copiados al seed; no se mergea la rama).
- **Externa (autor del front/mock)**: con prerrequisitos estrictos quedan **incorrectos** `sugerencia_mock.json` (26 créditos), el `MOCK_SUGERENCIA` embebido en `assets/js/app.js`, `validar-mocks.mjs` grupo 4 (recalcula con co-sugerencia) y el README del mock. Deben corregirse a la versión de 23 créditos; `MOCK_SUGERENCIA` debe quedar byte-idéntico al fixture corregido.

## Success Criteria

- [ ] `GET /api/estudiantes/{laura}/sugerencia` equivale al **fixture corregido**: 12 filas en orden de malla; sem. 6 603601–603606 `Sugerida`, `prerrequisitoCumplido=true` (18 cr.); sem. 7 603701 y 603703 `Sugerida`; 603702, 603704, 603705, 603706 `Prerrequisito`, `prerrequisitoCumplido=false`; `totalCreditos` = **23**. La comparación ignora `id`, `periodo`, `motivo` y el contenido de `notificaciones`.
- [ ] Toda fila de `sugerencia` trae `id` (int) válido y utilizable directamente en `materiaIds` del POST.
- [ ] Estudiante auxiliar (tope alcanzado): exactamente 3 N+1 `Sugerida`; el resto elegible en `Prerrequisito` con `prerrequisitoCumplido=true`.
- [ ] POST: 4 N+1 elegibles → 422 `LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO`; Laura 603601+603702 juntas → 422 `PREREQUISITO_NO_CUMPLIDO` (603702); materia de N+2 → 422 `SEMESTRE_EXCEDIDO`.
- [ ] POST con `codigosMaterias` funciona igual que con `materiaIds`.
- [ ] `MaxSemestersAhead` no existe en código ni config.
- [ ] Unit tests por regla (éxito + falla), e2e, Postman y README actualizados.
