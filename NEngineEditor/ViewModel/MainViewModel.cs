using NEngineEditor.Model;
using System.Collections.ObjectModel;

namespace NEngineEditor.ViewModel;
public class MainViewModel : ViewModelBase
{
    private string _projectDirectory = "";
    public string ProjectDirectory
    {
        get => _projectDirectory;
        set
        {
            _projectDirectory = value;
            OnPropertyChanged(nameof(ProjectDirectory));
        }
    }
    private Guid _loadedSceneGuid;
    public Guid LoadedSceneGuid
    {
        get => _loadedSceneGuid;
        set 
        {
            _loadedSceneGuid = value;
            OnPropertyChanged(nameof(LoadedSceneGuid));
        }
    }
    private ObservableCollection<GameObjectWrapperModel> _gameObjectWrapperModels = [];
    public ObservableCollection<GameObjectWrapperModel> GameObjectWrapperModels
    {
        get => _gameObjectWrapperModels;
        set
        {
            _gameObjectWrapperModels = value;
            OnPropertyChanged(nameof(GameObjectWrapperModels));
        }
    }

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
