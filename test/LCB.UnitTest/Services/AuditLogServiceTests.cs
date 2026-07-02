using System.Threading.Tasks;
using LCB.Domain.Constants;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LCB.UnitTest.Services;

public class AuditLogServiceTests
{
    [Fact]
    public async Task WriteAsync_PersistsAudit_WhenPayloadIsValid()
    {
        var repository = new Mock<IAuditLogRepository>();
        repository.Setup(x => x.CreateAsync(It.IsAny<AuditLogEntity>())).ReturnsAsync(true);

        var service = new AuditLogService(repository.Object, new NullLogger<AuditLogService>());

        var success = await service.WriteAsync(
            "alice@example.com",
            AuditLogCatalog.Action.WorkerStartSucceeded,
            AuditLogCatalog.Resource.WorkerControl,
            AuditLogStatusEnum.Success,
            "{\"metadataVersion\":1,\"correlationId\":\"corr\",\"eventCategory\":\"EndpointOperational\",\"occurredAtUtc\":\"2026-07-01T00:00:00.0000000Z\",\"endpointName\":\"POST /worker/start\",\"requestPath\":\"/worker/start\",\"httpStatus\":200}");

        Assert.True(success);
        repository.Verify(x => x.CreateAsync(It.IsAny<AuditLogEntity>()), Times.Once);
    }

    [Fact]
    public async Task WriteAsync_ReturnsFalse_WhenMetadataIsNotValidJson()
    {
        var repository = new Mock<IAuditLogRepository>();
        repository.Setup(x => x.CreateAsync(It.IsAny<AuditLogEntity>())).ReturnsAsync(true);

        var service = new AuditLogService(repository.Object, new NullLogger<AuditLogService>());

        var success = await service.WriteAsync(
            "alice@example.com",
            AuditLogCatalog.Action.WorkerStartSucceeded,
            AuditLogCatalog.Resource.WorkerControl,
            AuditLogStatusEnum.Success,
            "{\"metadataVersion\":1");

        Assert.False(success);
        repository.Verify(x => x.CreateAsync(It.IsAny<AuditLogEntity>()), Times.Never);
    }

    [Fact]
    public async Task WriteAsync_ReturnsFalse_WhenMetadataContainsSensitiveContent()
    {
        var repository = new Mock<IAuditLogRepository>();
        repository.Setup(x => x.CreateAsync(It.IsAny<AuditLogEntity>())).ReturnsAsync(true);

        var service = new AuditLogService(repository.Object, new NullLogger<AuditLogService>());

        var success = await service.WriteAsync(
            "alice@example.com",
            AuditLogCatalog.Action.WorkerStartSucceeded,
            AuditLogCatalog.Resource.WorkerControl,
            AuditLogStatusEnum.Success,
            "{\"metadataVersion\":1,\"correlationId\":\"corr\",\"eventCategory\":\"EndpointOperational\",\"occurredAtUtc\":\"2026-07-01T00:00:00.0000000Z\",\"endpointName\":\"POST /worker/start\",\"requestPath\":\"/worker/start\",\"httpStatus\":200,\"authorization\":\"Bearer abc\"}");

        Assert.False(success);
        repository.Verify(x => x.CreateAsync(It.IsAny<AuditLogEntity>()), Times.Never);
    }

    [Fact]
    public async Task WriteAsync_ReturnsFalse_WhenMetadataContractIsInvalid()
    {
        var repository = new Mock<IAuditLogRepository>();
        repository.Setup(x => x.CreateAsync(It.IsAny<AuditLogEntity>())).ReturnsAsync(true);

        var service = new AuditLogService(repository.Object, new NullLogger<AuditLogService>());

        var success = await service.WriteAsync(
            "alice@example.com",
            AuditLogCatalog.Action.WorkerStartSucceeded,
            AuditLogCatalog.Resource.WorkerControl,
            AuditLogStatusEnum.Success,
            "{\"metadataVersion\":1,\"correlationId\":\"corr\",\"eventCategory\":\"EndpointOperational\",\"occurredAtUtc\":\"2026-07-01T00:00:00.0000000Z\",\"endpointName\":\"POST /worker/start\"}");

        Assert.False(success);
        repository.Verify(x => x.CreateAsync(It.IsAny<AuditLogEntity>()), Times.Never);
    }

    [Fact]
    public async Task WriteWithPolicyAsync_RetriesOnce_WhenFirstAttemptFails()
    {
        var repository = new Mock<IAuditLogRepository>();
        repository.SetupSequence(x => x.CreateAsync(It.IsAny<AuditLogEntity>()))
            .ReturnsAsync(false)
            .ReturnsAsync(true);

        var service = new AuditLogService(repository.Object, new NullLogger<AuditLogService>());

        var success = await service.WriteWithPolicyAsync(
            "alice@example.com",
            AuditLogCatalog.Action.WorkerStartSucceeded,
            AuditLogCatalog.Resource.WorkerControl,
            AuditLogStatusEnum.Success,
            "{\"metadataVersion\":1,\"correlationId\":\"corr\",\"eventCategory\":\"EndpointOperational\",\"occurredAtUtc\":\"2026-07-01T00:00:00.0000000Z\",\"endpointName\":\"POST /worker/start\",\"requestPath\":\"/worker/start\",\"httpStatus\":200}");

        Assert.True(success);
        repository.Verify(x => x.CreateAsync(It.IsAny<AuditLogEntity>()), Times.Exactly(2));
    }
}
