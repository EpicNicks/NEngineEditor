using System.Text.Json.Serialization;
using System.Text.Json;

using SFML.System;

namespace NEngineEditor.Converters.Json;
public class Vector2iConverter : JsonConverter<Vector2i>
{
    public override Vector2i Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object");

        Vector2i result = new Vector2i();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return result;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected property name");

            string? propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "X":
                    result.X = reader.GetInt32();
                    break;
                case "Y":
                    result.Y = reader.GetInt32();
                    break;
                default:
                    throw new JsonException($"Unexpected property: {propertyName}");
            }
        }

        throw new JsonException("Expected end of object");
    }

    public override void Write(Utf8JsonWriter writer, Vector2i value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteEndObject();
    }
}
