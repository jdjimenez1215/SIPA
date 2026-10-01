using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Infrastructure.Persistence.Seed;

namespace MsInscripcion.Infrastructure.Persistence.Configurations;

internal sealed class InscripcionConfiguration : IEntityTypeConfiguration<Inscripcion>
{
    public const string ActiveIndexName = "ux_inscripciones_activa";

    public void Configure(EntityTypeBuilder<Inscripcion> builder)
    {
        builder.ToTable("inscripciones");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: SeedData.IdentityStart);
        builder.Property(x => x.PeriodoAcademico).HasMaxLength(10).IsRequired();
        // Stored as text, so the filter literal below ('Activa') matches EstadoInscripcion.Activa.
        builder.Property(x => x.Estado).HasConversion<string>().HasMaxLength(15).IsRequired();
        builder.Property(x => x.FechaInscripcion).HasColumnType("timestamp with time zone");

        // Race-condition safety net: at most one Activa enrollment per (student, materia, period).
        builder.HasIndex(x => new { x.EstudianteId, x.MateriaId, x.PeriodoAcademico })
            .IsUnique()
            .HasDatabaseName(ActiveIndexName)
            .HasFilter("estado = 'Activa'");
        builder.HasIndex(x => new { x.MateriaId, x.PeriodoAcademico });

        builder.HasOne(x => x.Estudiante).WithMany(x => x.Inscripciones).HasForeignKey(x => x.EstudianteId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Materia).WithMany().HasForeignKey(x => x.MateriaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(SeedData.Inscripciones);
    }
}
