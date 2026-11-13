import json
import os
import urllib.parse
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from urllib.request import Request, urlopen
from urllib.error import HTTPError, URLError

AUTH_ORIGIN = os.environ.get("AUTH_ORIGIN", "http://localhost:5132").rstrip("/")
ENROLLMENT_ORIGIN = os.environ.get("ENROLLMENT_ORIGIN", "http://localhost:8080").rstrip("/")
PORT = int(os.environ.get("PORT", "5500"))

AUTH_SEGMENTS = ("auth", "users", "roles", "demo")

# Static allowlist: only the front is public. Everything else in the repo
# (backend sources, .git/, appsettings.json, Makefile...) is answered with 404.
PUBLIC_FILES = {"/index.html", "/sugerencia-matricula.html"}
PUBLIC_DIRS = ("/assets/",)
MAX_BODY_BYTES = 1_048_576


def resolve_origin(path):
    """Route by first path segment after /api: identity vs ms-inscripcion."""
    segment = path[len("/api/"):].split("/", 1)[0].split("?", 1)[0]
    return AUTH_ORIGIN if segment in AUTH_SEGMENTS else ENROLLMENT_ORIGIN


class SipaHandler(SimpleHTTPRequestHandler):
    server_version = "SIPA-DevServer"

    # ---------- static files (allowlist only) ----------
    def _public_static_path(self):
        path = urllib.parse.urlsplit(self.path).path

        if path in ("", "/"):
            return "/index.html"

        if path in PUBLIC_FILES:
            return path

        if path.startswith(PUBLIC_DIRS):
            segments = [segment for segment in path.split("/") if segment]
            if any(segment.startswith(".") for segment in segments):
                return None
            return path

        return None

    def list_directory(self, path):
        # Never list directories (even /assets/).
        self.send_error(404)
        return None

    def _serve_static(self, head=False):
        target = self._public_static_path()
        if target is None:
            self.send_error(404)
            return

        self.path = target
        if head:
            super().do_HEAD()
        else:
            super().do_GET()

    # ---------- proxy ----------
    def _send_payload(self, status, content_type, payload):
        self.send_response(status)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(payload)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(payload)

    def _proxy_api(self):
        origin = resolve_origin(self.path)
        length = int(self.headers.get("Content-Length", "0"))

        if length > MAX_BODY_BYTES:
            problem = {
                "type": "about:blank",
                "title": "Solicitud demasiado grande",
                "status": 413,
                "code": "CUERPO_DEMASIADO_GRANDE",
            }
            self._send_payload(
                413,
                "application/problem+json",
                json.dumps(problem, ensure_ascii=False).encode("utf-8"),
            )
            return

        body = self.rfile.read(length) if length else None

        headers = {"Accept": self.headers.get("Accept", "application/json")}
        if body is not None:
            headers["Content-Type"] = self.headers.get("Content-Type", "application/json")
        if self.headers.get("Authorization"):
            headers["Authorization"] = self.headers["Authorization"]

        request = Request(f"{origin}{self.path}", data=body, method=self.command, headers=headers)

        try:
            with urlopen(request, timeout=15) as response:
                self._send_payload(
                    response.status,
                    response.headers.get("Content-Type", "application/json"),
                    response.read(),
                )
        except HTTPError as error:
            self._send_payload(
                error.code,
                error.headers.get("Content-Type", "application/json"),
                error.read(),
            )
        except (URLError, TimeoutError, OSError):
            problem = {
                "type": "about:blank",
                "title": "Servicio no disponible",
                "status": 502,
                "detail": f"No se pudo conectar con el servicio en {origin}",
                "code": "PROXY_SIN_CONEXION",
            }
            self._send_payload(
                502,
                "application/problem+json",
                json.dumps(problem, ensure_ascii=False).encode("utf-8"),
            )

    def _is_api(self):
        return self.path.startswith("/api/")

    # ---------- verbs ----------
    def end_headers(self):
        self.send_header("X-Content-Type-Options", "nosniff")
        super().end_headers()

    def do_GET(self):
        if self._is_api():
            self._proxy_api()
        else:
            self._serve_static()

    def do_HEAD(self):
        if self._is_api():
            self.send_error(405)
        else:
            self._serve_static(head=True)

    def do_POST(self):
        self._proxy_api() if self._is_api() else self.send_error(405)

    def do_PUT(self):
        self._proxy_api() if self._is_api() else self.send_error(405)

    def do_DELETE(self):
        self._proxy_api() if self._is_api() else self.send_error(405)

    def do_OPTIONS(self):
        self._proxy_api() if self._is_api() else self.send_error(405)


if __name__ == "__main__":
    print(f"SIPA dev server en http://localhost:{PORT}")
    print(f"  estáticos: {sorted(PUBLIC_FILES)} + {PUBLIC_DIRS}")
    print(f"  /api/{{{','.join(AUTH_SEGMENTS)}}}/* -> {AUTH_ORIGIN}")
    print(f"  /api/* (resto)          -> {ENROLLMENT_ORIGIN}")
    ThreadingHTTPServer(("localhost", PORT), SipaHandler).serve_forever()
