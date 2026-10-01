/* ============================================================
   login.js — SIPA | Pantalla de acceso (index.html)
   ------------------------------------------------------------
   Vanilla JavaScript (ES6) — Patrón Módulo (namespace)
   Principio guía: SRP (Single Responsibility Principle)

   Estructura en dos capas + punto de entrada:
     1. CAPA DE RED  · LoginApi   → envía JSON y recibe la respuesta.
     2. CAPA DE UI    · LoginUI    → solo escribe en #response y #respuesta.
     3. MAIN          · LoginApp   → valida y conecta ambas capas.

   INSTRUCCIÓN PARA EL BACKEND (contract-first, ver GUIA_BACKEND.md §2):
     Basta con publicar los tres endpoints de LOGIN_CONFIG.endpoints con la
     forma documentada en la guía. No hay que tocar la capa de UI.

   Historial: antes dependía de legacy-app.js (AJAX_PARAMETRIZADO/"AjaxLogin")
   y del atributo inline onsubmit. Hoy no queda ninguna dependencia legacy.
   ============================================================ */
'use strict';

/* ============================================================
   0 · CONFIGURACIÓN / CONTRATO
   ============================================================ */
const LOGIN_CONFIG = {
  baseURL: 'http://localhost:3000/api',   // misma base que API_CONFIG (app.js)
  endpoints: {
    login:             '/auth/login',
    recuperarPassword: '/auth/recuperar-password',
    consultarUsuario:  '/auth/consultar-usuario'
  },
  rutaTrasLogin: 'sugerencia-matricula.html'   // adonde va tras un 200 del login
};

/* ============================================================
   1 · CAPA DE RED (LoginApi)
   Responsabilidad Única: transporte de datos (fetch).
   No conoce el DOM.
   ============================================================ */
const LoginApi = {
  /**
   * POST /auth/login
   * @param   {string} usuario
   * @param   {string} password
   * @returns {Promise<Object>} { token, usuario: { codigo, nombreCompleto, rol } }
   */
  async autenticar(usuario, password) {
    return this._post(LOGIN_CONFIG.endpoints.login, { usuario, password });
  },

  /**
   * POST /auth/recuperar-password
   * @param   {string} usuario
   * @returns {Promise<Object>} { exito, mensaje }
   */
  async recuperarPassword(usuario) {
    return this._post(LOGIN_CONFIG.endpoints.recuperarPassword, { usuario });
  },

  /**
   * POST /auth/consultar-usuario
   * @param   {string} documento
   * @returns {Promise<Object>} { usuario }
   */
  async consultarUsuario(documento) {
    return this._post(LOGIN_CONFIG.endpoints.consultarUsuario, { documento });
  },

  /** Transporte común: JSON de ida y vuelta + control de estado HTTP. */
  async _post(ruta, cuerpo) {
    const respuesta = await fetch(`${LOGIN_CONFIG.baseURL}${ruta}`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json'
        // 'Authorization': `Bearer ${token}`   // ← si el endpoint lo exige
      },
      body: JSON.stringify(cuerpo)
    });
    if (!respuesta.ok) {
      const error = new Error(`HTTP ${respuesta.status} en POST ${ruta}`);
      error.status = respuesta.status;   // la UI distingue 401 / 404 / 429
      throw error;
    }
    return respuesta.json();
  }
};

/* ============================================================
   2 · CAPA DE UI (LoginUI)
   Responsabilidad Única: pintar mensajes en el DOM por id.
   No conoce la red. Usa textContent (nunca innerHTML) → anti-XSS.
   Contrato visual definido en assets/css/login.css:
   #response arranca con .is-hidden y alterna .error / .success / .info.
   ============================================================ */
const LoginUI = {
  el: {},

  /** Resuelve los IDs del HTML una sola vez. */
  cachearElementos() {
    this.el = {
      form:        document.getElementById('login-form'),
      usuario:     document.getElementById('username'),
      password:    document.getElementById('password'),
      response:    document.getElementById('response'),     // login y recuperación
      recuperaPWD: document.getElementById('recuperaPWD'),
      documento:   document.getElementById('documento'),
      consultar:   document.getElementById('consultar'),
      respuesta:   document.getElementById('respuesta')     // modal de consulta
    };
  },

  /**
   * Escribe en #response y la hace visible (quita .is-hidden).
   * @param {'error'|'success'|'info'} tipo
   * @param {string} texto
   */
  mostrarMensaje(tipo, texto) {
    const caja = this.el.response;
    if (!caja) return;
    caja.classList.remove('is-hidden', 'error', 'success', 'info');
    caja.classList.add(tipo);
    caja.textContent = texto;
  },

  /** Oculta #response (arranque y reinicio de cada intento). */
  limpiarMensaje() {
    const caja = this.el.response;
    if (!caja) return;
    caja.classList.add('is-hidden');
    caja.classList.remove('error', 'success', 'info');
    caja.textContent = '';
  },

  /** Mensaje dentro del modal "Consulte su usuario" (#respuesta). */
  mostrarConsulta(texto) {
    if (this.el.respuesta) this.el.respuesta.textContent = texto;
  }
};

