using System.Collections.ObjectModel;
using System.Collections.Specialized;

using NEngine.GameObjects;

namespace NEngineEditor.Model;
public class SceneModel
{
    public string? Name { get; set; }
    public ObservableCollection<GameObjectWrapperModel> SceneGameObjects = [];

    public SceneModel()
    {
        SceneGameObjects.CollectionChanged += SceneGameObjects_CollectionChanged;
    }

    private void SceneGameObjects_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is null)
        {
            return;
        }
        foreach (var item in e.NewItems)
        {
            if
            (
                item is GameObjectWrapperModel gameObjectWrapperModel
                && 
                (
                    gameObjectWrapperModel.GameObjectClass is null 
                    || (!Type.GetType(gameObjectWrapperModel.GameObjectClass)?.IsAssignableTo(typeof(GameObject)) ?? false)
                )
            )
            {
                SceneGameObjects.Remove(gameObjectWrapperModel);
            }
        }
    }

    public static SceneModel CreateFromFile(string path)
    {
        // try load file
        //  success: continue
        //  fail: notify path invalid
        // try read json
        //  success: return SceneModel with List
        //  fail: notify file invalid
        throw new NotImplementedException();
    }

    public static SceneModel FromJson(string jsonString)
    {
        // parse JSON, try return valid scene
        throw new NotImplementedException();
    }
}
