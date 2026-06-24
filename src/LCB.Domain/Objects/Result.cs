using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json.Serialization;

namespace LCB.Domain.Objects;

[ExcludeFromCodeCoverage]
public class Result<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }
    [JsonIgnore]
    public HttpStatusCode? StatusCode { get; private set; }


    public static Result<T> Ok(T data, HttpStatusCode? statusCode = HttpStatusCode.OK)
        => new() { Success = true, Data = data, StatusCode = statusCode };

    public static Result<T> Fail(string error, HttpStatusCode statusCode)
        => new() { Success = false, Error = error, StatusCode = statusCode };

    public static Result<T> Fail(string error, T data, HttpStatusCode statusCode)
        => new() { Success = false, Error = error, Data = data, StatusCode = statusCode };
}