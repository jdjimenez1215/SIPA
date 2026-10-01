using MsInscripcion.Application.Features.Horarios.Dtos;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Enums;

namespace MsInscripcion.Application.Features.Inscripciones.Dtos;

public sealed record InscripcionMateriaDto(
    int Id,
    string Codigo,
    string Nombre,
    int Creditos,
    int Semestre,
    IReadOnlyList<HorarioDto> Horarios);

public sealed record InscripcionDto(
    int Id,
    int EstudianteId,
    int MateriaId,
    string PeriodoAcademico,
    EstadoInscripcion Estado,
    DateTimeOffset FechaInscripcion,
    InscripcionMateriaDto? Materia);

public static class InscripcionMappings
{
    public static InscripcionDto ToDto(this Inscripcion i, Materia? materia = null)
    {
        var m = materia ?? i.Materia;
        return new InscripcionDto(
            i.Id,
            i.EstudianteId,
            i.MateriaId,
            i.PeriodoAcademico,
            i.Estado,
            i.FechaInscripcion,
            m is null
                ? null
                : new InscripcionMateriaDto(
                    m.Id,
                    m.Codigo,
                    m.Nombre,
                    m.Creditos,
                    m.Semestre,
                    m.Horarios.OrderBy(h => h.DiaSemana).ThenBy(h => h.HoraInicio).Select(h => h.ToDto()).ToList()));
    }
}
