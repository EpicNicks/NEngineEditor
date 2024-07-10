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
            Logger.LogWarning("No scene loaded currently");
        }
    }

    // private static readonly string[] _specialProperties = ["Position", "Rotation", "Scale"];
    public void LoadScene(string filePath)
    {
        static object? ConvertProperty(string typeOfValue, string value)
        {
            return typeOfValue switch
            {
                "string" or "String" => value,
                "bool" or "Boolean" => bool.Parse(value),
                "int" or "Int32" => int.Parse(value),
                "float" or "Single" => float.Parse(value),
                "double" or "Double" => double.Parse(value),
                "Vector2u" => Vector2uParser.ParseOrZero(value),
                "Vector2f" => Vector2fParser.ParseOrZero(value),
                "Vector2i" => Vector2iParser.ParseOrZero(value),
                "Vector3f" => Vector3fParser.ParseOrZero(value),
                "Reference" or "reference" or "Guid" or "guid" => Guid.Parse(value),
                _ => ""
            };
        }

        static string? FindFilePathMatchingTypeInProject(string subdirectory, string className)
        {
            try
            {
                foreach (string file in Directory.GetFiles(subdirectory))
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file);
                    if (fileNameWithoutExtension.Equals(className, StringComparison.OrdinalIgnoreCase))
                    {
                        return file;
                    }
                }

                // Recursively check subdirectories
                foreach (string directory in Directory.GetDirectories(subdirectory))
                {
                    string? result = FindFilePathMatchingTypeInProject(directory, className);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Logger.LogError($"Access denied to {subdirectory}");
            }
            catch (Exception e)
            {
                Logger.LogError($"Error accessing {subdirectory}: {e.Message}");
            }

            // File not found
            return null;
        }

        JsonSerializerOptions jsonSerializerOptions = new();
        JsonConverter[] jsonConverters = [
            new Vector2fConverter(),
            new Vector2iConverter(),
            new Vector2uConverter(),
            new Vector3fConverter()
        ];
        foreach (JsonConverter jsonConverter in jsonConverters)
        {
            jsonSerializerOptions.Converters.Add(jsonConverter);
        }
        if (!File.Exists(filePath))
        {
            Logger.LogError($"The filepath {filePath} to the scene provided does not exist");
            return;
        }
        try
        {
            string jsonString = File.ReadAllText(filePath);
            SceneModel? sceneModel = JsonSerializer.Deserialize<SceneModel>(jsonString, jsonSerializerOptions);
            if (sceneModel is null)
            {
                Logger.LogError($"An Error Occurred while Deserializing scene at: {filePath}");
                return;
            }
            SceneGameObjects.Clear();
            LoadedScene = (sceneModel.Name ?? "Unnamed Scene", filePath);
            List<GameObjectWrapperModel> sceneGameObjectData = sceneModel.SceneGameObjects ?? [];
            List<GameObjectWrapperModel> invalidGameObjects = [];
            foreach (GameObjectWrapperModel gameObjectData in sceneGameObjectData)
            {
                if
                (
                    gameObjectData.GameObjectClass is null
                    || FindFilePathMatchingTypeInProject(Path.Join(ProjectDirectory, "Assets"), gameObjectData.GameObjectClass) is not string pathToFile
                    || ScriptCompiler.CompileAndInstantiateFromFile(pathToFile) is not GameObject gameObject
                )
                {
                    invalidGameObjects.Add(gameObjectData);
                    Logger.LogWarning($"Invalid GameObject {gameObjectData} found in scene that was being loaded");
                    continue;
                }
                gameObject.Name = gameObjectData.Name;
                SceneGameObjects.Add(new() { RenderLayer = gameObjectData.RenderLayer, GameObject = gameObject });
            }
            sceneGameObjectData.RemoveAll(invalidGameObjects.Contains);
            // resolve properties (second loop to resolve Guid references to objects which need to be instantiated)
            foreach ((int i, GameObjectWrapperModel gameObjectData) in sceneGameObjectData.Select((value, i) => (i, value)))
            {
                try
                {
                    GameObject gameObject = SceneGameObjects[i].GameObject;
                    Type gameObjectType = gameObject.GetType();
                    if (gameObjectData.GameObjectPropertyNameTypeValue is null)
                    {
                        continue;
                    }
                    foreach (string memberName in gameObjectData.GameObjectPropertyNameTypeValue.Keys)
                    {
                        if (gameObjectType.GetProperty(memberName) is PropertyInfo propertyInfo)
                        {
                            GameObjectWrapperModel.TypeValuePair typeValue = gameObjectData.GameObjectPropertyNameTypeValue[memberName];
                            if (typeValue.Type is null || typeValue.Value is null || ConvertProperty(typeValue.Type, typeValue.Value) is not object propertyValue)
                            {
                                continue;
                            }
                            propertyInfo.SetValue(gameObject, propertyValue);
                        }
                        else if (gameObjectType.GetField(memberName) is FieldInfo fieldInfo)
                        {
                            GameObjectWrapperModel.TypeValuePair typeValue = gameObjectData.GameObjectPropertyNameTypeValue[memberName];
                            if (typeValue.Type is null || typeValue.Value is null)
                            {
                                continue;
                            }
                            object? fieldValue = ConvertProperty(typeValue.Type, typeValue.Value);
                            if (fieldValue is string fieldString && string.IsNullOrEmpty(fieldString))
                            {
                                Type? fieldType = gameObject.GetType().GetField(memberName)?.FieldType;
                                if (fieldType is not null)
                                {
                                    Enum.TryParse(fieldType, typeValue.Value, out fieldValue);
                                }
                            }
                            if (fieldValue == null)
                            {
                                continue;
                            }
                            if (fieldValue is Guid guidProperty)
                            {
                                int foundIndex = sceneGameObjectData.FindIndex(gObjData => gObjData.Guid == guidProperty);
                                if (foundIndex != -1)
                                {
                                    fieldInfo.SetValue(gameObject, SceneGameObjects[foundIndex].GameObject);
                                }
                                else
                                {
                                    fieldInfo.SetValue(gameObject, null);
                                }
                            }
                            else
                            {
                                fieldInfo.SetValue(gameObject, fieldValue);
                            }
                        }
                    }
                    Logger.LogInfo($"Loaded scene {sceneModel.Name}");
                }
                catch (Exception) { }
            }

        }
        catch (Exception ex)
        {
            Logger.LogError($"An Exception Occurred while loading scene {Path.GetFileNameWithoutExtension(filePath)}, Exception: {ex}");
        }
    }
    private static JsonSerializerOptions sceneSaveJsonSerializerOptions = new()
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
            static bool IsSupportedType(Type type)
            {
                return type == typeof(bool) ||
                    type == typeof(byte) ||
                    type == typeof(sbyte) ||
                    type == typeof(short) ||
                    type == typeof(ushort) ||
                    type == typeof(int) ||
                    type == typeof(uint) ||
                    type == typeof(long) ||
                    type == typeof(ulong) ||
                    type == typeof(float) ||
                    type == typeof(double) ||
                    type == typeof(decimal) ||
                    type == typeof(string) ||
                    type.IsEnum ||
                    type == typeof(Vector2f) ||
                    type == typeof(Vector2i) ||
                    type == typeof(Vector2u) ||
                    type == typeof(Vector3f);
            }

            static string? MemberValueToString(object? value) => value switch
            {
                sbyte or byte or int or uint or short or ushort or long or ulong or float or double or decimal or bool or string or Enum => value.ToString(),
                Vector2i v => $"{{ {v.X}, {v.Y} }}",
                Vector2f v => $"{{ {v.X}, {v.Y} }}",
                Vector2u v => $"{{ {v.X}, {v.Y} }}",
                Vector3f v => $"{{ {v.X}, {v.Y}, {v.Z} }}",
                _ => ""
            };
            SceneModel sceneToWrite = new() { Name = Path.GetFileNameWithoutExtension(filePath), SceneGameObjects = [] };
            int unnamedGOIndex = 1;
            foreach (LayeredGameObject lgo in SceneGameObjects)
            {
                sceneToWrite.SceneGameObjects.Add(new(lgo.GameObject.GetType().FullName ?? "GameObject")
                {
                    Name = lgo.GameObject.Name ?? $"Unnamed GO {unnamedGOIndex++}",
                    Guid = Guid.NewGuid(),
                    RenderLayer = lgo.RenderLayer,
                    // set through reflection in second foreach where GameObject references to each other are resolved or Guid.Empty
                    GameObjectPropertyNameTypeValue = [],
                });
            }
            foreach ((LayeredGameObject lgo, int i) in SceneGameObjects.Select((lgo, i) => (lgo, i)))
            {
                // resolve object references found in sceneToWrite.SceneGameObjects to their assigned Guids,
                // resolve regular references to their output string (ie. Vector2f -> { {x}, {y} }
                foreach (PropertyInfo property in lgo.GameObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (IsSupportedType(property.PropertyType))
                    {
                        sceneToWrite.SceneGameObjects[i].GameObjectPropertyNameTypeValue?.Add(property.Name, new()
                        {
                            Type = property.PropertyType.Name,
                            Value = MemberValueToString(property.GetValue(lgo.GameObject))
                        });
                    }
                    else if (property.PropertyType.IsAssignableTo(typeof(GameObject)))
                    {
                        object? foundObject = SceneGameObjects.FirstOrDefault(sgo => sgo.GameObject == property.GetValue(lgo.GameObject));
                        int sceneGameObjectIndex = foundObject is LayeredGameObject foundLgo ? SceneGameObjects.IndexOf(foundLgo) : -1;
                        sceneToWrite.SceneGameObjects[i].GameObjectPropertyNameTypeValue?.Add(property.Name, new()
                        {
                            Type = "Reference",
                            Value = (sceneGameObjectIndex != -1 ? sceneToWrite.SceneGameObjects[sceneGameObjectIndex].Guid : Guid.Empty).ToString()
                        });
                    }
                }

                foreach (FieldInfo field in lgo.GameObject.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (IsSupportedType(field.FieldType))
                    {
                        sceneToWrite.SceneGameObjects[i].GameObjectPropertyNameTypeValue?.Add(field.Name, new()
                        {
                            Type = field.FieldType.Name,
                            Value = MemberValueToString(field.GetValue(lgo.GameObject))
                        });
                    }
                    else if (field.FieldType.IsAssignableTo(typeof(GameObject)))
                    {
                        object? foundObject = SceneGameObjects.FirstOrDefault(sgo => sgo.GameObject == field.GetValue(lgo.GameObject));
                        int sceneGameObjectIndex = foundObject is LayeredGameObject foundLgo ? SceneGameObjects.IndexOf(foundLgo) : -1;
                        sceneToWrite.SceneGameObjects[i].GameObjectPropertyNameTypeValue?.Add(field.Name, new()
                        {
                            Type = "Reference",
                            Value = (sceneGameObjectIndex != -1 ? sceneToWrite.SceneGameObjects[sceneGameObjectIndex].Guid : Guid.Empty).ToString()
                        });
                    }
                }
            }
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
