using MiniIdentityApi.Domain.Enums;
using System.Data;
using System.Net;

namespace MiniIdentityApi.Domain.Entities;

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Username { get; private set; }
    public string Email { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public Credential Credential { get; private set; }
    public List<Role> Roles { get; private set; } = new();

    // Null for staff accounts (Admin); set for accounts that act on behalf of a student.
    public int? EstudianteId { get; private set; }

    public const int MaxFailedLoginAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockoutEnd { get; private set; }

    public User(string username, string email, Credential credential, int? estudianteId = null)
    {
        Username = username;
        Email = email;
        Credential = credential;
        EstudianteId = estudianteId;
    }

    // EF Core materialization (the credential is an owned navigation, not a constructor parameter).
    private User()
    {
        Username = string.Empty;
        Email = string.Empty;
        Credential = null!;
    }

    public void Activate() => Status = UserStatus.Active;
    public void Deactivate() => Status = UserStatus.Inactive;
    public void Block() => Status = UserStatus.Blocked;

    public void AddRole(Role role)
    {
        if (Roles.Any(r => r.Name.Equals(role.Name, StringComparison.OrdinalIgnoreCase)))
            return;

        Roles.Add(role);
    }

    public bool HasPermission(string code)
    {
        return Roles.Any(role => role.Permissions.Any(p =>
            p.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));
    }

    public bool IsLockedOut(DateTimeOffset now) => LockoutEnd is { } end && end > now;

    /// <summary>Counts a failed password attempt and locks the account after the configured threshold.</summary>
    public void RegisterFailedLogin(DateTimeOffset now)
    {
        if (LockoutEnd is { } end && end <= now)
        {
            FailedLoginAttempts = 0;
            LockoutEnd = null;
        }

        FailedLoginAttempts++;

        if (FailedLoginAttempts >= MaxFailedLoginAttempts)
        {
            LockoutEnd = now.Add(LockoutDuration);
            FailedLoginAttempts = 0;
        }
    }

    public void ResetFailedLogins()
    {
        FailedLoginAttempts = 0;
        LockoutEnd = null;
    }
}