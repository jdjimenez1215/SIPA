# Datos mock — Malla curricular e historial de estudiante

Datos simulados del programa de **Ingeniería de Sistemas** para probar el
microservicio de Evaluación Académica mientras no exista el SIAU real.

Viven en tres archivos JSON planos, sin dependencias. Cualquier lenguaje los
puede leer.

| Archivo | Qué representa | Campos clave |
|---|---|---|
| `malla_curricular.json` | Catálogo de asignaturas del programa | `codigo`, `creditos`, `semestre`, `prerrequisitos[]` |
| `estudiante_prueba.json` | Trayectoria de la estudiante de prueba | `semestreActual`, `historialAcademico[]` |
| `sugerencia_mock.json` | Respuesta esperada de `GET /api/v1/matricula/sugerencia` | `totalCreditos`, `materiasSugeridas[]`, `notificaciones[]` |

---

## 1. `malla_curricular.json`

14 asignaturas repartidas en los semestres 1, 2, 6, 7 y 8.

Los semestres 3, 4 y 5 están **vacíos a propósito**: el caso de uso es el
semestre 6, y un semestre sin asignaturas sirve para probar que la API responde
correctamente cuando no hay nada que sugerir.

`prerrequisitos` es una lista de **códigos**, no de objetos. Cualquier código
debe existir en el mismo archivo; el backend lo usa para decidir si una
asignatura queda bloqueada.

```
INF-301 (Inteligencia Artificial)
  └── prerrequisito: INF-201 (Bases de Datos II)
```

## 2. `estudiante_prueba.json`

**Laura Gómez Ríos** (`1045123456`), semestre actual **6**.

Ha aprobado los 4 primeros semestres:

| Código | Nota |
|---|---|
| `MAT-101` | 4.2 |
| `INF-101` | 4.5 |
| `MAT-102` | 3.8 |
| `INF-102` | 4.0 |

El punto importante: **`INF-201` e `INF-202` no aparecen en el historial**, aunque
están en el semestre 6. Laura va a *entrar* al semestre 6, no está saliendo de
él. Si aparecieran como aprobadas y además se sugirieran, el mock se
contradiría a sí mismo y ninguna regla de prerrequisitos sería coherente.

## 3. `sugerencia_mock.json`

La respuesta que el frontend espera. **No es una entrada más**: funciona como
fixture de comparación, y la API debe producir un JSON idéntico a este.

9 filas y `totalCreditos: 27`.

---

## Cómo se calcula la regla N+3

El microservicio implementa esta lógica; los JSON solo proves los datos.
La clave es entender que **el semestre N y el N+1 se cursan en el mismo
periodo**, lo que permite que una materia de N+1 tenga como prerrequisito una
materia de N que se está tomando a la vez.

```
Semestre 6 completo          →  INF-201  INF-202  INF-203  MAT-204  INF-204
Semestre 7, máximo 3          →  INF-301  INF-302  INF-303
Semestre 7, bloqueada         →  INF-304
```

### Por qué INF-301 sí entra

`INF-301` requiere `INF-201`. `INF-201` no está aprobada, pero **se está
sugiriendo en el mismo periodo**, así que el prerrequisito se considera
cumplido. Lo mismo aplica a `INF-303`, que requiere `INF-202`.

### Por qué INF-304 queda bloqueada

`INF-304` (Taller de Grado I) requiere `INF-305` (Práctica Empresarial,
semestre 8). Laura nunca va a alcanzar ese punto, así que la fila aparece con
`estado: "Prerrequisito"` y `prerrequisitoCumplido: false`.

Aparece en la respuesta **a propósito**: el frontend la pinta con badge ámbar
para que el usuario vea qué queda pendiente, no la omite.

### La suma de créditos

`totalCreditos` **solo cuenta las filas `Sugerida`**:

```
sem 6:  4 + 4 + 3 + 3 + 4  = 18
sem 7:  3 + 3 + 3          =  9
                             ───
                             27
```

`INF-304` aporta 0. Esta es la trampa más fácil de este contrato: sumar los
créditos de todas las filas da 31, que es incorrecto.

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
| `GET /api/v1/matricula/sugerencia` | 200, 9 filas, 27 créditos |
| Confirmar selección válida | 201 |
| Pedir 4 asignaturas del semestre 7 | 422 |
| Pedir `INF-304` sin su prerrequisito | 422 |
| Enviar `codigosMaterias: []` | 400 |
| Enviar un código inexistente | 400 |
| Confirmar dos veces en el mismo periodo | 409 |
| Estudiante inexistente | 404 |
| `PeriodoAbierto: false` | 404 |

---

## Al modificar estos archivos

1. **`sugerencia_mock.json` es un contrato, no un ejemplo.** Si cambias la
   malla y la respuesta deja de coincidir con lo que produce la API, la que
   está mal es la respuesta.
2. `totalCreditos` debe seguir siendo igual a la suma de las filas `Sugerida`.
3. Ningún `prerrequisitos` puede apuntar a un código inexistente.
4. Después de cualquier cambio, verifica que la API siga devolviendo un JSON
   idéntico a `sugerencia_mock.json`.

## Referencias

- `GUIA_BACKEND.md` — contrato de integración con el frontend (sección 4.2 es
  la forma exacta de la respuesta)
- `assets/js/app.js` — el mock equivalente del lado del frontend, que se
  desactiva con `API_CONFIG.useMock = false`
