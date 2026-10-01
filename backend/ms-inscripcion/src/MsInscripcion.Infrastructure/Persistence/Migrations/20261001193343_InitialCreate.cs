using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MsInscripcion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "carreras",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    duracion_semestres = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_carreras", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "estudiantes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    carrera_id = table.Column<int>(type: "integer", nullable: false),
                    semestre_actual = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_estudiantes", x => x.id);
                    table.ForeignKey(
                        name: "fk_estudiantes_carreras_carrera_id",
                        column: x => x.carrera_id,
                        principalTable: "carreras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "materias",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    creditos = table.Column<int>(type: "integer", nullable: false),
                    carrera_id = table.Column<int>(type: "integer", nullable: false),
                    semestre = table.Column<int>(type: "integer", nullable: false),
                    cupos_maximos = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_materias", x => x.id);
                    table.ForeignKey(
                        name: "fk_materias_carreras_carrera_id",
                        column: x => x.carrera_id,
                        principalTable: "carreras",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "historial_academico",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estudiante_id = table.Column<int>(type: "integer", nullable: false),
                    materia_id = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    nota = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: true),
                    periodo = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_historial_academico", x => x.id);
                    table.ForeignKey(
                        name: "fk_historial_academico_estudiantes_estudiante_id",
                        column: x => x.estudiante_id,
                        principalTable: "estudiantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_historial_academico_materias_materia_id",
                        column: x => x.materia_id,
                        principalTable: "materias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "horario_materia",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    materia_id = table.Column<int>(type: "integer", nullable: false),
                    dia_semana = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    hora_inicio = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_fin = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_horario_materia", x => x.id);
                    table.CheckConstraint("ck_horario_materia_rango", "hora_inicio < hora_fin");
                    table.ForeignKey(
                        name: "fk_horario_materia_materias_materia_id",
                        column: x => x.materia_id,
                        principalTable: "materias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inscripciones",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'1000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estudiante_id = table.Column<int>(type: "integer", nullable: false),
                    materia_id = table.Column<int>(type: "integer", nullable: false),
                    periodo_academico = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    estado = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    fecha_inscripcion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inscripciones", x => x.id);
                    table.ForeignKey(
                        name: "fk_inscripciones_estudiantes_estudiante_id",
                        column: x => x.estudiante_id,
                        principalTable: "estudiantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_inscripciones_materias_materia_id",
                        column: x => x.materia_id,
                        principalTable: "materias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prerrequisitos",
                columns: table => new
                {
                    materia_id = table.Column<int>(type: "integer", nullable: false),
                    materia_requisito_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prerrequisitos", x => new { x.materia_id, x.materia_requisito_id });
                    table.CheckConstraint("ck_prerrequisitos_no_self", "materia_id <> materia_requisito_id");
                    table.ForeignKey(
                        name: "fk_prerrequisitos_materias_materia_id",
                        column: x => x.materia_id,
                        principalTable: "materias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_prerrequisitos_materias_materia_requisito_id",
                        column: x => x.materia_requisito_id,
                        principalTable: "materias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "carreras",
                columns: new[] { "id", "codigo", "duracion_semestres", "nombre" },
                values: new object[,]
                {
                    { 1, "ING-SIS", 10, "Ingeniería de Sistemas" },
                    { 2, "ADM", 8, "Administración de Empresas" }
                });

            migrationBuilder.InsertData(
                table: "estudiantes",
                columns: new[] { "id", "carrera_id", "nombre", "semestre_actual" },
                values: new object[,]
                {
                    { 1, 1, "Laura Gómez Ríos", 6 },
                    { 2, 1, "Mateo Rojas Herrera", 7 },
                    { 3, 1, "Camila Torres Vargas", 7 },
                    { 4, 1, "Sofía Martínez Ruiz", 8 },
                    { 5, 1, "Andrés Pérez Molina", 9 }
                });

            migrationBuilder.InsertData(
                table: "materias",
                columns: new[] { "id", "carrera_id", "codigo", "creditos", "cupos_maximos", "nombre", "semestre" },
                values: new object[,]
                {
                    { 1, 1, "603101", 4, 30, "Algoritmia y Programación", 1 },
                    { 2, 1, "603102", 4, 30, "Introducción a la Ingeniería de Sistemas", 1 },
                    { 3, 1, "603103", 4, 30, "Cálculo Diferencial", 1 },
                    { 4, 1, "603104", 2, 30, "Desarrollo del Pensamiento Lógico Matemático", 1 },
                    { 5, 1, "603105", 2, 30, "Procesos Comunicativos", 1 },
                    { 6, 1, "603201", 4, 30, "Programación Orientada A Objetos", 2 },
                    { 7, 1, "603202", 3, 30, "Sistemas Digitales", 2 },
                    { 8, 1, "603203", 4, 30, "Cálculo Integral", 2 },
                    { 9, 1, "603204", 3, 30, "Álgebra Lineal", 2 },
                    { 10, 1, "603205", 4, 30, "Física Mecánica", 2 },
                    { 11, 1, "603301", 4, 30, "Estructuras de Datos", 3 },
                    { 12, 1, "603302", 3, 30, "Máquinas Digitales", 3 },
                    { 13, 1, "603303", 4, 30, "Cálculo Multivariado", 3 },
                    { 14, 1, "603304", 4, 30, "Electricidad y Magnetismo", 3 },
                    { 15, 1, "603305", 2, 30, "Democracias y Paz", 3 },
                    { 16, 1, "603401", 4, 30, "Bases de Datos", 4 },
                    { 17, 1, "603402", 4, 30, "Sistemas Operativos", 4 },
                    { 18, 1, "603403", 3, 30, "Ecuaciones Diferenciales y Modelado Matemático", 4 },
                    { 19, 1, "603404", 4, 30, "Estadística y Probabilidad", 4 },
                    { 20, 1, "603405", 3, 30, "Oscilaciones y Ondas", 4 },
                    { 21, 1, "603501", 4, 30, "Ingeniería de Software I", 5 },
                    { 22, 1, "603502", 3, 30, "Pensamiento Sistémico", 5 },
                    { 23, 1, "603503", 3, 30, "Matemáticas Especiales", 5 },
                    { 24, 1, "603504", 3, 30, "Procesos Estocásticos", 5 },
                    { 25, 1, "603505", 3, 30, "Sistemas de Comunicaciones", 5 },
                    { 26, 1, "603506", 2, 30, "Ciencias, Tecnología y Desarrollo", 5 },
                    { 27, 1, "603601", 3, 30, "Ingeniería de Software II", 6 },
                    { 28, 1, "603602", 3, 30, "Métodos Numéricos", 6 },
                    { 29, 1, "603603", 3, 30, "Procesamiento de Señales e Imágenes", 6 },
                    { 30, 1, "603604", 3, 30, "Optimización", 6 },
                    { 31, 1, "603605", 4, 30, "Redes de Computadores", 6 },
                    { 32, 1, "603606", 2, 30, "Administración Financiera para Ingeniería", 6 },
                    { 33, 1, "603701", 3, 30, "Metodología de Investigación", 7 },
                    { 34, 1, "603702", 3, 30, "Tecnologías Avanzadas", 7 },
                    { 35, 1, "603703", 2, 30, "Ética y Humanística", 7 },
                    { 36, 1, "603704", 3, 30, "Sistemas Distribuidos", 7 },
                    { 37, 1, "603705", 3, 30, "Seguridad de la Información", 7 },
                    { 38, 1, "603706", 3, 30, "Formulación y Gestión de Proyectos TI", 7 },
                    { 39, 1, "603801", 3, 30, "Curso I Profundización", 8 },
                    { 40, 1, "603802", 3, 30, "Electiva Profesional I", 8 },
                    { 41, 1, "603803", 3, 30, "Simulación Computacional", 8 },
                    { 42, 1, "603804", 3, 30, "Arquitectura Empresarial", 8 },
                    { 43, 1, "603805", 2, 30, "Trabajo de Grado I", 8 },
                    { 44, 1, "603806", 2, 30, "Cátedra Orinoquia", 8 },
                    { 45, 1, "603901", 3, 30, "Curso Ii Profundización", 9 },
                    { 46, 1, "603902", 3, 30, "Electiva Profesional Ii", 9 },
                    { 47, 1, "603903", 3, 1, "Electiva Profesional Iii", 9 },
                    { 48, 1, "603904", 3, 30, "Lenguajes de Programación", 9 },
                    { 49, 1, "603905", 3, 30, "Electiva Profesional Iv", 9 },
                    { 50, 1, "603001", 3, 30, "Curso Iii Profundización", 10 },
                    { 51, 1, "603002", 3, 30, "Electiva Profesional V", 10 },
                    { 52, 1, "603003", 4, 30, "Trabajo de Grado Ii", 10 },
                    { 53, 1, "603004", 2, 30, "Electiva Complementaria", 10 },
                    { 54, 2, "ADM101", 3, 30, "Contabilidad", 1 }
                });

            migrationBuilder.InsertData(
                table: "historial_academico",
                columns: new[] { "id", "estado", "estudiante_id", "materia_id", "nota", "periodo" },
                values: new object[,]
                {
                    { 1, "Aprobada", 1, 1, 4.5m, "2024-1" },
                    { 2, "Aprobada", 1, 2, 4.2m, "2024-1" },
                    { 3, "Aprobada", 1, 3, 4.2m, "2024-1" },
                    { 4, "Aprobada", 1, 4, 3.8m, "2024-1" },
                    { 5, "Aprobada", 1, 5, 4.0m, "2024-1" },
                    { 6, "Aprobada", 1, 6, 4.0m, "2024-2" },
                    { 7, "Aprobada", 1, 7, 3.9m, "2024-2" },
                    { 8, "Aprobada", 1, 8, 3.8m, "2024-2" },
                    { 9, "Aprobada", 1, 9, 3.6m, "2024-2" },
                    { 10, "Aprobada", 1, 10, 3.4m, "2024-2" },
                    { 11, "Aprobada", 1, 11, 4.1m, "2025-1" },
                    { 12, "Aprobada", 1, 12, 3.7m, "2025-1" },
                    { 13, "Aprobada", 1, 13, 3.9m, "2025-1" },
                    { 14, "Aprobada", 1, 14, 3.5m, "2025-1" },
                    { 15, "Aprobada", 1, 15, 4.0m, "2025-1" },
                    { 16, "Aprobada", 1, 16, 3.7m, "2025-2" },
                    { 17, "Aprobada", 1, 17, 4.0m, "2025-2" },
                    { 18, "Aprobada", 1, 18, 3.3m, "2025-2" },
                    { 19, "Aprobada", 1, 19, 3.8m, "2025-2" },
                    { 20, "Aprobada", 1, 20, 3.6m, "2025-2" },
                    { 21, "Aprobada", 1, 21, 3.9m, "2026-1" },
                    { 22, "Aprobada", 1, 22, 4.0m, "2026-1" },
                    { 23, "Aprobada", 1, 23, 3.4m, "2026-1" },
                    { 24, "Aprobada", 1, 24, 3.7m, "2026-1" },
                    { 25, "Aprobada", 1, 25, 3.5m, "2026-1" },
                    { 26, "Aprobada", 1, 26, 3.8m, "2026-1" },
                    { 27, "Aprobada", 2, 1, 4.0m, "2023-2" },
                    { 28, "Aprobada", 2, 2, 4.0m, "2023-2" },
                    { 29, "Aprobada", 2, 3, 4.0m, "2023-2" },
                    { 30, "Aprobada", 2, 4, 4.0m, "2023-2" },
                    { 31, "Aprobada", 2, 5, 4.0m, "2023-2" },
                    { 32, "Aprobada", 2, 6, 4.0m, "2024-1" },
                    { 33, "Aprobada", 2, 7, 4.0m, "2024-1" },
                    { 34, "Aprobada", 2, 8, 4.0m, "2024-1" },
                    { 35, "Aprobada", 2, 9, 4.0m, "2024-1" },
                    { 36, "Aprobada", 2, 10, 4.0m, "2024-1" },
                    { 37, "Aprobada", 2, 11, 4.0m, "2024-2" },
                    { 38, "Aprobada", 2, 12, 4.0m, "2024-2" },
                    { 39, "Aprobada", 2, 13, 4.0m, "2024-2" },
                    { 40, "Aprobada", 2, 14, 4.0m, "2024-2" },
                    { 41, "Aprobada", 2, 15, 4.0m, "2024-2" },
                    { 42, "Aprobada", 2, 16, 4.0m, "2025-1" },
                    { 43, "Aprobada", 2, 17, 4.0m, "2025-1" },
                    { 44, "Aprobada", 2, 18, 4.0m, "2025-1" },
                    { 45, "Aprobada", 2, 19, 4.0m, "2025-1" },
                    { 46, "Aprobada", 2, 20, 4.0m, "2025-1" },
                    { 47, "Aprobada", 2, 21, 4.0m, "2025-2" },
                    { 48, "Aprobada", 2, 22, 4.0m, "2025-2" },
                    { 49, "Aprobada", 2, 23, 4.0m, "2025-2" },
                    { 50, "Aprobada", 2, 24, 4.0m, "2025-2" },
                    { 51, "Aprobada", 2, 25, 4.0m, "2025-2" },
                    { 52, "Aprobada", 2, 26, 4.0m, "2025-2" },
                    { 53, "Aprobada", 2, 27, 4.0m, "2026-1" },
                    { 54, "Aprobada", 2, 28, 4.0m, "2026-1" },
                    { 55, "Aprobada", 2, 29, 4.0m, "2026-1" },
                    { 56, "Aprobada", 2, 30, 4.0m, "2026-1" },
                    { 57, "Aprobada", 2, 31, 4.0m, "2026-1" },
                    { 58, "Aprobada", 2, 32, 4.0m, "2026-1" },
                    { 59, "Aprobada", 3, 1, 4.0m, "2023-2" },
                    { 60, "Aprobada", 3, 2, 4.0m, "2023-2" },
                    { 61, "Aprobada", 3, 3, 4.0m, "2023-2" },
                    { 62, "Aprobada", 3, 4, 4.0m, "2023-2" },
                    { 63, "Aprobada", 3, 5, 4.0m, "2023-2" },
                    { 64, "Aprobada", 3, 6, 4.0m, "2024-1" },
                    { 65, "Aprobada", 3, 7, 4.0m, "2024-1" },
                    { 66, "Aprobada", 3, 8, 4.0m, "2024-1" },
                    { 67, "Aprobada", 3, 9, 4.0m, "2024-1" },
                    { 68, "Aprobada", 3, 10, 4.0m, "2024-1" },
                    { 69, "Aprobada", 3, 11, 4.0m, "2024-2" },
                    { 70, "Aprobada", 3, 12, 4.0m, "2024-2" },
                    { 71, "Aprobada", 3, 13, 4.0m, "2024-2" },
                    { 72, "Aprobada", 3, 14, 4.0m, "2024-2" },
                    { 73, "Reprobada", 3, 15, 2.5m, "2024-2" },
                    { 74, "Aprobada", 3, 16, 4.0m, "2025-1" },
                    { 75, "Aprobada", 3, 17, 4.0m, "2025-1" },
                    { 76, "Aprobada", 3, 18, 4.0m, "2025-1" },
                    { 77, "Aprobada", 3, 19, 4.0m, "2025-1" },
                    { 78, "Aprobada", 3, 20, 4.0m, "2025-1" },
                    { 79, "Aprobada", 3, 21, 4.0m, "2025-2" },
                    { 80, "Aprobada", 3, 22, 4.0m, "2025-2" },
                    { 81, "Aprobada", 3, 23, 4.0m, "2025-2" },
                    { 82, "Aprobada", 3, 24, 4.0m, "2025-2" },
                    { 83, "Aprobada", 3, 25, 4.0m, "2025-2" },
                    { 84, "Aprobada", 3, 26, 4.0m, "2025-2" },
                    { 85, "Aprobada", 3, 27, 4.0m, "2026-1" },
                    { 86, "Aprobada", 3, 28, 4.0m, "2026-1" },
                    { 87, "Aprobada", 3, 29, 4.0m, "2026-1" },
                    { 88, "Aprobada", 3, 30, 4.0m, "2026-1" },
                    { 89, "Aprobada", 3, 31, 4.0m, "2026-1" },
                    { 90, "Aprobada", 3, 32, 4.0m, "2026-1" },
                    { 91, "Aprobada", 4, 1, 4.0m, "2023-1" },
                    { 92, "Aprobada", 4, 2, 4.0m, "2023-1" },
                    { 93, "Aprobada", 4, 3, 4.0m, "2023-1" },
                    { 94, "Aprobada", 4, 4, 4.0m, "2023-1" },
                    { 95, "Aprobada", 4, 5, 4.0m, "2023-1" },
                    { 96, "Aprobada", 4, 6, 4.0m, "2023-2" },
                    { 97, "Aprobada", 4, 7, 4.0m, "2023-2" },
                    { 98, "Aprobada", 4, 8, 4.0m, "2023-2" },
                    { 99, "Aprobada", 4, 9, 4.0m, "2023-2" },
                    { 100, "Aprobada", 4, 10, 4.0m, "2023-2" },
                    { 101, "Aprobada", 4, 11, 4.0m, "2024-1" },
                    { 102, "Aprobada", 4, 12, 4.0m, "2024-1" },
                    { 103, "Aprobada", 4, 13, 4.0m, "2024-1" },
                    { 104, "Aprobada", 4, 14, 4.0m, "2024-1" },
                    { 105, "Aprobada", 4, 15, 4.0m, "2024-1" },
                    { 106, "Aprobada", 4, 16, 4.0m, "2024-2" },
                    { 107, "Aprobada", 4, 17, 4.0m, "2024-2" },
                    { 108, "Aprobada", 4, 18, 4.0m, "2024-2" },
                    { 109, "Aprobada", 4, 19, 4.0m, "2024-2" },
                    { 110, "Aprobada", 4, 20, 4.0m, "2024-2" },
                    { 111, "Aprobada", 4, 21, 4.0m, "2025-1" },
                    { 112, "Aprobada", 4, 22, 4.0m, "2025-1" },
                    { 113, "Aprobada", 4, 23, 4.0m, "2025-1" },
                    { 114, "Aprobada", 4, 24, 4.0m, "2025-1" },
                    { 115, "Aprobada", 4, 25, 4.0m, "2025-1" },
                    { 116, "Aprobada", 4, 26, 4.0m, "2025-1" },
                    { 117, "Aprobada", 4, 27, 4.0m, "2025-2" },
                    { 118, "Aprobada", 4, 28, 4.0m, "2025-2" },
                    { 119, "Aprobada", 4, 29, 4.0m, "2025-2" },
                    { 120, "Aprobada", 4, 30, 4.0m, "2025-2" },
                    { 121, "Aprobada", 4, 31, 4.0m, "2025-2" },
                    { 122, "Aprobada", 4, 32, 4.0m, "2025-2" },
                    { 123, "Aprobada", 4, 33, 4.0m, "2026-1" },
                    { 124, "Aprobada", 4, 34, 4.0m, "2026-1" },
                    { 125, "Aprobada", 4, 35, 4.0m, "2026-1" },
                    { 126, "Aprobada", 4, 36, 4.0m, "2026-1" },
                    { 127, "Aprobada", 4, 37, 4.0m, "2026-1" },
                    { 128, "Aprobada", 4, 38, 4.0m, "2026-1" },
                    { 129, "Aprobada", 5, 1, 4.0m, "2022-2" },
                    { 130, "Aprobada", 5, 2, 4.0m, "2022-2" },
                    { 131, "Aprobada", 5, 3, 4.0m, "2022-2" },
                    { 132, "Aprobada", 5, 4, 4.0m, "2022-2" },
                    { 133, "Aprobada", 5, 5, 4.0m, "2022-2" },
                    { 134, "Aprobada", 5, 6, 4.0m, "2023-1" },
                    { 135, "Aprobada", 5, 7, 4.0m, "2023-1" },
                    { 136, "Aprobada", 5, 8, 4.0m, "2023-1" },
                    { 137, "Aprobada", 5, 9, 4.0m, "2023-1" },
                    { 138, "Aprobada", 5, 10, 4.0m, "2023-1" },
                    { 139, "Aprobada", 5, 11, 4.0m, "2023-2" },
                    { 140, "Aprobada", 5, 12, 4.0m, "2023-2" },
                    { 141, "Aprobada", 5, 13, 4.0m, "2023-2" },
                    { 142, "Aprobada", 5, 14, 4.0m, "2023-2" },
                    { 143, "Aprobada", 5, 15, 4.0m, "2023-2" },
                    { 144, "Aprobada", 5, 16, 4.0m, "2024-1" },
                    { 145, "Aprobada", 5, 17, 4.0m, "2024-1" },
                    { 146, "Aprobada", 5, 18, 4.0m, "2024-1" },
                    { 147, "Aprobada", 5, 19, 4.0m, "2024-1" },
                    { 148, "Aprobada", 5, 20, 4.0m, "2024-1" },
                    { 149, "Aprobada", 5, 21, 4.0m, "2024-2" },
                    { 150, "Aprobada", 5, 22, 4.0m, "2024-2" },
                    { 151, "Aprobada", 5, 23, 4.0m, "2024-2" },
                    { 152, "Aprobada", 5, 24, 4.0m, "2024-2" },
                    { 153, "Aprobada", 5, 25, 4.0m, "2024-2" },
                    { 154, "Aprobada", 5, 26, 4.0m, "2024-2" },
                    { 155, "Aprobada", 5, 27, 4.0m, "2025-1" },
                    { 156, "Aprobada", 5, 28, 4.0m, "2025-1" },
                    { 157, "Aprobada", 5, 29, 4.0m, "2025-1" },
                    { 158, "Aprobada", 5, 30, 4.0m, "2025-1" },
                    { 159, "Aprobada", 5, 31, 4.0m, "2025-1" },
                    { 160, "Aprobada", 5, 32, 4.0m, "2025-1" },
                    { 161, "Aprobada", 5, 33, 4.0m, "2025-2" },
                    { 162, "Aprobada", 5, 34, 4.0m, "2025-2" },
                    { 163, "Aprobada", 5, 35, 4.0m, "2025-2" },
                    { 164, "Aprobada", 5, 36, 4.0m, "2025-2" },
                    { 165, "Aprobada", 5, 37, 4.0m, "2025-2" },
                    { 166, "Aprobada", 5, 38, 4.0m, "2025-2" },
                    { 167, "Aprobada", 5, 39, 4.0m, "2026-1" },
                    { 168, "Aprobada", 5, 40, 4.0m, "2026-1" },
                    { 169, "Aprobada", 5, 41, 4.0m, "2026-1" },
                    { 170, "Aprobada", 5, 42, 4.0m, "2026-1" },
                    { 171, "Aprobada", 5, 43, 4.0m, "2026-1" },
                    { 172, "Aprobada", 5, 44, 4.0m, "2026-1" }
                });

            migrationBuilder.InsertData(
                table: "horario_materia",
                columns: new[] { "id", "dia_semana", "hora_fin", "hora_inicio", "materia_id" },
                values: new object[,]
                {
                    { 1, "Lunes", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 1 },
                    { 2, "Miercoles", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 1 },
                    { 3, "Lunes", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 2 },
                    { 4, "Miercoles", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 2 },
                    { 5, "Martes", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 3 },
                    { 6, "Jueves", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 3 },
                    { 7, "Martes", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 4 },
                    { 8, "Jueves", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 4 },
                    { 9, "Viernes", new TimeOnly(11, 0, 0), new TimeOnly(7, 0, 0), 5 },
                    { 10, "Lunes", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 6 },
                    { 11, "Miercoles", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 6 },
                    { 12, "Lunes", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 7 },
                    { 13, "Miercoles", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 7 },
                    { 14, "Martes", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 8 },
                    { 15, "Jueves", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 8 },
                    { 16, "Martes", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 9 },
                    { 17, "Jueves", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 9 },
                    { 18, "Viernes", new TimeOnly(18, 0, 0), new TimeOnly(14, 0, 0), 10 },
                    { 19, "Lunes", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 11 },
                    { 20, "Miercoles", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 11 },
                    { 21, "Lunes", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 12 },
                    { 22, "Miercoles", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 12 },
                    { 23, "Martes", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 13 },
                    { 24, "Jueves", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 13 },
                    { 25, "Martes", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 14 },
                    { 26, "Jueves", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 14 },
                    { 27, "Sabado", new TimeOnly(13, 0, 0), new TimeOnly(11, 0, 0), 15 },
                    { 28, "Lunes", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 16 },
                    { 29, "Miercoles", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 16 },
                    { 30, "Lunes", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 17 },
                    { 31, "Miercoles", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 17 },
                    { 32, "Martes", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 18 },
                    { 33, "Jueves", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 18 },
                    { 34, "Martes", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 19 },
                    { 35, "Jueves", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 19 },
                    { 36, "Viernes", new TimeOnly(18, 0, 0), new TimeOnly(14, 0, 0), 20 },
                    { 37, "Lunes", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 21 },
                    { 38, "Miercoles", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 21 },
                    { 39, "Lunes", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 22 },
                    { 40, "Miercoles", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 22 },
                    { 41, "Martes", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 23 },
                    { 42, "Jueves", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 23 },
                    { 43, "Martes", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 24 },
                    { 44, "Jueves", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 24 },
                    { 45, "Viernes", new TimeOnly(11, 0, 0), new TimeOnly(7, 0, 0), 25 },
                    { 46, "Sabado", new TimeOnly(11, 0, 0), new TimeOnly(7, 0, 0), 26 },
                    { 47, "Lunes", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 27 },
                    { 48, "Miercoles", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 27 },
                    { 49, "Lunes", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 28 },
                    { 50, "Miercoles", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 28 },
                    { 51, "Martes", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 29 },
                    { 52, "Jueves", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 29 },
                    { 53, "Martes", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 30 },
                    { 54, "Jueves", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 30 },
                    { 55, "Viernes", new TimeOnly(18, 0, 0), new TimeOnly(14, 0, 0), 31 },
                    { 56, "Sabado", new TimeOnly(18, 0, 0), new TimeOnly(14, 0, 0), 32 },
                    { 57, "Lunes", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 33 },
                    { 58, "Miercoles", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 33 },
                    { 59, "Lunes", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 34 },
                    { 60, "Miercoles", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 34 },
                    { 61, "Martes", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 35 },
                    { 62, "Jueves", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 35 },
                    { 63, "Martes", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 36 },
                    { 64, "Jueves", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 36 },
                    { 65, "Viernes", new TimeOnly(11, 0, 0), new TimeOnly(7, 0, 0), 37 },
                    { 66, "Sabado", new TimeOnly(11, 0, 0), new TimeOnly(7, 0, 0), 38 },
                    { 67, "Lunes", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 39 },
                    { 68, "Miercoles", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 39 },
                    { 69, "Lunes", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 40 },
                    { 70, "Miercoles", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 40 },
                    { 71, "Martes", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 41 },
                    { 72, "Jueves", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 41 },
                    { 73, "Martes", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 42 },
                    { 74, "Jueves", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 42 },
                    { 75, "Viernes", new TimeOnly(18, 0, 0), new TimeOnly(14, 0, 0), 43 },
                    { 76, "Sabado", new TimeOnly(18, 0, 0), new TimeOnly(14, 0, 0), 44 },
                    { 77, "Lunes", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 45 },
                    { 78, "Miercoles", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 45 },
                    { 79, "Lunes", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 46 },
                    { 80, "Miercoles", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 46 },
                    { 81, "Martes", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 47 },
                    { 82, "Jueves", new TimeOnly(9, 0, 0), new TimeOnly(7, 0, 0), 47 },
                    { 83, "Martes", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 48 },
                    { 84, "Jueves", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 48 },
                    { 85, "Viernes", new TimeOnly(11, 0, 0), new TimeOnly(7, 0, 0), 49 },
                    { 86, "Viernes", new TimeOnly(18, 0, 0), new TimeOnly(14, 0, 0), 50 },
                    { 87, "Lunes", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 51 },
                    { 88, "Miercoles", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 51 },
                    { 89, "Martes", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 52 },
                    { 90, "Jueves", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 52 },
                    { 91, "Martes", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 53 },
                    { 92, "Jueves", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 53 },
                    { 93, "Sabado", new TimeOnly(20, 0, 0), new TimeOnly(18, 0, 0), 54 }
                });

            migrationBuilder.InsertData(
                table: "inscripciones",
                columns: new[] { "id", "estado", "estudiante_id", "fecha_inscripcion", "materia_id", "periodo_academico" },
                values: new object[] { 1, "Activa", 5, new DateTimeOffset(new DateTime(2026, 8, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 47, "2026-2" });

            migrationBuilder.InsertData(
                table: "prerrequisitos",
                columns: new[] { "materia_id", "materia_requisito_id" },
                values: new object[,]
                {
                    { 6, 1 },
                    { 8, 3 },
                    { 10, 3 },
                    { 11, 6 },
                    { 12, 7 },
                    { 13, 8 },
                    { 13, 9 },
                    { 14, 10 },
                    { 16, 11 },
                    { 17, 11 },
                    { 17, 12 },
                    { 18, 13 },
                    { 19, 13 },
                    { 20, 14 },
                    { 21, 16 },
                    { 23, 18 },
                    { 24, 19 },
                    { 25, 20 },
                    { 27, 21 },
                    { 28, 23 },
                    { 29, 23 },
                    { 30, 24 },
                    { 31, 25 },
                    { 33, 22 },
                    { 34, 27 },
                    { 36, 31 },
                    { 37, 31 },
                    { 38, 32 },
                    { 41, 22 },
                    { 43, 33 },
                    { 45, 39 },
                    { 50, 45 },
                    { 52, 43 }
                });

            migrationBuilder.CreateIndex(
                name: "ux_carreras_codigo",
                table: "carreras",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_estudiantes_carrera_id",
                table: "estudiantes",
                column: "carrera_id");

            migrationBuilder.CreateIndex(
                name: "ix_historial_academico_materia_id",
                table: "historial_academico",
                column: "materia_id");

            migrationBuilder.CreateIndex(
                name: "ux_historial_estudiante_materia_periodo",
                table: "historial_academico",
                columns: new[] { "estudiante_id", "materia_id", "periodo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_horario_materia_materia_id",
                table: "horario_materia",
                column: "materia_id");

            migrationBuilder.CreateIndex(
                name: "ix_inscripciones_materia_id_periodo_academico",
                table: "inscripciones",
                columns: new[] { "materia_id", "periodo_academico" });

            migrationBuilder.CreateIndex(
                name: "ux_inscripciones_activa",
                table: "inscripciones",
                columns: new[] { "estudiante_id", "materia_id", "periodo_academico" },
                unique: true,
                filter: "estado = 'Activa'");

            migrationBuilder.CreateIndex(
                name: "ix_materias_carrera_id",
                table: "materias",
                column: "carrera_id");

            migrationBuilder.CreateIndex(
                name: "ux_materias_codigo",
                table: "materias",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_prerrequisitos_materia_requisito_id",
                table: "prerrequisitos",
                column: "materia_requisito_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historial_academico");

            migrationBuilder.DropTable(
                name: "horario_materia");

            migrationBuilder.DropTable(
                name: "inscripciones");

            migrationBuilder.DropTable(
                name: "prerrequisitos");

            migrationBuilder.DropTable(
                name: "estudiantes");

            migrationBuilder.DropTable(
                name: "materias");

            migrationBuilder.DropTable(
                name: "carreras");
        }
    }
}
