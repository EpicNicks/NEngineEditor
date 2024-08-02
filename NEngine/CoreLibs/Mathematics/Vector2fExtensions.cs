using SFML.Graphics;
using SFML.System;

using static System.Math;

namespace NEngine.CoreLibs.Mathematics;

public static class Vector2fExtensions
{
    public static Vector2f ToVector(this (float x, float y) vectorTuple) => new Vector2f(vectorTuple.x, vectorTuple.y);
    public static double Length(this Vector2f vector2F) => vector2F == new Vector2f() ? 0 : Sqrt(Pow(vector2F.X, 2) + Pow(vector2F.Y, 2));
    public static double Distance(this Vector2f from, Vector2f to) => Sqrt(Pow(to.X - from.X, 2) + Pow(to.Y - from.Y, 2));
    public static Vector2f Normalize(this Vector2f vector2F)
    {
        double length = vector2F.Length();
        const double minThreshold = 1e-6;
        if (length == 0 || length < minThreshold)
        {
            return new Vector2f();
        }
        return vector2F / (float)vector2F.Length();
    }
    public static Vector2f Clamp(this Vector2f toClamp, float minX, float maxX, float minY, float maxY)
        => new(System.Math.Clamp(toClamp.X, minX, maxX), System.Math.Clamp(toClamp.Y, minY, maxY));
    public static Vector2f ClampX(this Vector2f toClamp, float minX, float maxX) => new(System.Math.Clamp(toClamp.X, minX, maxX), toClamp.Y);
    public static Vector2f ClampY(this Vector2f toClamp, float minY, float maxY) => new(toClamp.X, System.Math.Clamp(toClamp.Y, minY, maxY));
}
