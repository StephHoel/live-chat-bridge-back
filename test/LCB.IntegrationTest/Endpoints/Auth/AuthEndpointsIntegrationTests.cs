using System.Net;
using System.Net.Http.Json;
using LCB.IntegrationTest.Infrastructure;
using Xunit;

namespace LCB.IntegrationTest.Endpoints.Auth;

public class AuthEndpointsIntegrationTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_Then_Login_ReturnsJwtToken_WithResultEnvelope()
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
        var registerBody = await registerResponse.Content.ReadAsAsync<ApiResult<RegisterResponseDto>>();
        Assert.NotNull(registerBody);
        Assert.True(registerBody!.Success);
        Assert.NotNull(registerBody.Data);
        Assert.Equal(email, registerBody.Data!.Email);

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
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized_WithResultEnvelope()
    {
        var email = $"integration.{Guid.NewGuid():N}@livebridge.com";
        var password = "StrongP@ss1";

        await _client.PostAsJsonAsync("/auth/register", new
        {
            email,
            password,
            confirmPassword = password
        });

        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new
        {
            email,
            password = "WrongP@ss1"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        var loginBody = await loginResponse.Content.ReadAsAsync<ApiResult<LoginResponseDto>>();
        Assert.NotNull(loginBody);
        Assert.False(loginBody!.Success);
        Assert.Equal("Invalid email or password", loginBody.Error);
    }
}
