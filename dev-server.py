import json
import os
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from urllib.request import Request, urlopen
from urllib.error import HTTPError, URLError

AUTH_ORIGIN = os.environ.get("AUTH_ORIGIN", "http://localhost:5132").rstrip("/")
ENROLLMENT_ORIGIN = os.environ.get("ENROLLMENT_ORIGIN", "http://localhost:8080").rstrip("/")
PORT = int(os.environ.get("PORT", "5500"))

AUTH_SEGMENTS = ("auth", "users", "roles", "demo")


def resolve_origin(path):
    """Route by first path segment after /api: identity vs ms-inscripcion."""
    segment = path[len("/api/"):].split("/", 1)[0].split("?", 1)[0]
    return AUTH_ORIGIN if segment in AUTH_SEGMENTS else ENROLLMENT_ORIGIN


class SipaHandler(SimpleHTTPRequestHandler):
    def _send_payload(self, status, content_type, payload):
        self.send_response(status)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(payload)))
        self.end_headers()
        self.wfile.write(payload)

    def _proxy_api(self):
        origin = resolve_origin(self.path)
        length = int(self.headers.get("Content-Length", "0"))
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

    def do_GET(self):
        if self._is_api():
            self._proxy_api()
        else:
            super().do_GET()

    def do_POST(self):
        self._proxy_api() if self._is_api() else self.send_error(405)

    def do_PUT(self):
        self._proxy_api() if self._is_api() else self.send_error(405)

    def do_DELETE(self):
        self._proxy_api() if self._is_api() else self.send_error(405)

    def do_OPTIONS(self):
        if self._is_api():
            self._proxy_api()
        else:
            super().do_OPTIONS() if hasattr(super(), "do_OPTIONS") else self.send_error(405)


if __name__ == "__main__":
    print(f"SIPA dev server en http://localhost:{PORT}")
    print(f"  /api/{{{','.join(AUTH_SEGMENTS)}}}/* -> {AUTH_ORIGIN}")
    print(f"  /api/* (resto)          -> {ENROLLMENT_ORIGIN}")
    ThreadingHTTPServer(("localhost", PORT), SipaHandler).serve_forever()
