using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Input;
using System.Text.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using System.IO;

using SFML.System;

using NEngine.GameObjects;
using NEngine.Window;

using NEngineEditor.Commands;
using NEngineEditor.Converters.Json;
using NEngineEditor.Converters.Parsing;
using NEngineEditor.Helpers;
using NEngineEditor.Model;
using NEngineEditor.Model.JsonSerialized;
using NEngineEditor.Managers;
using NEngineEditor.Windows;

namespace NEngineEditor.ViewModel;
public class MainViewModel : ViewModelBase
{
    private ICommand? _saveCommand;
    public ICommand SaveCommand => _saveCommand ??= new ActionCommand(() => SaveScene());

    // set in MainWindow once the project is selected
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
    private (string name, string filepath) _loadedScene;
    public (string name, string filepath) LoadedScene
    {
        get => _loadedScene;
        set
        {
            _loadedScene = value;
            OnPropertyChanged(nameof(LoadedScene));
        }
    }
    public ContentBrowserViewModel? ContentBrowserViewModel { get; set; }

    // For the inspector (null when none is selected)
    private LayeredGameObject? _selectedGameObject;
    public LayeredGameObject? SelectedGameObject
    {
        get => _selectedGameObject;
        set
        {
            _selectedGameObject = value;
            OnPropertyChanged(nameof(SelectedGameObject));
        }
    }

    private ObservableCollection<LayeredGameObject> _sceneGameObjects;
    public ObservableCollection<LayeredGameObject> SceneGameObjects
    {
        get => _sceneGameObjects;
        set
        {
            _sceneGameObjects = value;
            OnPropertyChanged(nameof(SceneGameObjects));
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

    private static readonly Lazy<MainViewModel> instance = new(() => new MainViewModel());
    public static MainViewModel Instance => instance.Value;

    public class LayeredGameObject
    {
        public required RenderLayer RenderLayer { get; set; }
        public required GameObject GameObject { get; set; }
        public void Deconstruct(out RenderLayer renderLayer, out GameObject gameObject)
        {
            renderLayer = RenderLayer;
            gameObject = GameObject;
        }
        public override string ToString()
        {
            return GameObject?.Name ?? "Nameless GO (should be auto-renamed by the scene manager to avoid naming collisions)";
        }
    }

    private MainViewModel()
    {
        Logs.CollectionChanged += Logs_CollectionChanged;
        _loadedScene = ("Unnamed Scene", "");
        _sceneGameObjects = [];
    }

    public void ReloadScene()
    {
        if (!string.IsNullOrEmpty(_loadedScene.filepath))
        {
            LoadScene(_loadedScene.filepath);
        }
        else
        {
            try
            {
                SelectedGameObject = null;
                SceneModel tempScene = SceneLoader.WriteGameObjectsToScene(_loadedScene.name, SceneGameObjects);
                List<LayeredGameObject> loadedGameObjects = SceneLoader.LoadSceneFromSceneModel(tempScene);
                SceneGameObjects.Clear();
                loadedGameObjects.ForEach(SceneGameObjects.Add);
                Logger.LogInfo("Reloaded unnamed scene, refreshing all objects within");
            }
            catch (Exception ex)
            {
                Logger.LogError($"An Exception has occurred while reloading the unnamed scene: {ex}");
            }
        }
    }

    // private static readonly string[] _specialProperties = ["Position", "Rotation", "Scale"];
    public void LoadScene(string filePath)
    {
        string jsonString = File.ReadAllText(filePath);
        try
        {
            (string sceneName, List<LayeredGameObject> loadedGameObjects) = SceneLoader.LoadSceneFromJson(jsonString);
            SceneGameObjects.Clear();
            loadedGameObjects.ForEach(SceneGameObjects.Add);
            LoadedScene = (sceneName, filePath);
        }
        catch (Exception ex)
        {
            Logger.LogError($"An Exception Occurred while loading scene {Path.GetFileNameWithoutExtension(filePath)}, Exception: {ex}");
        }
    }
    private static readonly JsonSerializerOptions sceneSaveJsonSerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };
    public void SaveScene()
    {
        if (ContentBrowserViewModel is null)
        {
            Logger.LogError("ContentViewBrowser was null and couldn't be accessed to save the scene");
            return;
        }
        if (!File.Exists(LoadedScene.filepath))
        {
            LoadedScene = ("Unnamed Scene", "");
        }
        if (string.IsNullOrEmpty(LoadedScene.filepath))
        {
            ContentBrowserViewModel.CreateItemType createItemType = ContentBrowserViewModel.CreateItemType.SCENE;
            NewItemDialog newItemDialog = new(createItemType);
            if (newItemDialog.ShowDialog() == true)
            {
                if (string.IsNullOrEmpty(newItemDialog.EnteredName))
                {
                    MessageBox.Show("The Entered Name was empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Logger.LogError($"The name you entered for the {createItemType} you tried to create was empty somehow.");
                    return;
                }
                ContentBrowserViewModel.CreateItem(ContentBrowserViewModel.subDirectory.CurrentSubDir, createItemType, newItemDialog.EnteredName);
                Save(Path.Combine(ContentBrowserViewModel.subDirectory.CurrentSubDir, newItemDialog.EnteredName + ".scene"));
            }
        }
        else
        {
            Save(Path.Combine(ContentBrowserViewModel.subDirectory.CurrentSubDir, LoadedScene.filepath));
        }

        void Save(string filePath)
        {
            SceneModel sceneToWrite = SceneLoader.WriteGameObjectsToScene(Path.GetFileNameWithoutExtension(filePath), SceneGameObjects);
            string jsonSerializedScene = JsonSerializer.Serialize(sceneToWrite, sceneSaveJsonSerializerOptions);
            File.WriteAllText(filePath, jsonSerializedScene);
            ContentBrowserViewModel.LoadFilesInCurrentDir();
        }
    }

    public void OpenAddScenesToBuildWindow()
    {
        AddScenesToBuildWindow addScenesToBuildDialog = new();
        if (addScenesToBuildDialog.ShowDialog() == true)
        {
            try
            {
                string assetsPath = Path.Join(Instance.ProjectDirectory, "Assets");
                string projectConfigPath = Path.Combine(assetsPath, "ProjectConfig.json");
                string projectConfigString = File.ReadAllText(projectConfigPath);
                ProjectConfig? projectConfig = JsonSerializer.Deserialize<ProjectConfig>(projectConfigString);
                if (projectConfig is null)
                {
                    Logger.LogError("Scenes could not be processed, project config not found");
                    // probably just regenerate it
                    return;
                }
                projectConfig.Scenes = addScenesToBuildDialog.SelectedScenePaths;
                File.WriteAllText(projectConfigPath, JsonSerializer.Serialize(projectConfig, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                Logger.LogError($"An Exception occurred while processing the selected scenes\n\n{ex}");
            }
        }
    }

    private readonly object _lockObject = new();
    private void Logs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        const int MAX_LOGS = 10_000;
        if (e.NewItems is not null)
        {
            Task.Run(() => TrimLogs(MAX_LOGS));
        }
    }
    private void TrimLogs(int maxLogs)
    {
        lock (_lockObject)
        {
            try
            {
                while (Logs.Count > maxLogs)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (Logs.Count > maxLogs)
                        {
                            Logs.RemoveAt(0);
                        }
                    });
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"An Error Occurred while Trimming Logs from the Logger {e}");
            }
        }
    }
}
