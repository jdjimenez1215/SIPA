using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MsAsistente.Api;
using Xunit;

namespace MsAsistente.Tests;

public class AsistenteServiceTests
{
    private static MateriaSugerida Materia(
        int id, int creditos, int semestre, string estado = "Sugerida", string? motivo = null,
        params string[] dias) =>
        new(id, $"M{id}", $"Materia {id}", creditos, semestre, estado, motivo is null, motivo, 30, 30,
            dias.Select(d => new HorarioDto(d, "14:00", "16:00")).ToList());

    private static SugerenciaDto Sugerencia(params MateriaSugerida[] materias) => new(
        "Laura", "Ingeniería de Sistemas", 6, "2026-2", 18, 3.8m, 18, materias);

    private sealed class FakeLlm(Func<LlmPropuesta> response) : IAsistenteLlm
    {
        public Task<LlmPropuesta> ProponerAsync(AsistenteContexto contexto, CancellationToken ct = default) =>
            Task.FromResult(response());
    }

    private sealed class ThrowingLlm : IAsistenteLlm
    {
        public Task<LlmPropuesta> ProponerAsync(AsistenteContexto contexto, CancellationToken ct = default) =>
            throw new HttpRequestException("LLM down");
    }

    [Fact]
    public async Task UsesLlmProposal_AndFiltersIdsNotInSuggestion()
    {
        var service = new AsistenteService(
            new FakeLlm(() => new LlmPropuesta("Listo", [1, 999, 2])), NullLogger<AsistenteService>.Instance);
        var sugerencia = Sugerencia(Materia(1, 3, 6), Materia(2, 3, 7));

        var result = await service.ResponderAsync(sugerencia, "qué matriculo", default);

        result.MateriaIds.Should().Equal(1, 2);
        result.Fallback.Should().BeFalse();
    }

    [Fact]
    public async Task RespectsCreditLimit()
    {
        var service = new AsistenteService(
            new FakeLlm(() => new LlmPropuesta("Listo", [1, 2, 3])), NullLogger<AsistenteService>.Instance);
        var sugerencia = Sugerencia(Materia(1, 10, 6), Materia(2, 10, 6), Materia(3, 3, 6));

        var result = await service.ResponderAsync(sugerencia, "qué matriculo", default);

        result.MateriaIds.Should().Equal(1, 3);
    }

    [Fact]
    public async Task FallsBackWhenLlmFails()
    {
        var service = new AsistenteService(new ThrowingLlm(), NullLogger<AsistenteService>.Instance);
        var sugerencia = Sugerencia(Materia(1, 3, 6), Materia(2, 10, 6), Materia(3, 3, 7));

        var result = await service.ResponderAsync(sugerencia, "qué matriculo", default);

        result.Fallback.Should().BeTrue();
        result.MateriaIds.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task ExplainsBlockedSubjects()
    {
        var service = new AsistenteService(
            new FakeLlm(() => new LlmPropuesta("Listo", [1])), NullLogger<AsistenteService>.Instance);
        var sugerencia = Sugerencia(
            Materia(1, 3, 6),
            Materia(2, 3, 7, "Prerrequisito", "PREREQUISITO_NO_CUMPLIDO"),
            Materia(3, 3, 7, "Prerrequisito", "CUPO_AGOTADO"));

        var result = await service.ResponderAsync(sugerencia, "por qué no puedo", default);

        result.Explicaciones.Should().HaveCount(2);
        result.Explicaciones.Should().Contain(e => e.Codigo == "M2" && e.Mensaje.Contains("prerrequisito"));
        result.Explicaciones.Should().Contain(e => e.Codigo == "M3" && e.Mensaje.Contains("cupos"));
    }

    [Fact]
    public async Task ExcludesSubjectsOnDaysTheStudentDoesNotWant()
    {
        var service = new AsistenteService(
            new FakeLlm(() => new LlmPropuesta("Listo", [1, 2])), NullLogger<AsistenteService>.Instance);
        var sugerencia = Sugerencia(
            Materia(1, 3, 6, dias: "Lunes"),
            Materia(2, 3, 6, dias: "Martes"));

        var result = await service.ResponderAsync(sugerencia, "no quiero estudiar los lunes", default);

        result.MateriaIds.Should().Equal(2);
        result.Mensaje.Should().Contain("con clase en Lunes");
    }

    [Fact]
    public async Task ExcludesSubjectsThatStartBeforeRequestedTime()
    {
        var service = new AsistenteService(
            new FakeLlm(() => new LlmPropuesta("Listo", [1, 2])), NullLogger<AsistenteService>.Instance);
        var early = new MateriaSugerida(1, "M1", "Temprana", 3, 6, "Sugerida", true, null, 30, 30,
            [new HorarioDto("Lunes", "07:00", "09:00")]);
        var late = new MateriaSugerida(2, "M2", "Tarde", 3, 6, "Sugerida", true, null, 30, 30,
            [new HorarioDto("Lunes", "14:00", "16:00")]);

        var result = await service.ResponderAsync(Sugerencia(early, late), "solo después de las 10am", default);

        result.MateriaIds.Should().Equal(2);
        result.Mensaje.Should().Contain("10:00");
    }

    [Fact]
    public async Task ReturnsNoSubjectsWhenAllRemainingAreOnExcludedDays()
    {
        var service = new AsistenteService(
            new FakeLlm(() => new LlmPropuesta("Listo", [1])), NullLogger<AsistenteService>.Instance);
        var sugerencia = Sugerencia(Materia(1, 3, 6, dias: "Lunes"));

        var result = await service.ResponderAsync(sugerencia, "no puedo los lunes", default);

        result.MateriaIds.Should().BeEmpty();
        result.Mensaje.Should().Contain("no quedan materias");
    }

    [Fact]
    public async Task EmptyLlmProposal_UsesRulesFallback()
    {
        var service = new AsistenteService(
            new FakeLlm(() => new LlmPropuesta("", [])), NullLogger<AsistenteService>.Instance);
        var sugerencia = Sugerencia(Materia(1, 3, 6), Materia(2, 4, 6));

        var result = await service.ResponderAsync(sugerencia, "qué matriculo", default);

        result.Fallback.Should().BeTrue();
        result.MateriaIds.Should().Equal(1, 2);
    }
}
