namespace LlmTornado.Agents;

internal static class Threading
{
#if MODERN
    public static readonly ValueTask ValueTaskCompleted = ValueTask.CompletedTask;
    public static ValueTask<T> FromResult<T>(T result) => ValueTask.FromResult(result);
    public static bool TryResetCancellationTokenSource(CancellationTokenSource cts)
    {
        return cts.TryReset();
    }
#else
    public static readonly ValueTask ValueTaskCompleted = new ValueTask(Task.CompletedTask);
    public static ValueTask<T> FromResult<T>(T result) => new ValueTask<T>(result);
    public static bool TryResetCancellationTokenSource(CancellationTokenSource cts)
    {
        return false;
    }
#endif
}