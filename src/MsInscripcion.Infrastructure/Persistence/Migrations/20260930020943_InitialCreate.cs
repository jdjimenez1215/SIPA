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
                    { 1, 1, "Ana", 2 },
                    { 2, 1, "Carlos", 1 }
                });

            migrationBuilder.InsertData(
                table: "materias",
                columns: new[] { "id", "carrera_id", "codigo", "creditos", "cupos_maximos", "nombre", "semestre" },
                values: new object[,]
                {
                    { 1, 1, "MAT101", 4, 30, "Cálculo I", 1 },
                    { 2, 1, "FIS101", 4, 30, "Física I", 1 },
                    { 3, 1, "PRG101", 4, 30, "Programación I", 1 },
                    { 4, 1, "ETI101", 2, 30, "Ética", 1 },
                    { 5, 1, "HUM101", 2, 30, "Humanidades", 1 },
                    { 6, 1, "MAT201", 4, 30, "Cálculo II", 2 },
                    { 7, 1, "PRG201", 4, 30, "Programación II", 2 },
                    { 8, 1, "EST201", 3, 30, "Estadística", 2 },
                    { 9, 1, "ALG201", 3, 1, "Álgebra Lineal", 2 },
                    { 10, 1, "EDD301", 4, 30, "Estructuras de Datos", 3 },
                    { 11, 1, "ING601", 4, 30, "Ingeniería de Software", 6 },
                    { 12, 2, "ADM101", 3, 30, "Contabilidad", 1 }
                });

            migrationBuilder.InsertData(
                table: "historial_academico",
                columns: new[] { "id", "estado", "estudiante_id", "materia_id", "nota", "periodo" },
                values: new object[,]
                {
                    { 1, "Aprobada", 1, 1, 4.5m, "2026-1" },
                    { 2, "Aprobada", 1, 3, 4.0m, "2026-1" },
                    { 3, "Reprobada", 1, 2, 2.0m, "2026-1" }
                });

            migrationBuilder.InsertData(
                table: "horario_materia",
                columns: new[] { "id", "dia_semana", "hora_fin", "hora_inicio", "materia_id" },
                values: new object[,]
                {
                    { 1, "Lunes", new TimeOnly(8, 0, 0), new TimeOnly(6, 0, 0), 1 },
                    { 2, "Miercoles", new TimeOnly(8, 0, 0), new TimeOnly(6, 0, 0), 1 },
                    { 3, "Lunes", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 2 },
                    { 4, "Martes", new TimeOnly(10, 0, 0), new TimeOnly(8, 0, 0), 3 },
                    { 5, "Viernes", new TimeOnly(10, 0, 0), new TimeOnly(8, 0, 0), 4 },
                    { 6, "Viernes", new TimeOnly(11, 0, 0), new TimeOnly(9, 0, 0), 5 },
                    { 7, "Lunes", new TimeOnly(10, 0, 0), new TimeOnly(8, 0, 0), 6 },
                    { 8, "Miercoles", new TimeOnly(12, 0, 0), new TimeOnly(10, 0, 0), 6 },
                    { 9, "Martes", new TimeOnly(12, 0, 0), new TimeOnly(10, 0, 0), 7 },
                    { 10, "Jueves", new TimeOnly(12, 0, 0), new TimeOnly(10, 0, 0), 7 },
                    { 11, "Martes", new TimeOnly(14, 0, 0), new TimeOnly(12, 0, 0), 8 },
                    { 12, "Jueves", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 9 },
                    { 13, "Miercoles", new TimeOnly(16, 0, 0), new TimeOnly(14, 0, 0), 10 },
                    { 14, "Jueves", new TimeOnly(18, 0, 0), new TimeOnly(16, 0, 0), 11 },
                    { 15, "Sabado", new TimeOnly(10, 0, 0), new TimeOnly(8, 0, 0), 12 }
                });

            migrationBuilder.InsertData(
                table: "inscripciones",
                columns: new[] { "id", "estado", "estudiante_id", "fecha_inscripcion", "materia_id", "periodo_academico" },
                values: new object[,]
                {
                    { 1, "Activa", 1, new DateTimeOffset(new DateTime(2026, 8, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 4, "2026-2" },
                    { 2, "Activa", 2, new DateTimeOffset(new DateTime(2026, 8, 1, 12, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 9, "2026-2" }
                });

            migrationBuilder.InsertData(
                table: "prerrequisitos",
                columns: new[] { "materia_id", "materia_requisito_id" },
                values: new object[,]
                {
                    { 6, 1 },
                    { 7, 3 },
                    { 10, 7 }
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
