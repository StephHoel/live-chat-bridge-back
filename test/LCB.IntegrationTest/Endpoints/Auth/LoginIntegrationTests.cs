using System.Net;
using System.Net.Http.Json;
using LCB.Application.Commands.Login;
using LCB.Application.Commands.Register;
using LCB.Domain.Objects;
using LCB.IntegrationTest.Constants;
using LCB.IntegrationTest.Helpers;
using LCB.IntegrationTest.Infrastructure;
using Xunit;

namespace LCB.IntegrationTest.Endpoints.Auth;

public class LoginIntegrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly string endpointLogin = "/auth/login";

    [Fact]
    public async Task Login_UnknownUser_ReturnsUnauthorized()
    {
        var request = new LoginRequest(FakeData.BuildUniqueEmail(), FakeData.GetCorrectPass());
        var response = await _client.PostAsJsonAsync(endpointLogin, request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadAsync<Result<LoginResponse>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal("Invalid email or password", body.Error);
    }

    [Fact]
    public async Task Login_InvalidPassword_ReturnsUnauthorized()
    {
        await _client.RegisterAsync();

        var request = new LoginRequest(FakeData.BuildUniqueEmail(), FakeData.GetWrongPass());
        var response = await _client.PostAsJsonAsync(endpointLogin, request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadAsync<Result<LoginResponse>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal("Invalid email or password", body.Error);
    }

    [Fact]
    public async Task Login_Valid_ReturnToken()
    {
        await _client.RegisterAsync();

        var request = new LoginRequest(FakeData.BuildUniqueEmail(), FakeData.GetCorrectPass());
        var loginResponse = await _client.PostAsJsonAsync(endpointLogin, request);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var body = await loginResponse.Content.ReadAsync<Result<LoginResponse>>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        Assert.False(string.IsNullOrWhiteSpace(body.Data!.Token));
    }
}