#!/usr/bin/env bash
# E2E suite over every endpoint. Requires a FRESH seed (docker compose down -v && up). Order matters.
export LC_ALL=C.UTF-8
PORT="${PORT:-8080}"; B="http://localhost:$PORT/api"; J='Content-Type: application/json'
TMP="$(mktemp -d)"; trap 'rm -rf "$TMP"' EXIT
PASS=0; FAIL=0; FAILS=()

# t NAME EXPECTED_STATUS METHOD PATH [BODY] [MUST_CONTAIN] [MUST_NOT_CONTAIN]
t() {
  local name="$1" exp="$2" m="$3" p="$4" body="$5" has="$6" hasnot="$7" st
  if [ -n "$body" ]; then st=$(curl -s -o "$TMP/last" -w '%{http_code}' -X "$m" "$B$p" -H "$J" -d "$body")
  else st=$(curl -s -o "$TMP/last" -w '%{http_code}' -X "$m" "$B$p"); fi
  LAST="$(cat "$TMP/last")"; local ok=1
  [ "$st" = "$exp" ] || ok=0
  [ -z "$has" ] || printf '%s' "$LAST" | grep -q -F -- "$has" || ok=0
  [ -z "$hasnot" ] || ! printf '%s' "$LAST" | grep -q -F -- "$hasnot" || ok=0
  if [ $ok = 1 ]; then PASS=$((PASS+1)); printf '  PASS  %-4s %s %-58s %s\n' "$st" "$m" "$p" "$name"
  else FAIL=$((FAIL+1)); FAILS+=("$name"); printf '  FAIL  %-4s %s %-58s %s (expected %s%s)\n         body: %.300s\n' "$st" "$m" "$p" "$name" "$exp" "${has:+, contains $has}" "$LAST"; fi
}
id_of() { printf '%s' "$LAST" | grep -oP '"id":\K\d+' | head -1; }
enroll_bg() { curl -s -o "$TMP/$3" -w '%{http_code}' -X POST "$B/inscripciones" -H "$J" -d "{\"estudianteId\":$1,\"periodo\":\"2026-2\",\"materiaIds\":[$2]}" > "$TMP/$3.st"; }
race() { # race NAME A_STUDENT A_IDS B_STUDENT B_IDS LOSER_CODE
  enroll_bg "$2" "$3" ra & enroll_bg "$4" "$5" rb & wait
  local sa sb; sa=$(cat "$TMP/ra.st"); sb=$(cat "$TMP/rb.st")
  local codes; codes="$(cat "$TMP/ra" "$TMP/rb")"
  if { [ "$sa$sb" = "201422" ] || [ "$sa$sb" = "422201" ]; } && printf '%s' "$codes" | grep -q -F "$6"; then
    PASS=$((PASS+1)); printf '  PASS  %s+%s  concurrent  %-52s %s\n' "$sa" "$sb" "" "$1"
  else FAIL=$((FAIL+1)); FAILS+=("$1"); printf '  FAIL  %s+%s  concurrent  %s (expected 201+422 %s)\n' "$sa" "$sb" "$1" "$6"; fi
  if [ "$sa" = 201 ]; then RACE_WINNER=$(grep -oP '"id":\K\d+' "$TMP/ra" | head -1); else RACE_WINNER=$(grep -oP '"id":\K\d+' "$TMP/rb" | head -1); fi
}

echo "== Carreras"
t "listar carreras"                 200 GET    /carreras "" '"codigo":"ING-SIS"'
t "carrera por id"                  200 GET    /carreras/1 "" '"codigo":"ING-SIS"'
t "carrera inexistente"             404 GET    /carreras/999 "" CARRERA_NO_ENCONTRADA
t "crear carrera"                   201 POST   /carreras '{"codigo":"MED","nombre":"Medicina","duracionSemestres":12}' '"codigo":"MED"'
CID=$(id_of)
t "crear carrera codigo duplicado"  409 POST   /carreras '{"codigo":"MED","nombre":"Otra","duracionSemestres":10}' CODIGO_DUPLICADO
t "crear carrera invalida"          400 POST   /carreras '{"codigo":"","nombre":"","duracionSemestres":0}' VALIDACION_FALLIDA
t "actualizar carrera"              200 PUT    "/carreras/$CID" '{"codigo":"MED","nombre":"Medicina Humana","duracionSemestres":12}' '"nombre":"Medicina Humana"'
t "actualizar carrera inexistente"  404 PUT    /carreras/999 '{"codigo":"XXX","nombre":"X","duracionSemestres":10}' CARRERA_NO_ENCONTRADA
t "borrar carrera en uso"           409 DELETE /carreras/1 "" ENTIDAD_EN_USO

