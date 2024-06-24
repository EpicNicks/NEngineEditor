using NEngineEditor.Properties;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NEngineEditor.ViewModel;
public class ContentBrowserViewModel : ViewModelBase
{
    private readonly SubDirectory _subDirectory;

    public static readonly ImageSource FOLDER_ICON;
    public static readonly ImageSource CS_SCRIPT_ICON;

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

    static ContentBrowserViewModel()
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        FOLDER_ICON = new BitmapImage(new Uri($"pack://application:,,,/{assemblyName};component/Resources/folder-icon.png"));
        FOLDER_ICON.Freeze();
        CS_SCRIPT_ICON = new BitmapImage(new Uri($"pack://application:,,,/{assemblyName};component/Resources/csharp-script-icon.png"));
        CS_SCRIPT_ICON.Freeze();
    }

    public ContentBrowserViewModel()
    {
        _subDirectory = new SubDirectory(onSubDirectoryChanged: () =>
        {
            Items = LoadFilesInCurrentDir();
        });
        Items = LoadFilesInCurrentDir();
    }

    public class FileIconName(ImageSource icon, string fileName)
    {
        public ImageSource Icon => icon;
        public string FileName => fileName;
    }

    private class SubDirectory(Action onSubDirectoryChanged)
    {
        private readonly Action _onSubDirectoryChanged = onSubDirectoryChanged;
        private string currentSubDir = "/";
        public string CurrentSubDir
        {
            get => currentSubDir;
            set
            {
                currentSubDir = value;
                _onSubDirectoryChanged();
            }
        }
    }

    private ObservableCollection<FileIconName> LoadFilesInCurrentDir()
    {
        // spoof for now
        return [new(FOLDER_ICON, "subdir1"), new(CS_SCRIPT_ICON, "somescript.cs")];

        List<FileIconName> filesAndDirectories = [];
        string currentDir = Path.Combine(MainViewModel.Instance.ProjectDirectory, _subDirectory.CurrentSubDir);
        string[] directoryPaths = Directory.GetDirectories(currentDir);
        string[] filePaths = Directory.GetFiles(currentDir);
        foreach (string dir in directoryPaths)
        {
            string? dirName = Path.GetDirectoryName(dir);
            if (dirName is not null)
            {
                filesAndDirectories.Add(new(FOLDER_ICON, dirName));
            }
            else
            {
                // log an error to the console when a console is actually defined
            }
        }
        foreach (string filePath in filePaths)
        {
            string? fileName = Path.GetFileName(filePath);
            if (fileName is not null)
            {
                filesAndDirectories.Add(new(CS_SCRIPT_ICON, fileName));
            }
            else
            {
                // log an error to the console when a console is actually defined
            }
        }
        return new ObservableCollection<FileIconName>(filesAndDirectories);
    }
}
