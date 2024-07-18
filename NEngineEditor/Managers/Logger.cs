using NEngineEditor.Model;
using NEngineEditor.ViewModel;

namespace NEngineEditor.Managers;
public static class Logger
{
    private static void Log(string message, LogEntry.LogLevel logLevel)
    {
        MainViewModel.Instance.Logs.Add(new() { Level = logLevel, Message = message });
    }

    public static void LogInfo(params object[] message)
    {
        string toSend = string.Join(' ', message.Select(o => o.ToString() ?? ""));
        Log(toSend, LogEntry.LogLevel.INFO);
    }

    public static void LogWarning(params object[] message)
    {
        string toSend = string.Join(' ', message.Select(o => o.ToString() ?? ""));
        Log(toSend, LogEntry.LogLevel.WARNING);
    }

    public static void LogError(params object[] message)
    {
        string toSend = string.Join(' ', message.Select(o => o.ToString() ?? ""));
        Log(toSend, LogEntry.LogLevel.ERROR);
    }
}
