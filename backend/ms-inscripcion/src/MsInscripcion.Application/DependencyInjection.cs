using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MsInscripcion.Application.Common.Behaviors;
using MsInscripcion.Application.Common.Options;
using MsInscripcion.Domain.Rules;
using MsInscripcion.Domain.Services;

namespace MsInscripcion.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        // One rule = one Strategy. Rules are stateless, so singletons are safe.
        services.AddSingleton<IEnrollmentRule, CareerMatchRule>();
        services.AddSingleton<IEnrollmentRule, PrerequisitesRule>();
        services.AddSingleton<IEnrollmentRule, ScheduleOverlapRule>();
        services.AddSingleton<IEnrollmentRule, SemesterWindowRule>();
        services.AddSingleton<IEnrollmentRule, ExtraSubjectsCapRule>();
        services.AddSingleton<IEnrollmentRule, NoDuplicateRule>();
        services.AddSingleton<IEnrollmentRule, SeatAvailabilityRule>();
        services.AddSingleton<EnrollmentRulesEngine>();
        services.AddSingleton<EnrollmentSuggestionCalculator>();

        services.TryAddSingleton(TimeProvider.System);

        var section = configuration.GetSection(EnrollmentOptions.SectionName);
        services.AddOptions<EnrollmentOptions>()
            .Bind(section)
            .Validate(o => o.Validate().Count == 0, "Configuración 'Enrollment' inválida.");

        // Fail fast at startup: options are validated eagerly instead of on first use.
        var errors = (section.Get<EnrollmentOptions>() ?? new EnrollmentOptions()).Validate();
        if (errors.Count > 0)
            throw new InvalidOperationException(
                "Configuración 'Enrollment' inválida: " + string.Join(" ", errors));

        return services;
    }
}
