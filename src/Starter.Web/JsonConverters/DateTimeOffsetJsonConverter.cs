using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starter.Web.JsonConverters;

public class DateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetDateTimeOffset();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        // Always write in ISO 8601 format: "2024-12-25T00:00:00.0000000+00:00"
        writer.WriteStringValue(value.ToString("O"));
    }
}
