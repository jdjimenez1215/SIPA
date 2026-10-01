namespace MsInscripcion.Domain.Entities;

public class Materia
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int Creditos { get; set; }
    public int CarreraId { get; set; }
    public int Semestre { get; set; }
    public int CuposMaximos { get; set; }

    public Carrera? Carrera { get; set; }
    public ICollection<HorarioMateria> Horarios { get; set; } = new List<HorarioMateria>();

    /// <summary>Prerequisites of this materia (rows where MateriaId == Id).</summary>
    public ICollection<Prerrequisito> Prerrequisitos { get; set; } = new List<Prerrequisito>();
}
