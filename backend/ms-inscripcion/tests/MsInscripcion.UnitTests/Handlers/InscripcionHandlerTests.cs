using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Application.Common.Exceptions;
using MsInscripcion.Application.Common.Options;
using MsInscripcion.Application.Features.Inscripciones.Commands;
using MsInscripcion.Application.Features.Inscripciones.Queries;
using MsInscripcion.Application.Features.Materias.Queries;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Enums;
using MsInscripcion.Domain.Rules;
using MsInscripcion.Domain.Services;
using MsInscripcion.UnitTests.Fixtures;

namespace MsInscripcion.UnitTests.Handlers;

public class InscripcionHandlerTests
{
    private static readonly IOptions<EnrollmentOptions> Options =
        Microsoft.Extensions.Options.Options.Create(new EnrollmentOptions { CurrentPeriod = TestData.Period });

    // ---------- Cancel ----------

    [Fact]
    public async Task Cancel_ActiveEnrollment_CancelsAndSaves()
    {
        var repository = new Mock<IInscripcionRepository>();
        var uow = new Mock<IUnitOfWork>();
        var enrollment = new Inscripcion { Id = 5, EstudianteId = 1, MateriaId = 27, Estado = EstadoInscripcion.Activa };
        repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(enrollment);

        await new CancelCommandHandler(repository.Object, uow.Object).Handle(new CancelCommand(5), default);

        enrollment.Estado.Should().Be(EstadoInscripcion.Cancelada);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelled_ThrowsConflict()
    {
        var repository = new Mock<IInscripcionRepository>();
        var enrollment = new Inscripcion { Id = 5, Estado = EstadoInscripcion.Cancelada };
        repository.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(enrollment);

        var act = () => new CancelCommandHandler(repository.Object, Mock.Of<IUnitOfWork>())
            .Handle(new CancelCommand(5), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Cancel_Unknown_ThrowsNotFound()
    {
        var repository = new Mock<IInscripcionRepository>();
        repository.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Inscripcion?)null);

        var act = () => new CancelCommandHandler(repository.Object, Mock.Of<IUnitOfWork>())
            .Handle(new CancelCommand(99), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- List enrollments ----------

    [Fact]
    public async Task GetInscripciones_ReturnsActiveEnrollments_WithDefaultPeriod()
    {
        var students = new Mock<IStudentRepository>();
        var enrollments = new Mock<IInscripcionRepository>();
        students.Setup(s => s.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var materia = TestData.Materia(27, semestre: 6, codigo: "603601");
        var row = new Inscripcion
        {
            Id = 1000, EstudianteId = 1, MateriaId = 27, Materia = materia,
            PeriodoAcademico = TestData.Period, Estado = EstadoInscripcion.Activa,
            FechaInscripcion = DateTimeOffset.UtcNow
        };
        enrollments
            .Setup(r => r.GetActiveWithScheduleAsync(1, TestData.Period, It.IsAny<CancellationToken>()))
            .ReturnsAsync([row]);

        var result = await new GetInscripcionesQueryHandler(students.Object, enrollments.Object, Options)
            .Handle(new GetInscripcionesQuery(1), default);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(1000);
        result[0].PeriodoAcademico.Should().Be(TestData.Period);
        result[0].Materia!.Codigo.Should().Be("603601");
    }

    [Fact]
    public async Task GetInscripciones_UsesExplicitPeriod()
    {
        var students = new Mock<IStudentRepository>();
        var enrollments = new Mock<IInscripcionRepository>();
        students.Setup(s => s.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        enrollments.Setup(r => r.GetActiveWithScheduleAsync(1, "2026-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await new GetInscripcionesQueryHandler(students.Object, enrollments.Object, Options)
            .Handle(new GetInscripcionesQuery(1, "2026-1"), default);

        enrollments.Verify(r => r.GetActiveWithScheduleAsync(1, "2026-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetInscripciones_UnknownStudent_ThrowsNotFound()
    {
        var students = new Mock<IStudentRepository>();
        students.Setup(s => s.ExistsAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var act = () => new GetInscripcionesQueryHandler(students.Object, Mock.Of<IInscripcionRepository>(), Options)
            .Handle(new GetInscripcionesQuery(99), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- Available subjects ----------

    [Fact]
    public async Task MateriasDisponibles_ReturnsOnlySuggestedSubjects()
    {
        var student = TestData.Student(1, semestreActual: 6);
        var students = new Mock<IStudentRepository>();
        var materias = new Mock<IMateriaRepository>();
        var inscripciones = new Mock<IInscripcionRepository>();

        students.Setup(s => s.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(student);
        students.Setup(s => s.GetApprovedMateriaIdsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());
        students.Setup(s => s.GetAverageGradeAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(3.8m);
        var subject = TestData.Materia(27, semestre: 6, codigo: "603601");
        materias.Setup(m => m.GetByCarreraAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([subject]);
        inscripciones.Setup(i => i.GetActiveWithScheduleAsync(1, TestData.Period, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        inscripciones.Setup(i => i.CountActiveAsync(It.IsAny<IReadOnlyCollection<int>>(), TestData.Period, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int>());

        var calculator = new EnrollmentSuggestionCalculator(
            new EnrollmentRulesEngine(Array.Empty<IEnrollmentRule>()));

        var result = await new MateriasDisponiblesQueryHandler(
                students.Object, materias.Object, inscripciones.Object, calculator, Options)
            .Handle(new MateriasDisponiblesQuery(1), default);

        result.Should().ContainSingle(m => m.Codigo == "603601");
    }

    [Fact]
    public async Task MateriasDisponibles_UnknownStudent_ThrowsNotFound()
    {
        var students = new Mock<IStudentRepository>();
        students.Setup(s => s.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Estudiante?)null);
        var calculator = new EnrollmentSuggestionCalculator(new EnrollmentRulesEngine([]));

        var act = () => new MateriasDisponiblesQueryHandler(
                students.Object, Mock.Of<IMateriaRepository>(), Mock.Of<IInscripcionRepository>(), calculator, Options)
            .Handle(new MateriasDisponiblesQuery(99), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
