namespace NEngine.CoreLibs.Mathematics;
public static class BoolExtensions
{
    /// <summary>
    /// Converts true to 1 and false to 0
    /// </summary>
    /// <param name="b">the bool to convert</param>
    /// <returns>1 for true, 0 for false</returns>
    public static int ToInt(this bool b) => b ? 1 : 0;
}
