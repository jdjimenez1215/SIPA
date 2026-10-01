using MsInscripcion.Domain.Enums;

namespace MsInscripcion.Domain.Entities;

public class HistorialAcademico
{
    public int Id { get; set; }
    public int EstudianteId { get; set; }
    public int MateriaId { get; set; }
    public EstadoHistorial Estado { get; set; }
    public decimal? Nota { get; set; }
    public string Periodo { get; set; } = string.Empty;

    public Estudiante? Estudiante { get; set; }
    public Materia? Materia { get; set; }
}