echo "== Materias"
t "catalogo completo"               200 GET    /materias "" '"codigo":"MAT101"'
t "catalogo filtrado carrera+sem"   200 GET    "/materias?carreraId=1&semestre=1" "" '"codigo":"MAT101"' '"codigo":"ADM101"'
t "catalogo query invalida"         400 GET    "/materias?semestre=abc" "" VALIDACION_FALLIDA
t "materia por id (con horarios)"   200 GET    /materias/1 "" '"horarios":['
t "materia inexistente"             404 GET    /materias/999 "" MATERIA_NO_ENCONTRADA
t "crear materia (cupo 1)"          201 POST   /materias '{"codigo":"TST101","nombre":"Materia de prueba","creditos":2,"carreraId":1,"semestre":1,"cuposMaximos":1}' '"codigo":"TST101"'
M=$(id_of)
t "crear materia codigo duplicado"  409 POST   /materias '{"codigo":"TST101","nombre":"Dup","creditos":2,"carreraId":1,"semestre":1,"cuposMaximos":5}' CODIGO_DUPLICADO
t "crear materia carrera inexist."  404 POST   /materias '{"codigo":"TST999","nombre":"X","creditos":2,"carreraId":999,"semestre":1,"cuposMaximos":5}' CARRERA_NO_ENCONTRADA
t "actualizar materia"              200 PUT    "/materias/$M" '{"codigo":"TST101","nombre":"Materia de prueba E2E","creditos":2,"carreraId":1,"semestre":1,"cuposMaximos":1}' '"nombre":"Materia de prueba E2E"'

echo "== Horarios"
t "horarios de materia nueva"       200 GET    "/materias/$M/horarios" "" "[]"
t "crear horario"                   201 POST   "/materias/$M/horarios" '{"diaSemana":"Sabado","horaInicio":"08:00","horaFin":"10:00"}' '"horaInicio":"08:00"'
H=$(id_of)
t "horario rango invertido"         422 POST   "/materias/$M/horarios" '{"diaSemana":"Sabado","horaInicio":"10:00","horaFin":"08:00"}' HORARIO_INVALIDO
t "horario solapado propio"         422 POST   "/materias/$M/horarios" '{"diaSemana":"Sabado","horaInicio":"09:00","horaFin":"11:00"}' HORARIO_INVALIDO
t "actualizar horario"              200 PUT    "/materias/$M/horarios/$H" '{"diaSemana":"Sabado","horaInicio":"07:00","horaFin":"09:00"}' '"horaInicio":"07:00"'
t "horario en materia inexistente"  404 POST   /materias/999/horarios '{"diaSemana":"Lunes","horaInicio":"08:00","horaFin":"10:00"}' MATERIA_NO_ENCONTRADA
t "borrar horario inexistente"      404 DELETE "/materias/$M/horarios/999" "" HORARIO_NO_ENCONTRADO

echo "== Prerrequisitos"
t "prerrequisitos de EDD301"        200 GET    /materias/10/prerrequisitos "" '"materiaRequisitoId":7'
t "agregar prerrequisito"           201 POST   "/materias/$M/prerrequisitos" '{"materiaRequisitoId":1}' '"materiaRequisitoId":1'
t "prerrequisito duplicado"         409 POST   "/materias/$M/prerrequisitos" '{"materiaRequisitoId":1}' PRERREQUISITO_DUPLICADO
t "prerrequisito a si misma"        422 POST   "/materias/$M/prerrequisitos" "{\"materiaRequisitoId\":$M}" PRERREQUISITO_CICLICO
t "prerrequisito ciclico"           422 POST   /materias/1/prerrequisitos "{\"materiaRequisitoId\":$M}" PRERREQUISITO_CICLICO
t "quitar prerrequisito"            204 DELETE "/materias/$M/prerrequisitos/1"
t "quitar prerrequisito inexist."   404 DELETE "/materias/$M/prerrequisitos/1" "" PRERREQUISITO_NO_ENCONTRADO

echo "== Estudiantes"
t "inscripciones Ana 2026-2"        200 GET    "/estudiantes/1/inscripciones?periodo=2026-2" '"codigo":"ETI101"'
t "inscripciones periodo default"   200 GET    /estudiantes/1/inscripciones "" '"codigo":"ETI101"'
t "inscripciones est. inexistente"  404 GET    /estudiantes/999/inscripciones "" ESTUDIANTE_NO_ENCONTRADO
t "materias disponibles Ana"        200 GET    /estudiantes/1/materias-disponibles "" '"codigo":"MAT201"' '"id":1,"codigo":"MAT101"'
t "disponibles est. inexistente"    404 GET    /estudiantes/999/materias-disponibles "" ESTUDIANTE_NO_ENCONTRADO

echo "== Inscripciones: reglas y validaciones"
t "todas las violaciones (7)"       422 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","materiaIds":[1,2,6,4,9,10,11,12]}' INSCRIPCION_RECHAZADA
N=$(printf '%s' "$LAST" | grep -o '"materiaCodigo"' | wc -l | tr -d ' ')
for c in MATERIA_YA_APROBADA CRUCE_HORARIO MATERIA_YA_INSCRITA CUPO_AGOTADO PREREQUISITO_NO_CUMPLIDO SEMESTRE_EXCEDIDO CARRERA_NO_CORRESPONDE; do
  printf '%s' "$LAST" | grep -q "$c" && PASS=$((PASS+1)) && printf '  PASS        violacion presente: %s\n' "$c" || { FAIL=$((FAIL+1)); FAILS+=("falta $c"); printf '  FAIL        violacion ausente: %s\n' "$c"; }
