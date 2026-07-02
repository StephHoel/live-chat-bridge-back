using System;
using System.Net;
using System.Threading.Tasks;
using LCB.Application.Commands.Recover;
using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LCB.UnitTest.Handlers;

public class RecoverHandlerTests
{
    private readonly Mock<IUserRepository> repository = new();
    private readonly Mock<IRecoverAntiAbuseService> antiAbuseService = new();
    private readonly Mock<IRecoverTokenService> recoverTokenService = new();
    private readonly Mock<IHostEnvironment> hostEnvironment = new();
    private readonly Mock<ILogger<RecoverHandler>> logger = new();

    [Fact]
    public async Task Handle_ReturnsUnprocessableEntity_WhenEmailIsMissing()
    {
        var handler = CreateHandler("Development");

        var result = await handler.Handle(new RecoverRequest(" "), "127.0.0.1");

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, result.StatusCode);
        Assert.Equal("Email is required", result.Error);
    }

    [Fact]
    public async Task Handle_ReturnsUnprocessableEntity_WhenEmailIsInvalid()
    {
        var handler = CreateHandler("Development");

        var result = await handler.Handle(new RecoverRequest("invalid-email"), "127.0.0.1");

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, result.StatusCode);
        Assert.Equal("Invalid email format", result.Error);
    }

    [Fact]
    public async Task Handle_ReturnsTooManyRequests_WhenAntiAbuseBlocks()
    {
        antiAbuseService
            .Setup(x => x.TryAcquire(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(false);

        var handler = CreateHandler("Development");

        var result = await handler.Handle(new RecoverRequest("user@example.com"), "127.0.0.1");

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.TooManyRequests, result.StatusCode);
        Assert.Equal("Too many recover attempts. Try again later", result.Error);

        repository.Verify(x => x.GetByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsTemporaryToken_InDevelopment()
    {
        antiAbuseService
            .Setup(x => x.TryAcquire(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(true);

        repository
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((UserEntity?)null);

        recoverTokenService
            .Setup(x => x.GenerateTemporaryResetToken())
            .Returns("tmp-token");

        var handler = CreateHandler("Development");

        var result = await handler.Handle(new RecoverRequest("user@example.com"), "127.0.0.1");

        Assert.True(result.Success);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Data);
        Assert.False(string.IsNullOrWhiteSpace(result.Data!.Message));
        Assert.Equal("tmp-token", result.Data.TemporaryResetToken);
    }

    [Fact]
    public async Task Handle_DoesNotReturnTemporaryToken_InProduction()
    {
        antiAbuseService
            .Setup(x => x.TryAcquire(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(true);

        repository
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((UserEntity?)null);

        var handler = CreateHandler("Production");

        var result = await handler.Handle(new RecoverRequest("user@example.com"), "127.0.0.1");

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Null(result.Data!.TemporaryResetToken);
        Assert.Equal(
            "If the email exists, recovery instructions will be sent. Email recovery delivery is not implemented yet.",
            result.Data.Message);
    }

    private RecoverHandler CreateHandler(string environmentName)
    {
        hostEnvironment.SetupGet(x => x.EnvironmentName).Returns(environmentName);

        return new RecoverHandler(
            repository.Object,
            antiAbuseService.Object,
            recoverTokenService.Object,
            hostEnvironment.Object,
            logger.Object);
    }
}
