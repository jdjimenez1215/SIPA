using MsInscripcion.Domain.Enums;

namespace MsInscripcion.Domain.Entities;

public class HorarioMateria
{
    public int Id { get; set; }
    public int MateriaId { get; set; }
    public DiaSemana DiaSemana { get; set; }
    public TimeOnly HoraInicio { get; set; }
    public TimeOnly HoraFin { get; set; }

    public Materia? Materia { get; set; }

    /// <summary>
    /// Half-open interval overlap on the same day: back-to-back blocks do NOT overlap.
    /// </summary>
    public bool OverlapsWith(HorarioMateria other) =>
        DiaSemana == other.DiaSemana
        && HoraInicio < other.HoraFin
        && other.HoraInicio < HoraFin;
}
