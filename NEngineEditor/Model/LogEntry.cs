namespace NEngineEditor.Model;
public class LogEntry
{
    public enum LogLevel
    {
        INFO,
        WARNING,
        ERROR
    }

    public LogLevel Level { get; set; }
    public string? Message { get; set; }
}
