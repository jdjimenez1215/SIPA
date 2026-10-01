using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Application.Common;
using MsInscripcion.Application.Common.Exceptions;
using MsInscripcion.Application.Common.Options;
using MsInscripcion.Application.Features.Inscripciones.Commands;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Enums;
using MsInscripcion.Domain.Rules;
using MsInscripcion.UnitTests.Fixtures;

namespace MsInscripcion.UnitTests.Handlers;

public class EnrollCommandHandlerTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IStudentRepository> _students = new();
    private readonly Mock<IMateriaRepository> _materias = new();
    private readonly Mock<IInscripcionRepository> _inscripciones = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ITransaction> _transaction = new();
    private readonly EnrollCommandHandler _handler;

    public EnrollCommandHandlerTests()
    {
        _unitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transaction.Object);

        _students
            .Setup(s => s.GetApprovedMateriaIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<int>());

        _inscripciones
            .Setup(i => i.GetActiveWithScheduleAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Inscripcion>());

        _inscripciones
            .Setup(i => i.CountActiveAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int>());

        var engine = new EnrollmentRulesEngine(new IEnrollmentRule[]
        {
            new CareerMatchRule(),
            new PrerequisitesRule(),
            new ScheduleOverlapRule(),
            new SemesterWindowRule(),
            new ExtraSubjectsCapRule(),
            new NoDuplicateRule(),
            new SeatAvailabilityRule()
        });

        _handler = new EnrollCommandHandler(
            _students.Object,
            _materias.Object,
            _inscripciones.Object,
            _unitOfWork.Object,
            engine,
            Options.Create(new EnrollmentOptions { MaxNextSemesterSubjects = 3 }),
            new FixedTimeProvider(FixedNow));
    }

    private void GivenStudent(Estudiante? student) =>
        _students
            .Setup(s => s.LockByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(student);

    private void GivenLockedMaterias(params Materia[] materias) =>
        _materias
            .Setup(m => m.LockByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(materias.OrderBy(m => m.Id).ToList());

    private void VerifyNothingPersisted()
    {
        _inscripciones.Verify(i => i.AddRange(It.IsAny<IEnumerable<Inscripcion>>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AllRulesSatisfied_AddsAllEnrollmentsAndCommits()
    {
        GivenStudent(TestData.Student(id: 1, carreraId: 1, semestreActual: 2));
        GivenLockedMaterias(
            TestData.Materia(6, semestre: 3, horarios: [TestData.Block(DiaSemana.Lunes, 8, 10)]),
            TestData.Materia(7, semestre: 3, horarios: [TestData.Block(DiaSemana.Lunes, 10, 12)]));

        List<Inscripcion>? added = null;
        _inscripciones
            .Setup(i => i.AddRange(It.IsAny<IEnumerable<Inscripcion>>()))
            .Callback<IEnumerable<Inscripcion>>(items => added = items.ToList());

        // Request order 7,6 must be preserved even though locks are taken in ascending order.
        var result = await _handler.Handle(new EnrollCommand(1, TestData.Period, [7, 6]), CancellationToken.None);

        added.Should().NotBeNull();
        added!.Select(i => i.MateriaId).Should().Equal(7, 6);
        added.Should().OnlyContain(i =>
            i.EstudianteId == 1
            && i.PeriodoAcademico == TestData.Period
            && i.Estado == EstadoInscripcion.Activa
            && i.FechaInscripcion == FixedNow);

        result.Select(r => r.MateriaId).Should().Equal(7, 6);
        result.Should().OnlyContain(r => r.Materia != null);

        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LocksMateriasInAscendingIdOrder()
    {
        GivenStudent(TestData.Student());
        GivenLockedMaterias(TestData.Materia(1), TestData.Materia(2), TestData.Materia(3));

        await _handler.Handle(new EnrollCommand(1, TestData.Period, [3, 1, 2]), CancellationToken.None);

        _materias.Verify(
            m => m.LockByIdsAsync(
                It.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 1, 2, 3 })),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AnyViolation_ThrowsWithAllViolationsAndPersistsNothing()
    {
        GivenStudent(TestData.Student(carreraId: 1, semestreActual: 2));
        GivenLockedMaterias(
            TestData.Materia(1, semestre: 1),                      // valid
            TestData.Materia(2, carreraId: 2, semestre: 9),        // career mismatch + semester exceeded
            TestData.Materia(3, prerequisiteIds: [50]));           // prerequisite missing

        var act = () => _handler.Handle(new EnrollCommand(1, TestData.Period, [1, 2, 3]), CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<BusinessRuleViolationException>()).Which;
        ex.Code.Should().Be(AppErrorCodes.EnrollmentRejected);
        ex.Violations.Select(v => (v.MateriaId, v.Code)).Should().BeEquivalentTo(new[]
        {
            (2, ErrorCodes.CareerMismatch),
            (2, ErrorCodes.SemesterExceeded),
            (3, ErrorCodes.PrerequisiteNotMet)
        });

        _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNothingPersisted();
    }

    [Fact]
    public async Task Handle_SeatsExhausted_ThrowsAndPersistsNothing()
    {
        GivenStudent(TestData.Student());
        GivenLockedMaterias(TestData.Materia(9, cupos: 1));
        _inscripciones
            .Setup(i => i.CountActiveAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<int, int> { [9] = 1 });

        var act = () => _handler.Handle(new EnrollCommand(1, TestData.Period, [9]), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleViolationException>()).Which.Violations
            .Should().ContainSingle().Which.Code.Should().Be(ErrorCodes.SeatsExhausted);
        VerifyNothingPersisted();
    }

    [Fact]
    public async Task Handle_StudentNotFound_ThrowsNotFoundRollsBackAndPersistsNothing()
    {
        GivenStudent(null);

        var act = () => _handler.Handle(new EnrollCommand(99, TestData.Period, [1]), CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).Which.Code.Should().Be(AppErrorCodes.StudentNotFound);
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _materias.Verify(
            m => m.LockByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNothingPersisted();
    }

    [Fact]
    public async Task Handle_TakesStudentLockInsideTransactionBeforeMateriaLocks()
    {
        var calls = new List<string>();
        _unitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("begin"))
            .ReturnsAsync(_transaction.Object);
        _students
            .Setup(s => s.LockByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("lock-student"))
            .ReturnsAsync(TestData.Student());
        _materias
            .Setup(m => m.LockByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("lock-materias"))
            .ReturnsAsync(new List<Materia> { TestData.Materia(1) });

        await _handler.Handle(new EnrollCommand(1, TestData.Period, [1]), CancellationToken.None);

        calls.Should().Equal("begin", "lock-student", "lock-materias");
    }

    [Fact]
    public async Task Handle_MateriaNotFound_ThrowsNotFoundRollsBackAndPersistsNothing()
    {
        GivenStudent(TestData.Student());
        GivenLockedMaterias(TestData.Materia(1)); // id 2 missing

        var act = () => _handler.Handle(new EnrollCommand(1, TestData.Period, [1, 2]), CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<NotFoundException>()).Which;
        ex.Code.Should().Be(AppErrorCodes.MateriaNotFound);
        ex.Message.Should().Contain("2");
        _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNothingPersisted();
    }

    private void GivenCodes(params (string Codigo, int Id)[] map) =>
        _materias
            .Setup(m => m.GetIdsByCodigosAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(map.ToDictionary(x => x.Codigo, x => x.Id));

    [Fact]
    public async Task Handle_FourthExtraSubject_ThrowsCapViolationOnlyForThatSubjectAndPersistsNothing()
    {
        // Student in semester 2: semester 3 is N+1, so every requested subject is an extra (cap = 3).
        GivenStudent(TestData.Student(semestreActual: 2));
        GivenLockedMaterias(
            Enumerable.Range(1, 4)
                .Select(i => TestData.Materia(i, semestre: 3, horarios: [TestData.Block(DiaSemana.Martes, 6 + i * 2, 8 + i * 2)]))
                .ToArray());

        var act = () => _handler.Handle(new EnrollCommand(1, TestData.Period, [1, 2, 3, 4]), CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<BusinessRuleViolationException>()).Which;
        ex.Violations.Should().ContainSingle();
        ex.Violations[0].Code.Should().Be(ErrorCodes.ExtraSubjectsCapExceeded);
        ex.Violations[0].Code.Should().Be("LIMITE_SIGUIENTE_SEMESTRE_EXCEDIDO");
        ex.Violations[0].MateriaId.Should().Be(4);
        _transaction.Verify(t => t.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNothingPersisted();
    }

    [Fact]
    public async Task Handle_ThreeExtraSubjects_AreEnrolled()
    {
        GivenStudent(TestData.Student(semestreActual: 2));
        GivenLockedMaterias(
            Enumerable.Range(1, 3)
                .Select(i => TestData.Materia(i, semestre: 3, horarios: [TestData.Block(DiaSemana.Martes, 6 + i * 2, 8 + i * 2)]))
                .ToArray());

        var result = await _handler.Handle(new EnrollCommand(1, TestData.Period, [1, 2, 3]), CancellationToken.None);

        result.Should().HaveCount(3);
        _transaction.Verify(t => t.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ActiveExtrasCountTowardsTheCap()
    {
        GivenStudent(TestData.Student(semestreActual: 2));
        GivenLockedMaterias(TestData.Materia(5, semestre: 3));
        _inscripciones
            .Setup(i => i.GetActiveWithScheduleAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Inscripcion>
            {
                new() { MateriaId = 1, Materia = TestData.Materia(1, semestre: 3) },
                new() { MateriaId = 2, Materia = TestData.Materia(2, semestre: 3) },
                new() { MateriaId = 3, Materia = TestData.Materia(3, semestre: 3) }
            });

        var act = () => _handler.Handle(new EnrollCommand(1, TestData.Period, [5]), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleViolationException>()).Which.Violations
            .Should().ContainSingle().Which.Code.Should().Be(ErrorCodes.ExtraSubjectsCapExceeded);
        VerifyNothingPersisted();
    }

    [Fact]
    public async Task Handle_TwoSemestersAhead_ThrowsSemesterExceeded()
    {
        GivenStudent(TestData.Student(semestreActual: 2));
        GivenLockedMaterias(TestData.Materia(1, semestre: 4));

        var act = () => _handler.Handle(new EnrollCommand(1, TestData.Period, [1]), CancellationToken.None);

        (await act.Should().ThrowAsync<BusinessRuleViolationException>()).Which.Violations
            .Should().ContainSingle().Which.Code.Should().Be(ErrorCodes.SemesterExceeded);
    }

    [Fact]
    public async Task Handle_CodigosMaterias_AreResolvedToIdsPreservingRequestOrder()
    {
        GivenStudent(TestData.Student());
        GivenCodes(("603201", 6), ("603202", 7));
        GivenLockedMaterias(
            TestData.Materia(6, semestre: 3, horarios: [TestData.Block(DiaSemana.Lunes, 8, 10)]),
            TestData.Materia(7, semestre: 3, horarios: [TestData.Block(DiaSemana.Lunes, 10, 12)]));

        List<Inscripcion>? added = null;
        _inscripciones
            .Setup(i => i.AddRange(It.IsAny<IEnumerable<Inscripcion>>()))
            .Callback<IEnumerable<Inscripcion>>(items => added = items.ToList());

        var result = await _handler.Handle(
            new EnrollCommand(1, TestData.Period, null, ["603202", "603201"]), CancellationToken.None);

        added!.Select(i => i.MateriaId).Should().Equal(7, 6);
        result.Select(r => r.MateriaId).Should().Equal(7, 6);
        _materias.Verify(
            m => m.LockByIdsAsync(
                It.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 6, 7 })),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CodigosMaterias_AreResolvedBeforeTheTransactionBegins()
    {
        var calls = new List<string>();
        _materias
            .Setup(m => m.GetIdsByCodigosAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("resolve-codes"))
            .ReturnsAsync(new Dictionary<string, int> { ["603201"] = 1 });
        _unitOfWork
            .Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("begin"))
            .ReturnsAsync(_transaction.Object);
        _students
            .Setup(s => s.LockByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("lock-student"))
            .ReturnsAsync(TestData.Student());
        _materias
            .Setup(m => m.LockByIdsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("lock-materias"))
            .ReturnsAsync(new List<Materia> { TestData.Materia(1) });

        await _handler.Handle(new EnrollCommand(1, TestData.Period, null, ["603201"]), CancellationToken.None);

        calls.Should().Equal("resolve-codes", "begin", "lock-student", "lock-materias");
    }

    [Fact]
    public async Task Handle_UnknownCodigo_ThrowsNotFoundBeforeOpeningTheTransaction()
    {
        GivenStudent(TestData.Student());
        GivenCodes(("603201", 6));

        var act = () => _handler.Handle(
            new EnrollCommand(1, TestData.Period, null, ["603201", "NOPE99"]), CancellationToken.None);

        var ex = (await act.Should().ThrowAsync<NotFoundException>()).Which;
        ex.Code.Should().Be(AppErrorCodes.MateriaNotFound);
        ex.Message.Should().Contain("NOPE99").And.NotContain("603201");
        _unitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        VerifyNothingPersisted();
    }

    [Fact]
    public async Task Handle_MateriaIds_DoNotResolveCodigos()
    {
        GivenStudent(TestData.Student());
        GivenLockedMaterias(TestData.Materia(1));

        await _handler.Handle(new EnrollCommand(1, TestData.Period, [1]), CancellationToken.None);

        _materias.Verify(
            m => m.GetIdsByCodigosAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
