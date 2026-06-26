using System.Text.Json.Serialization;

namespace LCB.IntegrationTest.Infrastructure;

public sealed class ApiResult<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}

public sealed class LoginResponseDto
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }
}

public sealed class RegisterResponseDto
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
