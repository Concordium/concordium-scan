using System.Diagnostics;

namespace Application.Common;

internal class SplitTimeDiagnosticsLogger
{
    private readonly Stopwatch _sw;
    private string _currentLabel = "";
    private readonly List<string> _results = new();

    public SplitTimeDiagnosticsLogger()
    {
        _sw = new Stopwatch();
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

    public string Stop()
    {
        _results.Add($"[{_currentLabel}: {_sw.ElapsedMilliseconds}ms]");
        _sw.Stop();

        var result = string.Join(" ", _results);
        _results.Clear();
        return result;
    }
}