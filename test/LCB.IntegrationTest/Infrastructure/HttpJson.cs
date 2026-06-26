using System.Net.Http.Json;
using System.Text.Json;

namespace LCB.IntegrationTest.Infrastructure;

public static class HttpJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<T?> ReadAsync<T>(this HttpContent content)
        => await content.ReadFromJsonAsync<T>(SerializerOptions);
}
