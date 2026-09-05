using Microsoft.Extensions.Logging;

namespace StockMesh.Application.UnitTests;

internal sealed class TestLogger : ILogger
{
    private readonly List<string> _entries = [];

    public IReadOnlyList<string> Entries
    {
        get { return _entries; }
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _entries.Add($"[{logLevel}] {formatter(state, exception)}");
    }
}