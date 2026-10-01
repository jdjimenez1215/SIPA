using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MsInscripcion.Api.Json;

/// <summary>Serializes TimeOnly as "HH:mm" (the default System.Text.Json format is "HH:mm:ss").</summary>
public sealed class TimeOnlyHHmmConverter : JsonConverter<TimeOnly>
{
    private const string Format = "HH:mm";

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;

        if (text is not null && TimeOnly.TryParseExact(text, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var value))
            return value;

        throw new JsonException("La hora debe tener el formato HH:mm (por ejemplo 08:00).");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(Format, CultureInfo.InvariantCulture));
}
