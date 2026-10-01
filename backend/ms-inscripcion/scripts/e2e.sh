#!/usr/bin/env bash
# E2E suite over every endpoint (Unillanos seed, period 2026-2). Requires a FRESH seed (docker compose down -v && up). Order matters.
# Seed ids: students 1 Laura (sem 6), 2 Mateo (7), 3 Camila (7, owes 603305), 4 Sofia (8), 5 Andres (9, Activa en 603903 con cupo 1);
# materias 1-53 in malla order (603601 = id 27, 603801 = 39, 603902 = 46, 603903 = 47), 54 = ADM101 (carrera 2).
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

# ck NAME CMD... : counts PASS when CMD succeeds (used to assert on the last body, $LAST)
ck() { local name="$1"; shift; if "$@"; then PASS=$((PASS+1)); printf '  PASS        %s\n' "$name"; else FAIL=$((FAIL+1)); FAILS+=("$name"); printf '  FAIL        %s\n' "$name"; fi; }
occurrences() { printf '%s' "$LAST" | grep -o -F -- "$1" | wc -l | tr -d ' '; }   # occurrences PATTERN
count_is() { [ "$(occurrences "$1")" = "$2" ]; }                                   # count_is PATTERN N
# row CODIGO ESTADO PREREQ MOTIVO : the suggestion row of CODIGO matches (MOTIVO is the raw JSON: null or "CODE")
row() {
  local o; o=$(printf '%s' "$LAST" | grep -oP '\{[^{}]*"codigo":"'"$1"'"[^{}]*\}') || return 1
  printf '%s' "$o" | grep -q -F "\"estado\":\"$2\"" && printf '%s' "$o" | grep -q -F "\"prerrequisitoCumplido\":$3" \
    && printf '%s' "$o" | grep -q -F "\"motivo\":$4" && printf '%s' "$o" | grep -q -F '"id":'
}
only_sugeridas() { # only_sugeridas CODE... : the Sugerida rows are exactly these codes
  local got want; got=$(printf '%s' "$LAST" | grep -oP '"codigo":"\K[^"]+(?=[^{}]*"estado":"Sugerida")' | sort | tr '\n' ' ')
  want=$(printf '%s\n' "$@" | sort | tr '\n' ' '); [ "$got" = "$want" ]
}

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

# Read-only sections go BEFORE any materia is created: a new low-semester materia would show up as "adeudada" for everyone.
echo "== Estudiantes"
t "inscripciones Andres 2026-2"     200 GET    "/estudiantes/5/inscripciones?periodo=2026-2" "" '"codigo":"603903"'
t "inscripciones periodo default"   200 GET    /estudiantes/5/inscripciones "" '"codigo":"603903"'
t "inscripciones Laura (vacias)"    200 GET    "/estudiantes/1/inscripciones?periodo=2026-2" "" "[]"
t "inscripciones est. inexistente"  404 GET    /estudiantes/999/inscripciones "" ESTUDIANTE_NO_ENCONTRADO
t "materias disponibles Laura"      200 GET    /estudiantes/1/materias-disponibles "" '"codigo":"603601"' '"codigo":"603702"'
ck "disponibles = 8 Sugerida"       count_is '"codigo":' 8
t "disponibles excluye aprobadas"   200 GET    /estudiantes/1/materias-disponibles "" '"codigo":"603703"' '"codigo":"603101"'
t "disponibles est. inexistente"    404 GET    /estudiantes/999/materias-disponibles "" ESTUDIANTE_NO_ENCONTRADO

