using MiniIdentityApi.Application.Interfaces;
using MiniIdentityApi.Domain.Entities;

namespace MiniIdentityApi.Infrastructure.Repositories;

public class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public User? FindById(Guid id) => _users.FirstOrDefault(u => u.Id == id);

    public User? FindByUsernameOrEmail(string value)
    {
        return _users.FirstOrDefault(u =>
            u.Username.Equals(value, StringComparison.OrdinalIgnoreCase) ||
            u.Email.Equals(value, StringComparison.OrdinalIgnoreCase));
    }

    public List<User> GetAll() => _users;

    public void Save(User user)
    {
        // Upsert: services save both new and existing (modified) users.
        var index = _users.FindIndex(existing => existing.Id == user.Id);
        if (index >= 0)
            _users[index] = user;
        else
            _users.Add(user);
    }
}