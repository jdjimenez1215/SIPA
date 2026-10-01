using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Infrastructure.Persistence.Seed;

namespace MsInscripcion.Infrastructure.Persistence.Configurations;

internal sealed class MateriaConfiguration : IEntityTypeConfiguration<Materia>
{
    public void Configure(EntityTypeBuilder<Materia> builder)
    {
        builder.ToTable("materias");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: SeedData.IdentityStart);
        builder.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => x.Codigo).IsUnique().HasDatabaseName("ux_materias_codigo");
        builder.HasIndex(x => x.CarreraId);

        // Horarios and the materia's own prerequisites disappear with it.
        builder.HasMany(x => x.Horarios).WithOne(x => x.Materia).HasForeignKey(x => x.MateriaId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Prerrequisitos).WithOne(x => x.Materia).HasForeignKey(x => x.MateriaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(SeedData.Materias);
    }
}
