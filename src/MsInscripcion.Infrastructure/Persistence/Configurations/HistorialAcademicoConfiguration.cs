using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Infrastructure.Persistence.Seed;

namespace MsInscripcion.Infrastructure.Persistence.Configurations;

internal sealed class HistorialAcademicoConfiguration : IEntityTypeConfiguration<HistorialAcademico>
{
    public void Configure(EntityTypeBuilder<HistorialAcademico> builder)
    {
        builder.ToTable("historial_academico");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: SeedData.IdentityStart);
        builder.Property(x => x.Estado).HasConversion<string>().HasMaxLength(15).IsRequired();
        builder.Property(x => x.Nota).HasPrecision(4, 2);
        builder.Property(x => x.Periodo).HasMaxLength(10).IsRequired();
        builder.HasIndex(x => new { x.EstudianteId, x.MateriaId, x.Periodo }).IsUnique()
            .HasDatabaseName("ux_historial_estudiante_materia_periodo");
        builder.HasIndex(x => x.MateriaId);

        builder.HasOne(x => x.Estudiante).WithMany(x => x.Historial).HasForeignKey(x => x.EstudianteId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Materia).WithMany().HasForeignKey(x => x.MateriaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(SeedData.Historial);
    }
}
