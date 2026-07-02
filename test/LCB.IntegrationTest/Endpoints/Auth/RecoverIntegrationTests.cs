using System.Net;
using System.Net.Http.Json;
using System.Text;
using LCB.Application.Commands.Recover;
using LCB.Domain.Objects;
using LCB.IntegrationTest.Constants;
using LCB.IntegrationTest.Helpers;
using LCB.IntegrationTest.Infrastructure;
using Xunit;

namespace LCB.IntegrationTest.Endpoints.Auth;

public class RecoverIntegrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient client = factory.CreateClient();
    private const string Endpoint = "/auth/recover/";

    [Fact]
    public async Task Recover_ValidPayload_ReturnsOkWithTemporaryToken()
    {
        var request = new RecoverRequest(FakeData.BuildUniqueEmail());

        var response = await client.PostAsJsonAsync(Endpoint, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsync<Result<RecoverResponse>>();
        Assert.NotNull(body);
        Assert.True(body!.Success);
        Assert.NotNull(body.Data);
        Assert.False(string.IsNullOrWhiteSpace(body.Data!.Message));
        Assert.False(string.IsNullOrWhiteSpace(body.Data.TemporaryResetToken));
    }

    [Fact]
    public async Task Recover_ExistingAndMissingEmail_ReturnSameNeutralMessage()
    {
        var (existingEmail, _) = await client.RegisterAsync();

        var existingResponse = await client.PostAsJsonAsync(Endpoint, new RecoverRequest(existingEmail));
        var missingResponse = await client.PostAsJsonAsync(Endpoint, new RecoverRequest(FakeData.BuildUniqueEmail()));

        Assert.Equal(HttpStatusCode.OK, existingResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, missingResponse.StatusCode);

        var existingBody = await existingResponse.Content.ReadAsync<Result<RecoverResponse>>();
        var missingBody = await missingResponse.Content.ReadAsync<Result<RecoverResponse>>();

        Assert.NotNull(existingBody);
        Assert.NotNull(missingBody);
        Assert.NotNull(existingBody!.Data);
        Assert.NotNull(missingBody!.Data);

        Assert.Equal(existingBody.Data!.Message, missingBody.Data!.Message);
        Assert.False(string.IsNullOrWhiteSpace(existingBody.Data.TemporaryResetToken));
        Assert.False(string.IsNullOrWhiteSpace(missingBody.Data.TemporaryResetToken));
    }

    [Fact]
    public async Task Recover_InvalidEmail_ReturnsUnprocessableEntity()
    {
        var response = await client.PostAsJsonAsync(Endpoint, new RecoverRequest("invalid-email"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var body = await response.Content.ReadAsync<Result<RecoverResponse>>();
        Assert.NotNull(body);
        Assert.False(body!.Success);
        Assert.Equal("Invalid email format", body.Error);
    }

    [Fact]
    public async Task Recover_MalformedJson_ReturnsUnprocessableEntity()
    {
        using var content = new StringContent("{\"email\":", Encoding.UTF8, "application/json");

        var response = await client.PostAsync(Endpoint, content);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var body = await response.Content.ReadAsync<Result<RecoverResponse>>();
        Assert.NotNull(body);
        Assert.False(body!.Success);
        Assert.Equal("Invalid payload", body.Error);
    }

    [Fact]
    public async Task Recover_TooManyAttempts_ReturnsTooManyRequests()
    {
        var email = FakeData.BuildUniqueEmail();

        for (var i = 0; i < 5; i++)
        {
            var okResponse = await client.PostAsJsonAsync(Endpoint, new RecoverRequest(email));
            Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);
        }

        var blockedResponse = await client.PostAsJsonAsync(Endpoint, new RecoverRequest(email));

        Assert.Equal(HttpStatusCode.TooManyRequests, blockedResponse.StatusCode);

        var body = await blockedResponse.Content.ReadAsync<Result<RecoverResponse>>();
        Assert.NotNull(body);
        Assert.False(body!.Success);
        Assert.Equal("Too many recover attempts. Try again later", body.Error);
    }
}
