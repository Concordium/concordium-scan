using System.Diagnostics;

namespace Application.Common;

internal class SplitTimeDiagnosticsLogger
{
    private readonly Stopwatch _sw;
    private string _currentLabel = "";
    private readonly List<string> _results = new();
    private ILogger _logger;

    public SplitTimeDiagnosticsLogger()
    {
        _sw = new Stopwatch();
        _logger = Log.ForContext<SplitTimeDiagnosticsLogger>();

    }

    public void Start(string label)
    {
        _currentLabel = label;
        _sw.Start();
    }

    public void Restart(string label)
    {
        _results.Add($"[{_currentLabel}: {_sw.ElapsedMilliseconds}ms]");
        _currentLabel = label;
        _sw.Restart();
    }

    public void Stop(string message)
    {
        _results.Add($"[{_currentLabel}: {_sw.ElapsedMilliseconds}ms]");
        _sw.Stop();

        _logger.Information($"{message}: {string.Join(" ", _results)}");
    }
}