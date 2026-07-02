using System;
using System.Net;
using System.Threading.Tasks;
using LCB.Application.Commands.Config.Live;
using LCB.Application.Commands.Config.Live.Put;
using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Services;
using LCB.Infrastructure.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using static LCB.UnitTest.Factories.RepositoryTestDbFactory;

namespace LCB.UnitTest.Handlers;

public class PutLiveConfigHandlerTests
{
    [Fact]
    public async Task Handle_Creates_Record_WithDefaults_WhenMissing()
    {
        using var db = CreateContext();
        var userRepo = new UserRepository(db.Context, new NullLogger<UserRepository>());
        var repo = new LiveSettingsRepository(db.Context, new NullLogger<LiveSettingsRepository>());
        var user = UserEntity.Create("alice@example.com", "hash");
        await userRepo.CreateAsync([user]);

        var handler = new PutLiveConfigHandler(repo, CreateAuditService(), new NullLogger<PutLiveConfigHandler>());
        var request = new PutLiveConfigRequest(" https://tiktok.com/@alice ", null, null, 12);

        var result = await handler.Handle(user.Id, user.Email, request);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("alice", result.Data!.TikTokUsername);
        Assert.Equal(12, result.Data.ReloadTimeInSec);
    }

    [Fact]
    public async Task Handle_Updates_Only_Provided_Fields()
    {
        using var db = CreateContext();
        var userRepo = new UserRepository(db.Context, new NullLogger<UserRepository>());
        var repo = new LiveSettingsRepository(db.Context, new NullLogger<LiveSettingsRepository>());
        var user = UserEntity.Create("alice@example.com", "hash");
        await userRepo.CreateAsync([user]);
        await repo.UpsertAsync(LiveSettingsEntity.Create(user.Id, user.Email, "alice", "tw-old", "yt-old", 5));

        var handler = new PutLiveConfigHandler(repo, CreateAuditService(), new NullLogger<PutLiveConfigHandler>());
        var request = new PutLiveConfigRequest(null, "@tw-new", null, null);

        var result = await handler.Handle(user.Id, user.Email, request);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("alice", result.Data!.TikTokUsername);
        Assert.Equal("tw-new", result.Data.TwitchUsername);
        Assert.Equal("yt-old", result.Data.YouTubeUsername);
        Assert.Equal(5, result.Data.ReloadTimeInSec);
    }

    [Fact]
    public async Task Handle_ReturnsBadRequest_WhenReloadTimeIsInvalid()
    {
        using var db = CreateContext();
        var userRepo = new UserRepository(db.Context, new NullLogger<UserRepository>());
        var repo = new LiveSettingsRepository(db.Context, new NullLogger<LiveSettingsRepository>());
        var user = UserEntity.Create("alice@example.com", "hash");
        await userRepo.CreateAsync([user]);

        var handler = new PutLiveConfigHandler(repo, CreateAuditService(), new NullLogger<PutLiveConfigHandler>());
        var result = await handler.Handle(user.Id, user.Email, new PutLiveConfigRequest(null, null, null, 0));

        Assert.False(result.Success);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("ReloadTimeInSec must be greater than zero", result.Error);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenPayloadDoesNotChangePersistedValues()
    {
        using var db = CreateContext();
        var userRepo = new UserRepository(db.Context, new NullLogger<UserRepository>());
        var repo = new LiveSettingsRepository(db.Context, new NullLogger<LiveSettingsRepository>());
        var user = UserEntity.Create("alice@example.com", "hash");
        await userRepo.CreateAsync([user]);
        await repo.UpsertAsync(LiveSettingsEntity.Create(user.Id, user.Email, "alice", "tw", "yt", 5));

        var handler = new PutLiveConfigHandler(repo, CreateAuditService(), new NullLogger<PutLiveConfigHandler>());
        var result = await handler.Handle(user.Id, user.Email, new PutLiveConfigRequest("@alice", "tw", "yt", 5));

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("alice", result.Data!.TikTokUsername);
        Assert.Equal("tw", result.Data.TwitchUsername);
        Assert.Equal("yt", result.Data.YouTubeUsername);
        Assert.Equal(5, result.Data.ReloadTimeInSec);
    }
    private static IAuditLogService CreateAuditService()
    {
        var auditLogService = new Mock<IAuditLogService>();
        auditLogService
            .Setup(x => x.WriteWithPolicyAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LCB.Domain.Enums.AuditLogStatusEnum>(),
                It.IsAny<string?>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync(true);

        return auditLogService.Object;
    }
}
