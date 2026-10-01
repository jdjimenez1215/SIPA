using FluentAssertions;
using MsInscripcion.Application.Common;
using MsInscripcion.Application.Features.Inscripciones.Commands;
using MsInscripcion.UnitTests.Fixtures;

namespace MsInscripcion.UnitTests.Handlers;

public class EnrollCommandValidatorTests
{
    private readonly EnrollCommandValidator _validator = new();

    private static EnrollCommand Cmd(IReadOnlyList<int>? ids, IReadOnlyList<string>? codes, string periodo = TestData.Period) =>
        new(1, periodo, ids, codes);

    [Fact]
    public void Validate_OnlyMateriaIds_IsValid() =>
        _validator.Validate(Cmd([1, 2], null)).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_OnlyCodigosMaterias_IsValid() =>
        _validator.Validate(Cmd(null, ["603201", "603202"])).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_BothLists_ReportsASingleValidationError()
    {
        var result = _validator.Validate(Cmd([1], ["603201"]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public void Validate_BothListsEvenWithDuplicates_DoesNotReportDuplicateCode()
    {
        var result = _validator.Validate(Cmd([1, 1], ["603201", "603201"]));

        result.Errors.Should().ContainSingle();
        result.Errors.Should().NotContain(e => e.ErrorCode == AppErrorCodes.DuplicateMateriaInRequest);
    }

    [Fact]
    public void Validate_NeitherList_ReportsASingleValidationError()
    {
        var result = _validator.Validate(Cmd(null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
    }

    [Fact]
    public void Validate_BothListsEmpty_ReportsASingleValidationError()
    {
        var result = _validator.Validate(Cmd([], []));

        result.Errors.Should().ContainSingle();
        result.Errors.Should().NotContain(e => e.ErrorCode == AppErrorCodes.DuplicateMateriaInRequest);
    }

    [Fact]
    public void Validate_DuplicateMateriaIds_ReportsDuplicateCode() =>
        _validator.Validate(Cmd([1, 2, 1], null)).Errors
            .Should().Contain(e => e.ErrorCode == AppErrorCodes.DuplicateMateriaInRequest);

    [Fact]
    public void Validate_DuplicateCodigos_ReportsDuplicateCode() =>
        _validator.Validate(Cmd(null, ["603201", "603201"])).Errors
            .Should().Contain(e => e.ErrorCode == AppErrorCodes.DuplicateMateriaInRequest);

    [Fact]
    public void Validate_EmptyCodigo_IsInvalid() =>
        _validator.Validate(Cmd(null, ["603201", " "])).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NonPositiveMateriaId_IsInvalid() =>
        _validator.Validate(Cmd([0], null)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_BadPeriodo_IsInvalid() =>
        _validator.Validate(Cmd([1], null, "2026-3")).IsValid.Should().BeFalse();
}
