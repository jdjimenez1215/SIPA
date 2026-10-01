using MsInscripcion.Domain.Entities;
using MsInscripcion.Domain.Enums;

namespace MsInscripcion.Application.Features.Horarios.Dtos;

public sealed record HorarioDto(int Id, int MateriaId, DiaSemana DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin);

public static class HorarioMappings
{
    public static HorarioDto ToDto(this HorarioMateria h) =>
        new(h.Id, h.MateriaId, h.DiaSemana, h.HoraInicio, h.HoraFin);
}
