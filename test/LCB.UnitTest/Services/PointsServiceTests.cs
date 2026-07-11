using System;
using System.Threading.Tasks;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Interfaces.Repositories;
using LCB.Domain.Interfaces.Services;
using LCB.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LCB.UnitTest.Services;

public class PointsServiceTests
{
    private const ProviderTypeEnum Provider = ProviderTypeEnum.TIKTOK;
    private const string Channel = "streamer";
    private const string User = "user1";

    private static (IPointsService service, Mock<IPointsBalanceRepository> balanceRepo) CreateService(
        long currentBalance = 0)
    {
        var balanceRepo = new Mock<IPointsBalanceRepository>();

        balanceRepo
            .Setup(x => x.GetActiveBalanceAsync(Provider, Channel, User))
            .ReturnsAsync(currentBalance > 0 ? CreateBalance(currentBalance) : null);

        balanceRepo
            .Setup(x => x.UpsertAsync(It.IsAny<ProviderTypeEnum>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
            .ReturnsAsync((ProviderTypeEnum p, string c, string u, long d) => CreateBalance(Math.Max(0, currentBalance + d)));

        // CreditWithTransactionAsync: operação atômica
        balanceRepo
            .Setup(x => x.CreditWithTransactionAsync(It.IsAny<ProviderTypeEnum>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
            .ReturnsAsync(true);

        // DebitWithTransactionAsync: operação atômica
        balanceRepo
            .Setup(x => x.DebitWithTransactionAsync(Provider, Channel, User, It.IsAny<long>()))
            .ReturnsAsync((ProviderTypeEnum p, string c, string u, long points) =>
            {
                if (points <= 0) return false;
                if (currentBalance < points) return false;
                currentBalance -= points; // simula a dedução
                return true;
            });

        // ClearWithTransactionAsync: operação atômica
        balanceRepo
            .Setup(x => x.ClearWithTransactionAsync(It.IsAny<ProviderTypeEnum>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var service = new PointsService(balanceRepo.Object, new NullLogger<PointsService>());

        return (service, balanceRepo);
    }

    private static PointsBalanceEntity CreateBalance(long points)
    {
        var b = PointsBalanceEntity.Create(Provider, Channel, User, 0);
        b.ApplyDelta(points);
        return b;
    }

    [Fact]
    public async Task GetBalance_NoRecord_ReturnsZero()
    {
        var (service, _) = CreateService();

        var result = await service.GetBalanceAsync(Provider, Channel, User);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetBalance_ExistingRecord_ReturnsPoints()
    {
        var (service, _) = CreateService(currentBalance: 100);

        var result = await service.GetBalanceAsync(Provider, Channel, User);

        Assert.Equal(100, result);
    }

    [Fact]
    public async Task CreditAsync_UnsupportedProvider_SkipsAtomicOperation()
    {
        var (service, balanceRepo) = CreateService();

        await service.CreditAsync((ProviderTypeEnum)999, Channel, User, IntegrationTypeEnum.Message);

        balanceRepo.Verify(x => x.CreditWithTransactionAsync(It.IsAny<ProviderTypeEnum>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task CreditAsync_UnsupportedIntegrationType_SkipsAtomicOperation()
    {
        var (service, balanceRepo) = CreateService();

        await service.CreditAsync(Provider, Channel, User, (IntegrationTypeEnum)999);

        balanceRepo.Verify(x => x.CreditWithTransactionAsync(It.IsAny<ProviderTypeEnum>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()), Times.Never);
    }

    [Theory]
    [InlineData(IntegrationTypeEnum.Message)]
    [InlineData(IntegrationTypeEnum.Like)]
    public async Task CreditAsync_SupportedCombination_ExecutesAtomicOperation(IntegrationTypeEnum integrationType)
    {
        var (service, balanceRepo) = CreateService();

        await service.CreditAsync(Provider, Channel, User, integrationType);

        balanceRepo.Verify(x => x.CreditWithTransactionAsync(Provider, Channel, User, It.Is<long>(d => d > 0)), Times.Once);
    }

    [Fact]
    public async Task DebitAsync_SufficientBalance_ReturnsTrueAndExecutesAtomicOperation()
    {
        var (service, balanceRepo) = CreateService(currentBalance: 100);

        var result = await service.DebitAsync(Provider, Channel, User, 30);

        Assert.True(result);
        balanceRepo.Verify(x => x.DebitWithTransactionAsync(Provider, Channel, User, 30), Times.Once);
    }

    [Fact]
    public async Task DebitAsync_InsufficientBalance_ReturnsFalseAndRollsBack()
    {
        var (service, balanceRepo) = CreateService(currentBalance: 10);

        var result = await service.DebitAsync(Provider, Channel, User, 50);

        Assert.False(result);
        balanceRepo.Verify(x => x.DebitWithTransactionAsync(Provider, Channel, User, 50), Times.Once);
    }

    [Fact]
    public async Task DebitAsync_ZeroOrNegativePoints_ReturnsFalseImmediately()
    {
        var (service, balanceRepo) = CreateService(currentBalance: 100);

        var result = await service.DebitAsync(Provider, Channel, User, 0);

        Assert.False(result);
        balanceRepo.Verify(x => x.DebitWithTransactionAsync(It.IsAny<ProviderTypeEnum>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task ClearAsync_ExecutesAtomicClearWithTransaction()
    {
        var (service, balanceRepo) = CreateService(currentBalance: 75);

        await service.ClearAsync(Provider, Channel, User);

        balanceRepo.Verify(x => x.ClearWithTransactionAsync(Provider, Channel, User), Times.Once);
    }
}
