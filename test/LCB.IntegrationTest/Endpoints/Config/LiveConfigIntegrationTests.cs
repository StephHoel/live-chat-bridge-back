using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LCB.Application.Commands.Config.Live;
using LCB.Application.Commands.Config.Live.Put;
using LCB.Domain.Objects;
using LCB.IntegrationTest.Helpers;
using LCB.IntegrationTest.Infrastructure;
using Xunit;

namespace LCB.IntegrationTest.Endpoints.Config;

public class LiveConfigIntegrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly string endpoint = "/config/live";

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync(endpoint);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadAsync<Result<LiveConfigResponse>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal("Unauthorized", body.Error);
    }

    [Fact]
    public async Task Get_Creates_Default_Config_WhenMissing()
    {
        var token = await _client.LoginWithRegisterAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync(endpoint);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsync<Result<LiveConfigResponse>>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        Assert.Equal(5, body.Data!.ReloadTimeInSec);
        Assert.Null(body.Data.TikTokUsername);
    }

    [Fact]
    public async Task Put_Updates_Config_WithPartialMerge_AndNormalization()
    {
        var token = await _client.LoginWithRegisterAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request1 = new PutLiveConfigRequest("https://tiktok.com/@alice", null, null, 10);
        var request2 = new PutLiveConfigRequest(null, " @twitchAlice ", null, null);

        var firstResponse = await _client.PutAsJsonAsync(endpoint, request1);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await _client.PutAsJsonAsync(endpoint, request2);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        var body = await secondResponse.Content.ReadAsync<Result<LiveConfigResponse>>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        Assert.Equal("alice", body.Data!.TikTokUsername);
        Assert.Equal("twitchAlice", body.Data.TwitchUsername);
        Assert.Equal(10, body.Data.ReloadTimeInSec);
    }

    [Fact]
    public async Task Put_WithInvalidReloadTime_Returns400()
    {
        var token = await _client.LoginWithRegisterAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new PutLiveConfigRequest(null, null, null, 0);

        var response = await _client.PutAsJsonAsync(endpoint, request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsync<Result<LiveConfigResponse>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal("ReloadTimeInSec must be greater than zero", body.Error);
    }
}
