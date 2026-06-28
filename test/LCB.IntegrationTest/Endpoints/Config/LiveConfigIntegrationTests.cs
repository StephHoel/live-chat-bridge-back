using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LCB.Application.Commands.Config.Live;
using LCB.Application.Commands.Login;
using LCB.Domain.Objects;
using LCB.IntegrationTest.Constants;
using LCB.IntegrationTest.Infrastructure;
using Xunit;

namespace LCB.IntegrationTest.Endpoints.Config;

public class LiveConfigIntegrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/config/live");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadAsync<Result<LiveConfigResponse>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal("Unauthorized", body.Error);
    }

    [Fact]
    public async Task Get_Creates_Default_Config_WhenMissing()
    {
        var token = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/config/live");
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
        var token = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var firstResponse = await _client.PutAsJsonAsync("/config/live", new PutLiveConfigRequest("https://tiktok.com/@alice", null, null, 10));
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await _client.PutAsJsonAsync("/config/live", new PutLiveConfigRequest(null, " @twitchAlice ", null, null));
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
        var token = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync("/config/live", new PutLiveConfigRequest(null, null, null, 0));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsync<Result<LiveConfigResponse>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal("ReloadTimeInSec must be greater than zero", body.Error);
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        var email = FakeData.BuildUniqueEmail();
        var password = "StrongP@ss1";

        var registerResponse = await _client.PostAsJsonAsync("/auth/register", new
        {
            email,
            password,
            confirmPassword = password
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new
        {
            email,
            password
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginBody = await loginResponse.Content.ReadAsync<Result<LoginResponse>>();

        Assert.NotNull(loginBody);
        Assert.True(loginBody.Success);
        Assert.NotNull(loginBody.Data);
        Assert.False(string.IsNullOrWhiteSpace(loginBody.Data.Token));

        return loginBody.Data.Token!;
    }
}
