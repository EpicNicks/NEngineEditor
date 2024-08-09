﻿using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using NEngineEditor.Managers;
using NEngineEditor.Model;
using NEngineEditor.Properties;
using NEngineEditor.Windows;

namespace NEngineEditor.ViewModel;
public class ContentBrowserViewModel : ViewModelBase
{
    private readonly ProjectDirectoryWatcher _projectDirectoryWatcher;

    public readonly SubDirectory subDirectory;

    public static readonly ImageSource FOLDER_ICON;
    public static readonly ImageSource CS_SCRIPT_ICON;
    public static readonly ImageSource UP_ONE_LEVEL_ICON;
    public static readonly ImageSource SCENE_ICON;

    private ObservableCollection<FileIconName> _items = [];
    public ObservableCollection<FileIconName> Items
    {
        get => _items;
        set
        {
            _items = value;
            OnPropertyChanged(nameof(Items));
        }
    }

    public string DirectoryPath => $"Folder: {subDirectory.CurrentSubDir}";

    static ContentBrowserViewModel()
    {
        FOLDER_ICON = new BitmapImage(GetResourceFolderUri("folder-icon.png"));
        FOLDER_ICON.Freeze();
        CS_SCRIPT_ICON = new BitmapImage(GetResourceFolderUri("csharp-script-icon.png"));
        CS_SCRIPT_ICON.Freeze();
        UP_ONE_LEVEL_ICON = new BitmapImage(GetResourceFolderUri("ellipsis-horizontal.png"));
        UP_ONE_LEVEL_ICON.Freeze();
        SCENE_ICON = new BitmapImage(GetResourceFolderUri("scene-icon.png"));
        SCENE_ICON.Freeze();
    }

    // uri resource to explain this shit: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/pack-uris-in-wpf?view=netframeworkdesktop-4.8
    private static string GetResourceFolder(string resourcePath)
        => $"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name};component/Resources/{resourcePath}";
    private static Uri GetResourceFolderUri(string resourcePath) => new(GetResourceFolder(resourcePath));

    public ContentBrowserViewModel(Dispatcher contentBrowserDispatcher)
    {
        _projectDirectoryWatcher = new ProjectDirectoryWatcher(Path.Combine(MainWindow.ProjectDirectory, "Assets"), contentBrowserDispatcher);
        _projectDirectoryWatcher.FileDeleted += (o, e) => LoadFilesInCurrentDir();
        _projectDirectoryWatcher.FileCreated += (o, e) => LoadFilesInCurrentDir();
        _projectDirectoryWatcher.FileRenamed += (o, e) => LoadFilesInCurrentDir();
        subDirectory = new SubDirectory(MainWindow.ProjectDirectory, () =>
        {
            OnPropertyChanged(nameof(DirectoryPath));
            LoadFilesInCurrentDir();
        });
        LoadFilesInCurrentDir();
    }

    public class FileIconName(ImageSource icon, string fileName, string filePath)
    {
        public ImageSource Icon => icon;
        public string FileName => fileName;
        public string FilePath => filePath;
    }

    public class SubDirectory(string initialSubDirectory, Action onSubDirectoryChanged)
    {
        private readonly Action _onSubDirectoryChanged = onSubDirectoryChanged;
        private string _currentSubDir = initialSubDirectory;
        public string CurrentSubDir
        {
            get => _currentSubDir;
            set
            {
                _currentSubDir = value;
                _onSubDirectoryChanged();
            }
        }
    }

    public enum CreateItemType
    {
        CS_SCRIPT,
        FOLDER,
        SCENE,
    }

