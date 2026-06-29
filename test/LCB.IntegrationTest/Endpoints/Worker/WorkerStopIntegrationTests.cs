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
    private readonly HttpClient _client = factory.CreateClient();
    private readonly string endpoint = "/worker/stop";

    [Fact]
    public async Task Stop_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsync(endpoint, null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Stop_WhenInactive_ReturnsInactiveState()
    {
        var token = await _client.LoginWithRegisterAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsync(endpoint, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.ReadResultAsync<GetWorkerStatusResponse>();
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        Assert.Equal(WorkerStateEnum.Inactive, body.Data!.State);
    }

    [Fact]
    public async Task Stop_WhenActive_ReturnsInactiveState()
    {
        var token = await _client.LoginWithRegisterAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.StartWorkerAsync("@integration-user");

        var response = await _client.PostAsync(endpoint, null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.ReadResultAsync<GetWorkerStatusResponse>();
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        Assert.Equal(WorkerStateEnum.Inactive, body.Data!.State);
    }
}