/* ============================================================
   app.js — SIPA | Simulador de Matrícula (Regla N+3)
   ------------------------------------------------------------
   Vanilla JavaScript (ES6) — Patrón Módulo (namespace)
   Principio guía: SRP (Single Responsibility Principle)

   Estructura en dos capas + punto de entrada:
     1. CAPA DE RED  · MatriculaApi  → hace el fetch y devuelve JSON.
     2. CAPA DE UI    · UIManager     → solo inyecta ese JSON en el DOM
                                        (IDs de la plantilla vacía).
     3. MAIN          · App.init()    → conecta ambas capas en DOMContentLoaded.

   La plantilla HTML NO contiene datos: la tabla, el perfil y el total se
   pintan aquí a partir de la respuesta del backend.

   CONTRATO (ms-inscripcion, vía el proxy de dev-server.py):
     GET  /api/estudiantes/{id}/sugerencia?periodo=2026-2
     POST /api/inscripciones  { estudianteId, periodo, materiaIds[] }
     Errores: application/problem+json (detail, code, traceId, errors,
     violations[]). El 422 es atómico: no se inscribe ninguna materia.

   MODO OFFLINE: API_CONFIG.useMock = true (usa el mock, sin red).
   ============================================================ */
'use strict';

/* ============================================================
   0 · CONFIGURACIÓN
   ============================================================ */
const API_CONFIG = {
  baseURL: '',                               // el proxy local reenvía a la API
  estudianteId: 1,                           // demo: Laura Gómez Ríos
  periodo: '2026-2',
  endpoints: {
    sugerencia: (id, periodo) => `/api/estudiantes/${id}/sugerencia?periodo=${encodeURIComponent(periodo)}`,
    confirmar:  '/api/inscripciones'
  },
  useMock: false,         // ← true para la demo offline (sin backend)
  mockDelayMs: 400        // simula latencia de red en el mock
};

/* ============================================================
   1 · CAPA DE RED (MatriculaApi)
   Responsabilidad Única: transporte de datos (mock → fetch).
   No conoce el DOM.
   ============================================================ */

/* --- MOCK temporal: tiene la forma EXACTA del contrato JSON ---
   Existe solo para probar la inyección sin backend; desaparece al
   poner API_CONFIG.useMock = false. */
const MOCK_SUGERENCIA = {
  nombreEstudiante: 'Laura Gómez Ríos',
  programa: 'Ingeniería de Sistemas',
  semestreActual: 6,
  totalCreditos: 23,
  materiasSugeridas: [
    // Semestre N (6) — sugeridas
    { codigo: '603601', nombre: 'Ingeniería de Software II',                creditos: 3, semestre: 6, estado: 'Sugerida',     prerrequisitoCumplido: true },
    { codigo: '603602', nombre: 'Métodos Numéricos',                        creditos: 3, semestre: 6, estado: 'Sugerida',     prerrequisitoCumplido: true },
    { codigo: '603603', nombre: 'Procesamiento de Señales e Imágenes',      creditos: 3, semestre: 6, estado: 'Sugerida',     prerrequisitoCumplido: true },
    { codigo: '603604', nombre: 'Optimización',                             creditos: 3, semestre: 6, estado: 'Sugerida',     prerrequisitoCumplido: true },
    { codigo: '603605', nombre: 'Redes de Computadores',                    creditos: 4, semestre: 6, estado: 'Sugerida',     prerrequisitoCumplido: true },
    { codigo: '603606', nombre: 'Administración Financiera para Ingeniería', creditos: 2, semestre: 6, estado: 'Sugerida',     prerrequisitoCumplido: true },
    // Semestre N+1: solo entran las que tienen prerrequisito aprobado
    { codigo: '603701', nombre: 'Metodología de Investigación',             creditos: 3, semestre: 7, estado: 'Sugerida',     prerrequisitoCumplido: true },
    { codigo: '603702', nombre: 'Tecnologías Avanzadas',                    creditos: 3, semestre: 7, estado: 'Prerrequisito', prerrequisitoCumplido: false },
    { codigo: '603703', nombre: 'Ética y Humanística',                      creditos: 2, semestre: 7, estado: 'Sugerida',     prerrequisitoCumplido: true },
    // Filas bloqueadas por prerrequisito no aprobado: no suman al totalCreditos
    { codigo: '603704', nombre: 'Sistemas Distribuidos',                   creditos: 3, semestre: 7, estado: 'Prerrequisito', prerrequisitoCumplido: false },
    { codigo: '603705', nombre: 'Seguridad de la Información',              creditos: 3, semestre: 7, estado: 'Prerrequisito', prerrequisitoCumplido: false },
    { codigo: '603706', nombre: 'Formulación y Gestión de Proyectos TI',    creditos: 3, semestre: 7, estado: 'Prerrequisito', prerrequisitoCumplido: false }
  ],
  notificaciones: [
    { id: 1, titulo: 'Matrícula habilitada', mensaje: 'Tu periodo de matrícula N+3 está abierto.', fecha: '2026-09-30T08:00:00', leida: false }
  ]
};


