using Microsoft.EntityFrameworkCore;
using MiniIdentityApi.Domain.Entities;

namespace MiniIdentityApi.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL persistence for identity. Used when ConnectionStrings:Default is configured;
/// otherwise the API falls back to the in-memory repositories (local demo/tests).
/// </summary>
public class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Username).HasMaxLength(50).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(254).IsRequired();
            entity.Property(user => user.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            entity.Property(user => user.EstudianteId);
            entity.HasIndex(user => user.Username).IsUnique();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasIndex(user => user.EstudianteId);

            entity.OwnsOne(user => user.Credential, credential =>
            {
                credential.Property(c => c.PasswordHash).HasColumnName("password_hash").IsRequired();
                credential.Property(c => c.Salt).HasColumnName("salt").IsRequired();
                credential.Property(c => c.LastChangedAt).HasColumnName("last_changed_at").IsRequired();
            });

            entity.HasMany(user => user.Roles)
                .WithMany()
                .UsingEntity(join => join.ToTable("user_roles"));
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(role => role.Name).IsUnique();
            entity.HasMany(role => role.Permissions)
                .WithOne()
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(permission => permission.Id);
            entity.Property(permission => permission.Code).HasMaxLength(100).IsRequired();
            entity.Property(permission => permission.Description).HasMaxLength(255);
        });
    }
}
