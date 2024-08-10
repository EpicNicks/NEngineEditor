using System.IO;
using System.Security.Cryptography;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace NEngineEditor.ScriptCompilation;

public class VSCompatibleFileWatcher
{
    private string _projectPath;
    private ConcurrentDictionary<string, string> _fileHashes;
    private FileSystemWatcher _watcher;
    private ConcurrentDictionary<string, Timer> _debouncers;

    public event EventHandler<FileSystemEventArgs>? FileChanged;

    public VSCompatibleFileWatcher(string projectPath)
    {
        _projectPath = projectPath;
        _fileHashes = new ConcurrentDictionary<string, string>();
        _debouncers = new ConcurrentDictionary<string, Timer>();
        InitializeFileSystemWatcher();
    }

    [MemberNotNull(nameof(_watcher))]
    private void InitializeFileSystemWatcher()
    {
        _watcher = new FileSystemWatcher(_projectPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            Filter = "*.cs"
        };

        _watcher.Changed += OnFileChangedInternal;
        _watcher.Created += OnFileChangedInternal;
        _watcher.Deleted += OnFileDeletedInternal;
        _watcher.Renamed += OnFileRenamedInternal;

        _watcher.EnableRaisingEvents = true;
    }

    public void ScanForChanges()
    {
        foreach (string file in Directory.EnumerateFiles(_projectPath, "*.cs", SearchOption.AllDirectories))
        {
            string currentHash = CalculateFileHash(file);
            if (_fileHashes.TryGetValue(file, out string? storedHash))
            {
                if (currentHash != storedHash)
                {
                    _fileHashes[file] = currentHash;
                    FileChanged?.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Changed, Path.GetDirectoryName(file), Path.GetFileName(file)));
                }
            }
            else
            {
                _fileHashes[file] = currentHash;
                FileChanged?.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Created, Path.GetDirectoryName(file), Path.GetFileName(file)));
            }
        }

        // Check for deleted files
        foreach (var storedFile in _fileHashes.Keys)
        {
            if (!File.Exists(storedFile))
            {
                _fileHashes.TryRemove(storedFile, out _);
                FileChanged?.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Deleted, Path.GetDirectoryName(storedFile), Path.GetFileName(storedFile)));
            }
        }
    }

    public void StartWatching()
    {
        _watcher.EnableRaisingEvents = true;
    }

    public void StopWatching()
    {
        _watcher.EnableRaisingEvents = false;
    }

    private void OnFileChangedInternal(object sender, FileSystemEventArgs e)
    {
        // Debounce the event
        string key = e.FullPath.ToLower();
        if (_debouncers.TryGetValue(key, out Timer? existingTimer))
        {
            existingTimer.Change(500, Timeout.Infinite);
        }
        else
        {
            var timer = new Timer(_ => ProcessFileChange(e), null, 500, Timeout.Infinite);
            _debouncers[key] = timer;
        }
    }

    private void ProcessFileChange(FileSystemEventArgs e)
    {
        string key = e.FullPath.ToLower();
        _debouncers.TryRemove(key, out _);

        if (File.Exists(e.FullPath))
        {
            try
            {
                string hash = CalculateFileHash(e.FullPath);
                if (_fileHashes.TryGetValue(e.FullPath, out string oldHash) && oldHash != hash)
                {
                    _fileHashes[e.FullPath] = hash;
                    FileChanged?.Invoke(this, e);
                }
                else if (!_fileHashes.ContainsKey(e.FullPath))
                {
                    _fileHashes[e.FullPath] = hash;
                    FileChanged?.Invoke(this, e);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error processing file {e.FullPath}: {ex.Message}");
                // Optionally, schedule a retry
                ScheduleRetry(e);
            }
        }
    }

    private void ScheduleRetry(FileSystemEventArgs e)
    {
        Timer? retryTimer = null;
        retryTimer = new Timer(_ =>
        {
            ProcessFileChange(e);
            retryTimer?.Dispose();
        }, null, 1000, Timeout.Infinite);
    }

    private void OnFileDeletedInternal(object sender, FileSystemEventArgs e)
    {
        if (_fileHashes.TryRemove(e.FullPath, out _))
        {
            FileChanged?.Invoke(this, e);
        }
    }

    private void OnFileRenamedInternal(object sender, RenamedEventArgs e)
    {
        if (e.FullPath.EndsWith(".TMP"))
        {
            return;
        }
        _fileHashes.TryRemove(e.OldFullPath, out _);
        string hash = CalculateFileHash(e.FullPath);
        _fileHashes[e.FullPath] = hash;
        FileChanged?.Invoke(this, e);
    }

    private string CalculateFileHash(string filePath)
    {
        const int MAX_ITER = 300;
        const int SLEEP_MS = 100;

        for (int i = 0; i < MAX_ITER; i++)
        {
            try
            {
                using MD5 md5 = MD5.Create();
                using FileStream stream = File.OpenRead(filePath);
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
            catch (IOException)
            {
                // File might be locked by Visual Studio, retry after a short delay
                Thread.Sleep(SLEEP_MS);
            }
        }
        throw new TimeoutException($"Calulation of FileHash for file at: {filePath} exceeded {SLEEP_MS * MAX_ITER}");
    }
}