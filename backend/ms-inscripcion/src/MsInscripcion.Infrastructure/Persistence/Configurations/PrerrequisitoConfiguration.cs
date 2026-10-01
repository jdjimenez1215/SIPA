using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Infrastructure.Persistence.Seed;

namespace MsInscripcion.Infrastructure.Persistence.Configurations;

internal sealed class PrerrequisitoConfiguration : IEntityTypeConfiguration<Prerrequisito>
{
    public void Configure(EntityTypeBuilder<Prerrequisito> builder)
    {
        builder.ToTable("prerrequisitos", t => t.HasCheckConstraint(
            "ck_prerrequisitos_no_self", "materia_id <> materia_requisito_id"));
        builder.HasKey(x => new { x.MateriaId, x.MateriaRequisitoId });

        // FK materia_id -> materias (Cascade) is configured from MateriaConfiguration.
        // A materia used as requirement by another cannot be deleted (Restrict -> 23503 -> ENTIDAD_EN_USO).
        builder.HasOne(x => x.MateriaRequisito).WithMany().HasForeignKey(x => x.MateriaRequisitoId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.MateriaRequisitoId);

        builder.HasData(SeedData.Prerrequisitos);
    }
}
