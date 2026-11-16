using FluentAssertions;
using Moq;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Application.Common.Exceptions;
using MsInscripcion.Application.Features.Carreras.Commands;
using MsInscripcion.Application.Features.Horarios.Commands;
using MsInscripcion.Application.Features.Materias.Commands;
using MsInscripcion.Application.Features.Prerrequisitos.Commands;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Enums;
using MsInscripcion.Domain.Exceptions;
using MsInscripcion.UnitTests.Fixtures;

namespace MsInscripcion.UnitTests.Handlers;

public class CatalogHandlerTests
{
    // ---------- Carreras ----------

    [Fact]
    public async Task CreateCarrera_DuplicateCode_ThrowsConflict()
    {
        var carreras = new Mock<ICarreraRepository>();
        carreras.Setup(c => c.ExistsByCodigoAsync("MED", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => new CreateCarreraCommandHandler(carreras.Object, Mock.Of<IUnitOfWork>())
            .Handle(new CreateCarreraCommand("MED", "Medicina", 12), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task CreateCarrera_Valid_AddsAndSaves()
    {
        var carreras = new Mock<ICarreraRepository>();
        var uow = new Mock<IUnitOfWork>();
        carreras.Setup(c => c.ExistsByCodigoAsync("MED", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await new CreateCarreraCommandHandler(carreras.Object, uow.Object)
            .Handle(new CreateCarreraCommand(" MED ", " Medicina ", 12), default);

        result.Codigo.Should().Be("MED");
        result.Nombre.Should().Be("Medicina");
        carreras.Verify(c => c.Add(It.IsAny<Carrera>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCarrera_Unknown_ThrowsNotFound()
    {
        var carreras = new Mock<ICarreraRepository>();
        carreras.Setup(c => c.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Carrera?)null);

        var act = () => new UpdateCarreraCommandHandler(carreras.Object, Mock.Of<IUnitOfWork>())
            .Handle(new UpdateCarreraCommand(99, "X", "X", 1), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteCarrera_Unknown_ThrowsNotFound()
    {
        var carreras = new Mock<ICarreraRepository>();
        carreras.Setup(c => c.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((Carrera?)null);

        var act = () => new DeleteCarreraCommandHandler(carreras.Object, Mock.Of<IUnitOfWork>())
            .Handle(new DeleteCarreraCommand(99), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- Materias ----------

    [Fact]
    public async Task CreateMateria_UnknownCareer_ThrowsNotFound()
    {
        var materias = new Mock<IMateriaRepository>();
        var carreras = new Mock<ICarreraRepository>();
        carreras.Setup(c => c.ExistsAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var act = () => new CreateMateriaCommandHandler(materias.Object, carreras.Object, Mock.Of<IUnitOfWork>())
            .Handle(new CreateMateriaCommand("MAT101", "Materia", 3, 99, 1, 10), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateMateria_DuplicateCode_ThrowsConflict()
    {
        var materias = new Mock<IMateriaRepository>();
        var carreras = new Mock<ICarreraRepository>();
        carreras.Setup(c => c.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        materias.Setup(m => m.ExistsByCodigoAsync("MAT101", null, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var act = () => new CreateMateriaCommandHandler(materias.Object, carreras.Object, Mock.Of<IUnitOfWork>())
            .Handle(new CreateMateriaCommand("MAT101", "Materia", 3, 1, 1, 10), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    // ---------- Horarios ----------

    [Fact]
    public async Task AddHorario_InvertedRange_ThrowsDomainException()
    {
        var materias = new Mock<IMateriaRepository>();
        var horarios = new Mock<IHorarioRepository>();
        materias.Setup(m => m.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        horarios.Setup(h => h.GetByMateriaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var act = () => new AddHorarioCommandHandler(materias.Object, horarios.Object, Mock.Of<IUnitOfWork>())
            .Handle(new AddHorarioCommand(1, DiaSemana.Lunes, TestData.T(12), TestData.T(10)), default);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task AddHorario_OverlapsExistingBlock_ThrowsDomainException()
    {
        var materias = new Mock<IMateriaRepository>();
        var horarios = new Mock<IHorarioRepository>();
        materias.Setup(m => m.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var existing = TestData.Block(DiaSemana.Lunes, 10, 12, 1);
        existing.Id = 1;
        horarios.Setup(h => h.GetByMateriaAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync([existing]);

        var act = () => new AddHorarioCommandHandler(materias.Object, horarios.Object, Mock.Of<IUnitOfWork>())
            .Handle(new AddHorarioCommand(1, DiaSemana.Lunes, TestData.T(11), TestData.T(13)), default);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task AddHorario_AdjacentBlock_IsAllowed()
    {
        var materias = new Mock<IMateriaRepository>();
        var horarios = new Mock<IHorarioRepository>();
        var uow = new Mock<IUnitOfWork>();
        materias.Setup(m => m.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        horarios.Setup(h => h.GetByMateriaAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync([TestData.Block(DiaSemana.Lunes, 10, 12, 1)]);

        var result = await new AddHorarioCommandHandler(materias.Object, horarios.Object, uow.Object)
            .Handle(new AddHorarioCommand(1, DiaSemana.Lunes, TestData.T(12), TestData.T(14)), default);

        result.HoraInicio.Should().Be(TestData.T(12));
        horarios.Verify(h => h.Add(It.IsAny<HorarioMateria>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteHorario_WrongMateria_ThrowsNotFound()
    {
        var horarios = new Mock<IHorarioRepository>();
        horarios.Setup(h => h.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HorarioMateria { Id = 5, MateriaId = 2 });

        var act = () => new DeleteHorarioCommandHandler(horarios.Object, Mock.Of<IUnitOfWork>())
            .Handle(new DeleteHorarioCommand(1, 5), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ---------- Prerrequisitos ----------

    [Fact]
    public async Task AddPrerrequisito_SelfReference_ThrowsDomainException()
    {
        var materias = new Mock<IMateriaRepository>();
        var prereqs = new Mock<IPrerrequisitoRepository>();
        materias.Setup(m => m.ExistsAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        prereqs.Setup(p => p.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var act = () => new AddPrerrequisitoCommandHandler(materias.Object, prereqs.Object, Mock.Of<IUnitOfWork>())
            .Handle(new AddPrerrequisitoCommand(1, 1), default);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task AddPrerrequisito_Duplicate_ThrowsConflict()
    {
        var materias = new Mock<IMateriaRepository>();
        var prereqs = new Mock<IPrerrequisitoRepository>();
        materias.Setup(m => m.ExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var existing = new Prerrequisito { MateriaId = 2, MateriaRequisitoId = 1 };
        prereqs.Setup(p => p.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([existing]);

        var act = () => new AddPrerrequisitoCommandHandler(materias.Object, prereqs.Object, Mock.Of<IUnitOfWork>())
            .Handle(new AddPrerrequisitoCommand(2, 1), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task AddPrerrequisito_IndirectCycle_ThrowsDomainException()
    {
        var materias = new Mock<IMateriaRepository>();
        var prereqs = new Mock<IPrerrequisitoRepository>();
        materias.Setup(m => m.ExistsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        prereqs.Setup(p => p.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            new Prerrequisito { MateriaId = 2, MateriaRequisitoId = 3 },
            new Prerrequisito { MateriaId = 3, MateriaRequisitoId = 1 }
        ]);

        var act = () => new AddPrerrequisitoCommandHandler(materias.Object, prereqs.Object, Mock.Of<IUnitOfWork>())
            .Handle(new AddPrerrequisitoCommand(1, 2), default);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task DeletePrerrequisito_Unknown_ThrowsNotFound()
    {
        var prereqs = new Mock<IPrerrequisitoRepository>();
        prereqs.Setup(p => p.FindAsync(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync((Prerrequisito?)null);

        var act = () => new DeletePrerrequisitoCommandHandler(prereqs.Object, Mock.Of<IUnitOfWork>())
            .Handle(new DeletePrerrequisitoCommand(1, 2), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
