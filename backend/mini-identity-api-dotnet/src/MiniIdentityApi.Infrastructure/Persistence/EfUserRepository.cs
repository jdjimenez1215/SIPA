using Microsoft.EntityFrameworkCore;
using MiniIdentityApi.Application.Interfaces;
using MiniIdentityApi.Domain.Entities;

namespace MiniIdentityApi.Infrastructure.Persistence;

public class EfUserRepository(IdentityDbContext db) : IUserRepository
{
    private IQueryable<User> UsersWithRoles =>
        db.Users.Include(user => user.Roles).ThenInclude(role => role.Permissions);

    public User? FindById(Guid id) => UsersWithRoles.FirstOrDefault(user => user.Id == id);

    public User? FindByUsernameOrEmail(string value)
    {
        var normalized = (value ?? string.Empty).ToLowerInvariant();
        return UsersWithRoles.FirstOrDefault(user =>
            user.Username.ToLower() == normalized ||
            user.Email.ToLower() == normalized);
    }

    public List<User> GetAll() => UsersWithRoles.AsNoTracking().ToList();

    public void Save(User user)
    {
        if (db.Users.Any(existing => existing.Id == user.Id))
            db.Users.Update(user);
        else
            db.Users.Add(user);

        db.SaveChanges();
    }
}
