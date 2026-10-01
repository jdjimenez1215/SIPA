using Microsoft.EntityFrameworkCore;
using MsInscripcion.Domain.Entities;

namespace MsInscripcion.Infrastructure.Persistence;

public class InscripcionDbContext(DbContextOptions<InscripcionDbContext> options) : DbContext(options)
{
    public DbSet<Carrera> Carreras => Set<Carrera>();
    public DbSet<Materia> Materias => Set<Materia>();
    public DbSet<HorarioMateria> Horarios => Set<HorarioMateria>();
    public DbSet<Prerrequisito> Prerrequisitos => Set<Prerrequisito>();
    public DbSet<Estudiante> Estudiantes => Set<Estudiante>();
    public DbSet<HistorialAcademico> Historial => Set<HistorialAcademico>();
    public DbSet<Inscripcion> Inscripciones => Set<Inscripcion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InscripcionDbContext).Assembly);
    }
}