echo "== Sugerencia"
t "sugerencia Laura"                200 GET    /estudiantes/1/sugerencia "" '"totalCreditos":23'
ck "Laura: 12 filas"                count_is '"codigo":' 12
ck "Laura: toda fila trae id"       count_is '"id":' 12
ck "Laura: encabezado"              grep -q -F '"nombreEstudiante":"Laura Gómez Ríos","programa":"Ingeniería de Sistemas","semestreActual":6,"periodo":"2026-2"' <<<"$LAST"
ck "Laura: notificaciones vacias"   grep -q -F '"notificaciones":[]' <<<"$LAST"
ck "Laura: 603601 Sugerida"         row 603601 Sugerida true null
ck "Laura: 603606 Sugerida"         row 603606 Sugerida true null
ck "Laura: 603701 Sugerida (N+1)"   row 603701 Sugerida true null
ck "Laura: 603703 Sugerida (N+1)"   row 603703 Sugerida true null
ck "Laura: 603702 Prerrequisito"    row 603702 Prerrequisito false '"PREREQUISITO_NO_CUMPLIDO"'
ck "Laura: 603704 Prerrequisito"    row 603704 Prerrequisito false '"PREREQUISITO_NO_CUMPLIDO"'
ck "Laura: 603705 Prerrequisito"    row 603705 Prerrequisito false '"PREREQUISITO_NO_CUMPLIDO"'
ck "Laura: 603706 Prerrequisito"    row 603706 Prerrequisito false '"PREREQUISITO_NO_CUMPLIDO"'
ck "Laura: sin aprobadas ni sem 8"  bash -c '! printf "%s" "$0" | grep -q -E "\"codigo\":\"(603[1-5]|6038)"' "$LAST"
t "sugerencia con periodo explicito" 200 GET   "/estudiantes/1/sugerencia?periodo=2026-2" "" '"periodo":"2026-2"'
t "sugerencia periodo invalido"     400 GET    "/estudiantes/1/sugerencia?periodo=2026-9" "" VALIDACION_FALLIDA
t "sugerencia est. inexistente"     404 GET    /estudiantes/999/sugerencia "" ESTUDIANTE_NO_ENCONTRADO
t "sugerencia Mateo (tope 3)"       200 GET    /estudiantes/2/sugerencia
ck "Mateo: 603801 Sugerida"         row 603801 Sugerida true null
ck "Mateo: 603802 Sugerida"         row 603802 Sugerida true null
ck "Mateo: 603803 Sugerida"         row 603803 Sugerida true null
ck "Mateo: 603804 LIMITE"           row 603804 Prerrequisito true '"LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO"'
ck "Mateo: 603806 LIMITE"           row 603806 Prerrequisito true '"LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO"'
ck "Mateo: 603805 prerrequisito"    row 603805 Prerrequisito false '"PREREQUISITO_NO_CUMPLIDO"'
ck "Mateo: sem 7 completo Sugerida" row 603706 Sugerida true null
t "sugerencia Camila (adeudada)"    200 GET    /estudiantes/3/sugerencia
ck "Camila: 603305 adeudada Sugerida" row 603305 Sugerida true null
ck "Camila: 603801 Sugerida"        row 603801 Sugerida true null
ck "Camila: 603802 Sugerida"        row 603802 Sugerida true null
ck "Camila: 603803 LIMITE"          row 603803 Prerrequisito true '"LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO"'
ck "Camila: 603804 LIMITE"          row 603804 Prerrequisito true '"LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO"'
ck "Camila: 603806 LIMITE"          row 603806 Prerrequisito true '"LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO"'
ck "Camila: 603805 prerrequisito"   row 603805 Prerrequisito false '"PREREQUISITO_NO_CUMPLIDO"'
t "sugerencia Sofia (A3)"           200 GET    /estudiantes/4/sugerencia
ck "Sofia: 603901 prerrequisito"    row 603901 Prerrequisito false '"PREREQUISITO_NO_CUMPLIDO"'
ck "Sofia: 603902 CRUCE_HORARIO"    row 603902 Prerrequisito true '"CRUCE_HORARIO"'
ck "Sofia: 603903 CUPO_AGOTADO"     row 603903 Prerrequisito true '"CUPO_AGOTADO"'
ck "Sofia: 603904 Sugerida"         row 603904 Sugerida true null
ck "Sofia: 603905 Sugerida"         row 603905 Sugerida true null
ck "Sofia: 603801 Sugerida (N)"     row 603801 Sugerida true null
t "sugerencia Andres omite inscrita" 200 GET   /estudiantes/5/sugerencia "" '"semestreActual":9' '"codigo":"603903"'

