using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Application.Common;
using MsInscripcion.Application.Common.Exceptions;
using MsInscripcion.Application.Common.Options;
using MsInscripcion.Application.Features.Materias.Queries;
using MsInscripcion.Application.Features.Sugerencia.Queries;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Enums;
using MsInscripcion.Domain.Rules;
using MsInscripcion.Domain.Services;
using MsInscripcion.UnitTests.Fixtures;

namespace MsInscripcion.UnitTests.Handlers;

public class GetSugerenciaQueryHandlerTests
{
    private readonly Mock<IStudentRepository> _students = new();
    private readonly Mock<ICarreraRepository> _carreras = new();
    private readonly Mock<IMateriaRepository> _materias = new();
    private readonly Mock<IInscripcionRepository> _inscripciones = new();

    private static readonly EnrollmentSuggestionCalculator Calculator = new(new EnrollmentRulesEngine(new IEnrollmentRule[]
    {
        new CareerMatchRule(),
        new PrerequisitesRule(),
        new ScheduleOverlapRule(),
        new SemesterWindowRule(),
        new ExtraSubjectsCapRule(),
        new NoDuplicateRule(),
        new SeatAvailabilityRule()
    }));

    public GetSugerenciaQueryHandlerTests()
    {
        _students
            .Setup(s => s.GetApprovedMateriaIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());
        _inscripciones
            .Setup(i => i.GetActiveWithScheduleAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Inscripcion>());
        _inscripciones
            .Setup(i => i.CountActiveAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int>());
        _materias
            .Setup(m => m.GetByCarreraAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Materia>());
    }

    private GetSugerenciaQueryHandler BuildHandler(int cap = 3, string currentPeriod = "2026-2") =>
        new(
            _students.Object,
            _carreras.Object,
            _materias.Object,
            _inscripciones.Object,
            Calculator,
            Options.Create(new EnrollmentOptions { MaxNextSemesterSubjects = cap, CurrentPeriod = currentPeriod }));

