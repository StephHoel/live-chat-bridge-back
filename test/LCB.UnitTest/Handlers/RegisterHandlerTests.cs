using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LCB.Application.Commands.Register;
using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LCB.UnitTest.Handlers;

public class RegisterHandlerTests
{
    private readonly RegisterHandler handler;
    private readonly Mock<IUserRepository> repository;
    private readonly Mock<IPasswordHasher> passwordHasher;
    private readonly Mock<ILogger<RegisterHandler>> logger;

    public RegisterHandlerTests()
    {
        repository = new Mock<IUserRepository>();
        passwordHasher = new Mock<IPasswordHasher>();
        logger = new Mock<ILogger<RegisterHandler>>();

        handler = new RegisterHandler(repository.Object, passwordHasher.Object, logger.Object);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_AndPersistsHash_WhenRequestIsValid()
    {
        List<UserEntity> capturedUsers = null;

        repository
            .Setup(r => r.GetByEmailAsync("alice@example.com"))
            .ReturnsAsync((UserEntity)null);
        repository
            .Setup(r => r.CreateAsync(It.IsAny<IEnumerable<UserEntity>>()))
            .Callback<IEnumerable<UserEntity>>(users => capturedUsers = users.ToList())
            .ReturnsAsync(true);

        passwordHasher
            .Setup(h => h.Hash("StrongP@ss1"))
            .Returns("hashed-password");

        var request = new RegisterRequest(" Alice@Example.com ", "StrongP@ss1", "StrongP@ss1");

        var result = await handler.Handle(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Account created successfully", result.Data!.Message);
        Assert.Equal("alice@example.com", result.Data.Email);

        repository.Verify(r => r.GetByEmailAsync("alice@example.com"), Times.Once);
        repository.Verify(r => r.CreateAsync(It.IsAny<IEnumerable<UserEntity>>()), Times.Once);
        passwordHasher.Verify(h => h.Hash("StrongP@ss1"), Times.Once);

        Assert.NotNull(capturedUsers);
        Assert.Single(capturedUsers!);
        Assert.Equal("alice@example.com", capturedUsers[0].Email);
        Assert.Equal("hashed-password", capturedUsers[0].PasswordHash);
        Assert.NotEqual("StrongP@ss1", capturedUsers[0].PasswordHash);
    }

    [Fact]
    public async Task Handle_ReturnsConflict_WhenEmailAlreadyExists()
    {
        repository
            .Setup(r => r.GetByEmailAsync("alice@example.com"))
            .ReturnsAsync(UserEntity.Create("alice@example.com", "existing-hash"));

        var result = await handler.Handle(new RegisterRequest("alice@example.com", "StrongP@ss1", "StrongP@ss1"));

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        Assert.Equal("Email already registered", result.Error);

        repository.Verify(r => r.CreateAsync(It.IsAny<IEnumerable<UserEntity>>()), Times.Never);
        passwordHasher.Verify(h => h.Hash(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData("missing-domain@")]
    public async Task Handle_ReturnsBadRequest_WhenEmailIsInvalid(string email)
    {
        var result = await handler.Handle(new RegisterRequest(email, "StrongP@ss1", "StrongP@ss1"));

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("Invalid email format", result.Error);
    }

    [Fact]
    public async Task Handle_ReturnsBadRequest_WhenConfirmPasswordIsMissing()
    {
        var result = await handler.Handle(new RegisterRequest("alice@example.com", "StrongP@ss1", ""));

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("Confirm password is required", result.Error);
    }

    [Fact]
    public async Task Handle_ReturnsBadRequest_WhenPasswordsDoNotMatch()
    {
        var result = await handler.Handle(new RegisterRequest("alice@example.com", "StrongP@ss1", "StrongP@ss2"));

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("Passwords do not match", result.Error);
    }

    [Theory]
    [InlineData("Aa1$")]
    [InlineData("lowercase1$")]
    [InlineData("UPPERCASE1$")]
    [InlineData("NoNumber$$")]
    [InlineData("NoSpecial11")]
    public async Task Handle_ReturnsBadRequest_WhenPasswordViolatesPolicy(string password)
    {
        var result = await handler.Handle(new RegisterRequest("alice@example.com", password, password));

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal(
            "Password must be at least 8 characters and include uppercase, lowercase, number, and special character",
            result.Error);
    }

    [Fact]
    public async Task Handle_ReturnsConflict_WhenCreateFailsAndEmailExistsAfterward()
    {
        repository
            .SetupSequence(r => r.GetByEmailAsync("alice@example.com"))
            .ReturnsAsync((UserEntity)null)
            .ReturnsAsync(UserEntity.Create("alice@example.com", "existing-hash"));

        repository
            .Setup(r => r.CreateAsync(It.IsAny<IEnumerable<UserEntity>>()))
            .ReturnsAsync(false);

        passwordHasher
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Returns("hashed-password");

        var result = await handler.Handle(new RegisterRequest("alice@example.com", "StrongP@ss1", "StrongP@ss1"));

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.Conflict, result.StatusCode);
        Assert.Equal("Email already registered", result.Error);
    }

    [Fact]
    public async Task Handle_ReturnsInternalServerError_WhenCreateFailsWithoutDuplicate()
    {
        repository
            .SetupSequence(r => r.GetByEmailAsync("alice@example.com"))
            .ReturnsAsync((UserEntity)null)
            .ReturnsAsync((UserEntity)null);

        repository
            .Setup(r => r.CreateAsync(It.IsAny<IEnumerable<UserEntity>>()))
            .ReturnsAsync(false);

        passwordHasher
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Returns("hashed-password");

        var result = await handler.Handle(new RegisterRequest("alice@example.com", "StrongP@ss1", "StrongP@ss1"));

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.Equal("Could not create account", result.Error);
    }
}