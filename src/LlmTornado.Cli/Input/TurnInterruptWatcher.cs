namespace LlmTornado.Cli.Input;

/// <summary>
/// Reference-counted gate over interactive stdin ownership. The Esc watcher polls keys only
/// while nothing else (approval prompt, ask_question wizard) is reading the keyboard;
/// components that read keys wrap their prompt in <see cref="Suspend"/>.
/// </summary>
internal static class ConsoleInputGate
{
    private static int _suspended;

    public static bool IsSuspended => Volatile.Read(ref _suspended) > 0;

    public static IDisposable Suspend()
    {
        Interlocked.Increment(ref _suspended);
        return new Scope();
    }

    private sealed class Scope : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                Interlocked.Decrement(ref _suspended);
        }
    }
}

/// <summary>
/// Watches for Esc while an agent turn is in flight and cancels the runtime, giving
/// Claude-Code-style "press Esc to interrupt" without killing the session. Ctrl+C remains
/// the fallback path (handled by the console cancel-key event).
/// </summary>
internal static class TurnInterruptWatcher
{
    /// <summary>
    /// Awaits <paramref name="turn"/> while polling for Esc; on Esc invokes <paramref name="cancel"/>
    /// (once) and keeps waiting for the turn to unwind gracefully. No-ops the polling when input
    /// is redirected. Exceptions from the turn propagate unchanged.
    /// </summary>
    public static async Task WatchAsync(Task turn, Action cancel)
    {
        if (Console.IsInputRedirected)
        {
            await turn;
            return;
        }

        bool cancelled = false;
        while (!turn.IsCompleted)
        {
            try
            {
                while (!cancelled && !ConsoleInputGate.IsSuspended && Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        cancelled = true;
                        try { cancel(); } catch { /* best effort */ }
                    }
                    // Other keys typed mid-turn are swallowed; the line editor owns input between turns.
                }
            }
            catch (InvalidOperationException)
            {
                // Console state changed under us (e.g. redirection); stop polling, just await.
                await turn;
                return;
            }

            await Task.WhenAny(turn, Task.Delay(50));
        }

        await turn;
    }
}
