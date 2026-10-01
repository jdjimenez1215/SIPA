# Datos mock — Malla curricular e historial de estudiante

Catálogo y trayectoria de prueba del programa de **Ingeniería de Sistemas** de la
**Universidad de los Llanos** (Facultad de Ciencias Básicas e Ingeniería),
usados para probar el microservicio de Evaluación Académica mientras no exista
el SIAU real.

Viven en tres archivos JSON planos, sin dependencias. Cualquier lenguaje los
puede leer.

| Archivo | Qué representa | Campos clave |
|---|---|---|
| `malla_curricular.json` | Catálogo de asignaturas del programa | `codigo`, `creditos`, `semestre`, `prerrequisitos[]` |
| `estudiante_prueba.json` | Trayectoria de la estudiante de prueba | `semestreActual`, `historialAcademico[]` |
| `sugerencia_mock.json` | Respuesta esperada de `GET /api/v1/matricula/sugerencia` | `totalCreditos`, `materiasSugeridas[]`, `notificaciones[]` |

---

## 1. `malla_curricular.json`

53 asignaturas, 10 semestres, **165 créditos**.

Estos datos son **reales**, tomados del plan de estudios publicado por la
Facultad (plan `PIS 603 2018 - I`, definido por el Acuerdo Académico 001 de 2017
para los estudiantes que ingresen a partir del I periodo académico de 2018).
Los códigos son los institucionales de 6 dígitos.

Créditos por semestre:

| Sem | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10 |
|---|---|---|---|---|---|---|---|---|---|---|
| Créditos | 16 | 18 | 17 | 18 | 18 | 18 | 17 | 16 | 15 | 12 |

`prerrequisitos` es una lista de **códigos**, no de objetos. Cualquier código
debe existir en el mismo archivo; el backend lo usa para decidir si una
asignatura queda bloqueada.

```
603301 (Estructuras de Datos)
  └── prerrequisito: 603201 (Programación Orientada a Objetos)
```

Los prerrequisitos vienen publicados por *nombre* en la fuente; acá se
tradujeron a código, que es lo que necesita la regla.

## 2. `estudiante_prueba.json`

**Laura Gómez Ríos** (`1045123456`), semestre actual **6**.

Ha aprobado **las 26 asignaturas de los semestres 1 a 5** (87 créditos), que es lo
que corresponde a alguien que va a *entrar* al semestre 6:

| Sem | Asignaturas aprobadas |
|---|---|
| 1 | `603101`, `603102`, `603103`, `603104`, `603105` |
| 2 | `603201`, `603202`, `603203`, `603204`, `603205` |
| 3 | `603301`, `603302`, `603303`, `603304`, `603305` |
| 4 | `603401`, `603402`, `603403`, `603404`, `603405` |
| 5 | `603501`, `603502`, `603503`, `603504`, `603505`, `603506` |

Ninguna asignatura del semestre 6 aparece en el historial: si apareciera como
aprobada y además se sugiriera, el mock se contradiría a sí mismo y ninguna regla
de prerrequisitos sería coherente.

## 3. `sugerencia_mock.json`

La respuesta que el frontend espera. **No es una entrada más**: funciona como
fixture de comparación, y la API debe producir un JSON idéntico a este.

12 filas y `totalCreditos: 23`.

---

## Cómo se calcula la regla N+3

El microservicio implementa esta lógica; los JSON solo proveen los datos.

Una estudiante en el semestre N recibe **todo el semestre N** más hasta **3
asignaturas extra en total**. Ese cupo de 3 es **compartido** entre dos grupos:

1. **Adeudadas**: asignaturas de semestres menores a N que no están aprobadas
   (incluida una `Reprobada`). Tienen **prioridad**: se sugieren primero, pero
   no son obligatorias.
2. **Semestre N+1**: se sugieren con lo que quede del cupo.

Dentro de cada grupo el orden es ascendente por `codigo`. Si N es el último
semestre, no hay filas de N+1.

### Prerrequisitos estrictos

