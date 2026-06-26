using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LCB.IntegrationTest.Infrastructure;
using Xunit;

namespace LCB.IntegrationTest.Endpoints.Messages;

public class MessageEndpointsIntegrationTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Ingest_WithoutToken_Returns401_WithResultEnvelope()
    {
        var response = await _client.PostAsJsonAsync("/messages/ingest", new
        {
            provider = 0,
            author = "integration-user",
            text = "!fila",
            timestamp = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadAsAsync<ApiResult<object>>();
        Assert.NotNull(body);
        Assert.False(body!.Success);
        Assert.Equal("Unauthorized", body.Error);
    }

    [Fact]
    public async Task Ingest_WithInvalidToken_Returns401_WithResultEnvelope()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var response = await _client.PostAsJsonAsync("/messages/ingest", new
        {
            provider = 0,
            author = "integration-user",
            text = "!fila",
            timestamp = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadAsAsync<ApiResult<object>>();
        Assert.NotNull(body);
        Assert.False(body!.Success);
        Assert.Equal("Unauthorized", body.Error);
    }

    [Fact]
    public async Task Ingest_WithValidToken_Returns200_WithResultEnvelope()
    {
        var token = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/messages/ingest", new
        {
            provider = 0,
            author = "integration-user",
            text = "!fila",
            timestamp = DateTime.UtcNow
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsAsync<ApiResult<object>>();
        Assert.NotNull(body);
        Assert.True(body!.Success);
    }

    private async Task<string> RegisterAndLoginAsync()
    {
        var email = $"integration.{Guid.NewGuid():N}@livebridge.com";
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
        var loginBody = await loginResponse.Content.ReadAsAsync<ApiResult<LoginResponseDto>>();

        Assert.NotNull(loginBody);
        Assert.True(loginBody!.Success);
        Assert.NotNull(loginBody.Data);
        Assert.False(string.IsNullOrWhiteSpace(loginBody.Data!.Token));

        return loginBody.Data.Token!;
    }
}
