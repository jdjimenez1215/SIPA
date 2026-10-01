using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Infrastructure.Persistence.Seed;

namespace MsInscripcion.Infrastructure.Persistence.Configurations;

internal sealed class CarreraConfiguration : IEntityTypeConfiguration<Carrera>
{
    public void Configure(EntityTypeBuilder<Carrera> builder)
    {
        builder.ToTable("carreras");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: SeedData.IdentityStart);
        builder.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        builder.HasIndex(x => x.Codigo).IsUnique().HasDatabaseName("ux_carreras_codigo");

        builder.HasMany(x => x.Materias).WithOne(x => x.Carrera).HasForeignKey(x => x.CarreraId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Estudiantes).WithOne(x => x.Carrera).HasForeignKey(x => x.CarreraId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(SeedData.Carreras);
    }
}
