using SFML.System;
using SFML.Window;

using NEngine.CoreLibs.Mathematics;

namespace NEngine.GameObjects;

/// <summary>
/// Mixin/Trait class for things which can move in all directions on the screen.
/// </summary>
public abstract class Moveable : Positionable
{
    /// <summary>
    /// The speed at which this Moveable moves over frames
    /// </summary>
    public float moveSpeed;

    /// <summary>
    /// For the common initialization of a dictionary for keys which are being pressed or released.
    /// No backing field will be provided by default since a Moveable may move by other means including mouse input or controller input.
    /// </summary>
    /// <param name="keys">The keys which may be pressed or released</param>
    /// <returns>A Dictionary of Keys which may be pressed mapped to a boolean representing whether or not the key is currently pressed</returns>
    public static Dictionary<Keyboard.Key, bool> KeysToPressedDict(IEnumerable<Keyboard.Key> keys) => new(keys.Select(key => new KeyValuePair<Keyboard.Key, bool>(key, false)));

    /// <summary>
    /// Moves the Moveable by the given Vector2f.
    /// </summary>
    /// <param name="input">The input Vector to move the Moveable by.</param>
    public void Move(Vector2f input)
    {
        Position += input;
    }
}
