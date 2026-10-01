using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MsInscripcion.Domain.Entities;
using MsInscripcion.Infrastructure.Persistence.Seed;

namespace MsInscripcion.Infrastructure.Persistence.Configurations;

internal sealed class HorarioMateriaConfiguration : IEntityTypeConfiguration<HorarioMateria>
{
    public void Configure(EntityTypeBuilder<HorarioMateria> builder)
    {
        builder.ToTable("horario_materia", t => t.HasCheckConstraint("ck_horario_materia_rango", "hora_inicio < hora_fin"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: SeedData.IdentityStart);
        builder.Property(x => x.DiaSemana).HasConversion<string>().HasMaxLength(15).IsRequired();
        builder.Property(x => x.HoraInicio).HasColumnType("time without time zone");
        builder.Property(x => x.HoraFin).HasColumnType("time without time zone");
        builder.HasIndex(x => x.MateriaId);

        builder.HasData(SeedData.Horarios);
    }
}
