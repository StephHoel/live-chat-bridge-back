using System;
using LCB.Application.Services;
using LCB.Domain.Entities;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.UnitTest.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LCB.UnitTest.Helpers;

public static class ServiceHelper
{
    public static (WorkerControlService Service, Guid UserIdA, Guid UserIdB) Create(LiveSettingsEntity settings = null)
    {
        var userIdA = Guid.NewGuid();
        var userIdB = Guid.NewGuid();

        if (settings is not null)
            settings = LiveSettingsEntity.Create(
                userIdA,
                settings.UpdatedByUser,
                settings.TikTokUsername,
                settings.TwitchUsername,
                settings.YouTubeUsername,
                settings.ReloadTimeInSec);

        var repository = new Mock<ILiveSettingsRepository>();
        repository
            .Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => id == userIdA ? settings : null);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(x => x.GetService(typeof(ILiveSettingsRepository)))
            .Returns(repository.Object);

        var auditLogService = new Mock<IAuditLogService>();
        auditLogService
            .Setup(x => x.WriteWithPolicyAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<LCB.Domain.Enums.AuditLogStatusEnum>(),
                It.IsAny<string>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync(true);

        serviceProvider
            .Setup(x => x.GetService(typeof(IAuditLogService)))
            .Returns(auditLogService.Object);

        var scope = new Mock<IServiceScope>();
        scope
            .SetupGet(x => x.ServiceProvider)
            .Returns(serviceProvider.Object);

        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory
            .Setup(x => x.CreateScope())
            .Returns(scope.Object);

        var service = new WorkerControlService(
            scopeFactory.Object,
            new FakeTikTokChatProvider(),
            NullLogger<WorkerControlService>.Instance);

        return (service, userIdA, userIdB);
    }
}