echo "== Materias"
t "catalogo completo"               200 GET    /materias "" '"codigo":"603101"'
t "catalogo filtrado carrera+sem"   200 GET    "/materias?carreraId=1&semestre=1" "" '"codigo":"603101"' '"codigo":"ADM101"'
t "catalogo de la otra carrera"     200 GET    "/materias?carreraId=2" "" '"codigo":"ADM101"' '"codigo":"603101"'
t "catalogo query invalida"         400 GET    "/materias?semestre=abc" "" VALIDACION_FALLIDA
t "materia por id (con horarios)"   200 GET    /materias/27 "" '"horarios":['
t "materia inexistente"             404 GET    /materias/999 "" MATERIA_NO_ENCONTRADA
t "crear materia (cupo 1)"          201 POST   /materias '{"codigo":"TST101","nombre":"Materia de prueba","creditos":2,"carreraId":1,"semestre":1,"cuposMaximos":1}' '"codigo":"TST101"'
M=$(id_of)
t "crear materia codigo duplicado"  409 POST   /materias '{"codigo":"TST101","nombre":"Dup","creditos":2,"carreraId":1,"semestre":1,"cuposMaximos":5}' CODIGO_DUPLICADO
t "crear materia carrera inexist."  404 POST   /materias '{"codigo":"TST999","nombre":"X","creditos":2,"carreraId":999,"semestre":1,"cuposMaximos":5}' CARRERA_NO_ENCONTRADA
t "actualizar materia"              200 PUT    "/materias/$M" '{"codigo":"TST101","nombre":"Materia de prueba E2E","creditos":2,"carreraId":1,"semestre":1,"cuposMaximos":1}' '"nombre":"Materia de prueba E2E"'

echo "== Horarios"
t "horarios de materia nueva"       200 GET    "/materias/$M/horarios" "" "[]"
t "horarios de materia sembrada"    200 GET    /materias/39/horarios "" '"diaSemana":"Lunes"'
t "crear horario"                   201 POST   "/materias/$M/horarios" '{"diaSemana":"Sabado","horaInicio":"08:00","horaFin":"10:00"}' '"horaInicio":"08:00"'
H=$(id_of)
t "horario rango invertido"         422 POST   "/materias/$M/horarios" '{"diaSemana":"Sabado","horaInicio":"10:00","horaFin":"08:00"}' HORARIO_INVALIDO
t "horario solapado propio"         422 POST   "/materias/$M/horarios" '{"diaSemana":"Sabado","horaInicio":"09:00","horaFin":"11:00"}' HORARIO_INVALIDO
t "actualizar horario"              200 PUT    "/materias/$M/horarios/$H" '{"diaSemana":"Sabado","horaInicio":"07:00","horaFin":"09:00"}' '"horaInicio":"07:00"'
t "horario en materia inexistente"  404 POST   /materias/999/horarios '{"diaSemana":"Lunes","horaInicio":"08:00","horaFin":"10:00"}' MATERIA_NO_ENCONTRADA
t "borrar horario inexistente"      404 DELETE "/materias/$M/horarios/999" "" HORARIO_NO_ENCONTRADO

echo "== Prerrequisitos"
t "prerrequisitos de 603401"        200 GET    /materias/16/prerrequisitos "" '"materiaRequisitoId":11'
t "agregar prerrequisito"           201 POST   "/materias/$M/prerrequisitos" '{"materiaRequisitoId":1}' '"materiaRequisitoId":1'
t "prerrequisito duplicado"         409 POST   "/materias/$M/prerrequisitos" '{"materiaRequisitoId":1}' PRERREQUISITO_DUPLICADO
t "prerrequisito a si misma"        422 POST   "/materias/$M/prerrequisitos" "{\"materiaRequisitoId\":$M}" PRERREQUISITO_CICLICO
t "prerrequisito ciclico"           422 POST   /materias/1/prerrequisitos "{\"materiaRequisitoId\":$M}" PRERREQUISITO_CICLICO
t "quitar prerrequisito"            204 DELETE "/materias/$M/prerrequisitos/1"
t "quitar prerrequisito inexist."   404 DELETE "/materias/$M/prerrequisitos/1" "" PRERREQUISITO_NO_ENCONTRADO

