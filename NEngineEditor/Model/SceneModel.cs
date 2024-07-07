using System.Collections.ObjectModel;
using System.Collections.Specialized;

using NEngine.GameObjects;

namespace NEngineEditor.Model;
public class SceneModel
{
    public string? Name { get; set; }
    public List<GameObjectWrapperModel> SceneGameObjects { get; set; } = [];
}
