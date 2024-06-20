namespace NEngine.CoreLibs.Mathematics;
public static class Math
{
    /// <summary>
    /// PingPong returns a value that increments and decrements between zero and the length. It follows the triangle wave formula where the bottom is set to zero and the peak is set to length.
    /// PingPong requires the value t to be a self-incrementing value.For example, GameWindow.Time
    /// </summary>
    /// <param name="time">The absolute time elapsed (usually GameWindow.Time)</param>
    /// <param name="length">The max value to PingPong from zero to</param>
    /// <returns></returns>
    public static float PingPong(float time, float length) => length - System.Math.Abs(time % (2 * length) - length);
}