    public void LoadFilesInCurrentDir()
    {
        List<FileIconName> filesAndDirectories = [];
        string currentDir = subDirectory.CurrentSubDir;
        string[] directoryPaths = Directory.GetDirectories(currentDir);
        string[] filePaths = Directory.GetFiles(currentDir);
        if (currentDir != MainWindow.ProjectDirectory)
        {
            filesAndDirectories.Add(new(UP_ONE_LEVEL_ICON, "", Directory.GetParent(currentDir)!.FullName));
        }
        foreach (string dir in directoryPaths)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dir);
            string? dirName = dirInfo.Name;
            List<string> specialFolders = ["bin", "obj"];
            if (dirName is not null && !dirInfo.Attributes.HasFlag(FileAttributes.Hidden) && !specialFolders.Contains(dirName))
            {
                filesAndDirectories.Add(new(FOLDER_ICON, dirName, dir));
            }
        }
        foreach (string filePath in filePaths)
        {
            string? fileName = Path.GetFileName(filePath);
            string? extension = Path.GetExtension(filePath);
            if (fileName is not null && extension is not null)
            {
                if (extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    filesAndDirectories.Add(new(CS_SCRIPT_ICON, fileName, filePath));
                }
                else if (extension.Equals(".scene", StringComparison.OrdinalIgnoreCase))
                {
                    filesAndDirectories.Add(new(SCENE_ICON, fileName, filePath));
                }
            }
        }
        Items = new ObservableCollection<FileIconName>(filesAndDirectories);
    }

    public static void AddScriptToScene(string filePath)
    {
        try
        {
            if (MainViewModel.Instance.AddGameObjectToScene(filePath) is null)
            {
                Logger.LogError("The script you have tried to add either was not derived from GameObject or was not found in the loaded assemblies.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error: {ex.Message}");
        }
    }

    public void DeleteItem(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        LoadFilesInCurrentDir();
    }

    public void RenameItem(string filePath, string newName)
    {
        if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(newName))
        {
            return;
        }
        string? path = Path.GetDirectoryName(filePath);
        if (path is null)
        {
            return;
        }
        // prevent multiple calls/race condition
        if (!File.Exists(Path.Join(Path.GetDirectoryName(filePath), newName)))
        {
            File.Move(filePath, Path.Join(path, newName));
            LoadFilesInCurrentDir();
        }
    }

    public bool CreateScript(string path, string itemName, NewScriptDialog.CsScriptType csScriptType)
    {
        try
        {
            string fullPath = Path.Join(path, itemName);
            if (!fullPath.EndsWith(".cs"))
            {
                fullPath += ".cs";
            }
            string gameObjectScriptTemplate = csScriptType switch
            {
                NewScriptDialog.CsScriptType.GAMEOBJECT => Resources.GameObjectTemplate_cs,
                NewScriptDialog.CsScriptType.UIANCHORED => Resources.UIAnchoredTemplate_cs,
                NewScriptDialog.CsScriptType.POSITIONABLE => Resources.PositionableTemplate_cs,
                NewScriptDialog.CsScriptType.MOVEABLE => Resources.MoveableTemplate_cs,
                _ => throw new InvalidOperationException($"Provided CsScriptType ({csScriptType}) was out of bounds of the enum")
            };
            string scriptOutput = gameObjectScriptTemplate.Replace("{CLASSNAME}", itemName.Replace("-", "_"));
            File.WriteAllText(fullPath, scriptOutput);

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"An exception occurred while creating script named {itemName}: {ex}");
            return false;
        }
    }

    private static readonly JsonSerializerOptions newSceneJsonSerializerOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };
    public bool CreateItem(string path, CreateItemType createItemType, string itemName)
    {
        string fullPath = Path.Join(path, itemName);
        if (File.Exists(fullPath))
        {
            return false;
        }
        if (createItemType == CreateItemType.FOLDER)
        {
            Directory.CreateDirectory(fullPath);
        }
        else if (createItemType == CreateItemType.CS_SCRIPT)
        {
            if (!fullPath.EndsWith(".cs"))
            {
                fullPath += ".cs";
            }
            string gameObjectScriptTemplate = Resources.GameObjectTemplate_cs;
            string scriptOutput = gameObjectScriptTemplate.Replace("{CLASSNAME}", itemName.Replace("-", "_"));
            File.WriteAllText(fullPath, scriptOutput);
        }
        else if (createItemType == CreateItemType.SCENE)
        {
            if (!fullPath.EndsWith(".scene"))
            {
                fullPath += ".scene";
            }
            SceneModel emptySceneModel = new() { Name = itemName, SceneGameObjects = [] };
            File.WriteAllText(fullPath, JsonSerializer.Serialize(emptySceneModel, newSceneJsonSerializerOptions));
        }
        else
        {
            // log warning: unexpected type once logging is implemented
        }
        LoadFilesInCurrentDir();
        return true;
    }
}
