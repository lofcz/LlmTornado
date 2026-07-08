namespace LlmTornado.Cli.Input;

/// <summary>
/// Disk persistence for the input history: an append-only UTF-8 text file, one entry per line
/// with real newlines escaped, so history survives across CLI runs. Corrupt or missing files
/// degrade to an empty history — never to a startup failure.
/// </summary>
internal static class PersistentInputHistory
{
    public static List<string> Load(string path, int maxEntries = 1000)
    {
        try
        {
            if (!File.Exists(path))
                return [];

            string[] lines = File.ReadAllLines(path);
            IEnumerable<string> tail = lines.Length > maxEntries ? lines[^maxEntries..] : lines;
            return tail
                .Select(Unescape)
                .Where(entry => entry.Length > 0)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    public static void Append(string path, string entry)
    {
        if (string.IsNullOrWhiteSpace(entry))
            return;

        try
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(path, Escape(entry.Trim()) + Environment.NewLine);
        }
        catch
        {
            // History is a convenience; never break the REPL over it.
        }
    }

    internal static string Escape(string entry) =>
        entry.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\n", "\\n");

    internal static string Unescape(string line)
    {
        if (!line.Contains('\\'))
            return line.Trim();

        System.Text.StringBuilder sb = new(line.Length);
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '\\' && i + 1 < line.Length)
            {
                char next = line[++i];
                sb.Append(next switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    '\\' => '\\',
                    _ => next,
                });
            }
            else
            {
                sb.Append(line[i]);
            }
        }

        return sb.ToString().Trim();
    }
}
