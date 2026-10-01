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

   CONTRATO (contract-first, ver GUIA_BACKEND.md §4):
     GET  /api/v1/matricula/sugerencia
     POST /api/v1/matricula/confirmar
     Forma JSON mínima que el backend DEBE devolver:
       { nombreEstudiante, programa, totalCreditos, materiasSugeridas[] }

   PARA CONECTAR EL BACKEND REAL: API_CONFIG.useMock = false.
   ============================================================ */
'use strict';

/* ============================================================
   0 · CONFIGURACIÓN
   ============================================================ */
const API_CONFIG = {
  baseURL: '',                               // el proxy local reenvía a la API
  endpoints: {
    sugerencia: '/api/v1/matricula/sugerencia',
    confirmar:  '/api/v1/matricula/confirmar'
  },
  useMock: true,          // ← false cuando el microservicio esté desplegado
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

const MatriculaApi = {
  _headers(includeJson = false) {
    const token = sessionStorage.getItem('sipa.auth.token');
    const headers = { 'Accept': 'application/json' };
    if (includeJson) headers['Content-Type'] = 'application/json';
    if (token) headers['Authorization'] = `Bearer ${token}`;
    return headers;
  },

  /**
   * GET /api/v1/matricula/sugerencia
   * @returns {Promise<Object>} { nombreEstudiante, programa, semestreActual,
   *                              totalCreditos, materiasSugeridas[], notificaciones[] }
   */
  async obtenerSugerencia() {
    if (API_CONFIG.useMock) {
      await this._esperar(API_CONFIG.mockDelayMs);
      // copia profunda para no mutar el mock entre renderizados
      return JSON.parse(JSON.stringify(MOCK_SUGERENCIA));
    }

    const respuesta = await fetch(`${API_CONFIG.baseURL}${API_CONFIG.endpoints.sugerencia}`, {
      method: 'GET',
      headers: this._headers()
    });
    if (!respuesta.ok) {
      throw new Error(`HTTP ${respuesta.status} en GET ${API_CONFIG.endpoints.sugerencia}`);
    }
    return respuesta.json();
  },

  /**
   * POST /api/v1/matricula/confirmar
   * @param   {Object} payload - { codigoEstudiante, codigosMaterias[] }
   * @returns {Promise<Object>} { exito, matriculaId, mensaje }
   */
  async confirmarMatricula(payload) {
    if (API_CONFIG.useMock) {
      await this._esperar(API_CONFIG.mockDelayMs);
      return {
        exito: true,
        matriculaId: `MAT-2026-${String(Date.now()).slice(-6)}`,
        mensaje: `Matrícula confirmada: ${payload.codigosMaterias.length} asignaturas.`
      };
    }

    const respuesta = await fetch(`${API_CONFIG.baseURL}${API_CONFIG.endpoints.confirmar}`, {
      method: 'POST',
      headers: this._headers(true),
      body: JSON.stringify(payload)
    });
    if (!respuesta.ok) {
      throw new Error(`HTTP ${respuesta.status} en POST ${API_CONFIG.endpoints.confirmar}`);
    }
    return respuesta.json();
  },

  /** Simula la latencia de la red en modo mock. */
  _esperar(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
};

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
      const fila = document.createElement('tr');
      fila.setAttribute('data-codigo', materia.codigo);
      fila.setAttribute('data-estado', materia.estado);

      fila.appendChild(this._celda(materia.codigo));
      fila.appendChild(this._celda(materia.nombre));
      fila.appendChild(this._celda(materia.creditos));
      fila.appendChild(this._celda(materia.semestre));
      fila.appendChild(this._celdaEstado(materia.estado));

      tbody.appendChild(fila);
    });
  },

  _celda(valor) {
    const td = document.createElement('td');
    td.textContent = valor;
    return td;
  },

  _celdaEstado(estado) {
    const td = document.createElement('td');
    const badge = document.createElement('span');
    badge.className = estado === 'Sugerida' ? 'badge badge-success' : 'badge badge-warning';
    badge.textContent = estado;
    td.appendChild(badge);
    return td;
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
    alerta.className = `alert alert-${tipo} alert-dismissible fade show`;
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

  async init() {
    UIManager.cachearElementos();
    UIManager.iniciarReloj();
    this._suscribirEventos();

    try {
      const data = await MatriculaApi.obtenerSugerencia();

      this.materiasActuales = Array.isArray(data.materiasSugeridas) ? data.materiasSugeridas : [];

      UIManager.renderPerfil(data);                 // #user-nombre / #user-programa
      UIManager.renderMaterias(this.materiasActuales); // #tabla-materias-body
      UIManager.renderTotalCreditos(data.totalCreditos, this.materiasActuales); // #total-creditos
      if (data.notificaciones) UIManager.renderNotificaciones(data.notificaciones);

      UIManager.setBotonConfirmar({ habilitado: true });
    } catch (error) {
      console.error('[SIPA] Error cargando la sugerencia:', error);
      UIManager.mostrarAlerta('danger', 'No fue posible cargar la sugerencia de matrícula. Intente de nuevo más tarde.');
      UIManager.setBotonConfirmar({ habilitado: false });
    }
  },

  _suscribirEventos() {
    if (UIManager.el.btnConfirmar) {
      UIManager.el.btnConfirmar.addEventListener('click', () => this.confirmarMatricula());
    }
  },

  async confirmarMatricula() {
    const codigosMaterias = this.materiasActuales
      .filter(m => m.estado === 'Sugerida')
      .map(m => m.codigo);

    if (codigosMaterias.length === 0) {
      UIManager.mostrarAlerta('warning', 'No hay asignaturas seleccionadas para confirmar.');
      return;
    }

    UIManager.setBotonConfirmar({ habilitado: false, texto: 'Confirmando…' });
    try {
      const resultado = await MatriculaApi.confirmarMatricula({
        codigoEstudiante: null,        // ← el backend lo resuelve con el token de sesión
        codigosMaterias
      });
      UIManager.mostrarAlerta('success', resultado.mensaje || 'Matrícula confirmada exitosamente.');
    } catch (error) {
      console.error('[SIPA] Error confirmando matrícula:', error);
      UIManager.mostrarAlerta('danger', 'No fue posible confirmar la matrícula.');
    } finally {
      UIManager.setBotonConfirmar({ habilitado: true, texto: 'Confirmar Matrícula' });
    }
  }
};

/* ============================================================
   INICIALIZACIÓN
   ============================================================ */
document.addEventListener('DOMContentLoaded', () => App.init());
