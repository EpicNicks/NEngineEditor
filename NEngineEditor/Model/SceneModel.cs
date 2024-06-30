namespace NEngineEditor.Model;
public class SceneModel
{
    public string? Name { get; set; }
    public List<GameObjectWrapperModel> SceneGameObjects = [];

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
