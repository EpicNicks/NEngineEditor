using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.IO;

using NEngine.GameObjects;
using NEngine.Window;

using NEngineEditor.Commands;
using NEngineEditor.Commands.Generic;
using NEngineEditor.Helpers;
using NEngineEditor.Model;
using NEngineEditor.Model.JsonSerialized;
using NEngineEditor.Managers;
using NEngineEditor.Windows;
using NEngineEditor.View;
using NEngineEditor.ScriptCompilation;
using NEngine.CoreLibs.GameObjects;

namespace NEngineEditor.ViewModel;
public partial class MainViewModel : ViewModelBase
{
    private readonly EditorActionHistory _editorActionHistory;
    private ScriptCompilationSystem _scriptCompilationSystem;

    private ICommand? _saveCommand;
    public ICommand SaveCommand => _saveCommand ??= new ActionCommand(SaveScene);

    private ICommand? _deleteSelectedInstanceCommand;
    public ICommand DeleteSelectedInstanceCommand => _deleteSelectedInstanceCommand ??= new ActionCommand(() =>
    {
        if (SelectedGameObject is not null)
        {
            DeleteInstanceCommand.Execute(SelectedGameObject);
            SelectedGameObject = null;
        }
    });

    private ICommand? _deleteInstanceCommand;
    public ICommand DeleteInstanceCommand => _deleteInstanceCommand ??= new ActionCommand<LayeredGameObject>(selectedLgo =>
    {
        PerformActionCommand.Execute(new EditorAction
        {
            DoAction = () => SceneGameObjects.Remove(selectedLgo),
            UndoAction = () => SceneGameObjects.Add(selectedLgo)
        });
        SceneGameObjects.Remove(selectedLgo);
    });
    private ICommand? _duplicateSelectedInstanceCommand;
    public ICommand DuplicateSelectedInstanceCommand => _duplicateSelectedInstanceCommand ??= new ActionCommand(() =>
    {
        if (SelectedGameObject is not null)
        {
            DuplicateInstanceCommand.Execute(SelectedGameObject);
        }
    });

