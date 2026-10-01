using MsInscripcion.Application.Features.Horarios.Dtos;
using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Application.Features.Materias.Dtos;

public sealed record MateriaDto(
    int Id,
    string Codigo,
    string Nombre,
    int Creditos,
    int CarreraId,
    int Semestre,
    int CuposMaximos,
    IReadOnlyList<HorarioDto> Horarios,
    IReadOnlyList<int> PrerrequisitoIds);

public static class MateriaMappings
{
    public static MateriaDto ToDto(this Materia m) => new(
        m.Id,
        m.Codigo,
        m.Nombre,
        m.Creditos,
        m.CarreraId,
        m.Semestre,
        m.CuposMaximos,
        m.Horarios.OrderBy(h => h.DiaSemana).ThenBy(h => h.HoraInicio).Select(h => h.ToDto()).ToList(),
        m.Prerrequisitos.Select(p => p.MateriaRequisitoId).OrderBy(id => id).ToList());
}
