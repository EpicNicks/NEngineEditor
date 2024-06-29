using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using NEngineEditor.Managers;
using NEngineEditor.Properties;

namespace NEngineEditor.ViewModel;
public class ContentBrowserViewModel : ViewModelBase
{
    private ProjectDirectoryWatcher _projectDirectoryWatcher;

    public readonly SubDirectory subDirectory;

    public static readonly ImageSource FOLDER_ICON;
    public static readonly ImageSource CS_SCRIPT_ICON;
    public static readonly ImageSource UP_ONE_LEVEL_ICON;

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
    }

    // uri resource to explain this shit: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/pack-uris-in-wpf?view=netframeworkdesktop-4.8
    private static string GetResourceFolder(string resourcePath)
        => $"pack://application:,,,/{Assembly.GetExecutingAssembly().GetName().Name};component/Resources/{resourcePath}";
    private static Uri GetResourceFolderUri(string resourcePath) => new(GetResourceFolder(resourcePath));

    public ContentBrowserViewModel(Dispatcher contentBrowserDispatcher)
    {
        _projectDirectoryWatcher = new ProjectDirectoryWatcher(MainViewModel.Instance.ProjectDirectory, contentBrowserDispatcher);
        subDirectory = new SubDirectory(MainViewModel.Instance.ProjectDirectory, () =>
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

    private void LoadFilesInCurrentDir()
    {
        // spoof for now
        //var spoofBaseUrl = "C:/Deez/Nuts/NEngineProject";
        //Items = [new(FOLDER_ICON, "subdir1", $"{spoofBaseUrl}/subdir1"), new(CS_SCRIPT_ICON, "somescript.cs", $"{spoofBaseUrl}/somescript.cs")];
        //return;

        // should generate a .. go up button for directories which are not the root directory
        List<FileIconName> filesAndDirectories = [];
        string currentDir = subDirectory.CurrentSubDir;
        string[] directoryPaths = Directory.GetDirectories(currentDir);
        string[] filePaths = Directory.GetFiles(currentDir);
        if (currentDir != MainViewModel.Instance.ProjectDirectory)
        {
            filesAndDirectories.Add(new(UP_ONE_LEVEL_ICON, "", Directory.GetParent(currentDir)!.FullName));
        }
        foreach (string dir in directoryPaths)
        {
            string? dirName = new DirectoryInfo(dir).Name;
            if (dirName is not null)
            {
                filesAndDirectories.Add(new(FOLDER_ICON, dirName, dir));
            }
            else
            {
                // log an error to the console when a console is actually defined
            }
        }
        foreach (string filePath in filePaths)
        {
            string? fileName = Path.GetFileName(filePath);
            string? extension = Path.GetExtension(filePath);
            if (fileName is not null && extension is not null && extension.Equals(".cs", StringComparison.OrdinalIgnoreCase))
            {
                filesAndDirectories.Add(new(CS_SCRIPT_ICON, fileName, filePath));
            }
            else
            {
                // log an error to the console when a console is actually defined
            }
        }
        Items = new ObservableCollection<FileIconName>(filesAndDirectories);
    }

    public void DeleteItem(string filePath)
    {
        // delete logic
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
        File.Move(filePath, Path.Join(path, newName));
    }

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
            using FileStream fileStream = File.Create(fullPath);
            using StreamWriter writer = new StreamWriter(fileStream);
            writer.Write(scriptOutput);
            // TODO: generate Guid in metadata file for the script for scene reference
        }
        else
        {
            // log warning: unexpected type once logging is implemented
        }
        LoadFilesInCurrentDir();
        return true;
    }
}
