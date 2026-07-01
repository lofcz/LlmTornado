namespace LlmTornado.Cli.Commands;

internal sealed class AutoLoopController
{
    private readonly object _lock = new();
    private CancellationTokenSource? _current;

    public bool IsRunning
    {
        get
        {
            lock (_lock)
                return _current is not null;
        }
    }

    public CancellationToken Begin()
    {
        lock (_lock)
        {
            if (_current is not null)
                throw new InvalidOperationException("An auto-loop is already running.");

            _current = new CancellationTokenSource();
            return _current.Token;
        }
    }

    public void Stop()
    {
        lock (_lock)
            _current?.Cancel();
    }

    public void End()
    {
        CancellationTokenSource? current;
        lock (_lock)
        {
            current = _current;
            _current = null;
        }

        current?.Dispose();
    }
}

internal sealed class AutoLoopCommand : IRawCliCommand
{
    public string Name => "loop";
    public string Description => "Repeat a predefined statement until Ctrl+C stops the loop";
    public string Usage => "/loop <statement>";

    private readonly AutoLoopController _controller;
    private readonly Func<string, CancellationToken, Task> _runTurnAsync;

    public AutoLoopCommand(
        AutoLoopController controller,
        Func<string, CancellationToken, Task> runTurnAsync)
    {
        _controller = controller;
        _runTurnAsync = runTurnAsync;
    }

    public Task<bool> ExecuteAsync(string[] args) =>
        ExecuteRawAsync(string.Join(' ', args));

    public async Task<bool> ExecuteRawAsync(string rawArgs)
    {
        string statement = rawArgs.Trim();
        if (string.IsNullOrWhiteSpace(statement))
        {
            ConsoleRenderer.WriteError($"Usage: {Usage}");
            return true;
        }

        CancellationToken cancellationToken;
        try
        {
            cancellationToken = _controller.Begin();
        }
        catch (InvalidOperationException ex)
        {
            ConsoleRenderer.WriteError(ex.Message);
            return true;
        }

        int iterations = 0;
        ConsoleRenderer.WriteInfo("[auto-loop started; press Ctrl+C to stop]");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                iterations++;
                ConsoleRenderer.WriteInfo($"[auto-loop iteration {iterations}]");
                await _runTurnAsync(statement, cancellationToken);
                Console.WriteLine();
            }
        }
        catch (OperationCanceledException)
        {
            ConsoleRenderer.EndStreamingResponse();
        }
        catch (Exception ex)
        {
            ConsoleRenderer.EndStreamingResponse();
            ConsoleRenderer.WriteError($"[auto-loop error: {ex.Message}]");
        }
        finally
        {
            _controller.End();
            ConsoleRenderer.WriteInfo($"[auto-loop stopped after {iterations} iteration(s)]");
        }

        return true;
    }
}