Un prerrequisito se cumple **solo si la asignatura está `APROBADO` en el
historial**. No hay co-sugerencia: que una asignatura se esté sugiriendo en este
mismo periodo **no** satisface el prerrequisito de otra, porque todavía no está
aprobada.

Cada fila se clasifica así (las bloqueadas **siguen apareciendo** en la
respuesta; el frontend las pinta con badge ámbar):

| Situación | `estado` | `prerrequisitoCumplido` |
|---|---|---|
| Prerrequisito aprobado y entra en la sugerencia | `Sugerida` | `true` |
| Prerrequisito aprobado pero se pasó del cupo de 3 | `Prerrequisito` | `true` |
| Prerrequisito no aprobado | `Prerrequisito` | `false` |

### El caso de Laura (semestre 6, 26 aprobadas de los semestres 1 a 5)

```
Semestre 6 completo           →  603601  603602  603603  603604  603605  603606   (Sugerida)
Semestre 7, prereq aprobado   →  603701  603703                                    (Sugerida)
Semestre 7, prereq NO aprobado →  603702  603704  603705  603706                    (Prerrequisito)
```

- `603701` entra: su prerrequisito `603502` está aprobado.
- `603703` entra: no tiene prerrequisitos.
- `603702` (Tecnologías Avanzadas) queda **bloqueada**: requiere `603601`
  (Ingeniería de Software II), que **aún no está aprobada** (apenas se sugiere
  este periodo). Por eso `prerrequisitoCumplido` es `false`.
- `603704`, `603705` y `603706` quedan bloqueadas por prerrequisito: requieren
  `603605` (las dos primeras) y `603606` (la última), que son del semestre 6 y
  todavía no están aprobadas.

Laura no tiene adeudadas y solo **2** asignaturas de N+1 son elegibles, así que
el tope de 3 no se alcanza: **este fixture no ejercita el caso "prerrequisito
cumplido pero fuera de cupo"**. Esa rama la cubre la reimplementación de la
regla en las pruebas, no el mock.

### La suma de créditos

`totalCreditos` **solo cuenta las filas `Sugerida`**:

```
sem 6:  3 + 3 + 3 + 3 + 4 + 2  = 18
sem 7:  3 + 2                  =  5     (603701 y 603703)
                                  ───
                                  23
```

Las cuatro filas bloqueadas aportan 0. Esta es la trampa más fácil de este
contrato: sumar los créditos de todas las filas da 35, que es incorrecto.

---

## Cómo usarlos desde el backend

Los archivos se leen una vez y se cachean. El punto importante de la
arquitectura: **solo la capa de infraestructura conoce las rutas**; dominio y
servicio nunca saben de dónde salen los datos.

```
Sipa.Domain          entidades + ReglaN3.Calcular()      sin dependencias
Sipa.Application     interfaces + ServicioSugerencia      conoce las interfaces
Sipa.Infrastructure  lee estos JSON                       conoce los archivos
Sipa.Api             endpoints                            no sabe nada de JSON
```

Para migrar al SIAU real se escriben clases nuevas que implementen
`IRepositorioMallaCurricular` e `IRepositorioEstudiante`. Ni el servicio ni el
dominio cambian.

```csharp
// Hoy
services.AddSingleton<IRepositorioMallaCurricular>(
    new RepositorioMallaCurricularJson(ruta));

// Con SIAU: cambiar solo esta línea
services.AddSingleton<IRepositorioMallaCurricular>(
    new RepositorioMallaCurricularSiau(httpClient));
```

Configuración de las rutas (`appsettings.json`):

```json
"DatosMock": {
  "RutaMalla":      "datos/malla_curricular.json",
  "RutaEstudiante": "datos/estudiante_prueba.json",
  "RutaSugerencia": "datos/sugerencia_mock.json",
  "PeriodoId": "2026-03",
  "PeriodoAbierto": true
}
```

---

## Casos de prueba que permiten estos datos

Cada regla de negocio tiene un caso que la ejercita:

