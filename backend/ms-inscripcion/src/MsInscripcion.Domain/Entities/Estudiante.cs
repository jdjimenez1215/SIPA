namespace MsInscripcion.Domain.Entities;

public class Estudiante
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int CarreraId { get; set; }
    public int SemestreActual { get; set; }

    public Carrera? Carrera { get; set; }
    public ICollection<HistorialAcademico> Historial { get; set; } = new List<HistorialAcademico>();
    public ICollection<Inscripcion> Inscripciones { get; set; } = new List<Inscripcion>();
}
