using NEngine.CoreLibs.GameObjects;
using NEngine.GameObjects;
using NEngine.Window;

namespace NEngineEditor.Model;
/// <summary>
/// A GUID identified container for a NEngine GameObject and its RenderLayer
/// </summary>
public class GameObjectWrapperModel
{
    /// <summary>
    /// Used to get the script source itself from the script loader which generates meta files containing GUIDs
    /// </summary>
    public Guid Guid { get; private set; }
    public GameObject GameObject { get; private set; }
    public RenderLayer RenderLayer { get; private set; }

    public GameObjectWrapperModel(Guid guid, GameObject gameObject)
    {
        Guid = guid;
        GameObject = gameObject;
        RenderLayer = DefaultRenderLayer(gameObject);
    }

    private static RenderLayer DefaultRenderLayer(GameObject gameObject) 
        => gameObject switch
    {
        UIAnchored => RenderLayer.UI,
        Positionable => RenderLayer.BASE,
        _ => RenderLayer.NONE
    };
}
