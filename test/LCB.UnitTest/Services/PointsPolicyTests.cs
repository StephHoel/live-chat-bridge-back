using LCB.Domain.Enums;
using LCB.Infrastructure.Policies;
using Xunit;

namespace LCB.UnitTest.Services;

public class PointsPolicyTests
{
    [Theory]
    [InlineData(ProviderTypeEnum.TIKTOK, IntegrationTypeEnum.Message)]
    [InlineData(ProviderTypeEnum.TIKTOK, IntegrationTypeEnum.Like)]
    [InlineData(ProviderTypeEnum.TWITCH, IntegrationTypeEnum.Message)]
    [InlineData(ProviderTypeEnum.TWITCH, IntegrationTypeEnum.Like)]
    [InlineData(ProviderTypeEnum.YOUTUBE, IntegrationTypeEnum.Message)]
    [InlineData(ProviderTypeEnum.YOUTUBE, IntegrationTypeEnum.Like)]
    public void GetDelta_SupportedCombination_ReturnsPositive(ProviderTypeEnum provider, IntegrationTypeEnum integrationType)
    {
        var delta = PointsPolicy.GetDelta(provider, integrationType);

        Assert.True(delta > 0);
    }

    [Theory]
    [InlineData(ProviderTypeEnum.TIKTOK)]
    [InlineData(ProviderTypeEnum.TWITCH)]
    [InlineData(ProviderTypeEnum.YOUTUBE)]
    public void IsProviderSupported_KnownProviders_ReturnsTrue(ProviderTypeEnum provider)
    {
        Assert.True(PointsPolicy.IsProviderSupported(provider));
    }

    [Theory]
    [InlineData(IntegrationTypeEnum.Message)]
    [InlineData(IntegrationTypeEnum.Like)]
    public void IsIntegrationTypeSupported_KnownTypes_ReturnsTrue(IntegrationTypeEnum integrationType)
    {
        Assert.True(PointsPolicy.IsIntegrationTypeSupported(integrationType));
    }

    [Fact]
    public void GetDelta_MessageAndLike_HaveDifferentDeltas()
    {
        var messagePoints = PointsPolicy.GetDelta(ProviderTypeEnum.TIKTOK, IntegrationTypeEnum.Message);
        var likePoints = PointsPolicy.GetDelta(ProviderTypeEnum.TIKTOK, IntegrationTypeEnum.Like);

        Assert.NotEqual(messagePoints, likePoints);
    }
}
