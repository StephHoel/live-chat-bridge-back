using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using LCB.Domain.Extensions;

namespace LCB.Api.Json;

public class PermissiveDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s))
                return null;

            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
                return dt;

            if (DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
                return dto.UtcDateTime;

            throw new JsonException($"Unsupported DateTime format: '{s}'");
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            // Accept unix epoch seconds or milliseconds
            if (reader.TryGetInt64(out var number))
            {
                try
                {
                    // heuristics: milliseconds if greater than year 3000 in seconds
                    if (number > 9999999999L)
                    {
                        var dto = DateTimeOffset.FromUnixTimeMilliseconds(number);
                        return dto.UtcDateTime;
                    }
                    else
                    {
                        var dto = DateTimeOffset.FromUnixTimeSeconds(number);
                        return dto.UtcDateTime;
                    }
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    throw new JsonException("Invalid Unix timestamp value.", ex);
                }
            }
        }

        throw new JsonException($"Unsupported token parsing DateTime: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.NormalizeToUtcMinus3().AsUtcMinus3Offset().ToString("o", CultureInfo.InvariantCulture));
    }
}