/* ============================================================
   3 · CASOS DE USO (LoginApp)
   ============================================================ */
const LoginApp = {
  init() {
    LoginUI.cachearElementos();

    if (LoginUI.el.usuario) {
      // Sin eventos inline en el HTML: el usuario no debe escribir espacios.
      LoginUI.el.usuario.addEventListener('input', function () {
        this.value = this.value.replace(/[\s\t]/g, '');
      });
      LoginUI.el.usuario.focus();
    }

    // El submit SIEMPRE lo intercepta este script: la página nunca se recarga.
    if (LoginUI.el.form) {
      LoginUI.el.form.addEventListener('submit', evento => this.autenticar(evento));
    }
    if (LoginUI.el.recuperaPWD) {
      LoginUI.el.recuperaPWD.addEventListener('click', evento => {
        evento.preventDefault();
        return this.recuperarPassword();
      });
    }
    if (LoginUI.el.consultar) {
      LoginUI.el.consultar.addEventListener('click', evento => this.consultarUsuario(evento));
    }
  },

  /** Submit del formulario de acceso (#login-form). */
  async autenticar(evento) {
    evento.preventDefault();                 // ← sustituía al onsubmit inline

    const usuario  = (LoginUI.el.usuario && LoginUI.el.usuario.value || '').trim();
    const password = LoginUI.el.password ? LoginUI.el.password.value : '';

    if (!usuario || !password) {
      LoginUI.mostrarMensaje('error', 'Ingrese usuario y contraseña.');
      return;
    }

    LoginUI.limpiarMensaje();
    try {
      const sesion = await LoginApi.autenticar(usuario, password);
      // El backend devuelve { token, usuario }. Guardar aquí la sesión
      // (localStorage / cookie) según defina el equipo de backend.
      const nombre = (sesion && sesion.usuario && sesion.usuario.nombreCompleto) || usuario;
      LoginUI.mostrarMensaje('success', `Autenticación correcta. Bienvenido(a) ${nombre}.`);
      setTimeout(() => { window.location.href = LOGIN_CONFIG.rutaTrasLogin; }, 300);
    } catch (error) {
      console.error('[SIPA] Error de autenticación:', error);
      if (error.status === 401) {
        LoginUI.mostrarMensaje('error', 'Usuario o contraseña incorrectos.');
      } else if (error.status === 429) {
        LoginUI.mostrarMensaje('error', 'Demasiados intentos. Intente de nuevo en unos minutos.');
      } else {
        LoginUI.mostrarMensaje('error', 'El servicio de autenticación no está disponible. Intente más tarde.');
      }
    }
  },

  /** Enlace "Recuperar contraseña" (#recuperaPWD). */
  async recuperarPassword() {
    const usuario = (LoginUI.el.usuario && LoginUI.el.usuario.value || '').trim();
    if (!usuario) {
      LoginUI.mostrarMensaje('error', 'Indique su usuario para recuperar la contraseña.');
      return;
    }
    if (!window.confirm(`¿Recuperar la contraseña del usuario "${usuario}"?`)) return;

    LoginUI.limpiarMensaje();
    try {
      const resultado = await LoginApi.recuperarPassword(usuario);
      // El contrato exige responder siempre 200 aunque el usuario no exista
      // (evita enumerar usuarios): el texto del correo lo decide el backend.
      LoginUI.mostrarMensaje('success',
        (resultado && resultado.mensaje) ||
        'Si el usuario existe, la contraseña llegará a su correo. Revise también el spam.');
    } catch (error) {
      console.error('[SIPA] Error recuperando contraseña:', error);
      LoginUI.mostrarMensaje('error', 'El servicio de recuperación no está disponible en este momento.');
    }
  },

  /** Botón "Consultar" del modal #modalConsulta. */
  async consultarUsuario(evento) {
    if (evento) evento.preventDefault();

    const documento = (LoginUI.el.documento && LoginUI.el.documento.value || '').trim();
    if (!documento) {
      LoginUI.mostrarConsulta('Ingrese su número de documento.');
      return;
    }

    LoginUI.mostrarConsulta('Consultando…');
    try {
      const datos = await LoginApi.consultarUsuario(documento);
      LoginUI.mostrarConsulta((datos && datos.usuario) ? `Usuario: ${datos.usuario}` : 'Documento no registrado.');
    } catch (error) {
      console.error('[SIPA] Error consultando usuario:', error);
      LoginUI.mostrarConsulta(error.status === 404 ? 'Documento no registrado.'
                                                   : 'El servicio de consulta no está disponible.');
    }
  }
};

/* ============================================================
   INICIALIZACIÓN
   ============================================================ */
document.addEventListener('DOMContentLoaded', () => LoginApp.init());
