using System.Net;
using System.Net.Http.Headers;
using LCB.Application.Commands.Worker.Get;
using LCB.Domain.Enums;
using LCB.IntegrationTest.Helpers;
using LCB.IntegrationTest.Infrastructure;
using Xunit;

namespace LCB.IntegrationTest.Endpoints.Worker;

public class WorkerStatusIntegrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory = factory;
    private readonly string endpoint = "/worker/status";

    [Fact]
    public async Task Status_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.GetAsync(endpoint);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Status_WithAuthenticatedUserAndNoSession_ReturnsInactive()
    {
        using var client = _factory.CreateClient();
        var token = await client.LoginWithRegisterAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(endpoint);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.ReadResultAsync<GetWorkerStatusResponse>();
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        Assert.Equal(WorkerStateEnum.Inactive, body.Data!.State);
    }

    [Fact]
    public async Task Status_IsolatedByAuthenticatedUser()
    {
        using var client = _factory.CreateClient();
        var tokenA = await client.LoginWithRegisterAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        await client.StartWorkerAsync("@user-a", null, null);

        using var clientB = _factory.CreateClient();
        var tokenB = await clientB.LoginWithRegisterAsync();
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var response = await clientB.GetAsync(endpoint);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.ReadResultAsync<GetWorkerStatusResponse>();
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        Assert.Equal(WorkerStateEnum.Inactive, body.Data!.State);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        await client.StopWorkerAsync();
    }
}