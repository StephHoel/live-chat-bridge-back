using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using LCB.Application.Commands.Login;
using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LCB.UnitTest.Handlers;

public class LoginHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsToken_WhenUserExists()
    {
        var repository = new FakeUserRepository(UserEntity.Create("alice@example.com", "hash"));
        var tokenService = new FakeTokenService("token-123");
        var handler = new LoginHandler(repository, tokenService, NullLogger<LoginHandler>.Instance);

        var result = await handler.Handle(new LoginRequest("alice@example.com", "pwd"));

        Assert.True(result.Success);
        Assert.Equal("token-123", result.Data!.Token);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenUserDoesNotExist()
    {
        var handler = new LoginHandler(new FakeUserRepository(null), new FakeTokenService("token-123"), NullLogger<LoginHandler>.Instance);

        var result = await handler.Handle(new LoginRequest("missing@example.com", "pwd"));

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.NotFound, result.ErrorType);
        Assert.Equal("User not found", result.Error);
    }

    [Fact]
    public async Task Handle_ReturnsInternalServerError_WhenRepositoryThrows()
    {
        var handler = new LoginHandler(new FakeUserRepository(null, throwOnRead: true), new FakeTokenService("token-123"), NullLogger<LoginHandler>.Instance);

        var result = await handler.Handle(new LoginRequest("boom@example.com", "pwd"));

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.InternalServerError, result.ErrorType);
        Assert.Equal("Erro inesperado", result.Error);
    }

    [Fact]
    public async Task Handle_ReturnsInternalServerError_WhenTokenGenerationFails()
    {
        var repository = new FakeUserRepository(UserEntity.Create("alice@example.com", "hash"));
        var handler = new LoginHandler(repository, new FakeTokenService(null), NullLogger<LoginHandler>.Instance);

        var result = await handler.Handle(new LoginRequest("alice@example.com", "pwd"));

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.InternalServerError, result.ErrorType);
        Assert.Equal("Fail on JWT generation", result.Error);
    }

    private sealed class FakeUserRepository(UserEntity user, bool throwOnRead = false) : IUserRepository
    {
        public Task<UserEntity> GetByEmailAsync(string email)
        {
            if (throwOnRead)
                throw new InvalidOperationException("boom");

            return Task.FromResult(user);
        }

        public Task<bool> CreateAsync(IEnumerable<UserEntity> users)
            => Task.FromResult(true);
    }

    private sealed class FakeTokenService(string token) : ITokenService
    {
        public string GenerateToken(UserEntity user)
            => token!;
    }
}