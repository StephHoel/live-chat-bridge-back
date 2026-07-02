using System.Net;
using System.Net.Http.Json;
using LCB.Application.Commands.Register;
using LCB.IntegrationTest.Constants;
using Xunit;

namespace LCB.IntegrationTest.Helpers;

public static class RegisterHelper
{
    public static async Task<(string Email, string Password)> RegisterAsync(
        this HttpClient client,
        string? email = null,
        string? password = null)
    {
        email ??= FakeData.BuildUniqueEmail();
        password ??= FakeData.GetCorrectPass();

        var registerRequest = new RegisterRequest(email, password, password);
        var registerResponse = await client.PostAsJsonAsync("/auth/register", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        return (email, password);
    }
}