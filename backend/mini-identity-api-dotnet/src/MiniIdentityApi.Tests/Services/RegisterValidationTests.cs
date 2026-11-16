using MiniIdentityApi.Application.DTOs.Auth;
using MiniIdentityApi.Application.Services;
using MiniIdentityApi.Infrastructure.Repositories;
using MiniIdentityApi.Infrastructure.Security;
using MiniIdentityApi.Tests.TestDoubles;
using Xunit;

namespace MiniIdentityApi.Tests.Services;

public class RegisterValidationTests
{
    private static AuthenticationService Service(out InMemoryUserRepository users)
    {
        users = new InMemoryUserRepository();
        return new AuthenticationService(users, new Pbkdf2PasswordHasher(), new FakeTokenService());
    }

    private static RegisterRequest Request(string password = "Pass123!", string email = "user@example.com") => new()
    {
        Username = "student1",
        Email = email,
        Password = password
    };

    [Theory]
    [InlineData("short1!")]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoDigits!!")]
    [InlineData("NoSymbols123")]
    public void Register_RejectsWeakPasswords(string password)
    {
        var service = Service(out _);

        Assert.Throws<ArgumentException>(() => service.Register(Request(password)));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    public void Register_RejectsInvalidEmail(string email)
    {
        var service = Service(out _);

        Assert.Throws<ArgumentException>(() => service.Register(Request(email: email)));
    }

    [Fact]
    public void Register_RejectsInvalidUsername()
    {
        var service = Service(out _);
        var request = Request();
        request.Username = "a b";

        Assert.Throws<ArgumentException>(() => service.Register(request));
    }

    [Fact]
    public void Register_LinksUserToStudent()
    {
        var service = Service(out var users);
        var request = Request();
        request.EstudianteId = 42;

        service.Register(request);

        var saved = users.FindByUsernameOrEmail("student1");
        Assert.NotNull(saved);
        Assert.Equal(42, saved!.EstudianteId);
    }

    [Fact]
    public void Register_RejectsNonPositiveEstudianteId()
    {
        var service = Service(out _);
        var request = Request();
        request.EstudianteId = 0;

        Assert.Throws<ArgumentException>(() => service.Register(request));
    }
}
