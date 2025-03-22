using Microsoft.Extensions.Logging;

namespace Ocelot.Discovery.Nacos.UnitTests;

public class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> LogEntries { get; } = new();

    public IDisposable BeginScope<TState>(TState state) => throw new NotImplementedException();

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        LogEntries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), exception));
    }
}
