using SFML.Graphics;

namespace NEngine.CoreLibs.Mathematics;
public static class TransformableHelper
{
    public static void CenterOrigin(Transformable transformable, FloatRect localBounds)
    {
        transformable.Origin = new SFML.System.Vector2f(
            localBounds.Left + localBounds.Width / 2f,
            localBounds.Top + localBounds.Height / 2f
        );
    }

    public static void CenterOrigins(List<Drawable> drawables)
    {
        if (drawables is not null)
        {
            foreach (Transformable transformable in drawables.OfType<Transformable>())
            {
                if (transformable is Shape shape)
                {
                    CenterOrigin(shape, shape.GetLocalBounds());
                }
                else if (transformable is Text text)
                {
                    CenterOrigin(text, text.GetLocalBounds());
                }
                else if (transformable is Sprite sprite)
                {
                    CenterOrigin(sprite, sprite.GetLocalBounds());
                }
            }
        }
    }
}
