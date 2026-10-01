using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Infrastructure.Persistence.Seed;

namespace MsInscripcion.Infrastructure.Persistence.Configurations;

internal sealed class EstudianteConfiguration : IEntityTypeConfiguration<Estudiante>
{
    public void Configure(EntityTypeBuilder<Estudiante> builder)
    {
        builder.ToTable("estudiantes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: SeedData.IdentityStart);
        builder.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => x.CarreraId);

        builder.HasData(SeedData.Estudiantes);
    }
}
