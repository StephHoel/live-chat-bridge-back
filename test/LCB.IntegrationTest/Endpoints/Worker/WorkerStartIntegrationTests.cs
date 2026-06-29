using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LCB.Application.Commands.Worker.Get;
using LCB.Application.Commands.Worker.Start;
using LCB.Domain.Enums;
using LCB.Domain.Objects;
using LCB.IntegrationTest.Helpers;
using LCB.IntegrationTest.Infrastructure;
using Xunit;

namespace LCB.IntegrationTest.Endpoints.Worker;

public class WorkerStartIntegrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly string endpoint = "/worker/start";

    [Fact]
    public async Task Start_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var request = new WorkerStartRequest(true, false, false);

        var response = await _client.PostAsJsonAsync(endpoint, request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Start_WithoutAnyPlatformEnabled_Returns400()
    {
        var token = await _client.LoginWithRegisterAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new WorkerStartRequest(false, false, false);
        var response = await _client.PostAsJsonAsync(endpoint, request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.ReadResultAsync<GetWorkerStatusResponse>();
        Assert.NotNull(body);
        Assert.False(body.Success);
    }

    [Fact]
    public async Task Start_WithTikTokEnabledAndNoConfig_Returns409()
    {
        var token = await _client.LoginWithRegisterAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new WorkerStartRequest(true, false, false);
        var response = await _client.PostAsJsonAsync(endpoint, request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Start_WithUnsupportedListener_Returns503()
    {
        var token = await _client.LoginWithRegisterAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.ConfigureLiveAsync("@integration-user", "integration-twitch", null);

        var request = new WorkerStartRequest(false, true, false);
        var response = await _client.PostAsJsonAsync(endpoint, request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Start_WithValidTikTokConfig_ReturnsActiveState()
    {
        var token = await _client.LoginWithRegisterAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.ConfigureLiveAsync("@integration-user", null, null);

        var request = new WorkerStartRequest(true, false, false);
        var response = await _client.PostAsJsonAsync(endpoint, request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.ReadResultAsync<GetWorkerStatusResponse>();
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        Assert.Equal(WorkerStateEnum.Active, body.Data!.State);
        Assert.True(body.Data.TikTok);
    }

    [Fact]
    public async Task Start_WhenAlreadyActive_ReturnsCurrentState()
    {
        var token = await _client.LoginWithRegisterAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await _client.ConfigureLiveAsync("@integration-user", null, null);

        var request = new WorkerStartRequest(true, false, false);
        var firstResponse = await _client.PostAsJsonAsync(endpoint, request);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await _client.PostAsJsonAsync(endpoint, request);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var body = await secondResponse.ReadResultAsync<GetWorkerStatusResponse>();
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        Assert.Equal(WorkerStateEnum.Active, body.Data!.State);
    }
}