using NEngine.Properties;
using SFML.Graphics;

namespace NEngine.CoreLibs.StandardFonts;
/// <summary>
/// Some Engine-Default preset fonts to use without loading in your own into your project.
/// </summary>
public static class Fonts
{
    public static readonly Font Arial = new(Resources.Arial);
    public static readonly Font Inter = new(Resources.Inter_Regular);
    public static readonly Font Roboto = new(Resources.Roboto_Regular);
}
