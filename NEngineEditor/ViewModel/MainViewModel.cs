using NEngineEditor.Model;
using System.Collections.ObjectModel;

namespace NEngineEditor.ViewModel;
public class MainViewModel : ViewModelBase
{
    public string ProjectDirectory { get; set; }
    public Guid LoadedSceneGuid { get; }
    public ObservableCollection<GameObjectWrapperModel> GameObjectWrapperModels { get; } = [];

    private static MainViewModel? instance;
    public static MainViewModel Instance => instance ??= new MainViewModel();

    private MainViewModel()
    {
        // for testing
        GameObjectWrapperModels = [
            new GameObjectWrapperModel(Guid.NewGuid(), new NEngine.GameObjects.GameObject{ Name = "Sid"})
        ];
    }
}
