using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MsInscripcion.Application.Abstractions.Persistence;
using MsInscripcion.Infrastructure.Persistence;
using MsInscripcion.Infrastructure.Persistence.Repositories;

namespace MsInscripcion.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'ConnectionStrings:Default'.");

        services.AddDbContext<InscripcionDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IMateriaRepository, MateriaRepository>();
        services.AddScoped<ICarreraRepository, CarreraRepository>();
        services.AddScoped<IStudentRepository, StudentRepository>();
        services.AddScoped<IInscripcionRepository, InscripcionRepository>();
        services.AddScoped<IHorarioRepository, HorarioRepository>();
        services.AddScoped<IPrerrequisitoRepository, PrerrequisitoRepository>();

        return services;
    }
}
