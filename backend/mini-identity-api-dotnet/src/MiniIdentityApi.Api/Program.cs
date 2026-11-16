using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MiniIdentityApi.Application.Interfaces;
using MiniIdentityApi.Application.Services;
using MiniIdentityApi.Domain.Entities;
using MiniIdentityApi.Infrastructure.Persistence;
using MiniIdentityApi.Infrastructure.Repositories;
using MiniIdentityApi.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Liveness/readiness probes (anonymous, used by Docker healthcheck and CI).
builder.Services.AddHealthChecks();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MiniIdentity API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token.",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });

    options.AddSecurityRequirement(document => new()
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

// With a connection string, users and roles are persisted in PostgreSQL.
// Without one (unit tests, quick local run), the in-memory repositories are used.
var identityConnectionString = builder.Configuration.GetConnectionString("Default");
var useDatabase = !string.IsNullOrWhiteSpace(identityConnectionString);

if (useDatabase)
{
    builder.Services.AddDbContext<IdentityDbContext>(options => options.UseNpgsql(identityConnectionString));
    builder.Services.AddScoped<IUserRepository, EfUserRepository>();
    builder.Services.AddScoped<IRoleRepository, EfRoleRepository>();
}
else
{
    builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
    builder.Services.AddSingleton<IRoleRepository, InMemoryRoleRepository>();
}

builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<AuthorizationService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RoleService>();

// The signing key must never live in source control: use the JWT_KEY environment variable
// (or Jwt__Key in configuration) or `dotnet user-secrets`. appsettings.Development.json only
// carries a throwaway key that is rejected outside Development.
// JWT_KEY wins over configuration so both processes can share one key in any environment.
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
    ?? builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException(
        "Jwt:Key is missing. Set the JWT_KEY environment variable (Jwt__Key) or use `dotnet user-secrets`.");

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException(
        "Jwt:Key must be at least 32 bytes (256 bits) long. Generate one with: openssl rand -base64 48");

if (!builder.Environment.IsDevelopment() && jwtKey.StartsWith("dev-only-", StringComparison.Ordinal))
    throw new InvalidOperationException(
        "Jwt:Key is the development key. Set a real secret through the JWT_KEY environment variable.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is missing in configuration.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is missing in configuration.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// Brute-force protection: at most 5 login attempts per minute per client IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});

var app = builder.Build();

if (useDatabase && app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
{
    await using var migrationScope = app.Services.CreateAsyncScope();
    var db = migrationScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    await db.Database.MigrateAsync();
}

await using (var seedScope = app.Services.CreateAsyncScope())
{
    var userRepository = seedScope.ServiceProvider.GetRequiredService<IUserRepository>();
    var roleRepository = seedScope.ServiceProvider.GetRequiredService<IRoleRepository>();
    var passwordHasher = seedScope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    var adminRole = roleRepository.FindByName("Admin");
    if (adminRole is null)
    {
        adminRole = new Role("Admin");
        adminRole.AddPermission(new Permission("users.read", "Can read users"));
        adminRole.AddPermission(new Permission("users.manage", "Can manage users"));
        adminRole.AddPermission(new Permission("roles.read", "Can read roles"));
        adminRole.AddPermission(new Permission("roles.manage", "Can manage roles"));

        roleRepository.Save(adminRole);
    }

    var studentRole = roleRepository.FindByName("Estudiante");
    if (studentRole is null)
    {
        studentRole = new Role("Estudiante");
        roleRepository.Save(studentRole);
    }

    var adminUser = userRepository.FindByUsernameOrEmail("admin");
    if (adminUser is null)
    {
        var salt = passwordHasher.GenerateSalt();
        var hash = passwordHasher.Hash("Admin123*", salt);

        adminUser = new User("admin", "admin@example.com", new Credential(hash, salt));
        adminUser.AddRole(adminRole);
        userRepository.Save(adminUser);
    }

    // Demo student account: can only read/enroll for estudianteId 1 (Laura).
    var studentUser = userRepository.FindByUsernameOrEmail("laura");
    if (studentUser is null)
    {
        var salt = passwordHasher.GenerateSalt();
        var hash = passwordHasher.Hash("Laura123*", salt);

        studentUser = new User("laura", "laura@example.com", new Credential(hash, salt), estudianteId: 1);
        studentUser.AddRole(studentRole);
        userRepository.Save(studentUser);
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Domain/service exceptions become ProblemDetails instead of opaque 500s.
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var (status, title, code) = error switch
    {
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Credenciales inválidas", "CREDENCIALES_INVALIDAS"),
        ArgumentException => (StatusCodes.Status400BadRequest, "Solicitud inválida", "VALIDACION_FALLIDA"),
        KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso no encontrado", "RECURSO_NO_ENCONTRADO"),
        InvalidOperationException => (StatusCodes.Status409Conflict, "Conflicto", "CONFLICTO"),
        _ => (StatusCodes.Status500InternalServerError, "Error interno", "ERROR_INTERNO")
    };

    var problem = new ProblemDetails
    {
        Status = status,
        Title = title,
        Detail = status == StatusCodes.Status500InternalServerError
            ? "Ocurrió un error inesperado."
            : error?.Message,
        Instance = context.Request.Path
    };
    problem.Extensions["code"] = code;

    context.Response.StatusCode = status;
    context.Response.ContentType = "application/problem+json";
    await context.Response.WriteAsJsonAsync(problem);
}));

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/ready");

app.Run();