﻿using NEngineEditor.Properties;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NEngineEditor.ViewModel;
public class ContentBrowserViewModel : ViewModelBase
{
    public readonly SubDirectory subDirectory;

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

    public string DirectoryPath => $"Folder: {subDirectory.CurrentSubDir}";

    static ContentBrowserViewModel()
    {
        // uri resource to explain this shit: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/app-development/pack-uris-in-wpf?view=netframeworkdesktop-4.8
        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        FOLDER_ICON = new BitmapImage(new Uri($"pack://application:,,,/{assemblyName};component/Resources/folder-icon.png"));
        FOLDER_ICON.Freeze();
        CS_SCRIPT_ICON = new BitmapImage(new Uri($"pack://application:,,,/{assemblyName};component/Resources/csharp-script-icon.png"));
        CS_SCRIPT_ICON.Freeze();
    }

    public ContentBrowserViewModel()
    {
        subDirectory = new SubDirectory(MainViewModel.Instance.ProjectDirectory, () =>
        {
            OnPropertyChanged(nameof(DirectoryPath));
            Items = LoadFilesInCurrentDir();
        });
        Items = LoadFilesInCurrentDir();
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

    private ObservableCollection<FileIconName> LoadFilesInCurrentDir()
    {
        // spoof for now
        var spoofBaseUrl = "C:/Deez/Nuts/NEngineProject";
        return [new(FOLDER_ICON, "subdir1", $"{spoofBaseUrl}/subdir1"), new(CS_SCRIPT_ICON, "somescript.cs", $"{spoofBaseUrl}/somescript.cs")];

        // should generate a .. go up button for directories which are not the root directory
        List<FileIconName> filesAndDirectories = [];
        string currentDir = Path.Combine(MainViewModel.Instance.ProjectDirectory, subDirectory.CurrentSubDir);
        string[] directoryPaths = Directory.GetDirectories(currentDir);
        string[] filePaths = Directory.GetFiles(currentDir);
        foreach (string dir in directoryPaths)
        {
            string? dirName = Path.GetDirectoryName(dir);
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
            if (fileName is not null)
            {
                filesAndDirectories.Add(new(CS_SCRIPT_ICON, fileName, filePath));
            }
            else
            {
                // log an error to the console when a console is actually defined
            }
        }
        return new ObservableCollection<FileIconName>(filesAndDirectories);
    }
}