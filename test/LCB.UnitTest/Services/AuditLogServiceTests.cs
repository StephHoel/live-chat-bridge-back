using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Threading.Tasks;
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
            "worker.start",
            "worker",
            AuditLogStatusEnum.Success,
            "{\"metadataVersion\":1,\"attempt\":1}");

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
            "worker.start",
            "worker",
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
            "worker.start",
            "worker",
            AuditLogStatusEnum.Success,
            "{\"authorization\":\"Bearer abc\"}");

        Assert.False(success);
        repository.Verify(x => x.CreateAsync(It.IsAny<AuditLogEntity>()), Times.Never);
    }
}
