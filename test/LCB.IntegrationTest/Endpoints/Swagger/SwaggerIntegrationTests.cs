using System.Net;
using LCB.IntegrationTest.Infrastructure;
using Xunit;

namespace LCB.IntegrationTest.Endpoints.Swagger;

public class SwaggerIntegrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SwaggerIndex_WithoutToken_Returns200AndHtml()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/swagger/index.html");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.Equal("text/html", contentType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Swagger UI", body);
    }

    [Fact]
    public async Task SwaggerJson_WithoutToken_Returns200AndOpenApiPayload()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.Equal("application/json", contentType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"openapi\"", body);
        Assert.Contains("\"/auth/login\"", body);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_RemainsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/worker/status");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
