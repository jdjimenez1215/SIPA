using MiniIdentityApi.Infrastructure.Security;
using Xunit;

namespace MiniIdentityApi.Tests.Security;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Verify_ShouldReturnTrue_WhenPasswordMatchesHash()
    {
        // Arrange
        var password = "Pass123!";
        var salt = _hasher.GenerateSalt();
        var hash = _hasher.Hash(password, salt);

        // Act
        var result = _hasher.Verify(password, salt, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenPasswordDoesNotMatchHash()
    {
        // Arrange
        var salt = _hasher.GenerateSalt();
        var hash = _hasher.Hash("Pass123!", salt);

        // Act
        var result = _hasher.Verify("WrongPass", salt, hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Hash_ShouldUsePbkdf2FormatWithIterations()
    {
        // Arrange
        var salt = _hasher.GenerateSalt();

        // Act
        var hash = _hasher.Hash("Pass123!", salt);

        // Assert
        Assert.StartsWith("pbkdf2-sha256$", hash);
        Assert.True(int.Parse(hash.Split('$')[1]) >= 100_000);
    }

    [Fact]
    public void Hash_ShouldProduceDifferentOutput_ForDifferentSalts()
    {
        // Arrange
        var password = "Pass123!";

        // Act
        var first = _hasher.Hash(password, _hasher.GenerateSalt());
        var second = _hasher.Hash(password, _hasher.GenerateSalt());

        // Assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenHashIsTampered()
    {
        // Arrange
        var salt = _hasher.GenerateSalt();
        var hash = _hasher.Hash("Pass123!", salt);
        var tampered = hash[..^4] + "AAAA";

        // Act
        var result = _hasher.Verify("Pass123!", salt, tampered);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenHashUsesUnknownFormat()
    {
        // Act
        var result = _hasher.Verify("Pass123!", _hasher.GenerateSalt(), "not-a-valid-hash");

        // Assert
        Assert.False(result);
    }
}
