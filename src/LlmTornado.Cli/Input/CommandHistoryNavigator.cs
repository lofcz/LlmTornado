namespace LlmTornado.Cli.Input;

/// <summary>
/// Tracks in-session input history and supports readline-style navigation.
/// </summary>
internal sealed class CommandHistoryNavigator
{
    private readonly List<string> _entries = [];
    private readonly int _maxEntries;
    private int _index = -1;
    private string? _draft;

    internal CommandHistoryNavigator(int maxEntries = 1000)
        : this([], maxEntries)
    {
    }

    internal CommandHistoryNavigator(IEnumerable<string> seed, int maxEntries = 1000)
    {
        _maxEntries = Math.Max(1, maxEntries);
        foreach (string entry in seed)
        {
            string trimmed = entry.Trim();
            if (trimmed.Length == 0 || (_entries.Count > 0 && _entries[^1] == trimmed))
                continue;
            _entries.Add(trimmed);
        }

        if (_entries.Count > _maxEntries)
            _entries.RemoveRange(0, _entries.Count - _maxEntries);
    }

    /// <summary>Raised when a new (non-duplicate) entry is recorded; used to persist history to disk.</summary>
    internal event Action<string>? EntryAdded;

    internal IReadOnlyList<string> Entries => _entries;

    internal void AddSubmitted(string input)
    {
        // Record any non-blank submission so the user can recall whatever they typed,
        // not just slash commands. Always re-arm navigation afterwards.
        string command = (input ?? string.Empty).Trim();
        if (command.Length == 0)
        {
            ResetNavigation();
            return;
        }

        // Skip consecutive duplicates so holding Up doesn't crawl through repeats.
        if (_entries.Count == 0 || _entries[^1] != command)
        {
            _entries.Add(command);

            if (_entries.Count > _maxEntries)
                _entries.RemoveAt(0);

            EntryAdded?.Invoke(command);
        }

        ResetNavigation();
    }

    internal bool TryMovePrevious(string currentBuffer, out string recalled)
    {
        recalled = currentBuffer;

        if (_entries.Count == 0)
            return false;

        if (_index < 0)
        {
            _draft = currentBuffer;
            _index = _entries.Count - 1;
        }
        else if (_index > 0)
        {
            _index--;
        }

        recalled = _entries[_index];
        return true;
    }

    internal bool TryMoveNext(string currentBuffer, out string recalled)
    {
        recalled = currentBuffer;

        if (_index < 0 || _entries.Count == 0)
            return false;

        if (_index < _entries.Count - 1)
        {
            _index++;
            recalled = _entries[_index];
            return true;
        }

        recalled = _draft ?? string.Empty;
        ResetNavigation();
        return true;
    }

    internal void ResetNavigation()
    {
        _index = -1;
        _draft = null;
    }
}
