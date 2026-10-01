/* ============================================================
 * PRUEBAS DE LOS DATOS MOCK  ·  Persona 4
 * ------------------------------------------------------------
 * Valida que los tres JSON de ../mocks/ sean coherentes entre si y
 * que el contrato de la API se cumpla, sin depender del backend.
 *
 * Uso:  node src/pruebas/validar-mocks.mjs
 * Sale con codigo 1 si algo falla, para poder encadenarlo en CI.
 * ============================================================ */

import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const aqui = dirname(fileURLToPath(import.meta.url));
const mocks = (archivo) =>
  JSON.parse(readFileSync(join(aqui, '..', 'data', 'mocks', archivo), 'utf8'));

const malla = mocks('malla_curricular.json');
const estudiante = mocks('estudiante_prueba.json');
const sugerencia = mocks('sugerencia_mock.json');

/* --- Datos oficiales del plan PIS 603 2018-I de Unillanos --- */
const ASIGNATURAS_ESPERADAS = 53;
const CREDITOS_ESPERADOS = 165;
const SEMESTRES_ESPERADOS = 10;
const CUPO_EXTRA = 3; // regla N+3: tope TOTAL de filas extra (adeudadas + N+1)

let fallos = 0;
let total = 0;

function revisar(titulo, condicion, detalle = '') {
  total++;
  if (condicion) {
    console.log(`  ok    ${titulo}`);
  } else {
    fallos++;
    console.log(`  FALLA ${titulo}${detalle ? ` -> ${detalle}` : ''}`);
  }
}

function grupo(titulo) {
  console.log(`\n${titulo}`);
}

const porCodigo = new Map(malla.asignaturas.map((a) => [a.codigo, a]));
const codigos = new Set(porCodigo.keys());
const semestreDe = (codigo) => porCodigo.get(codigo)?.semestre;

/* ============================================================
 * 1. malla_curricular.json
 * ============================================================ */
grupo('1. malla_curricular.json');

revisar(
  `${ASIGNATURAS_ESPERADAS} asignaturas`,
  malla.asignaturas.length === ASIGNATURAS_ESPERADAS,
  `hay ${malla.asignaturas.length}`,
);
revisar(
  `${CREDITOS_ESPERADOS} creditos`,
  malla.asignaturas.reduce((s, a) => s + a.creditos, 0) === CREDITOS_ESPERADOS,
);
revisar('codigos unicos', codigos.size === malla.asignaturas.length);
revisar(
  `${SEMESTRES_ESPERADOS} semestres, del 1 al ${SEMESTRES_ESPERADOS}`,
  [...new Set(malla.asignaturas.map((a) => a.semestre))]
    .sort((a, b) => a - b)
    .join(',') === Array.from({ length: SEMESTRES_ESPERADOS }, (_, i) => i + 1).join(','),
);
revisar(
  'formato de codigo de 6 digitos',
  malla.asignaturas.every((a) => /^\d{6}$/.test(a.codigo)),
);
revisar(
  'nombre no vacio y creditos > 0',
  malla.asignaturas.every((a) => a.nombre?.trim() && a.creditos > 0),
);

const prereqInexistente = malla.asignaturas.flatMap((a) =>
  a.prerrequisitos.filter((p) => !codigos.has(p)).map((p) => `${a.codigo} -> ${p}`),
);
revisar('todo prerrequisito existe en la malla', prereqInexistente.length === 0, prereqInexistente.join(', '));

const prereqFuturo = malla.asignaturas.flatMap((a) =>
  a.prerrequisitos.filter((p) => semestreDe(p) >= a.semestre).map((p) => `${a.codigo}(s${a.semestre}) -> ${p}(s${semestreDe(p)})`),
);
revisar('ningun prerrequisito es de un semestre igual o posterior', prereqFuturo.length === 0, prereqFuturo.join(', '));

const sinDuplicarPorSemestre = malla.asignaturas.every((a) => {
  const gemelas = malla.asignaturas.filter((o) => o.semestre === a.semestre && o.nombre === a.nombre);
  return gemelas.length === 1;
});
revisar('no hay asignaturas repetidas dentro de un semestre', sinDuplicarPorSemestre);

/* ============================================================
 * 2. estudiante_prueba.json
 * ============================================================ */
grupo('2. estudiante_prueba.json');

const historial = estudiante.historialAcademico;
const n = estudiante.semestreActual;
const aprobados = new Set(historial.filter((h) => h.estado === 'APROBADO').map((h) => h.codigo));

revisar('todos los codigos del historial existen', historial.every((h) => codigos.has(h.codigo)));
revisar('todas las notas estan entre 0 y 5', historial.every((h) => h.nota > 0 && h.nota <= 5));
revisar('semestreActual es 6', n === 6, `es ${n}`);
revisar(
  'ninguna asignatura del semestre actual esta aprobada',
  historial.every((h) => semestreDe(h.codigo) < n),
);
revisar('hay historial repetido', new Set(historial.map((h) => h.codigo)).size === historial.length);

/* Para entrar al semestre 6 el estudiante debe tener aprobados los
 * prerrequisitos de las asignaturas de ese semestre. */
