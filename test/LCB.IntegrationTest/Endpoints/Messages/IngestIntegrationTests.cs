using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LCB.Application.Commands.Login;
using LCB.Application.Commands.Message.Ingest;
using LCB.Domain.Enums;
using LCB.Domain.Objects;
using LCB.IntegrationTest.Constants;
using LCB.IntegrationTest.Helpers;
using LCB.IntegrationTest.Infrastructure;
using Xunit;

namespace LCB.IntegrationTest.Endpoints.Messages;

public class IngestIntegrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly string endpoint = "/messages/ingest";

    [Fact]
    public async Task Ingest_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var request = new MessageIngestRequest(ProviderTypeEnum.TIKTOK, "integration-user", "!fila", DateTime.UtcNow);
        var response = await _client.PostAsJsonAsync(endpoint, request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadAsync<Result<MessageIngestResponse>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal("Unauthorized", body.Error);
    }

    [Fact]
    public async Task Ingest_WithInvalidToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "invalid-token");

        var request = new MessageIngestRequest(ProviderTypeEnum.TIKTOK, "integration-user", "!fila", DateTime.UtcNow);
        var response = await _client.PostAsJsonAsync(endpoint, request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadAsync<Result<MessageIngestResponse>>();
        Assert.NotNull(body);
        Assert.False(body.Success);
        Assert.Equal("Unauthorized", body.Error);
    }

    [Fact]
    public async Task Ingest_WithValidToken_Returns200()
    {
        var token = await _client.RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var request = new MessageIngestRequest(ProviderTypeEnum.TIKTOK, "integration-user", "!fila", DateTime.UtcNow);
        var response = await _client.PostAsJsonAsync(endpoint, request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsync<Result<MessageIngestResponse>>();
        Assert.NotNull(body);
        Assert.True(body.Success);
    }

    [Fact]
    public async Task Ingest_WithDuplicateMessage_ReturnsBadRequest()
    {
        var token = await _client.RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new MessageIngestRequest(ProviderTypeEnum.TIKTOK, "duplicate-user", "!fila", new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        var firstResponse = await _client.PostAsJsonAsync(endpoint, payload);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var duplicateResponse = await _client.PostAsJsonAsync(endpoint, payload);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);

        var duplicateBody = await duplicateResponse.Content.ReadAsync<Result<MessageIngestResponse>>();
        Assert.NotNull(duplicateBody);
        Assert.False(duplicateBody.Success);
        Assert.Equal("Invalid payload", duplicateBody.Error);
        Assert.NotNull(duplicateBody.Data);
        Assert.Equal(StatusResultEnum.Duplicate, duplicateBody.Data!.Status);
    }
}