    private void GivenStudent(Estudiante? student) =>
        _students
            .Setup(s => s.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

    private void GivenMaterias(params Materia[] materias) =>
        _materias
            .Setup(m => m.GetByCarreraAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(materias.ToList());

    private void GivenCarrera(string nombre) =>
        _carreras
            .Setup(c => c.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Carrera { Id = 1, Codigo = "ING", Nombre = nombre });

    [Fact]
    public async Task Handle_UnknownStudent_ThrowsNotFound()
    {
        GivenStudent(null);

        var act = () => BuildHandler().Handle(new GetSugerenciaQuery(99), CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be(AppErrorCodes.StudentNotFound);
    }

    [Fact]
    public async Task Handle_TotalCreditos_SumsOnlySuggestedRows()
    {
        // Semester 3 student: C1 (current) + N1..N4 (next). The cap leaves N4 out => 4 suggested x 3 credits.
        GivenStudent(TestData.Student(semestreActual: 3));
        GivenCarrera("Ingeniería de Sistemas");
        GivenMaterias(
            TestData.Materia(1, semestre: 3, codigo: "C1"),
            TestData.Materia(2, semestre: 4, codigo: "N1"),
            TestData.Materia(3, semestre: 4, codigo: "N2"),
            TestData.Materia(4, semestre: 4, codigo: "N3"),
            TestData.Materia(5, semestre: 4, codigo: "N4"));

        var result = await BuildHandler().Handle(new GetSugerenciaQuery(1), CancellationToken.None);

        result.MateriasSugeridas.Should().HaveCount(5);
        result.MateriasSugeridas.Count(m => m.Estado == EstadoSugerencia.Sugerida).Should().Be(4);
        result.TotalCreditos.Should().Be(12);
    }

    [Fact]
    public async Task Handle_MapsRowsWithEstadoPrerrequisitoCumplidoAndMotivo()
    {
        GivenStudent(TestData.Student(semestreActual: 3));
        GivenCarrera("Ingeniería de Sistemas");
        GivenMaterias(
            TestData.Materia(1, semestre: 3, codigo: "C1"),
            TestData.Materia(2, semestre: 4, codigo: "N1", prerequisiteIds: [99]));

        var result = await BuildHandler().Handle(new GetSugerenciaQuery(1), CancellationToken.None);

        var suggested = result.MateriasSugeridas.Single(m => m.Codigo == "C1");
        suggested.Id.Should().Be(1);
        suggested.Nombre.Should().Be("Materia 1");
        suggested.Creditos.Should().Be(3);
        suggested.Semestre.Should().Be(3);
        suggested.Estado.Should().Be(EstadoSugerencia.Sugerida);
        suggested.PrerrequisitoCumplido.Should().BeTrue();
        suggested.Motivo.Should().BeNull();

        var blocked = result.MateriasSugeridas.Single(m => m.Codigo == "N1");
        blocked.Estado.Should().Be(EstadoSugerencia.Prerrequisito);
        blocked.PrerrequisitoCumplido.Should().BeFalse();
        blocked.Motivo.Should().Be(ErrorCodes.PrerequisiteNotMet);
        result.TotalCreditos.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ProgramaComesFromCarreraAndHeaderFromStudent()
    {
        GivenStudent(new Estudiante { Id = 7, Nombre = "Laura Gómez Ríos", CarreraId = 1, SemestreActual = 6 });
        GivenCarrera("Ingeniería de Sistemas");

        var result = await BuildHandler().Handle(new GetSugerenciaQuery(7), CancellationToken.None);

        result.NombreEstudiante.Should().Be("Laura Gómez Ríos");
        result.Programa.Should().Be("Ingeniería de Sistemas");
        result.SemestreActual.Should().Be(6);
        result.Notificaciones.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CarreraWithoutMaterias_ReturnsEmptyListAndZeroCredits()
    {
        GivenStudent(TestData.Student());
        GivenCarrera("Ingeniería de Sistemas");

        var result = await BuildHandler().Handle(new GetSugerenciaQuery(1), CancellationToken.None);

        result.MateriasSugeridas.Should().BeEmpty();
        result.TotalCreditos.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithoutPeriodo_UsesConfiguredCurrentPeriod()
    {
        GivenStudent(TestData.Student());
        GivenCarrera("X");

        var result = await BuildHandler(currentPeriod: "2027-1").Handle(new GetSugerenciaQuery(1), CancellationToken.None);

        result.Periodo.Should().Be("2027-1");
    }

    [Fact]
    public async Task Handle_WithPeriodo_UsesItForTheActiveEnrollmentLookup()
    {
        GivenStudent(TestData.Student());
        GivenCarrera("X");
        GivenMaterias(TestData.Materia(1, semestre: 2));

        var result = await BuildHandler().Handle(new GetSugerenciaQuery(1, "2026-1"), CancellationToken.None);

        result.Periodo.Should().Be("2026-1");
        _inscripciones.Verify(
            i => i.GetActiveWithScheduleAsync(1, "2026-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ConfiguredCap_IsAppliedToTheSuggestion()
    {
        GivenStudent(TestData.Student(semestreActual: 3));
        GivenCarrera("X");
        GivenMaterias(
            TestData.Materia(1, semestre: 4, codigo: "N1"),
            TestData.Materia(2, semestre: 4, codigo: "N2"));

        var result = await BuildHandler(cap: 1).Handle(new GetSugerenciaQuery(1), CancellationToken.None);

        result.MateriasSugeridas.Single(m => m.Codigo == "N1").Estado.Should().Be(EstadoSugerencia.Sugerida);
        result.MateriasSugeridas.Single(m => m.Codigo == "N2").Motivo.Should().Be(ErrorCodes.ExtraSubjectsCapExceeded);
    }
}

public class MateriasDisponiblesQueryHandlerTests
{
    private readonly Mock<IStudentRepository> _students = new();
    private readonly Mock<IMateriaRepository> _materias = new();
    private readonly Mock<IInscripcionRepository> _inscripciones = new();

    private MateriasDisponiblesQueryHandler BuildHandler() =>
        new(
            _students.Object,
            _materias.Object,
            _inscripciones.Object,
            new EnrollmentSuggestionCalculator(new EnrollmentRulesEngine(new IEnrollmentRule[]
            {
                new CareerMatchRule(),
                new PrerequisitesRule(),
                new ScheduleOverlapRule(),
                new SemesterWindowRule(),
                new ExtraSubjectsCapRule(),
                new NoDuplicateRule(),
                new SeatAvailabilityRule()
            })),
            Options.Create(new EnrollmentOptions { MaxNextSemesterSubjects = 3 }));

    [Fact]
    public async Task Handle_UnknownStudent_ThrowsNotFound()
    {
        _students
            .Setup(s => s.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Estudiante?)null);

        var act = () => BuildHandler().Handle(new MateriasDisponiblesQuery(99), CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be(AppErrorCodes.StudentNotFound);
    }

    [Fact]
    public async Task Handle_ReturnsOnlySuggestedRows()
    {
        _students
            .Setup(s => s.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestData.Student(semestreActual: 3));
        _students
            .Setup(s => s.GetApprovedMateriaIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());
        _materias
            .Setup(m => m.GetByCarreraAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Materia>
            {
                TestData.Materia(1, semestre: 3, codigo: "C1"),
                TestData.Materia(2, semestre: 4, codigo: "N1", prerequisiteIds: [99])
            });
        _inscripciones
            .Setup(i => i.GetActiveWithScheduleAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Inscripcion>());
        _inscripciones
            .Setup(i => i.CountActiveAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int>());

        var result = await BuildHandler().Handle(new MateriasDisponiblesQuery(1), CancellationToken.None);

        result.Select(m => m.Codigo).Should().Equal("C1");
    }
}
