using MiniIdentityApi.Domain.Entities;

namespace MiniIdentityApi.Application.DTOs.Users;

/// <summary>
/// Safe projection of <see cref="User"/> for API responses.
/// It deliberately excludes <see cref="Credential"/> (password hash and salt).
/// </summary>
public sealed class UserDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = [];

    public static UserDto FromEntity(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        Status = user.Status.ToString(),
        Roles = user.Roles.Select(role => role.Name).ToList()
    };
}
