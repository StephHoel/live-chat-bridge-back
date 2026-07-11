using System.Net;
using System.Net.Http.Headers;
using LCB.Application.Commands.Worker.Get;
using LCB.Domain.Enums;
using LCB.IntegrationTest.Helpers;
using LCB.IntegrationTest.Infrastructure;
using Xunit;

namespace LCB.IntegrationTest.Endpoints.Worker;

public class WorkerStopIntegrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory = factory;
    private readonly string endpoint = "/worker/stop";

    [Fact]
    public async Task Stop_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

        var response = await client.PostAsync(endpoint, null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Stop_WhenInactive_ReturnsInactiveState()
    {
        using var client = _factory.CreateClient();
        var token = await client.LoginWithRegisterAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PostAsync(endpoint, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.ReadResultAsync<GetWorkerStatusResponse>();
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        Assert.Equal(WorkerStateEnum.Inactive, body.Data!.State);
    }

    [Fact]
    public async Task Stop_WhenActive_ReturnsInactiveState()
    {
        using var client = _factory.CreateClient();
        var token = await client.LoginWithRegisterAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await client.StartWorkerAsync("@integration-user");

        var response = await client.PostAsync(endpoint, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.ReadResultAsync<GetWorkerStatusResponse>();
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        Assert.Equal(WorkerStateEnum.Inactive, body.Data!.State);
    }
}