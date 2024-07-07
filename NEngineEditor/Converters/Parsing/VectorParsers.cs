using SFML.System;
using System.Text.RegularExpressions;

namespace NEngineEditor.Converters.Parsing;

public static partial class Vector2fParser
{
    public static Vector2f ParseOrZero(string vector2fString)
    {
        var match = ValidVector2fRegex().Match(vector2fString);
        if (match.Groups.Count == 3 && float.TryParse(match.Groups[1].Value, out float x) && float.TryParse(match.Groups[2].Value, out float y))
        {
            return new(x, y);
        }
        return new(0, 0);
    }
    [GeneratedRegex(@"\{\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*\}")]
    public static partial Regex ValidVector2fRegex();
}

public static partial class Vector2iParser
{
    public static Vector2i ParseOrZero(string vector2iString)
    {
        var match = ValidVector2iRegex().Match(vector2iString);
        if (match.Groups.Count == 3 && int.TryParse(match.Groups[1].Value, out int x) && int.TryParse(match.Groups[2].Value, out int y))
        {
            return new(x, y);
        }
        return new(0, 0);
    }
    [GeneratedRegex(@"\{\s*(\d+)\s*,\s*(\d+)\s*\}")]
    public static partial Regex ValidVector2iRegex();
}
public static partial class Vector2uParser
{
    public static Vector2u ParseOrZero(string vector2uString)
    {
        var match = ValidVector2uRegex().Match(vector2uString);
        if (match.Groups.Count == 3 && uint.TryParse(match.Groups[1].Value, out uint x) && uint.TryParse(match.Groups[2].Value, out uint y))
        {
            return new(x, y);
        }
        return new(0, 0);
    }
    [GeneratedRegex(@"\{\s*(\d+)\s*,\s*(\d+)\s*\}")]
    public static partial Regex ValidVector2uRegex();
}

public static partial class Vector3fParser
{
    public static Vector3f ParseOrZero(string vector3fString)
    {
        var match = ValidVector3fRegex().Match(vector3fString);
        if (match.Groups.Count == 4 && float.TryParse(match.Groups[1].Value, out float x) && float.TryParse(match.Groups[2].Value, out float y) && float.TryParse(match.Groups[3].Value, out float z))
        {
            return new(x, y, z);
        }
        return new(0, 0, 0);
    }
    [GeneratedRegex(@"\{\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*\}")]
    public static partial Regex ValidVector3fRegex();
}
