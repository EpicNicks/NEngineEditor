using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;

using NEngineEditor.Model;
using NEngineEditor.ViewModel;


namespace NEngineEditor.Managers;
public class Logger : ViewModelBase
{
    const int MAX_LOGS = 10_000;
    private readonly object _lockObject = new();

    private static readonly Lazy<Logger> _instance = new(() => new());
    public static Logger Instance => _instance.Value;

    private ObservableCollection<LogEntry> _logs = [];
    public ObservableCollection<LogEntry> Logs
    {
        get => _logs;
        set
        {
            _logs = value;
            OnPropertyChanged(nameof(Logs));
        }
    }

    private Logger()
    {
        Logs.CollectionChanged += Logs_CollectionChanged;
    }

    public static void LogInfo(params object?[] message)
    {
        string toSend = string.Join(' ', message.Select(o => o?.ToString()));
        Log(toSend, LogEntry.LogLevel.INFO);
    }

    public static void LogWarning(params object?[] message)
    {
        string toSend = string.Join(' ', message.Select(o => o?.ToString()));
        Log(toSend, LogEntry.LogLevel.WARNING);
    }

    public static void LogError(params object?[] message)
    {
        string toSend = string.Join(' ', message.Select(o => o?.ToString()));
        Log(toSend, LogEntry.LogLevel.ERROR);
    }

    private static void Log(string? message, LogEntry.LogLevel logLevel)
    {
        Instance.Logs.Add(new() { Level = logLevel, Message = message ?? "null" });
    }

    private void Logs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
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
                LogError($"An Error Occurred while Trimming Logs from the Logger {e}. There are now {Logs.Count} logs (MAX_LOGS={MAX_LOGS})");
            }
        }
    }
}
