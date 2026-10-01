namespace MsInscripcion.Domain.Entities;

/// <summary>Composite key (MateriaId, MateriaRequisitoId): MateriaId requires MateriaRequisitoId.</summary>
public class Prerrequisito
{
    public int MateriaId { get; set; }
    public int MateriaRequisitoId { get; set; }

    public Materia? Materia { get; set; }
    public Materia? MateriaRequisito { get; set; }
}
