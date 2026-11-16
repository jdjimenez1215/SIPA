import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { JSDOM } from 'jsdom';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../..');

const sugerencia = {
  nombreEstudiante: 'Laura Gómez Ríos',
  programa: 'Ingeniería de Sistemas',
  semestreActual: 6,
  periodo: '2026-2',
  totalCreditos: 3,
  promedio: 3.82,
  limiteCreditos: 18,
  materiasSugeridas: [
    { id: 27, codigo: '603601', nombre: 'Ingeniería de Software II', creditos: 3, semestre: 6, estado: 'Sugerida', prerrequisitoCumplido: true, cuposDisponibles: 25, cuposMaximos: 30 },
    { id: 34, codigo: '603704', nombre: 'Sistemas Distribuidos', creditos: 3, semestre: 7, estado: 'Prerrequisito', prerrequisitoCumplido: false, motivo: 'PREREQUISITO_NO_CUMPLIDO', cuposDisponibles: 10, cuposMaximos: 30 }
  ],
  notificaciones: []
};

const inscripciones = [
  { id: 1000, estudianteId: 1, materiaId: 28, periodoAcademico: '2026-2', estado: 'Activa', materia: { id: 28, codigo: '603602', nombre: 'Métodos Numéricos', creditos: 3 } },
  { id: 1001, estudianteId: 1, materiaId: 29, periodoAcademico: '2026-2', estado: 'Activa', materia: { id: 29, codigo: '603603', nombre: 'Procesamiento de Señales e Imágenes', creditos: 3 } }
];

async function renderPage() {
  const html = fs.readFileSync(path.join(root, 'sugerencia-matricula.html'), 'utf8')
    .replace(/<script[\s\S]*?<\/script>/gi, '');
  const dom = new JSDOM(html, {
    url: 'http://localhost:5500/sugerencia-matricula.html',
    runScripts: 'outside-only',
    pretendToBeVisual: true
  });
  const { window } = dom;
  window.sessionStorage.setItem('sipa.auth.token', 'fake');
  window.sessionStorage.setItem('sipa.auth.estudianteId', '1');

  window.fetch = async (url) => {
    const body = String(url).includes('/inscripciones?') ? inscripciones : sugerencia;
    return {
      ok: true,
      status: 200,
      headers: { get: () => 'application/json' },
      text: async () => JSON.stringify(body)
    };
  };

  window.eval(fs.readFileSync(path.join(root, 'assets/js/app.js'), 'utf8'));
  window.document.dispatchEvent(new window.Event('DOMContentLoaded'));
  await new Promise(resolve => setTimeout(resolve, 50));
  return dom;
}

test('pinta sugerencia, cupos, inscritas y selección por materia', async () => {
  const dom = await renderPage();
  const { document } = dom.window;

  assert.equal(document.querySelectorAll('#tabla-materias-body tr').length, 2);
  assert.equal(document.querySelector('#tabla-inscritas-body .btn-cancelar-inscripcion').textContent, 'Cancelar');
  assert.equal(document.querySelectorAll('#tabla-inscritas-body .btn-cancelar-inscripcion').length, 2);
  assert.equal(document.getElementById('limite-creditos').textContent, '18');
  assert.equal(document.getElementById('promedio-estudiante').textContent, '3.82');

  const cupos = document.querySelector('#tabla-materias-body tr[data-codigo="603601"] td:nth-child(6)').textContent;
  assert.equal(cupos.trim(), '25 / 30');

  const checks = document.querySelectorAll('#tabla-materias-body .materia-check');
  assert.equal(checks.length, 1, 'solo las materias Sugerida tienen casilla');
  assert.equal(document.getElementById('creditos-seleccionados').textContent, '3');

  checks[0].checked = false;
  checks[0].dispatchEvent(new dom.window.Event('change', { bubbles: true }));
  assert.equal(document.getElementById('creditos-seleccionados').textContent, '0');
  assert.equal(document.getElementById('btn-confirmar').disabled, true);
  assert.ok(document.querySelector('#tabla-materias-body tr[data-codigo="603601"]').classList.contains('materia-excluida'));

  dom.window.close();
});
