using System.Security.Claims;
using FluentAssertions;
using MsInscripcion.Api.Security;

namespace MsInscripcion.UnitTests.Security;

public class StudentAccessTests
{
    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "TestAuth"));

    [Fact]
    public void Admin_CanAccessAnyStudent()
    {
        var user = Principal(new Claim(ClaimTypes.Role, "Admin"));

        StudentAccess.CanAccess(user, 99).Should().BeTrue();
    }

    [Fact]
    public void Student_CanAccessOwnRecord()
    {
        var user = Principal(new Claim(StudentAccess.StudentClaim, "7"));

        StudentAccess.CanAccess(user, 7).Should().BeTrue();
    }

    [Fact]
    public void Student_CannotAccessAnotherRecord()
    {
        var user = Principal(new Claim(StudentAccess.StudentClaim, "7"));

        StudentAccess.CanAccess(user, 8).Should().BeFalse();
    }

    [Fact]
    public void UserWithoutStudentClaim_CannotAccess()
    {
        var user = Principal(new Claim(ClaimTypes.Name, "staff"));

        StudentAccess.CanAccess(user, 1).Should().BeFalse();
    }

    [Fact]
    public void Anonymous_CannotAccess()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        StudentAccess.CanAccess(user, 1).Should().BeFalse();
    }
}
