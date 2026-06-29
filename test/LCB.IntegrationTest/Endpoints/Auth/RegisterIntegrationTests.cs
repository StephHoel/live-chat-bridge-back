using System.Net;
using System.Net.Http.Json;
using LCB.Application.Commands.Register;
using LCB.Domain.Objects;
using LCB.IntegrationTest.Constants;
using LCB.IntegrationTest.Infrastructure;
using Xunit;

namespace LCB.IntegrationTest.Endpoints.Auth;

public class RegisterIntegrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly string endpoint = "/auth/register";

    [Fact]
    public async Task Register_Success()
    {
        var email = FakeData.BuildUniqueEmail();
        var pass = FakeData.GetCorrectPass();

        var response = await Register(email, pass, pass, HttpStatusCode.Created);

        var body = await response.Content.ReadAsync<Result<RegisterResponse>>();
        Assert.NotNull(body);
        Assert.True(body.Success);
        Assert.NotNull(body.Data);
        Assert.Equal(email, body.Data!.Email);
    }

    [Fact]
    public async Task DuplicateEmail_ReturnsConflict()
    {
        var email = FakeData.BuildUniqueEmail();
        var pass = FakeData.GetCorrectPass();

        await Register(email, pass, pass, HttpStatusCode.Created);
        var response = await Register(email, pass, pass, HttpStatusCode.Conflict);

        var body = await response.Content.ReadAsync<Result<RegisterResponse>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal("Email already registered", body.Error);
    }

    [Fact]
    public async Task InvalidEmail_ReturnsBadRequest()
    {
        var pass = FakeData.GetCorrectPass();

        var response = await Register("invalid-email", pass, pass, HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsync<Result<RegisterResponse>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal("Invalid email format", body.Error);
    }

    [Fact]
    public async Task InvalidPass_ReturnsBadRequest()
    {
        var email = FakeData.BuildUniqueEmail();
        var pass = FakeData.GetCorrectPass();

        var response = await Register(email, pass + "1", pass, HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsync<Result<RegisterResponse>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal("Passwords do not match", body.Error);
    }

    [Fact]
    public async Task InvalidPass_WithoutConfirmationPass_ReturnsBadRequest()
    {
        var email = FakeData.BuildUniqueEmail();
        var pass = FakeData.GetCorrectPass();

        var response = await Register(email, pass, string.Empty, HttpStatusCode.BadRequest);

        var body = await response.Content.ReadAsync<Result<RegisterResponse>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal("Confirm password is required", body.Error);
    }

    private async Task<HttpResponseMessage> Register(string email, string pass, string confirmPass, HttpStatusCode expectedStatusCode)
    {
        var request = new RegisterRequest(email, pass, confirmPass);
        var response = await _client.PostAsJsonAsync(endpoint, request);
        Assert.Equal(expectedStatusCode, response.StatusCode);
        return response;
    }
}
