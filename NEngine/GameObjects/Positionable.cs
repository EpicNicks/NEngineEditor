using SFML.Graphics;
using SFML.System;

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
            foreach (Transformable t in Drawables.OfType<Transformable>())
            {
                t.Position += delta;
            }
            if (Collider != null)
            {
                Collider.Bounds = new FloatRect(new Vector2f(Collider.Bounds.Left, Collider.Bounds.Top) + delta, new Vector2f(Collider.Bounds.Width, Collider.Bounds.Height));
            }
            position = value;
        }
    }

    private float rotation;
    public virtual float Rotation
    {
        get => rotation;
        set
        {
            float delta = value - rotation;
            foreach (Transformable t in Drawables.OfType<Transformable>())
            {
                t.Rotation += delta;
            }
            // FloatRect, and therefore Collider.Bounds, can't be rotated. May want to consider changing how collision works.
            rotation = value;
        }
    }

    private Vector2f scale = new(1f, 1f);
    public virtual Vector2f Scale
    {
        get => scale;
        set
        {
            Vector2f delta = value - Scale;
            foreach (Transformable t in Drawables.OfType<Transformable>())
            {
                t.Scale += delta;
            }
            scale = value;
        }
    }
}
