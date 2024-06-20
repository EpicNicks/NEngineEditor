using SFML.System;
using SFML.Window;

using NEngine.CoreLibs.Mathematics;
using NEngine.Window;

namespace NEngine.GameObjects;

/// <summary>
/// Mixin/Trait class for things which can move in all directions on the screen.
/// </summary>
public abstract class Moveable : Positionable
{
    // add callbacks to GameWindow.RenderWindow for controlling an instance of a derived class
    public required float MoveSpeed { get; set; }

    /// <summary>
    /// For the common initialization of a dictionary for keys which are being pressed or released.
    /// No backing field will be provided by default since a Moveable may move by other means including mouse input or controller input.
    /// </summary>
    /// <param name="keys">The keys which may be pressed or released</param>
    /// <returns>A Dictionary of Keys which may be pressed mapped to a boolean representing whether or not the key is currently pressed</returns>
    public static Dictionary<Keyboard.Key, bool> KeysToPressedDict(IEnumerable<Keyboard.Key> keys) => new(keys.Select(key => new KeyValuePair<Keyboard.Key, bool>(key, false)));

    public void Move(Vector2f input)
    {
        if (input != new Vector2f())
        {
            Vector2f inputNormal = input.Normalize();
            Vector2f delta = inputNormal * MoveSpeed * GameWindow.DeltaTime.AsSeconds();
            Position += delta;
        }
    }
}
