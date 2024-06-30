using NEngine.GameObjects;
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

    public ObservableCollection<LogEntry> _logs = [];
    public ObservableCollection<LogEntry> Logs
    {
        get => _logs;
        set
        {
            _logs = value;
            OnPropertyChanged(nameof(Logs));
        }
    }

    private static MainViewModel? instance;
    public static MainViewModel Instance => instance ??= new MainViewModel();

    private MainViewModel()
    {
        // for testing
        GameObjectWrapperModels = [
            new GameObjectWrapperModel(nameof(GameObject)){ Name = "Sid" }
        ];
    }
}
