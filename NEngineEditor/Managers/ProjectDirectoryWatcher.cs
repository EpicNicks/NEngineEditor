using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace NEngineEditor.Managers;
public class ProjectDirectoryWatcher
{
    private FileSystemWatcher _fileSystemWatcher;
    private Dispatcher _dispatcher;

    public event FileSystemEventHandler? FileCreated;
    public event FileSystemEventHandler? FileChanged;
    public event FileSystemEventHandler? FileDeleted;
    public event RenamedEventHandler? FileRenamed;

    public ProjectDirectoryWatcher(string projectPath, Dispatcher dispatcher)
    {
        _fileSystemWatcher = new FileSystemWatcher(projectPath);
        _dispatcher = dispatcher;

        _fileSystemWatcher.Created += OnFileCreated;
        _fileSystemWatcher.Changed += OnFileChanged;
        _fileSystemWatcher.Deleted += OnFileDeleted;
        _fileSystemWatcher.Renamed += OnFileRenamed;

        _fileSystemWatcher.EnableRaisingEvents = true;
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        _dispatcher?.Invoke(() =>
        {
            FileRenamed?.Invoke(sender, e);
        });
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        _dispatcher.Invoke(() =>
        {
            FileDeleted?.Invoke(sender, e);
        });
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _dispatcher.Invoke(() =>
        {
            FileChanged?.Invoke(sender, e);
        });
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        _dispatcher.Invoke(() =>
        {
            FileCreated?.Invoke(sender, e);
        });
    }
}