const pendientes = malla.asignaturas.filter((a) => a.semestre === n).flatMap((a) => a.prerrequisitos);
revisar(
  'tiene aprobados todos los prerrequisitos del semestre 6',
  pendientes.every((p) => aprobados.has(p)),
  pendientes.filter((p) => !aprobados.has(p)).join(', '),
);

/* Y tambien los de esos prerrequisitos, y los de los de estos. */
function cierreTransitivo(iniciales) {
  const cierre = new Set(iniciales);
  let cambio = true;
  while (cambio) {
    cambio = false;
    for (const c of [...cierre]) {
      for (const p of porCodigo.get(c)?.prerrequisitos ?? []) {
        if (!cierre.has(p)) {
          cierre.add(p);
          cambio = true;
        }
      }
    }
  }
  return cierre;
}
const sinAprobar = [...cierreTransitivo(pendientes)].filter((c) => !aprobados.has(c));
revisar(
  'tiene aprobados tambien los prerrequisitos heredados',
  sinAprobar.length === 0,
  sinAprobar.join(', '),
);

const creditosAprobados = historial
  .filter((h) => h.estado === 'APROBADO')
  .reduce((s, h) => s + porCodigo.get(h.codigo).creditos, 0);
revisar(
  'no aprueba asignaturas de un semestre ya cursado dos veces',
  creditosAprobados === [...aprobados].reduce((s, c) => s + porCodigo.get(c).creditos, 0),
);
console.log(`        (${aprobados.size} asignaturas, ${creditosAprobados} creditos aprobados)`);

/* ============================================================
 * 3. sugerencia_mock.json
 * ============================================================ */
grupo('3. sugerencia_mock.json');

const filas = sugerencia.materiasSugeridas;

revisar('los codigos sugeridos existen en la malla', filas.every((f) => codigos.has(f.codigo)));
revisar(
  'nombre y creditos coinciden con la malla',
  filas.every((f) => f.nombre === porCodigo.get(f.codigo).nombre && f.creditos === porCodigo.get(f.codigo).creditos),
);
revisar(
  'solo hay filas de los semestres N y N+1',
  filas.every((f) => f.semestre === n || f.semestre === n + 1),
);
revisar(
  'estados validos',
  filas.every((f) => ['Sugerida', 'Prerrequisito'].includes(f.estado)),
);
revisar('no hay filas repetidas', new Set(filas.map((f) => f.codigo)).size === filas.length);
const extrasSugeridas = filas.filter((f) => f.semestre !== n && f.estado === 'Sugerida');
revisar(
  `como maximo ${CUPO_EXTRA} filas Sugerida entre adeudadas y N+1 (cupo compartido)`,
  extrasSugeridas.length <= CUPO_EXTRA,
  `hay ${extrasSugeridas.length}`,
);

const sumaSugerida = filas.filter((f) => f.estado === 'Sugerida').reduce((s, f) => s + f.creditos, 0);
revisar(
  'totalCreditos suma solo las filas Sugerida',
  sugerencia.totalCreditos === sumaSugerida,
  `totalCreditos=${sugerencia.totalCreditos}, suma=${sumaSugerida}`,
);
revisar(
  'totalCreditos no es la suma de todas las filas',
  sugerencia.totalCreditos !== filas.reduce((s, f) => s + f.creditos, 0),
  'si coincidiera, el fixture no probaria la trampa del contrato',
);

/* Prerrequisitos estrictos: una fila Sugerida siempre los cumple. Una fila
 * Prerrequisito puede tener prerrequisitoCumplido=true solo si quedo fuera
 * por el cupo de 3, y eso solo puede pasar en las filas extra (no en N). */
revisar(
  'prerrequisitoCumplido es coherente con el estado',
  filas
    .filter((f) => f.estado === 'Sugerida')
    .every((f) => f.prerrequisitoCumplido === true) &&
    filas
      .filter((f) => f.estado === 'Prerrequisito')
      .every((f) => f.prerrequisitoCumplido === false || f.semestre !== n),
);

/* ============================================================
 * 4. La sugerencia es la que produce la regla N+3
 * ============================================================ */
grupo('4. La sugerencia es la que produce la regla N+3');

/* Reimplementacion de ReglaN3.Calcular con la regla acordada:
 *  - Se sugiere TODO el semestre N.
 *  - Cupo extra de 3 filas, COMPARTIDO entre adeudadas (semestres < N sin
 *    aprobar, incluida Reprobada) y semestre N+1. Las adeudadas van primero
 *    (prioridad, no obligatorias); dentro de cada grupo, orden de codigo.
 *  - Prerrequisitos ESTRICTOS: solo cuentan las asignaturas APROBADAS del
 *    historial. Lo que se sugiere en este mismo periodo NO los satisface.
 *  - Si N es el ultimo semestre no hay filas N+1.
 * Se repite aqui a proposito: si el fixture dejara de ser lo que la regla
 * produce, el contrato se rompio y el error se ve sin levantar el backend. */
