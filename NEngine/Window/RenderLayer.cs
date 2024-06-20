namespace NEngine.Window;
/// <summary>
/// The layer on which a GameObject is rendered.
/// The enum names are just helpers, the GameObjects's drawables will be drawn according to the underlying integer value.
/// These values can be negative.
/// </summary>
public enum RenderLayer
{
    /// <summary>
    /// The placeholder layer for objects that aren't rendered (no drawable)
    /// </summary>
    NONE = -1,
    /// <summary>
    /// The General "Base" layer for GameObjects which are rendered
    /// </summary>
    BASE = 0,
    /// <summary>
    /// The General UI layer for all UI objects meant to be drawn above other GameObjects.
    /// Space is left between BASE and UI to allow for intermediary layers which UI can be drawn over but BASE will not be drawn over.
    /// </summary>
    UI = 10,
}