    private ICommand? _duplicateInstanceCommand;
    public ICommand DuplicateInstanceCommand => _duplicateInstanceCommand ??= new ActionCommand<LayeredGameObject>(selectedLgo =>
    {
        if (Activator.CreateInstance(selectedLgo.GameObject.GetType()) is not GameObject reloadedGameObject)
        {
            MessageBox.Show("Failed to create duplicate instance of GameObject", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        ObjectCloner.CloneMembers(selectedLgo.GameObject, reloadedGameObject, ObjectCloner.MemberTypes.Fields | ObjectCloner.MemberTypes.Properties);
        reloadedGameObject.Name = selectedLgo.GameObject.Name ?? "Nameless GO";
        SelectedGameObject = AddGameObjectToScene(new LayeredGameObject { GameObject = reloadedGameObject, RenderLayer = selectedLgo.RenderLayer });
    });

    private ICommand? _performActionCommand;
    public ICommand PerformActionCommand => _performActionCommand ??= new ActionCommand<EditorAction>(_editorActionHistory.PerformAction);

    private ICommand? _undoActionCommand;
    public ICommand UndoActionCommand => _undoActionCommand ??= new ActionCommand(() => _editorActionHistory.UndoAction());

    private ICommand? _redoActionCommand;
    public ICommand RedoActionCommand => _redoActionCommand ??= new ActionCommand(() => _editorActionHistory.RedoAction());

    private (string name, string filepath) _loadedScene;
    public (string name, string filepath) LoadedScene
    {
        get => _loadedScene;
        set
        {
            _loadedScene = value;
            OnPropertyChanged(nameof(LoadedScene));
            OnPropertyChanged(nameof(LoadedSceneName));
        }
    }
    public string LoadedSceneName
    {
        get => LoadedScene.name;
        set
        {
            LoadedScene = (value, LoadedScene.filepath);
            OnPropertyChanged(nameof(LoadedSceneName));
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

    private static Lazy<MainViewModel> instance = new(() => new());
    public static MainViewModel Instance => instance.Value;
    public static void ClearInstance()
    {
        instance = new(() => new());
    }

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
        _loadedScene = ("Unnamed Scene", "");
        _sceneGameObjects = [];
        string projectName = Directory.GetFiles(MainWindow.ProjectDirectory).FirstOrDefault(path => path.EndsWith(".csproj")) ?? throw new InvalidDataException("csproj is missing from root directory!");
        _scriptCompilationSystem = new ScriptCompilationSystem(projectName);
        _scriptCompilationSystem.InitializeAssembliesAsync();
        _scriptCompilationSystem.AssemblyInitialized += (_, _) => _scriptCompilationSystem.StartWatching();
        _editorActionHistory = new EditorActionHistory();
    }

    private void ModifySceneObjectsListWrapper(Action modifySceneAction)
    {
        if (SceneEditViewUserControl.LazyInstance is null)
        {
            modifySceneAction?.Invoke();
            return;
        }
        SceneEditViewUserControl.LazyInstance.ShouldRender = false;
        modifySceneAction?.Invoke();
        SceneEditViewUserControl.LazyInstance.ShouldRender = true;
    }

    public LayeredGameObject? AddGameObjectToScene(string filepath)
    {
        string? className = Path.GetFileNameWithoutExtension(filepath);
        if (className is not null)
        {
            GameObject? gameObjectInstance = _scriptCompilationSystem.CreateInstance<GameObject>(className);
            if (gameObjectInstance is not null)
            {
                gameObjectInstance.Name = $"New {className}";
                RenderLayer renderLayer = gameObjectInstance switch
                {
                    UIAnchored => RenderLayer.UI,
                    _ => RenderLayer.BASE
                };
                return AddGameObjectToScene(new LayeredGameObject { GameObject = gameObjectInstance, RenderLayer = renderLayer });
            }
            else
            {
                // the className may have not matched any compiled types
            }
        }
        return null;
    }

    /// <summary>
    /// Creates a LayeredGameObject based on the one passed, adds it to the Scene, and returns the result.
    /// </summary>
    /// <param name="lgo">The LayeredGameObject to add.</param>
    /// <returns>The created LayeredGameObject actually added.</returns>
    public LayeredGameObject AddGameObjectToScene(LayeredGameObject lgo)
    {
        GameObject toAdd = lgo.GameObject;
        toAdd.Name ??= "Nameless GO";
        while (SceneGameObjects.Any(sgo => sgo.GameObject.Name == toAdd.Name))
        {
            Match match = SceneGameObjectInstanceNumberRegex().Match(toAdd.Name);
            if (match.Groups.Count == 3 && int.TryParse(match.Groups[2].Value, out int instanceNumber))
            {
                toAdd.Name = $"{match.Groups[1].Value}({instanceNumber + 1})";
            }
            else
            {
                toAdd.Name += " (1)";
            }
        }
        LayeredGameObject layeredGameObjectToAdd = new LayeredGameObject { GameObject = toAdd, RenderLayer = lgo.RenderLayer };
        SceneGameObjects.Add(layeredGameObjectToAdd);
        PerformActionCommand.Execute(new EditorAction
        {
            DoAction = () => SceneGameObjects.Add(layeredGameObjectToAdd),
            UndoAction = () => SceneGameObjects.Remove(layeredGameObjectToAdd)
        });
        SoftReloadScene();
        return layeredGameObjectToAdd;
    }

    public void ReloadScene()
    {
        if (!string.IsNullOrEmpty(_loadedScene.filepath))
        {
            LoadScene(_loadedScene.filepath);
        }
        else
        {
            SoftReloadScene();
            Logger.LogInfo("Reloaded unnamed scene, refreshing all objects within");
        }
    }

    public void SoftReloadScene()
    {
        ModifySceneObjectsListWrapper(() =>
        {
            try
            {
                SelectedGameObject = null;
                SceneModel tempScene = SceneLoader.WriteGameObjectsToScene(_loadedScene.name, SceneGameObjects);
                List<LayeredGameObject> loadedGameObjects = SceneLoader.LoadSceneFromSceneModel(tempScene, _scriptCompilationSystem);
                SceneGameObjects.Clear();
                loadedGameObjects.ForEach(SceneGameObjects.Add);
            }
            catch (Exception ex)
            {
                Logger.LogError($"An Exception has occurred while soft-reloading the scene: {ex}");
            }
        });
    }

    public void LoadScene(string filePath)
    {
        ModifySceneObjectsListWrapper(() =>
        {
            string jsonString = File.ReadAllText(filePath);
            try
            {
                (string sceneName, List<LayeredGameObject> loadedGameObjects) = SceneLoader.LoadSceneFromJson(jsonString, _scriptCompilationSystem);
                SceneGameObjects.Clear();
                loadedGameObjects.ForEach(SceneGameObjects.Add);
                LoadedScene = (sceneName, filePath);
                _editorActionHistory.ClearHistory();
            }
            catch (Exception ex)
            {
                Logger.LogError($"An Exception Occurred while loading scene {Path.GetFileNameWithoutExtension(filePath)}, Exception: {ex}");
            }
        });
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
                string newSceneFilePath = Path.Combine(ContentBrowserViewModel.subDirectory.CurrentSubDir, newItemDialog.EnteredName + ".scene");
                Save(newSceneFilePath);
                LoadedScene = (newItemDialog.EnteredName, newSceneFilePath);
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
                string assetsPath = Path.Join(MainWindow.ProjectDirectory, "Assets");
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

    [GeneratedRegex(@"(.+)\((\d+)\)$")]
    private static partial Regex SceneGameObjectInstanceNumberRegex();
}
