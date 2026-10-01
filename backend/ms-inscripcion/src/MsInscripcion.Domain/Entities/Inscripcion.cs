using MsInscripcion.Domain.Enums;

namespace MsInscripcion.Domain.Entities;

public class Inscripcion
{
    public int Id { get; set; }
    public int EstudianteId { get; set; }
    public int MateriaId { get; set; }
    public string PeriodoAcademico { get; set; } = string.Empty;
    public EstadoInscripcion Estado { get; set; } = EstadoInscripcion.Activa;
    public DateTimeOffset FechaInscripcion { get; set; }

    public Estudiante? Estudiante { get; set; }
    public Materia? Materia { get; set; }
}
