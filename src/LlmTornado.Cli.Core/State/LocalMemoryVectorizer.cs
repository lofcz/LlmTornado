using System.Globalization;
using System.Text.RegularExpressions;

namespace LlmTornado.Cli.Core.State;

internal static partial class LocalMemoryVectorizer
{
    public const string Provider = "local-hash-v1";
    public const int Dimensions = 256;

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "are", "as", "at", "be", "but", "by", "for", "from", "has", "have", "he",
        "her", "his", "i", "in", "is", "it", "its", "of", "on", "or", "our", "she", "that", "the",
        "their", "this", "to", "was", "we", "were", "with", "you", "your"
    };

    public static float[] Embed(string? text, IReadOnlyList<string>? tags = null, string? key = null)
    {
        float[] vector = new float[Dimensions];
        AddTerms(vector, Tokenize(text), 1.0f);
        AddTerms(vector, Tokenize(key), 1.25f);

        if (tags is not null)
        {
            foreach (string tag in tags)
                AddTerms(vector, Tokenize(tag), 1.5f);
        }

        Normalize(vector);
        return vector;
    }

    public static double CosineSimilarity(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        int count = Math.Min(left.Count, right.Count);
        if (count == 0)
            return 0;

        double dot = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;

        for (int i = 0; i < count; i++)
        {
            dot += left[i] * right[i];
            leftMagnitude += left[i] * left[i];
            rightMagnitude += right[i] * right[i];
        }

        if (leftMagnitude <= 0 || rightMagnitude <= 0)
            return 0;

        return Math.Max(0, dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude)));
    }

    public static double LexicalScore(string? query, AgentMemoryRecord memory)
    {
        HashSet<string> queryTokens = Tokenize(query).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (queryTokens.Count == 0)
            return 0;

        HashSet<string> memoryTokens = Tokenize($"{memory.Key} {memory.Content} {string.Join(' ', memory.Tags)}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (memoryTokens.Count == 0)
            return 0;

        int overlap = queryTokens.Count(memoryTokens.Contains);
        double tokenOverlap = (double)overlap / queryTokens.Count;
        double phraseBoost = !string.IsNullOrWhiteSpace(query) &&
                             memory.Content.Contains(query.Trim(), StringComparison.OrdinalIgnoreCase)
            ? 0.25
            : 0;

        return Math.Min(1, tokenOverlap + phraseBoost);
    }

    private static void AddTerms(float[] vector, IEnumerable<string> terms, float weight)
    {
        foreach (string term in terms)
        {
            AddHashedFeature(vector, term, weight);

            if (term.Length >= 5)
            {
                for (int i = 0; i <= term.Length - 3; i++)
                    AddHashedFeature(vector, term.Substring(i, 3), weight * 0.35f);
            }
        }
    }

    private static void AddHashedFeature(float[] vector, string term, float weight)
    {
        int hash = StringComparer.OrdinalIgnoreCase.GetHashCode(term);
        int index = (hash & int.MaxValue) % vector.Length;
        vector[index] += hash % 2 == 0 ? weight : -weight;
    }

    private static IEnumerable<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        foreach (Match match in WordRegex().Matches(text.ToLower(CultureInfo.InvariantCulture)))
        {
            string token = match.Value;
            if (token.Length < 2 || StopWords.Contains(token))
                continue;

            yield return token;
        }
    }

    private static void Normalize(float[] vector)
    {
        double magnitude = Math.Sqrt(vector.Sum(value => value * value));
        if (magnitude <= 0)
            return;

        for (int i = 0; i < vector.Length; i++)
            vector[i] = (float)(vector[i] / magnitude);
    }

    [GeneratedRegex("[a-z0-9][a-z0-9_\\-]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();
}
