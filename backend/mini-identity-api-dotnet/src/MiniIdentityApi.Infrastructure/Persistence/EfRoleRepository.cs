using Microsoft.EntityFrameworkCore;
using MiniIdentityApi.Application.Interfaces;
using MiniIdentityApi.Domain.Entities;

namespace MiniIdentityApi.Infrastructure.Persistence;

public class EfRoleRepository(IdentityDbContext db) : IRoleRepository
{
    public Role? FindByName(string name)
    {
        var normalized = (name ?? string.Empty).ToLowerInvariant();
        return db.Roles
            .Include(role => role.Permissions)
            .FirstOrDefault(role => role.Name.ToLower() == normalized);
    }

    public List<Role> GetAll() => db.Roles.Include(role => role.Permissions).AsNoTracking().ToList();

    public void Save(Role role)
    {
        if (db.Roles.Any(existing => existing.Id == role.Id))
            db.Roles.Update(role);
        else
            db.Roles.Add(role);

        db.SaveChanges();
    }
}
