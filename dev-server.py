from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from urllib.request import Request, urlopen
from urllib.error import HTTPError, URLError

API_ORIGIN = "http://localhost:5132"


class SipaHandler(SimpleHTTPRequestHandler):
    def _proxy_api(self):
        target = f"{API_ORIGIN}{self.path}"
        length = int(self.headers.get("Content-Length", "0"))
        body = self.rfile.read(length) if length else None
        request = Request(
            target,
            data=body,
            method=self.command,
            headers={
                "Accept": self.headers.get("Accept", "application/json"),
                "Content-Type": self.headers.get("Content-Type", "application/json"),
                "Authorization": self.headers.get("Authorization", ""),
            },
        )

        try:
            with urlopen(request, timeout=15) as response:
                payload = response.read()
                self.send_response(response.status)
                self.send_header("Content-Type", response.headers.get("Content-Type", "application/json"))
                self.send_header("Content-Length", str(len(payload)))
                self.end_headers()
                self.wfile.write(payload)
        except HTTPError as error:
            payload = error.read()
            self.send_response(error.code)
            self.send_header("Content-Type", error.headers.get("Content-Type", "application/json"))
            self.send_header("Content-Length", str(len(payload)))
            self.end_headers()
            self.wfile.write(payload)
        except URLError:
            self.send_error(502, "No se pudo conectar con la API en localhost:5132")

    def do_GET(self):
        if self.path.startswith("/api/"):
            self._proxy_api()
        else:
            super().do_GET()

    def do_POST(self):
        if self.path.startswith("/api/"):
            self._proxy_api()
        else:
            self.send_error(405)


if __name__ == "__main__":
    ThreadingHTTPServer(("localhost", 5500), SipaHandler).serve_forever()
