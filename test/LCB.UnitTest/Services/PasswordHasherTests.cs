using System;
using LCB.Infrastructure.Services;
using Xunit;

namespace LCB.UnitTest.Services;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_ReturnsNonEmptyString()
    {
        var hasher = new PasswordHasher();
        var password = "my_secure_password_123";

        var hash = hasher.Hash(password);

        Assert.NotEmpty(hash);
        Assert.NotEqual(password, hash); // Should not store plaintext
    }

    [Fact]
    public void Hash_ProducesDifferentHashesForSamePassword()
    {
        var hasher = new PasswordHasher();
        var password = "my_secure_password_123";

        var hash1 = hasher.Hash(password);
        var hash2 = hasher.Hash(password);

        Assert.NotEqual(hash1, hash2); // Different salts should produce different hashes
    }

    [Fact]
    public void Verify_ReturnsTrue_WhenPasswordMatchesHash()
    {
        var hasher = new PasswordHasher();
        var password = "my_secure_password_123";
        var hash = hasher.Hash(password);

        var result = hasher.Verify(password, hash);

        Assert.True(result);
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenPasswordDoesNotMatchHash()
    {
        var hasher = new PasswordHasher();
        var password = "my_secure_password_123";
        var hash = hasher.Hash(password);

        var result = hasher.Verify("wrong_password", hash);

        Assert.False(result);
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenHashIsEmpty()
    {
        var hasher = new PasswordHasher();

        var result = hasher.Verify("any_password", "");

        Assert.False(result);
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenHashIsNull()
    {
        var hasher = new PasswordHasher();

        var result = hasher.Verify("any_password", null!);

        Assert.False(result);
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenPasswordIsEmpty()
    {
        var hasher = new PasswordHasher();
        var validHash = hasher.Hash("some_password");

        var result = hasher.Verify("", validHash);

        Assert.False(result);
    }

    [Fact]
    public void Verify_ReturnsFalse_WhenPasswordIsNull()
    {
        var hasher = new PasswordHasher();
        var validHash = hasher.Hash("some_password");

        var result = hasher.Verify(null!, validHash);

        Assert.False(result);
    }

    [Fact]
    public void Hash_ThrowsException_WhenPasswordIsEmpty()
    {
        var hasher = new PasswordHasher();

        Assert.Throws<ArgumentException>(() => hasher.Hash(""));
    }

    [Fact]
    public void Hash_ThrowsException_WhenPasswordIsNull()
    {
        var hasher = new PasswordHasher();

        Assert.Throws<ArgumentException>(() => hasher.Hash(null!));
    }
}
