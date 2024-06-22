using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace NEngineEditor.Managers;
public class ProjectDirectoryWatcher
{
    private FileSystemWatcher _fileSystemWatcher;
    private Dispatcher _dispatcher;

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
            MessageBox.Show("File Renamed");
        });
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        throw new NotImplementedException();
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        throw new NotImplementedException();
    }
}
