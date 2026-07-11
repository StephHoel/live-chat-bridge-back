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

    private static (IPointsService service, Mock<IPointsBalanceRepository> balanceRepo, Mock<IPointsTransactionRepository> txRepo) CreateService(
        long currentBalance = 0)
    {
        var balanceRepo = new Mock<IPointsBalanceRepository>();
        var txRepo = new Mock<IPointsTransactionRepository>();

        balanceRepo
            .Setup(x => x.GetActiveBalanceAsync(Provider, Channel, User))
            .ReturnsAsync(currentBalance > 0 ? CreateBalance(currentBalance) : null);

        balanceRepo
            .Setup(x => x.UpsertAsync(It.IsAny<ProviderTypeEnum>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
            .ReturnsAsync((ProviderTypeEnum p, string c, string u, long d) => CreateBalance(Math.Max(0, currentBalance + d)));

        // TryDebitAsync comporta-se de forma atômica: validação + atualização dentro de transação
        balanceRepo
            .Setup(x => x.TryDebitAsync(Provider, Channel, User, It.IsAny<long>()))
            .ReturnsAsync((ProviderTypeEnum p, string c, string u, long points) =>
            {
                if (points <= 0) return false;
                if (currentBalance < points) return false;
                currentBalance -= points; // simula a dedução
                return true;
            });

        balanceRepo
            .Setup(x => x.ClearAsync(It.IsAny<ProviderTypeEnum>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        txRepo
            .Setup(x => x.CreateAsync(It.IsAny<PointsTransactionEntity>()))
            .ReturnsAsync(true);

        var service = new PointsService(balanceRepo.Object, txRepo.Object, new NullLogger<PointsService>());

        return (service, balanceRepo, txRepo);
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
        var (service, _, _) = CreateService();

        var result = await service.GetBalanceAsync(Provider, Channel, User);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task GetBalance_ExistingRecord_ReturnsPoints()
    {
        var (service, _, _) = CreateService(currentBalance: 100);

        var result = await service.GetBalanceAsync(Provider, Channel, User);

        Assert.Equal(100, result);
    }

    [Fact]
    public async Task CreditAsync_UnsupportedProvider_SkipsUpsertAndTransaction()
    {
        var (service, balanceRepo, txRepo) = CreateService();

        await service.CreditAsync((ProviderTypeEnum)999, Channel, User, IntegrationTypeEnum.Message);

        balanceRepo.Verify(x => x.UpsertAsync(It.IsAny<ProviderTypeEnum>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()), Times.Never);
        txRepo.Verify(x => x.CreateAsync(It.IsAny<PointsTransactionEntity>()), Times.Never);
    }

    [Fact]
    public async Task CreditAsync_UnsupportedIntegrationType_SkipsUpsertAndTransaction()
    {
        var (service, balanceRepo, txRepo) = CreateService();

        await service.CreditAsync(Provider, Channel, User, (IntegrationTypeEnum)999);

        balanceRepo.Verify(x => x.UpsertAsync(It.IsAny<ProviderTypeEnum>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()), Times.Never);
        txRepo.Verify(x => x.CreateAsync(It.IsAny<PointsTransactionEntity>()), Times.Never);
    }

    [Theory]
    [InlineData(IntegrationTypeEnum.Message)]
    [InlineData(IntegrationTypeEnum.Like)]
    public async Task CreditAsync_SupportedCombination_UpsertsAndCreatesTransaction(IntegrationTypeEnum integrationType)
    {
        var (service, balanceRepo, txRepo) = CreateService();

        await service.CreditAsync(Provider, Channel, User, integrationType);

        balanceRepo.Verify(x => x.UpsertAsync(Provider, Channel, User, It.Is<long>(d => d > 0)), Times.Once);
        txRepo.Verify(x => x.CreateAsync(It.Is<PointsTransactionEntity>(t => t.Situation == PointsTransactionSituationEnum.Credit)), Times.Once);
    }

    [Fact]
    public async Task DebitAsync_SufficientBalance_ReturnsTrueAndCreatesTransaction()
    {
        var (service, balanceRepo, txRepo) = CreateService(currentBalance: 100);

        var result = await service.DebitAsync(Provider, Channel, User, 30);

        Assert.True(result);
        balanceRepo.Verify(x => x.TryDebitAsync(Provider, Channel, User, 30), Times.Once);
        txRepo.Verify(x => x.CreateAsync(It.Is<PointsTransactionEntity>(t => t.Situation == PointsTransactionSituationEnum.Debit)), Times.Once);
    }

    [Fact]
    public async Task DebitAsync_InsufficientBalance_ReturnsFalseAndNoTransaction()
    {
        var (service, balanceRepo, txRepo) = CreateService(currentBalance: 10);

        var result = await service.DebitAsync(Provider, Channel, User, 50);

        Assert.False(result);
        balanceRepo.Verify(x => x.TryDebitAsync(Provider, Channel, User, 50), Times.Once);
        txRepo.Verify(x => x.CreateAsync(It.IsAny<PointsTransactionEntity>()), Times.Never);
    }

    [Fact]
    public async Task DebitAsync_ZeroOrNegativePoints_ReturnsFalseImmediately()
    {
        var (service, balanceRepo, txRepo) = CreateService(currentBalance: 100);

        var result = await service.DebitAsync(Provider, Channel, User, 0);

        Assert.False(result);
        balanceRepo.Verify(x => x.TryDebitAsync(It.IsAny<ProviderTypeEnum>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task ClearAsync_CallsClearAndCreatesTransaction()
    {
        var (service, balanceRepo, txRepo) = CreateService(currentBalance: 75);

        await service.ClearAsync(Provider, Channel, User);

        balanceRepo.Verify(x => x.ClearAsync(Provider, Channel, User), Times.Once);
        txRepo.Verify(x => x.CreateAsync(It.Is<PointsTransactionEntity>(t => t.Situation == PointsTransactionSituationEnum.Clear)), Times.Once);
    }
}