done
[ "$N" = 7 ] && { PASS=$((PASS+1)); echo "  PASS        exactamente 7 violaciones"; } || { FAIL=$((FAIL+1)); FAILS+=("conteo violaciones=$N"); echo "  FAIL        $N violaciones (esperado 7)"; }
t "cruce con inscrita (HUM101)"     422 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","materiaIds":[5]}' '"conflictsWith":"ETI101"'
t "ids duplicados en solicitud"     400 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","materiaIds":[6,6]}' MATERIA_DUPLICADA_EN_SOLICITUD
t "lista vacia"                     400 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","materiaIds":[]}' VALIDACION_FALLIDA
t "periodo invalido"                400 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-9","materiaIds":[6]}' VALIDACION_FALLIDA
t "estudiante inexistente"          404 POST   /inscripciones '{"estudianteId":999,"periodo":"2026-2","materiaIds":[6]}' ESTUDIANTE_NO_ENCONTRADO
t "materia inexistente"             404 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","materiaIds":[999]}' MATERIA_NO_ENCONTRADA
t "atomicidad: 1 falla, ninguna"    422 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","materiaIds":[6,1]}' MATERIA_YA_APROBADA
t "atomicidad: MAT201 no quedo"     200 GET    "/estudiantes/1/inscripciones?periodo=2026-2" "" '"codigo":"ETI101"' '"codigo":"MAT201"'

echo "== Inscripciones: concurrencia"
race "mismo estudiante, materias que se cruzan (FIS101||MAT201)" 1 2 1 6 CRUCE_HORARIO
t "cancelar ganador de la carrera"  204 DELETE "/inscripciones/$RACE_WINNER"
race "ultimo cupo TST101 (Ana||Carlos)" 1 "$M" 2 "$M" CUPO_AGOTADO

echo "== Inscripciones: camino feliz y cancelacion"
t "inscripcion exitosa [6,7,8]"     201 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","materiaIds":[6,7,8]}' '"materiaId":8'
t "ya inscrita"                     422 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","materiaIds":[6]}' MATERIA_YA_INSCRITA
t "cancelar inscripcion Carlos"     204 DELETE /inscripciones/2
t "cancelar dos veces"              409 DELETE /inscripciones/2 "" INSCRIPCION_YA_CANCELADA
t "cancelar inexistente"            404 DELETE /inscripciones/999 "" INSCRIPCION_NO_ENCONTRADA
t "reinscribir tras cancelar"       201 POST   /inscripciones '{"estudianteId":2,"periodo":"2026-2","materiaIds":[9]}' '"materiaId":9'

echo "== Borrados"
t "borrar materia con inscripciones" 409 DELETE "/materias/$M" "" ENTIDAD_EN_USO
t "borrar materia inexistente"      404 DELETE /materias/999 "" MATERIA_NO_ENCONTRADA
t "crear materia descartable"       201 POST   /materias '{"codigo":"TMP101","nombre":"Temporal","creditos":1,"carreraId":1,"semestre":1,"cuposMaximos":5}'
X=$(id_of)
t "borrar materia libre"            204 DELETE "/materias/$X"
t "borrar carrera libre"            204 DELETE "/carreras/$CID"
t "carrera borrada ya no existe"    404 GET    "/carreras/$CID" "" CARRERA_NO_ENCONTRADA

echo "== Errores del framework (ProblemDetails)"
t "ruta inexistente"                404 GET    /no-existe "" RECURSO_NO_ENCONTRADO
t "metodo no permitido"             405 PATCH  /carreras "" METODO_NO_PERMITIDO
st=$(curl -s -o "$TMP/last" -w '%{http_code}' -X POST "$B/carreras" -H 'Content-Type: text/plain' -d 'x')
grep -q TIPO_CONTENIDO_NO_SOPORTADO "$TMP/last" && [ "$st" = 415 ] && { PASS=$((PASS+1)); echo "  PASS  415  POST /carreras (text/plain)"; } || { FAIL=$((FAIL+1)); FAILS+=("415"); echo "  FAIL  $st POST text/plain: $(cat "$TMP/last" | head -c 300)"; }
st=$(curl -s -o /dev/null -w '%{http_code}' "http://localhost:$PORT/swagger/v1/swagger.json")
[ "$st" = 200 ] && { PASS=$((PASS+1)); echo "  PASS  200  swagger.json"; } || { FAIL=$((FAIL+1)); FAILS+=("swagger"); echo "  FAIL  $st swagger.json"; }

echo; echo "TOTAL: $PASS passed, $FAIL failed"
for f in "${FAILS[@]}"; do echo "  - $f"; done
