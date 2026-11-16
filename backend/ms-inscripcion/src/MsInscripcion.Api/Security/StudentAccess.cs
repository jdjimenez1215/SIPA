using System.Security.Claims;

namespace MsInscripcion.Api.Security;

/// <summary>
/// Enforces that a caller only touches their own student record.
/// Admins can operate on any student; other users need the "estudianteId" claim
/// issued by MiniIdentityApi, and it must match the requested student.
/// </summary>
public static class StudentAccess
{
    public const string StudentClaim = "estudianteId";

    public static bool CanAccess(ClaimsPrincipal user, int estudianteId)
    {
        if (user.Identity?.IsAuthenticated != true)
            return false;

        if (user.IsInRole("Admin"))
            return true;

        var raw = user.FindFirst(StudentClaim)?.Value;
        return int.TryParse(raw, out var ownId) && ownId == estudianteId;
    }
}
