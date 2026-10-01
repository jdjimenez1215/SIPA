using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MiniIdentityApi.Application.Interfaces;
using MiniIdentityApi.Application.Services;
using MiniIdentityApi.Domain.Entities;
using MiniIdentityApi.Infrastructure.Repositories;
using MiniIdentityApi.Infrastructure.Security;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

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

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<IRoleRepository, InMemoryRoleRepository>();
builder.Services.AddSingleton<IPasswordHasher, Sha256PasswordHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

builder.Services.AddScoped<AuthenticationService>();
builder.Services.AddScoped<AuthorizationService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<RoleService>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is missing in configuration.");

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
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

var userRepository = app.Services.GetRequiredService<IUserRepository>();
var roleRepository = app.Services.GetRequiredService<IRoleRepository>();
var passwordHasher = app.Services.GetRequiredService<IPasswordHasher>();

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

var adminUser = userRepository.FindByUsernameOrEmail("admin");
if (adminUser is null)
{
    var salt = passwordHasher.GenerateSalt();
    var hash = passwordHasher.Hash("Admin123*", salt);

    var credential = new Credential(hash, salt);
    adminUser = new User("admin", "admin@example.com", credential);
    adminUser.AddRole(adminRole);

    userRepository.Save(adminUser);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();