function reglaN3(catalogo, semestreActual, codigosAprobados) {
  const ultimo = Math.max(...catalogo.map((x) => x.semestre));
  const porCodigoAsc = (x, y) => x.codigo.localeCompare(y.codigo);
  const cumplePrereq = (a) => a.prerrequisitos.every((p) => codigosAprobados.has(p));
  const fila = (a, estado, cumple) => ({ codigo: a.codigo, semestre: a.semestre, creditos: a.creditos, estado, cumple });

  const salida = [];

  for (const a of catalogo.filter((x) => x.semestre === semestreActual).sort(porCodigoAsc)) {
    if (codigosAprobados.has(a.codigo)) continue;
    const cumple = cumplePrereq(a);
    salida.push(fila(a, cumple ? 'Sugerida' : 'Prerrequisito', cumple));
  }

  const adeudadas = catalogo
    .filter((x) => x.semestre < semestreActual && !codigosAprobados.has(x.codigo))
    .sort(porCodigoAsc);
  const siguientes =
    semestreActual < ultimo ? catalogo.filter((x) => x.semestre === semestreActual + 1).sort(porCodigoAsc) : [];

  let cupo = CUPO_EXTRA;
  for (const a of [...adeudadas, ...siguientes]) {
    const cumple = cumplePrereq(a);
    const entra = cumple && cupo > 0;
    if (entra) cupo--;
    salida.push(fila(a, entra ? 'Sugerida' : 'Prerrequisito', cumple));
  }
  return salida;
}

const esperado = reglaN3(malla.asignaturas, n, aprobados);

revisar(
  'mismas asignaturas y en el mismo orden',
  esperado.map((e) => e.codigo).join(',') === filas.map((f) => f.codigo).join(','),
  `esperado=${esperado.map((e) => e.codigo).join(',')} | fixture=${filas.map((f) => f.codigo).join(',')}`,
);

const esperadoPorCodigo = new Map(esperado.map((e) => [e.codigo, e]));

revisar(
  'mismos estados y mismos prerrequisitoCumplido',
  filas.every((f) => {
    const e = esperadoPorCodigo.get(f.codigo);
    return e && f.estado === e.estado && f.prerrequisitoCumplido === e.cumple;
  }),
);

const creditosEsperados = esperado.filter((e) => e.estado === 'Sugerida').reduce((s, e) => s + e.creditos, 0);
revisar('totalCreditos coincide con la regla', sugerencia.totalCreditos === creditosEsperados, `${sugerencia.totalCreditos} vs ${creditosEsperados}`);
revisar('totalCreditos de Laura es 23', sugerencia.totalCreditos === 23, `es ${sugerencia.totalCreditos}`);

const f603702 = filas.find((f) => f.codigo === '603702');
revisar(
  '603702 bloqueada: requiere 603601, que aun no esta aprobada (sin co-sugerencia)',
  f603702?.estado === 'Prerrequisito' && f603702.prerrequisitoCumplido === false,
);

const bloqueadasPorPrereq = ['603704', '603705', '603706'].map((c) => filas.find((f) => f.codigo === c));
revisar(
  '603704, 603705 y 603706 bloqueadas por prerrequisito no aprobado',
  bloqueadasPorPrereq.every((f) => f?.estado === 'Prerrequisito' && f.prerrequisitoCumplido === false),
);

revisar(
  'las filas Sugerida del semestre N+1 son solo 603701 y 603703',
  filas.filter((f) => f.semestre === n + 1 && f.estado === 'Sugerida').map((f) => f.codigo).join(',') === '603701,603703',
);

const extrasRegla = esperado.filter((e) => e.semestre !== n && e.estado === 'Sugerida').length;
revisar(`entre adeudadas y N+1 hay como maximo ${CUPO_EXTRA} Sugerida (regla)`, extrasRegla <= CUPO_EXTRA, `hay ${extrasRegla}`);

/* ============================================================
 * 5. Integracion con el mock del frontend
 * ============================================================ */
grupo('5. Integracion con el mock del frontend');

const appJs = readFileSync(join(aqui, '..', '..', 'assets', 'js', 'app.js'), 'utf8');
const inicio = appJs.indexOf('const MOCK_SUGERENCIA');
const desde = appJs.indexOf('{', inicio);
let nivel = 0;
let fin = desde;
for (let i = desde; i < appJs.length; i++) {
  if (appJs[i] === '{') nivel++;
  if (appJs[i] === '}') nivel--;
  if (nivel === 0) {
    fin = i + 1;
    break;
  }
}
const embebido = Function(`"use strict"; return (${appJs.slice(desde, fin)});`)();

revisar(
  'el MOCK_SUGERENCIA de assets/js/app.js es identico al fixture',
  JSON.stringify(embebido) === JSON.stringify(sugerencia),
  'el frontend se desincronizo: el contrato de la API esta partido en dos',
);

revisar(
  'el nombre del estudiante es el mismo en ambos lados',
  embebido.nombreEstudiante === estudiante.nombreEstudiante,
);

/* ============================================================ */
console.log(`\n${fallos === 0 ? 'TODO OK' : 'FALLOS'}: ${total - fallos}/${total} comprobaciones.`);
process.exit(fallos === 0 ? 0 : 1);