echo "== Inscripciones: reglas y validaciones (regla N + max 3 compartido)"
t "Laura 603601+603702 (prereq estricto)" 422 POST /inscripciones '{"estudianteId":1,"periodo":"2026-2","codigosMaterias":["603601","603702"]}' PREREQUISITO_NO_CUMPLIDO '"code":"MATERIA_YA_APROBADA"'
t "Laura 603801 (sem 8 > N+1)"      422 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","codigosMaterias":["603801"]}' SEMESTRE_EXCEDIDO
ck "SEMESTRE_EXCEDIDO: details"     grep -q -F '"materiaSemestre":8,"maxAllowedSemestre":7' <<<"$LAST"
t "Laura ADM101 (otra carrera)"     422 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","codigosMaterias":["ADM101"]}' CARRERA_NO_CORRESPONDE
t "Laura 603101 (aprobada)"         422 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","codigosMaterias":["603101"]}' MATERIA_YA_APROBADA
t "violaciones mezcladas (4)"       422 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","codigosMaterias":["603101","603702","603801","ADM101"]}' INSCRIPCION_RECHAZADA
ck "exactamente 4 violaciones"      count_is '"materiaCodigo"' 4
for c in MATERIA_YA_APROBADA PREREQUISITO_NO_CUMPLIDO SEMESTRE_EXCEDIDO CARRERA_NO_CORRESPONDE; do
  ck "violacion presente: $c"       grep -q -F "\"code\":\"$c\"" <<<"$LAST"
done
t "Mateo 4 extras por ids (tope 3)" 422 POST   /inscripciones '{"estudianteId":2,"periodo":"2026-2","materiaIds":[39,40,41,42]}' LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO
ck "LIMITE: details max 3, used 3"  grep -q -F '"maxExtraSubjects":3,"used":3' <<<"$LAST"
ck "solo la 4a extra se reporta"    count_is '"materiaCodigo"' 1
t "Mateo 3 extras + toda su N (no excede)" 422 POST /inscripciones '{"estudianteId":2,"periodo":"2026-2","codigosMaterias":["603801","603802","603803","603804","603701"]}' LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO '"materiaCodigo":"603701"'
ck "solo 603804 excede (N no cuenta)" count_is '"materiaCodigo"' 1
ck "la excedida es 603804"          grep -q -F '"materiaCodigo":"603804"' <<<"$LAST"
t "Sofia 603801+603902 (cruce)"     422 POST   /inscripciones '{"estudianteId":4,"periodo":"2026-2","codigosMaterias":["603801","603902"]}' CRUCE_HORARIO
t "Sofia 603903 (cupo agotado)"     422 POST   /inscripciones '{"estudianteId":4,"periodo":"2026-2","codigosMaterias":["603903"]}' CUPO_AGOTADO
t "Camila 305+801-803 (tope compartido)" 422 POST /inscripciones '{"estudianteId":3,"periodo":"2026-2","codigosMaterias":["603305","603801","603802","603803"]}' LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO
ck "Camila: excede la 4a (603803)"  grep -q -F '"materiaCodigo":"603803"' <<<"$LAST"
t "ambas listas"                    400 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","materiaIds":[27],"codigosMaterias":["603601"]}' VALIDACION_FALLIDA
t "ninguna lista"                   400 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2"}' VALIDACION_FALLIDA
t "codigo desconocido"              404 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","codigosMaterias":["999999"]}' MATERIA_NO_ENCONTRADA '"code":"INSCRIPCION_RECHAZADA"'
t "codigos duplicados en solicitud" 400 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","codigosMaterias":["603601","603601"]}' MATERIA_DUPLICADA_EN_SOLICITUD
t "ids duplicados en solicitud"     400 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","materiaIds":[27,27]}' MATERIA_DUPLICADA_EN_SOLICITUD
t "lista vacia"                     400 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","materiaIds":[]}' VALIDACION_FALLIDA
t "periodo invalido"                400 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-9","materiaIds":[27]}' VALIDACION_FALLIDA
t "estudiante inexistente"          404 POST   /inscripciones '{"estudianteId":999,"periodo":"2026-2","materiaIds":[27]}' ESTUDIANTE_NO_ENCONTRADO
t "materia inexistente (id)"        404 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","materiaIds":[999]}' MATERIA_NO_ENCONTRADA
t "atomicidad: 1 falla, ninguna"    422 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","codigosMaterias":["603601","603101"]}' MATERIA_YA_APROBADA
t "atomicidad: 603601 no quedo"     200 GET    "/estudiantes/1/inscripciones?periodo=2026-2" "" "[]" '"codigo":"603601"'

