using SFML.Graphics;
using SFML.System;

using NEngine.GameObjects;

namespace NEngine.GameObjects;

/// <summary>
/// The base class for GameObjects which have a position.
/// Provides a base field implementation Vector2f 'Position' which handles itself, its collider, and all underlying drawables.
/// </summary>
public class Positionable: GameObject
{
    private Vector2f position = new Vector2f();
    public virtual Vector2f Position 
    {
        get => position;
        set
        {
            Vector2f delta = value - Position;
            foreach (Drawable d in Drawables)
            {
                if (d is Transformable t)
                {
                    t.Position += delta;
                }
            }
            if (Collider != null)
            {
                Collider.Bounds = new FloatRect(new Vector2f(Collider.Bounds.Left, Collider.Bounds.Top) + delta, new Vector2f(Collider.Bounds.Width, Collider.Bounds.Height));
            }
            position = value;
        }
    }
}
