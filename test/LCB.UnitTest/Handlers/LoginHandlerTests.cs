using System;
using System.Net;
using System.Threading.Tasks;
using LCB.Application.Commands.Login;
using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LCB.UnitTest.Handlers;

public class LoginHandlerTests
{
    private readonly LoginHandler handler;
    private readonly Mock<IUserRepository> repository;
    private readonly Mock<ITokenService> tokenService;
    private readonly Mock<IPasswordHasher> passwordHasher;
    private readonly Mock<ILogger<LoginHandler>> logger;

    public LoginHandlerTests()
    {
        repository = new Mock<IUserRepository>();
        tokenService = new Mock<ITokenService>();
        passwordHasher = new Mock<IPasswordHasher>();
        logger = new Mock<ILogger<LoginHandler>>();

        handler = new LoginHandler(repository.Object, tokenService.Object, passwordHasher.Object, logger.Object);
    }

    [Fact]
    public async Task Handle_ReturnsToken_WhenCredentialsAreValid()
    {
        var user = UserEntity.Create("alice@example.com", "hashed_password");

        repository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        passwordHasher
            .Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        tokenService
            .Setup(t => t.GenerateToken(It.IsAny<UserEntity>()))
            .Returns("token-123");

        var result = await handler.Handle(new LoginRequest("alice@example.com", "correct_password"));

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("token-123", result.Data!.Token);
    }

    [Fact]
    public async Task Handle_ReturnsUnauthorized_WhenPasswordIsInvalid()
    {
        var user = UserEntity.Create("alice@example.com", "hashed_password");

        repository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        passwordHasher
            .Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var result = await handler.Handle(new LoginRequest("alice@example.com", "wrong_password"));

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal("Invalid email or password", result.Error);
    }

    [Fact]
    public async Task Handle_ReturnsUnauthorized_WhenUserDoesNotExist()
    {
        repository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((UserEntity)null);

        var result = await handler.Handle(new LoginRequest("missing@example.com", "any_password"));

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Equal("Invalid email or password", result.Error);
    }

    [Fact]
    public async Task Handle_ReturnsInternalServerError_WhenRepositoryThrows()
    {
        repository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var result = await handler.Handle(new LoginRequest("boom@example.com", "pwd"));

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.Equal("Erro inesperado", result.Error);
    }

    [Fact]
    public async Task Handle_ReturnsInternalServerError_WhenTokenGenerationFails()
    {
        var user = UserEntity.Create("alice@example.com", "hashed_password");

        repository
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(user);
        passwordHasher
            .Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(true);
        tokenService
            .Setup(t => t.GenerateToken(It.IsAny<UserEntity>()))
            .Returns((string)null);

        var result = await handler.Handle(new LoginRequest("alice@example.com", "correct_password"));

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.InternalServerError, result.StatusCode);
        Assert.Equal("Fail on JWT generation", result.Error);
    }
}