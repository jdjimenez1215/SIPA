namespace MiniIdentityApi.Application.DTOs.Auth;

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>Student this account belongs to. Null for staff accounts.</summary>
    public int? EstudianteId { get; set; }
}