/* Texto en español para cada código de `motivo` que envía el backend.
   Va DESPUÉS del mock a propósito (validar-mocks.mjs lee ese bloque).
   Código desconocido → se muestra el código tal cual. */
const MOTIVO_LABELS = {
  PREREQUISITO_NO_CUMPLIDO: 'Prerrequisito no aprobado',
  LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO: 'Cupo de 3 asignaturas extra alcanzado',
  CUPO_AGOTADO: 'Sin cupos disponibles',
  CRUCE_HORARIO: 'Cruce de horario',
  SEMESTRE_EXCEDIDO: 'Fuera de la ventana de semestres',
  MATERIA_YA_INSCRITA: 'Ya inscrita en el periodo',
  MATERIA_YA_APROBADA: 'Ya aprobada',
  CARRERA_NO_CORRESPONDE: 'No pertenece a su programa'
};

const etiquetaMotivo = codigo => MOTIVO_LABELS[codigo] || codigo;

const MatriculaApi = {
  _headers(includeJson = false) {
    const token = sessionStorage.getItem('sipa.auth.token');
    const headers = { 'Accept': 'application/json' };
    if (includeJson) headers['Content-Type'] = 'application/json';
    if (token) headers['Authorization'] = `Bearer ${token}`;
    return headers;
  },

  /**
   * Transporte único. NUNCA lanza por errores HTTP: el flujo normal
   * ramifica por status (201 / 422 / 404...).
   * @returns {Promise<{ok:boolean, status:number, body:*}>} status 0 = sin red.
   */
  async _request(method, path, body) {
    let respuesta;
    try {
      respuesta = await fetch(`${API_CONFIG.baseURL}${path}`, {
        method,
        headers: this._headers(body !== undefined),
        body: body !== undefined ? JSON.stringify(body) : undefined
      });
    } catch (error) {
      console.error('[SIPA] Fallo de red:', error);
      return { ok: false, status: 0, body: null };
    }

    let cuerpo = null;
    const tipo = respuesta.headers.get('content-type') || '';
    if (tipo.includes('json')) {
      try {
        cuerpo = JSON.parse(await respuesta.text());
      } catch (error) {
        cuerpo = null;
      }
    }
    return { ok: respuesta.ok, status: respuesta.status, body: cuerpo };
  },

  /** GET /api/estudiantes/{id}/sugerencia?periodo=... */
  async obtenerSugerencia() {
    if (API_CONFIG.useMock) {
      await this._esperar(API_CONFIG.mockDelayMs);
      // copia profunda para no mutar el mock entre renderizados
      return { ok: true, status: 200, body: JSON.parse(JSON.stringify(MOCK_SUGERENCIA)) };
    }
    const { estudianteId, periodo } = API_CONFIG;
    return this._request('GET', API_CONFIG.endpoints.sugerencia(estudianteId, periodo));
  },

  /**
   * POST /api/inscripciones
   * @param {{estudianteId:number, periodo:string, materiaIds:Array}} payload
   * @returns 201 con InscripcionDto[]; 422 con violations[] (atómico).
   */
  async confirmarMatricula(payload) {
    if (API_CONFIG.useMock) {
      await this._esperar(API_CONFIG.mockDelayMs);
      return { ok: true, status: 201, body: payload.materiaIds.map(id => ({ materiaId: id })) };
    }
    return this._request('POST', API_CONFIG.endpoints.confirmar, payload);
  },

  /** Simula la latencia de la red en modo mock. */
  _esperar(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
};

/**
 * Convierte una respuesta de error en texto para el usuario. Pura.
 * @param {{status:number, body:*}} result
 */
function describirProblema(result) {
  const { status, body } = result;
  if (status === 0) return 'Sin conexión con el servidor local (dev-server.py).';
  if (!body || typeof body !== 'object') return `Error HTTP ${status}`;

  let mensaje = body.detail || body.title || `Error HTTP ${status}`;
  if (status === 400 && body.errors && typeof body.errors === 'object') {
    const detalles = Object.values(body.errors).flat().join(' · ');
    if (detalles) mensaje += `: ${detalles}`;
  }
  if (status >= 500 && body.traceId) mensaje += ` (traceId: ${body.traceId})`;
  return mensaje;
}

/* ============================================================
   2 · CAPA DE UI (UIManager)
   Responsabilidad Única: recibir JSON y pintarlo en el DOM.
   No conoce la red. Usa textContent (nunca innerHTML con datos)
   para evitar inyección de código (XSS).
   ============================================================ */
const UIManager = {
  el: {},

  /** Resuelve los IDs de la plantilla vacía una sola vez. */
  cachearElementos() {
    this.el = {
      userNombre:          document.getElementById('user-nombre'),
      userPrograma:        document.getElementById('user-programa'),
      tablaBody:           document.getElementById('tabla-materias-body'),
      totalCreditos:       document.getElementById('total-creditos'),
      btnConfirmar:        document.getElementById('btn-confirmar'),
      alertContainer:      document.getElementById('alert-container'),
      navFechaHoraTxt:     document.getElementById('navFechaHoraTxt'),
      numNotificaciones:   document.getElementById('num-notificaciones'),
      notificacionesTitulo: document.getElementById('notificaciones-titulo'),
      notificacionesLista:  document.getElementById('notificaciones-lista')
    };
  },

  /* ---------- Perfil (navbar superior derecha) ---------- */
  renderPerfil(datos) {
    if (this.el.userNombre) {
      this.el.userNombre.textContent = datos.nombreEstudiante || '';
      this.el.userNombre.setAttribute('data-semestre', datos.semestreActual != null ? datos.semestreActual : '');
    }
    if (this.el.userPrograma) {
      this.el.userPrograma.textContent = datos.programa || '';
    }
  },

  /* ---------- Estado de carga ---------- */
  setCargando() {
    this.setBotonConfirmar({ habilitado: false });
    const tbody = this.el.tablaBody;
    if (!tbody) return;
    tbody.textContent = '';
    const fila = document.createElement('tr');
    const celda = document.createElement('td');
    celda.colSpan = 5;
    celda.className = 'text-center text-muted';
    celda.textContent = 'Cargando…';
    fila.appendChild(celda);
    tbody.appendChild(fila);
  },

  vaciarTabla() {
    if (this.el.tablaBody) this.el.tablaBody.textContent = '';
  },

  /* ---------- Tabla de materias sugeridas ---------- */
  renderMaterias(materias) {
    const tbody = this.el.tablaBody;
    if (!tbody) return;
    tbody.textContent = '';                       // limpia filas anteriores

    if (!Array.isArray(materias) || materias.length === 0) {
      const fila = document.createElement('tr');
      const celda = document.createElement('td');
      celda.colSpan = 5;
      celda.className = 'text-center text-muted';
      celda.textContent = 'No hay asignaturas sugeridas para este periodo.';
      fila.appendChild(celda);
      tbody.appendChild(fila);
      return;
    }

    materias.forEach(materia => {
      const bloqueada = materia.estado !== 'Sugerida';
      const fila = document.createElement('tr');
      fila.setAttribute('data-codigo', materia.codigo);
      fila.setAttribute('data-estado', materia.estado);
      if (materia.id != null) fila.setAttribute('data-id', materia.id);
      if (bloqueada) {
        fila.className = 'text-muted';
        fila.setAttribute('aria-disabled', 'true');
      }

      fila.appendChild(this._celda(materia.codigo));
      fila.appendChild(this._celda(materia.nombre));
      fila.appendChild(this._celda(materia.creditos));
      fila.appendChild(this._celda(materia.semestre));
      fila.appendChild(this._celdaEstado(materia));

      tbody.appendChild(fila);
    });
  },

  _celda(valor) {
    const td = document.createElement('td');
    td.textContent = valor;
    return td;
  },

  /* Badge de estado + (si está bloqueada) motivo en español. */
  _celdaEstado(materia) {
    const td = document.createElement('td');
    const badge = document.createElement('span');
    badge.className = materia.estado === 'Sugerida' ? 'badge badge-success' : 'badge badge-warning';
    badge.textContent = materia.estado;
    td.appendChild(badge);

    if (materia.motivo) {
      const etiqueta = etiquetaMotivo(materia.motivo);
      const motivo = document.createElement('small');
      motivo.className = 'motivo';
      motivo.textContent = etiqueta;
      td.appendChild(motivo);
      td.title = etiqueta;
    }
    return td;
  },

  /* ---------- Violaciones del 422 (por fila) ---------- */
  limpiarViolaciones() {
    const tbody = this.el.tablaBody;
    if (!tbody) return;
    tbody.querySelectorAll('tr.danger').forEach(fila => {
      fila.classList.remove('danger');
      fila.removeAttribute('title');
    });
    tbody.querySelectorAll('.violacion, .badge-violacion').forEach(nodo => nodo.remove());
  },

  /**
   * Pinta cada violación en su fila (materiaCodigo, si no materiaId).
   * @returns {Array} violaciones sin fila asociada.
   */
  renderViolaciones(violations) {
    const tbody = this.el.tablaBody;
    const sinFila = [];
    const lista = Array.isArray(violations) ? violations : [];

    const porFila = new Map();
    lista.forEach(v => {
      let fila = null;
      if (tbody && v.materiaCodigo != null) {
        fila = Array.from(tbody.querySelectorAll('tr[data-codigo]'))
          .find(f => f.getAttribute('data-codigo') === String(v.materiaCodigo)) || null;
      }
      if (!fila && tbody && v.materiaId != null) {
        fila = Array.from(tbody.querySelectorAll('tr[data-id]'))
          .find(f => f.getAttribute('data-id') === String(v.materiaId)) || null;
      }
      if (!fila) { sinFila.push(v); return; }
      if (!porFila.has(fila)) porFila.set(fila, []);
      porFila.get(fila).push(v);
    });

    porFila.forEach((vs, fila) => {
      fila.classList.add('danger');
      const mensajes = vs.map(v => v.message || etiquetaMotivo(v.code));
      fila.title = mensajes.join('\n');
      const celda = fila.lastElementChild;
      vs.forEach((v, i) => {
        const badge = document.createElement('span');
        badge.className = 'badge badge-danger badge-violacion';
        badge.textContent = etiquetaMotivo(v.code);
        badge.title = mensajes[i];
        celda.appendChild(badge);
        const msg = document.createElement('small');
        msg.className = 'violacion';
        msg.textContent = mensajes[i];
        celda.appendChild(msg);
      });
    });
    return sinFila;
  },

  /* ---------- Total de créditos ----------
     El valor autoritativo es data.totalCreditos (lo envía el backend).
     Si llegara ausente, se calcula aquí como respaldo para no romper la UI. */
  renderTotalCreditos(totalCreditos, materias) {
    let total = totalCreditos;

    if (typeof total !== 'number') {
      total = (Array.isArray(materias) ? materias : [])
        .filter(m => m.estado === 'Sugerida')
        .reduce((suma, m) => suma + Number(m.creditos || 0), 0);
      console.warn('[SIPA] El backend no envió totalCreditos; se calculó en el frontend.');
    }

    if (this.el.totalCreditos) {
      this.el.totalCreditos.textContent = total;
      this.el.totalCreditos.setAttribute('data-total', total);
    }
    return total;
  },

  /* ---------- Notificaciones (campo opcional del contrato) ---------- */
  renderNotificaciones(notificaciones) {
    const lista = Array.isArray(notificaciones) ? notificaciones : [];
    const cuenta = lista.length;

    if (this.el.numNotificaciones) {
      this.el.numNotificaciones.textContent = cuenta;
      this.el.numNotificaciones.setAttribute('data-count', cuenta);
    }
    if (this.el.notificacionesTitulo) {
      this.el.notificacionesTitulo.textContent = `Notificaciones (${cuenta})`;
      this.el.notificacionesTitulo.setAttribute('data-count', cuenta);
    }
    if (!this.el.notificacionesLista) return;

    this.el.notificacionesLista.textContent = '';
    lista.forEach(n => {
      const item = document.createElement('div');
      item.className = 'dropdown-item notification-item' + (n.leida ? '' : ' notification-unread');
      item.setAttribute('data-id', n.id);

      const titulo = document.createElement('div');
      titulo.className = 'notification-title';
      titulo.textContent = n.titulo;

      const mensaje = document.createElement('div');
      mensaje.className = 'notification-message';
      mensaje.textContent = n.mensaje;

      const hora = document.createElement('div');
      hora.className = 'notification-time';
      hora.textContent = n.fecha || '';

      item.appendChild(titulo);
      item.appendChild(mensaje);
      item.appendChild(hora);
      this.el.notificacionesLista.appendChild(item);
    });
  },

  /* ---------- Alertas (contenedor #alert-container) ---------- */
  mostrarAlerta(tipo, mensaje) {
    if (!this.el.alertContainer) return;
    this.el.alertContainer.textContent = '';

    const alerta = document.createElement('div');
    // Bootstrap 3.3.7: 'fade in' (no existe .fade.show, eso es BS4)
    alerta.className = `alert alert-${tipo} alert-dismissible fade in`;
    alerta.setAttribute('role', 'alert');
    alerta.textContent = mensaje;

    const cerrar = document.createElement('button');
    cerrar.type = 'button';
    cerrar.className = 'close';
    cerrar.setAttribute('data-dismiss', 'alert');
    cerrar.setAttribute('aria-label', 'Cerrar');
    const aspa = document.createElement('span');
    aspa.setAttribute('aria-hidden', 'true');
    aspa.textContent = '×';                       // equivalente a &times;
    cerrar.appendChild(aspa);

    alerta.appendChild(cerrar);
    this.el.alertContainer.appendChild(alerta);
  },

  limpiarAlertas() {
    if (this.el.alertContainer) this.el.alertContainer.textContent = '';
  },

  /* ---------- Reloj del header (dato de cliente, no del backend) ---------- */
  iniciarReloj() {
    const pintar = () => {
      if (!this.el.navFechaHoraTxt) return;
      this.el.navFechaHoraTxt.textContent = new Intl.DateTimeFormat('es-CO', {
        dateStyle: 'medium', timeStyle: 'medium'
      }).format(new Date());
    };
    pintar();
    setInterval(pintar, 1000);
  },

  /* ---------- Estado del botón de confirmación ---------- */
  setBotonConfirmar({ habilitado, texto }) {
    if (!this.el.btnConfirmar) return;
    this.el.btnConfirmar.disabled = !habilitado;
    if (texto) this.el.btnConfirmar.textContent = texto;
  }
};

/* ============================================================
   3 · MAIN (punto de entrada)
   Orquesta: pide datos a la capa de red y se los pasa a la capa de UI.
   ============================================================ */
const App = {
  materiasActuales: [],
  enviando: false,

  async init() {
    UIManager.cachearElementos();
    UIManager.iniciarReloj();
    this._suscribirEventos();
    await this.cargarSugerencia();
  },

  _haySugeridas() {
    return this.materiasActuales.some(m => m.estado === 'Sugerida');
  },

  /** Carga y pinta la sugerencia. `conservarAlerta` mantiene el aviso previo. */
  async cargarSugerencia({ conservarAlerta = false } = {}) {
    if (!conservarAlerta) UIManager.limpiarAlertas();
    UIManager.setCargando();

    const resultado = await MatriculaApi.obtenerSugerencia();
    if (!resultado.ok) {
      this.materiasActuales = [];
      UIManager.vaciarTabla();
      UIManager.mostrarAlerta('danger', describirProblema(resultado));
      UIManager.setBotonConfirmar({ habilitado: false });
      return;
    }

    const data = resultado.body || {};
    this.materiasActuales = Array.isArray(data.materiasSugeridas) ? data.materiasSugeridas : [];

    UIManager.renderPerfil(data);                    // #user-nombre / #user-programa
    UIManager.renderMaterias(this.materiasActuales); // #tabla-materias-body
    UIManager.renderTotalCreditos(data.totalCreditos, this.materiasActuales); // #total-creditos
    if (data.notificaciones) UIManager.renderNotificaciones(data.notificaciones);

    UIManager.setBotonConfirmar({ habilitado: this._haySugeridas() });
  },

  _suscribirEventos() {
    if (UIManager.el.btnConfirmar) {
      UIManager.el.btnConfirmar.addEventListener('click', () => this.confirmarMatricula());
    }
  },

  async confirmarMatricula() {
    if (this.enviando) return;

    const sugeridas = this.materiasActuales.filter(m => m.estado === 'Sugerida');
    if (sugeridas.length === 0) {
      UIManager.mostrarAlerta('warning', 'No hay asignaturas seleccionadas para confirmar.');
      return;
    }
    // El mock no trae `id`; en modo real es obligatorio (no hay lookup código→id).
    const materiaIds = sugeridas.map(m => (m.id != null ? m.id : (API_CONFIG.useMock ? m.codigo : null)));
    if (materiaIds.some(id => id == null)) {
      UIManager.mostrarAlerta('danger', 'La respuesta del servidor no incluye el identificador de las asignaturas.');
      return;
    }

    this.enviando = true;
    UIManager.limpiarViolaciones();
    UIManager.setBotonConfirmar({ habilitado: false, texto: 'Confirmando…' });

    let recargar = false;
    try {
      const resultado = await MatriculaApi.confirmarMatricula({
        estudianteId: API_CONFIG.estudianteId,
        periodo: API_CONFIG.periodo,
        materiaIds
      });

      if (resultado.status === 201) {
        const n = Array.isArray(resultado.body) ? resultado.body.length : materiaIds.length;
        UIManager.mostrarAlerta('success', `${n} ${n === 1 ? 'asignatura inscrita' : 'asignaturas inscritas'} (${API_CONFIG.periodo})`);
        recargar = !API_CONFIG.useMock;
      } else if (resultado.status === 422 && resultado.body && Array.isArray(resultado.body.violations)) {
        const sinFila = UIManager.renderViolaciones(resultado.body.violations);
        let mensaje = `No se inscribió ninguna asignatura: ${resultado.body.violations.length} ` +
          `${resultado.body.violations.length === 1 ? 'problema' : 'problemas'} en la selección.`;
        if (sinFila.length > 0) {
          mensaje += ' ' + sinFila.map(v => v.message || etiquetaMotivo(v.code)).join(' · ');
        }
        UIManager.mostrarAlerta('danger', mensaje);
      } else {
        UIManager.mostrarAlerta('danger', describirProblema(resultado));
      }
    } finally {
      this.enviando = false;
      UIManager.setBotonConfirmar({ habilitado: this._haySugeridas(), texto: 'Confirmar Matrícula' });
    }

    if (recargar) await this.cargarSugerencia({ conservarAlerta: true });
  }
};

/* ============================================================
   INICIALIZACIÓN
   ============================================================ */
document.addEventListener('DOMContentLoaded', () => App.init());
