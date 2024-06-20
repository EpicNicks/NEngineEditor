using SFML.Graphics;
using SFML.System;

namespace NEngine.CoreLibs.Mathematics;
public static class FloatRectExtensions
{
    public static Vector2f Size(this FloatRect floatRect) => new(floatRect.Width, floatRect.Height);
    public static Vector2f Position(this FloatRect floatRect) => new(floatRect.Left, floatRect.Top);
    public static (Vector2f size, Vector2f position) Destructure(this FloatRect floatRect) => (floatRect.Size(), floatRect.Position());
}
