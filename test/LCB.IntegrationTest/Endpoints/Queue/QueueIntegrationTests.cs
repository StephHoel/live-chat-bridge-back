using System.Net;
using System.Net.Http.Headers;
using LCB.Application.Commands.Queue.Get;
using LCB.Domain.Entities;
using LCB.Domain.Enums;
using LCB.Domain.Objects;
using LCB.Infrastructure.Data;
using LCB.IntegrationTest.Helpers;
using LCB.IntegrationTest.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LCB.IntegrationTest.Endpoints.Queue;

public class QueueIntegrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();
    private readonly string _endpoint = "/queue";

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync(_endpoint);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadAsync<Result<GetQueueResponse>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal("Unauthorized", body.Error);
    }

    [Fact]
    public async Task Get_WithValidToken_Returns_Queue_Ordered_ByCreatedAt()
    {
        await ResetAndSeedQueueAsync();

        var token = await _client.LoginWithRegisterAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync(_endpoint);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsync<Result<GetQueueResponse>>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        Assert.Equal(["alice", "bob", "charlie"], [.. body.Data!.Items.Select(x => x.User)]);
    }

    private async Task ResetAndSeedQueueAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LcbDbContext>();

        dbContext.Queues.RemoveRange(dbContext.Queues);

        dbContext.Queues.AddRange(
            NewQueue(Guid.Parse("11111111-1111-1111-1111-111111111111"), ProviderTypeEnum.TWITCH, "charlie", new DateTime(2026, 1, 1, 10, 5, 0, DateTimeKind.Utc)),
            NewQueue(Guid.Parse("22222222-2222-2222-2222-222222222222"), ProviderTypeEnum.TIKTOK, "alice", new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc)),
            NewQueue(Guid.Parse("33333333-3333-3333-3333-333333333333"), ProviderTypeEnum.YOUTUBE, "bob", new DateTime(2026, 1, 1, 10, 2, 0, DateTimeKind.Utc)));

        await dbContext.SaveChangesAsync();
    }

    private static QueueEntity NewQueue(Guid id, ProviderTypeEnum provider, string user, DateTime createdAt)
        => new(id, provider, user, false, createdAt)
        {
            CreatedAt = createdAt,
            JoinedAt = createdAt,
            UpdatedAt = createdAt
        };
}