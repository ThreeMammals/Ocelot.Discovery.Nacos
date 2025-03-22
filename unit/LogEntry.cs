using Microsoft.Extensions.Logging;

namespace Ocelot.Discovery.Nacos.UnitTests;

public class LogEntry
{
    public LogEntry(LogLevel logLevel, EventId eventId, string message, Exception exception)
    {
        LogLevel = logLevel;
        EventId = eventId;
        Message = message;
        Exception = exception;
    }

    public LogLevel LogLevel { get; set; }
    public EventId EventId { get; set; }
    public string Message { get; set; }
    public Exception Exception { get; set; }
}
