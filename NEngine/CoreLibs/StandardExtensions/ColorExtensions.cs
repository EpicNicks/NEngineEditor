using SFML.Graphics;
using Math = NEngine.CoreLibs.Mathematics.Math;

// Extensions to the underlying standard pieces, may be swapped out with a new implementation if SFML is dropped and the Color type is changed
//  though types such as SFML.Graphics.Color may be used even after SFML is dropped since it's just a simple struct
namespace NEngine.CoreLibs.StandardExtensions;

public static class ColorExtensions
{
    public static Color Lerp(this Color color1, Color color2, float t)
    {
        t = System.Math.Clamp(t, 0.0f, 1.0f);

        byte r = (byte)(color1.R + (color2.R - color1.R) * t);
        byte g = (byte)(color1.G + (color2.G - color1.G) * t);
        byte b = (byte)(color1.B + (color2.B - color1.B) * t);
        byte a = (byte)(color1.A + (color2.A - color1.A) * t);
        
        return new Color(r, g, b, a);
    }

    public static Color PingPong(this Color color1, Color color2, float time, float cycleDuration)
    {
        return Lerp(color1, color2, Math.PingPong(time, cycleDuration) / cycleDuration);
    }

    public static Color Random(Random rnd)
        => new Color(unchecked((uint)rnd.Next(int.MaxValue)));

    public static Color RandomOpaque(Random rnd)
        => new Color((byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256));

    public static Color RandomBrightColor(Random rnd) 
        => new Color((byte)rnd.Next(128, 256), (byte)rnd.Next(128, 256), (byte)rnd.Next(128, 256));

    public static Color RandomDarkColor(Random rnd) 
        => new Color((byte)rnd.Next(128), (byte)rnd.Next(128), (byte)rnd.Next(128));

    public static Color RandomWarmColor(Random rnd) 
        => new Color((byte)rnd.Next(128, 256), (byte)rnd.Next(256), (byte)rnd.Next(128));

    public static Color RandomColdColor(Random rnd) 
        => new Color((byte)rnd.Next(128), (byte)rnd.Next(128), (byte)rnd.Next(128, 256));
}
