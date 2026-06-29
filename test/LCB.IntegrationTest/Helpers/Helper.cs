using System.Net;
using System.Net.Http.Json;
using LCB.Application.Commands.Login;
using LCB.Application.Commands.Register;
using LCB.Domain.Objects;
using LCB.IntegrationTest.Constants;
using LCB.IntegrationTest.Infrastructure;
using Xunit;

namespace LCB.IntegrationTest.Helpers;

public static class Helper
{
    public static async Task RegisterAsync(this HttpClient _client)
    {
        var email = FakeData.BuildUniqueEmail();
        var password = FakeData.GetCorrectPass();

        var registerRequest = new RegisterRequest(email, password, password);
        var registerResponse = await _client.PostAsJsonAsync("/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
    }

    public static async Task<string> RegisterAndLoginAsync(this HttpClient _client)
    {
        var email = FakeData.BuildUniqueEmail();
        var password = FakeData.GetCorrectPass();

        await _client.RegisterAsync();

        var loginRequest = new LoginRequest(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginBody = await loginResponse.Content.ReadAsync<Result<LoginResponse>>();

        Assert.NotNull(loginBody);
        Assert.True(loginBody.Success);
        Assert.NotNull(loginBody.Data);
        Assert.False(string.IsNullOrWhiteSpace(loginBody.Data.Token));

        return loginBody.Data.Token!;
    }
}