using MiniIdentityApi.Application.DTOs.Auth;
using MiniIdentityApi.Application.Services;
using MiniIdentityApi.Infrastructure.Repositories;
using MiniIdentityApi.Infrastructure.Security;
using MiniIdentityApi.Tests.TestDoubles;
using Xunit;

namespace MiniIdentityApi.Tests.Services;

public class LoginLockoutTests
{
    private sealed class FakeClock(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static (AuthenticationService Service, InMemoryUserRepository Users, FakeClock Clock) Create()
    {
        var users = new InMemoryUserRepository();
        var clock = new FakeClock(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var service = new AuthenticationService(
            users, new Pbkdf2PasswordHasher(), new FakeTokenService(), clock);

        service.Register(new RegisterRequest
        {
            Username = "alumno",
            Email = "alumno@example.com",
            Password = "Pass123!"
        });

        return (service, users, clock);
    }

    [Fact]
    public void Login_LocksAccountAfterFiveFailedAttempts()
    {
        var (service, _, _) = Create();

        for (var i = 0; i < 5; i++)
            Assert.Throws<UnauthorizedAccessException>(() =>
                service.Login(new LoginRequest { UsernameOrEmail = "alumno", Password = "wrong" }));

        Assert.Throws<UnauthorizedAccessException>(() =>
            service.Login(new LoginRequest { UsernameOrEmail = "alumno", Password = "Pass123!" }));
    }

    [Fact]
    public void Login_WorksAgainAfterLockoutExpires()
    {
        var (service, _, clock) = Create();

        for (var i = 0; i < 5; i++)
            Assert.Throws<UnauthorizedAccessException>(() =>
                service.Login(new LoginRequest { UsernameOrEmail = "alumno", Password = "wrong" }));

        clock.Now = clock.Now.AddMinutes(16);

        var response = service.Login(new LoginRequest { UsernameOrEmail = "alumno", Password = "Pass123!" });
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
    }

    [Fact]
    public void Login_ResetsFailedAttemptsOnSuccess()
    {
        var (service, _, _) = Create();

        for (var i = 0; i < 4; i++)
            Assert.Throws<UnauthorizedAccessException>(() =>
                service.Login(new LoginRequest { UsernameOrEmail = "alumno", Password = "wrong" }));

        service.Login(new LoginRequest { UsernameOrEmail = "alumno", Password = "Pass123!" });

        for (var i = 0; i < 4; i++)
            Assert.Throws<UnauthorizedAccessException>(() =>
                service.Login(new LoginRequest { UsernameOrEmail = "alumno", Password = "wrong" }));

        // Still not locked: the counter was reset after the successful login.
        var response = service.Login(new LoginRequest { UsernameOrEmail = "alumno", Password = "Pass123!" });
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
    }
}
