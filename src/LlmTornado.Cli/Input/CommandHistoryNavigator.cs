namespace LlmTornado.Cli.Input;

/// <summary>
/// Tracks in-session slash-command history and supports readline-style navigation.
/// </summary>
internal sealed class CommandHistoryNavigator
{
    private readonly List<string> _entries = [];
    private readonly int _maxEntries;
    private int _index = -1;
    private string? _draft;

    internal CommandHistoryNavigator(int maxEntries = 1000)
    {
        _maxEntries = Math.Max(1, maxEntries);
    }

    internal IReadOnlyList<string> Entries => _entries;

    internal void AddSubmitted(string input)
    {
        if (!IsSlashCommand(input))
        {
            ResetNavigation();
            return;
        }

        string command = input.Trim();
        _entries.Add(command);

        if (_entries.Count > _maxEntries)
            _entries.RemoveAt(0);

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

    internal static bool IsSlashCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return input.TrimStart().StartsWith('/');
    }
}
