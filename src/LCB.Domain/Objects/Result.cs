using System.Net;
using System.Text.Json.Serialization;

namespace LCB.Domain.Objects;

public class Result<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }
    [JsonIgnore]
    public HttpStatusCode? ErrorType { get; private set; }

    public static Result<T> Ok(T data)
        => new() { Success = true, Data = data };

    public static Result<T> Fail(string error, HttpStatusCode type)
        => new() { Success = false, Error = error, ErrorType = type };
}