using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Application.Features.Prerrequisitos.Dtos;

/// <summary>MateriaId requires MateriaRequisitoId.</summary>
public sealed record PrerrequisitoDto(int MateriaId, int MateriaRequisitoId);

public static class PrerrequisitoMappings
{
    public static PrerrequisitoDto ToDto(this Prerrequisito p) => new(p.MateriaId, p.MateriaRequisitoId);
}
