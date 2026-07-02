using System.Net;
using System.Net.Http.Json;
using LCB.Application.Commands.Login;
using LCB.Domain.Objects;
using LCB.IntegrationTest.Constants;
using LCB.IntegrationTest.Infrastructure;
using Xunit;

namespace LCB.IntegrationTest.Helpers;

public static class LoginHelper
{
    public static async Task<string> LoginWithRegisterAsync(
        this HttpClient client,
        string? email = null,
        string? password = null)
    {
        email ??= FakeData.BuildUniqueEmail();
        password ??= FakeData.GetCorrectPass();
        await client.RegisterAsync(email, password);

        var loginRequest = new LoginRequest(email, password);
        var loginResponse = await client.PostAsJsonAsync("/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginBody = await loginResponse.Content.ReadAsync<Result<LoginResponse>>();

        Assert.NotNull(loginBody);
        Assert.True(loginBody.Success);
        Assert.NotNull(loginBody.Data);
        Assert.False(string.IsNullOrWhiteSpace(loginBody.Data.Token));

        return loginBody.Data.Token!;
    }
}