echo "== Inscripciones: concurrencia"
race "mismo estudiante, materias que se cruzan (603801||603902)" 4 39 4 46 CRUCE_HORARIO
t "cancelar ganador de la carrera"  204 DELETE "/inscripciones/$RACE_WINNER"
race "ultimo cupo TST101 (Mateo||Andres)" 2 "$M" 5 "$M" CUPO_AGOTADO
t "cancelar ganador del cupo"       204 DELETE "/inscripciones/$RACE_WINNER"
t "reinscribir tras cancelar (cupo liberado)" 201 POST /inscripciones "{\"estudianteId\":5,\"periodo\":\"2026-2\",\"materiaIds\":[$M]}" "\"materiaId\":$M"

echo "== Inscripciones: camino feliz y cancelacion"
t "Laura inscribe sus 8 Sugerida"   201 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","codigosMaterias":["603601","603602","603603","603604","603605","603606","603701","603703"]}' '"materiaId":27'
ck "se crean 8 inscripciones"       count_is '"estudianteId":' 8
LAURA1=$(id_of)
t "sugerencia omite las inscritas"  200 GET    /estudiantes/1/sugerencia "" '"codigo":"603702"' '"codigo":"603601"'
t "ya inscrita"                     422 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","codigosMaterias":["603601"]}' MATERIA_YA_INSCRITA
t "Camila 801-803 sin adeudada (A2)" 201 POST  /inscripciones '{"estudianteId":3,"periodo":"2026-2","codigosMaterias":["603801","603802","603803"]}' '"materiaId":39'
CAM1=$(id_of)
t "Camila 305 con 3 activas (cuentan)" 422 POST /inscripciones '{"estudianteId":3,"periodo":"2026-2","codigosMaterias":["603305"]}' LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO
ck "Camila: used 3 (activas cuentan)" grep -q -F '"used":3' <<<"$LAST"
t "cancelar una extra de Camila"    204 DELETE "/inscripciones/$CAM1"
t "Camila 305 tras liberar tope"    201 POST   /inscripciones '{"estudianteId":3,"periodo":"2026-2","codigosMaterias":["603305"]}' '"codigo":"603305"'
t "Sofia inscribe 603801"           201 POST   /inscripciones '{"estudianteId":4,"periodo":"2026-2","codigosMaterias":["603801"]}' '"materiaId":39'
t "cruce con inscrita (603902)"     422 POST   /inscripciones '{"estudianteId":4,"periodo":"2026-2","codigosMaterias":["603902"]}' '"conflictsWith":"603801"'
t "cancelar inscripcion de Laura"   204 DELETE "/inscripciones/$LAURA1"
t "cancelar dos veces"              409 DELETE "/inscripciones/$LAURA1" "" INSCRIPCION_YA_CANCELADA
t "cancelar inexistente"            404 DELETE /inscripciones/999999 "" INSCRIPCION_NO_ENCONTRADA
t "reinscribir tras cancelar"       201 POST   /inscripciones '{"estudianteId":1,"periodo":"2026-2","codigosMaterias":["603601"]}' '"materiaId":27'

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
