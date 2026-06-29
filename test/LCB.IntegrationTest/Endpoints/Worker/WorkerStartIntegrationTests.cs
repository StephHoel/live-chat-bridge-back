using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LCB.Application.Commands.Worker.Get;
using LCB.Application.Commands.Worker.Start;
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
    public async Task Start_WithoutAnyPlatformEnabled_Returns400()
    {
        var token = await _client.RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new WorkerStartRequest(false, false, false);
        var response = await _client.PostAsJsonAsync(endpoint, request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsync<Result<GetWorkerStatusResponse>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
    }

    [Fact]
    public async Task Start_WithoutTikTokUsernameConfigured_Returns409()
    {
        var token = await _client.RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new WorkerStartRequest(true, false, false);
        var response = await _client.PostAsJsonAsync(endpoint, request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}