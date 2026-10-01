namespace MsInscripcion.Domain.Entities;

public class Carrera
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int DuracionSemestres { get; set; }

    public ICollection<Materia> Materias { get; set; } = new List<Materia>();
    public ICollection<Estudiante> Estudiantes { get; set; } = new List<Estudiante>();
}