| Caso | Código esperado |
|---|---|
| `GET /api/v1/matricula/sugerencia` | 200, 12 filas, 23 créditos |
| Confirmar selección válida | 201 |
| Pedir 4 asignaturas del semestre 7 | 422 |
| Pedir `603602` sin `603503` aprobado | 422 |
| Enviar `codigosMaterias: []` | 400 |
| Enviar un código inexistente | 400 |
| Confirmar dos veces en el mismo periodo | 409 |
| Estudiante inexistente | 404 |
| `PeriodoAbierto: false` | 404 |

---

## Pruebas de estos datos

`src/pruebas/validar-mocks.mjs` comprueba que los tres archivos sean coherentes
entre sí y que el contrato se cumpla. No necesita instalar nada: solo Node.

```bash
node src/pruebas/validar-mocks.mjs
```

Sale con código 1 si algo falla, así que se puede encadenar en CI.

Qué verifica, en 36 comprobaciones:

| Grupo | Qué comprueba |
|---|---|
| 1. Malla | 53 asignaturas, 165 créditos, 10 semestres, códigos únicos de 6 dígitos, todo prerrequisito existe y es de un semestre anterior |
| 2. Estudiante | Códigos válidos, notas en rango, ninguna asignatura del semestre 6 aprobada, y que estén aprobados los prerrequisitos del semestre 6 **y los heredados de esos** |
| 3. Contrato | `totalCreditos` suma solo las filas `Sugerida`, máximo 3 filas `Sugerida` entre adeudadas y N+1 (cupo compartido), nombres y créditos coinciden con la malla |
| 4. Regla N+3 | Recalcula la sugerencia con prerrequisitos estrictos y cupo compartido (adeudadas primero), y exige que coincida con el fixture en asignaturas, orden, estado y créditos; además fija `totalCreditos` de Laura en 23 y 603702 bloqueada |
| 5. Integración | El `MOCK_SUGERENCIA` de `assets/js/app.js` es idéntico a `sugerencia_mock.json` |

El grupo 4 reimplementa la regla a propósito. Si el fixture dejara de ser lo que
la regla produce, el contrato estaría partido y el error aparecería acá, sin
necesidad de levantar el backend.

El grupo 5 cubre el punto más frágil de la integración: **el contrato está
duplicado**. La misma respuesta vive en `sugerencia_mock.json` y embebida en
`assets/js/app.js` como `MOCK_SUGERENCIA`, porque el frontend la necesita para
funcionar sin backend. Son dos copias de un mismo contrato, así que cambiarlas
una sola rompe el contrato. Si se edita el JSON, hay que editar también el
mock del frontend, o correr estas pruebas para que avisen.

---

## Al modificar estos archivos

1. **`sugerencia_mock.json` es un contrato, no un ejemplo.** Si cambias la
   malla y la respuesta deja de coincidir con lo que produce la API, la que
   está mal es la respuesta.
2. `totalCreditos` debe seguir siendo igual a la suma de las filas `Sugerida`.
3. Ningún `prerrequisitos` puede apuntar a un código inexistente.
4. Si tocás `sugerencia_mock.json`, tocá también el `MOCK_SUGERENCIA` de
   `assets/js/app.js`: es la misma respuesta en otro formato.
5. Después de cualquier cambio, verifica que la API siga devolviendo un JSON
   idéntico a `sugerencia_mock.json` y corré `node src/pruebas/validar-mocks.mjs`.

## Referencias

- Fuente de la malla: <https://fcbi.unillanos.edu.co/fcbi/is>
- `src/pruebas/validar-mocks.mjs` — pruebas de estos datos
- `GUIA_BACKEND.md` — contrato de integración con el frontend (sección 4.2 es
  la forma exacta de la respuesta)
- `assets/js/app.js` — el mock equivalente del lado del frontend, que se
  desactiva con `API_CONFIG.useMock = false`. El `MOCK_SUGERENCIA` de ahí debe
  quedar **idéntico** a `sugerencia_mock.json`
