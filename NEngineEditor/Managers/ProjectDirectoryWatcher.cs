using System.IO;
using System.Windows.Threading;

namespace NEngineEditor.Managers;
public class ProjectDirectoryWatcher
{
    private readonly FileSystemWatcher _fileSystemWatcher;
    private readonly Dispatcher _dispatcher;

    public event FileSystemEventHandler? FileCreated;
    public event FileSystemEventHandler? FileChanged;
    public event FileSystemEventHandler? FileDeleted;
    public event RenamedEventHandler? FileRenamed;

    public ProjectDirectoryWatcher(string projectPath, Dispatcher dispatcher)
    {
        _fileSystemWatcher = new FileSystemWatcher(projectPath)
        {
            Filter = "*.*",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            EnableRaisingEvents = true,
            IncludeSubdirectories = true,
        };
        _dispatcher = dispatcher;

        _fileSystemWatcher.Created += OnFileCreated;
        _fileSystemWatcher.Changed += OnFileChanged;
        _fileSystemWatcher.Deleted += OnFileDeleted;
        _fileSystemWatcher.Renamed += OnFileRenamed;
        
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (Path.GetFileName(e.FullPath).EndsWith('~') || Path.GetFileName(e.FullPath).StartsWith('.'))
        {
            return;
        }
        _dispatcher?.Invoke(() =>
        {
            FileRenamed?.Invoke(sender, e);
        });
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (Path.GetFileName(e.FullPath).EndsWith('~') || Path.GetFileName(e.FullPath).StartsWith('.'))
        {
            return;
        }
        _dispatcher.Invoke(() =>
        {
            FileDeleted?.Invoke(sender, e);
        });
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // visual studio messes this up with writing to temp files, not sure how to work around it yet, but notepad++ doesn't do this same garbage
        //  need to determine how to catch these change events with the correct filepath since the real file's filepath is never propagated,
        //  just the containing folder and temp file
        if (Path.GetFileName(e.FullPath).EndsWith('~') || Path.GetFileName(e.FullPath).StartsWith('.'))
        {
            return;
        }
        _dispatcher.Invoke(() =>
        {
            FileChanged?.Invoke(sender, e);
        });
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (Path.GetFileName(e.FullPath).EndsWith('~') || Path.GetFileName(e.FullPath).StartsWith('.'))
        {
            return;
        }
        _dispatcher.Invoke(() =>
        {
            FileCreated?.Invoke(sender, e);
        });
    }
}
