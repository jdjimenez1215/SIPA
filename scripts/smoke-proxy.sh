#!/usr/bin/env bash
# Smoke test a través del proxy del front (dev-server.py). Solo lectura / rechazo:
# NO hay caso 201 porque mutaría la BD. Uso: BASE=http://localhost:5500 bash scripts/smoke-proxy.sh
export LC_ALL=C.UTF-8
BASE="${BASE:-http://localhost:5500}"
BASE="${BASE%/}"
PASS=0; FAIL=0

# check <nombre> <status esperado> <patrón grep esperado|""> <args curl...>
check() {
  local name="$1" want="$2" pat="$3"; shift 3
  local out status body
  out=$(curl -s -m 15 -w $'\n%{http_code}' "$@" 2>/dev/null)
  status="${out##*$'\n'}"
  body="${out%$'\n'*}"
  if [ "$status" = "$want" ] && { [ -z "$pat" ] || printf '%s' "$body" | grep -Eq "$pat"; }; then
    echo "PASS  $name (HTTP $status)"; PASS=$((PASS+1))
  else
    echo "FAIL  $name (esperado $want, obtenido $status; patrón: ${pat:-—})"
    printf '      %s\n' "$(printf '%s' "$body" | head -c 200)"
    FAIL=$((FAIL+1))
  fi
}

echo "Smoke vía $BASE"
check "static index.html"                  200 "<html|<!DOCTYPE" "$BASE/index.html"
check "POST /api/auth/login (identity)"    200 '"accessToken"' -X POST "$BASE/api/auth/login" \
  -H 'Content-Type: application/json' -d '{"usernameOrEmail":"admin","password":"Admin123*"}'
check "GET sugerencia Laura (ms)"          200 '"totalCreditos"[[:space:]]*:.*"id"|"id".*"totalCreditos"' \
  "$BASE/api/estudiantes/1/sugerencia?periodo=2026-2"
check "POST inscripciones -> 422 (sin escritura)" 422 'INSCRIPCION_RECHAZADA' -X POST "$BASE/api/inscripciones" \
  -H 'Content-Type: application/json' \
  -d '{"estudianteId":1,"periodo":"2026-2","codigosMaterias":["603601","603702"]}'
check "GET estudiante inexistente -> 404 problem+json" 404 '"code"' \
  -H 'Accept: application/json' "$BASE/api/estudiantes/999/sugerencia?periodo=2026-2"

echo "PASS: $PASS  FAIL: $FAIL  TOTAL: $((PASS+FAIL))"
[ "$FAIL" -eq 0 ]
