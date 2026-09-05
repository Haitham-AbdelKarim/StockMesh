using Microsoft.Extensions.Logging;

namespace StockMesh.Application.UnitTests;

internal sealed class TestLoggerProvider : ILoggerProvider
{
    private readonly TestLogger _logger;

    public TestLoggerProvider(TestLogger logger)
    {
        _logger = logger;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _logger;
    }

    public void Dispose()
    {
    }
}