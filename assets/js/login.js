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
  baseURL: '/api',
  endpoints: {
    login: '/auth/login'
    // Recuperación de contraseña y consulta de usuario: no implementadas.
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
    return this._post(LOGIN_CONFIG.endpoints.login, { usernameOrEmail: usuario, password });
  },

  /**
   * Recuperación de contraseña: fuera de alcance. No llama a la red.
   */
  async recuperarPassword() {
    return { exito: false, mensaje: 'La recuperación de contraseña no está disponible.' };
  },

  /**
   * Consulta de usuario: fuera de alcance. No llama a la red.
   */
  async consultarUsuario() {
    return { usuario: null, mensaje: 'La consulta de usuario no está disponible.' };
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
      error.body = await respuesta.text();
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
      const token = sesion && (sesion.token || sesion.appToken || sesion.accessToken);
      if (!token) {
        throw new Error('La respuesta de autenticación no contiene un token.');
      }
      sessionStorage.setItem('sipa.auth.token', token);
      if (sesion.estudianteId != null) {
        sessionStorage.setItem('sipa.auth.estudianteId', String(sesion.estudianteId));
      } else {
        sessionStorage.removeItem('sipa.auth.estudianteId');
      }
      if (sesion.usuario) {
        sessionStorage.setItem('sipa.auth.user', JSON.stringify(sesion.usuario));
      } else if (sesion.user) {
        sessionStorage.setItem('sipa.auth.user', JSON.stringify(sesion.user));
      }
      const usuarioRespuesta = sesion.usuario || sesion.user;
      const nombre = (usuarioRespuesta && (usuarioRespuesta.nombreCompleto || usuarioRespuesta.fullName || usuarioRespuesta.email)) || usuario;
      LoginUI.mostrarMensaje('success', `Autenticación correcta. Bienvenido(a) ${nombre}.`);
      setTimeout(() => { window.location.href = LOGIN_CONFIG.rutaTrasLogin; }, 300);
    } catch (error) {
      console.error('[SIPA] Error de autenticación:', error);
      const credencialesInvalidas = error.status === 401 ||
        (error.status === 500 && /invalid credentials|credenciales inválidas/i.test(error.body || ''));
      if (credencialesInvalidas) {
        LoginUI.mostrarMensaje('error', 'Usuario o contraseña incorrectos.');
      } else if (error.status === 429) {
        LoginUI.mostrarMensaje('error', 'Demasiados intentos. Intente de nuevo en unos minutos.');
      } else {
        LoginUI.mostrarMensaje('error', 'El servicio de autenticación no está disponible. Intente más tarde.');
      }
    }
  },

  /** Enlace "Recuperar contraseña" (#recuperaPWD): deshabilitado a propósito. */
  recuperarPassword() {
    LoginUI.mostrarMensaje('error', 'La recuperación de contraseña no está disponible.');
  },

  /** Botón "Consultar" del modal #modalConsulta: deshabilitado a propósito. */
  consultarUsuario(evento) {
    if (evento) evento.preventDefault();
    LoginUI.mostrarConsulta('La consulta de usuario no está disponible.');
  }
};

/* ============================================================
   INICIALIZACIÓN
   ============================================================ */
document.addEventListener('DOMContentLoaded', () => LoginApp.init());
