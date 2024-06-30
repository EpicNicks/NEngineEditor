using NEngineEditor.Model;
using NEngineEditor.ViewModel;

namespace NEngineEditor.Managers;
public static class Logger
{
    private static void Log(string message, LogEntry.LogLevel logLevel)
    {
        MainViewModel.Instance.Logs.Add(new() { Level = logLevel, Message = message });
    }

    public static void LogInfo(string message)
    {
        Log(message, LogEntry.LogLevel.INFO);
    }

    public static void LogWarning(string message)
    {
        Log(message, LogEntry.LogLevel.WARNING);
    }

    public static void LogError(string message)
    {
        Log(message, LogEntry.LogLevel.ERROR);
    }
}
