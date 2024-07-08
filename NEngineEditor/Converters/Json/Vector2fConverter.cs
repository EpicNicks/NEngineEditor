using System.Text.Json.Serialization;
using System.Text.Json;

using SFML.System;

namespace NEngineEditor.Converters.Json;
public class Vector2fConverter : JsonConverter<Vector2f>
{
    public override Vector2f Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected start of object");

        Vector2f result = new Vector2f();
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
                    result.X = reader.GetSingle();
                    break;
                case "Y":
                    result.Y = reader.GetSingle();
                    break;
                default:
                    throw new JsonException($"Unexpected property: {propertyName}");
            }
        }

        throw new JsonException("Expected end of object");
    }

    public override void Write(Utf8JsonWriter writer, Vector2f value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteEndObject();
    }
}
