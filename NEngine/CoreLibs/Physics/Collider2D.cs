using SFML.Graphics;

using NEngine.CoreLibs.Mathematics;
using NEngine.GameObjects;

namespace NEngine.CoreLibs.Physics;

/// <summary>
/// Base for all colliders, provides lifecycle functions to override
/// </summary>
public class Collider2D
{
    /// <summary>
    /// The list of colliders that the collider is in contact with (used by GameWindow to determine which OnCollision/Trigger method to call on a frame)
    /// </summary>
    public List<Positionable> CollidingWith { get; private set; } = [];

    /// <summary>
    /// The GameObject attached to the Collider
    /// </summary>
    public required Positionable PositionableGameObject { get; set; }
    /// <summary>
    /// Whether or not the collider can be moved by another collider (can still be moved programmatically)
    /// </summary>
    public required bool IsStatic { get; set; }
    protected bool isActive = true;
    public bool IsActive
    {
        get => isActive;
        set
        {
            isActive = value;
            if (!value)
            {
                // unregister all ongoing collisions
                CollidingWith.Clear();
            }
        }
    }
    public bool IsTrigger { get; set; }
    public FloatRect Bounds { get; set; }

    public void RepositionFromCollision(FloatRect boundingBox)
    {
        PositionableGameObject.Position = Collisions.GetRepositionFromCollision(boundingBox, Bounds, out _);
    }

    public override string ToString()
    {
        return $"[Collider2D] bounds: {Bounds}";
    }
}
