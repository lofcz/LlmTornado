using System.Buffers;
using System.Globalization;
using System.Text;

namespace LlmTornado.Cli.Rendering;

/// <summary>
/// Terminal display-width measurement. Counts columns, not UTF-16 code units:
/// combining marks and joiners are 0, East Asian wide/fullwidth and emoji are 2, everything else 1.
/// Emoji ZWJ sequences are approximated as the sum of their parts (may overcount by a column).
/// </summary>
internal static class DisplayWidth
{
    /// <summary>Measures the display width of a string in terminal columns.</summary>
    public static int Measure(ReadOnlySpan<char> text)
    {
        int width = 0;
        int i = 0;
        while (i < text.Length)
        {
            if (Rune.DecodeFromUtf16(text[i..], out Rune rune, out int consumed) != OperationStatus.Done)
            {
                // Lone surrogate — render engines typically show a replacement glyph.
                width += 1;
                i += 1;
                continue;
            }
            width += MeasureRune(rune);
            i += consumed;
        }
        return width;
    }

    /// <summary>Measures the display width of a single rune in terminal columns.</summary>
    public static int MeasureRune(Rune rune)
    {
        int cp = rune.Value;

        // Zero-width: joiners, variation selectors, BOM/formatting.
        if (cp == 0x200D || cp is >= 0xFE00 and <= 0xFE0F || cp == 0x200B || cp == 0xFEFF)
        {
            return 0;
        }

        UnicodeCategory category = Rune.GetUnicodeCategory(rune);
        if (category is UnicodeCategory.NonSpacingMark or UnicodeCategory.EnclosingMark or UnicodeCategory.Format)
        {
            return 0;
        }

        return IsWide(cp) ? 2 : 1;
    }

    /// <summary>
    /// Truncates <paramref name="text"/> so its display width does not exceed <paramref name="maxWidth"/>.
    /// </summary>
    public static string TruncateToWidth(string text, int maxWidth)
    {
        if (maxWidth <= 0) return string.Empty;

        int width = 0;
        int i = 0;
        while (i < text.Length)
        {
            if (Rune.DecodeFromUtf16(text.AsSpan(i), out Rune rune, out int consumed) != OperationStatus.Done)
            {
                consumed = 1;
                rune = (Rune)'�';
            }
            int runeWidth = MeasureRune(rune);
            if (width + runeWidth > maxWidth)
            {
                return text[..i];
            }
            width += runeWidth;
            i += consumed;
        }
        return text;
    }

    // Sorted, non-overlapping intervals of code points rendered two columns wide
    // (East Asian Wide/Fullwidth per UAX #11, plus common emoji blocks).
    private static readonly int[] WideStarts;
    private static readonly int[] WideEnds;

    static DisplayWidth()
    {
        (int Start, int End)[] ranges =
        [
            (0x1100, 0x115F),   // Hangul Jamo (leading consonants)
            (0x2329, 0x232A),   // Angle brackets
            (0x231A, 0x231B),   // Watch, hourglass
            (0x23E9, 0x23EC),   // Fast-forward etc.
            (0x23F0, 0x23F0),
            (0x23F3, 0x23F3),
            (0x25FD, 0x25FE),
            (0x2614, 0x2615),   // Umbrella, hot beverage
            (0x2648, 0x2653),   // Zodiac
            (0x267F, 0x267F),
            (0x2693, 0x2693),
            (0x26A1, 0x26A1),
            (0x26AA, 0x26AB),
            (0x26BD, 0x26BE),
            (0x26C4, 0x26C5),
            (0x26CE, 0x26CE),
            (0x26D4, 0x26D4),
            (0x26EA, 0x26EA),
            (0x26F2, 0x26F3),
            (0x26F5, 0x26F5),
            (0x26FA, 0x26FA),
            (0x26FD, 0x26FD),
            (0x2705, 0x2705),
            (0x270A, 0x270B),
            (0x2728, 0x2728),
            (0x274C, 0x274C),
            (0x274E, 0x274E),
            (0x2753, 0x2755),
            (0x2757, 0x2757),
            (0x2795, 0x2797),
            (0x27B0, 0x27B0),
            (0x27BF, 0x27BF),
            (0x2B1B, 0x2B1C),
            (0x2B50, 0x2B50),
            (0x2B55, 0x2B55),
            (0x2E80, 0x303E),   // CJK Radicals … CJK Symbols and Punctuation
            (0x3041, 0x33FF),   // Hiragana … CJK Compatibility
            (0x3400, 0x4DBF),   // CJK Extension A
            (0x4E00, 0x9FFF),   // CJK Unified Ideographs
            (0xA000, 0xA4CF),   // Yi
            (0xA960, 0xA97F),   // Hangul Jamo Extended-A
            (0xAC00, 0xD7A3),   // Hangul Syllables
            (0xF900, 0xFAFF),   // CJK Compatibility Ideographs
            (0xFE10, 0xFE19),   // Vertical forms
            (0xFE30, 0xFE52),   // CJK Compatibility Forms
            (0xFE54, 0xFE66),   // Small Form Variants
            (0xFE68, 0xFE6B),
            (0xFF00, 0xFF60),   // Fullwidth Forms
            (0xFFE0, 0xFFE6),   // Fullwidth signs
            (0x16FE0, 0x16FE4), // Tangut/Nushu marks
            (0x17000, 0x18AFF), // Tangut
            (0x1B000, 0x1B2FF), // Kana Supplement/Extended
            (0x1F004, 0x1F004), // Mahjong red dragon
            (0x1F0CF, 0x1F0CF), // Playing card joker
            (0x1F18E, 0x1F18E),
            (0x1F191, 0x1F19A),
            (0x1F200, 0x1F2FF), // Enclosed ideographic supplement
            (0x1F300, 0x1F64F), // Misc symbols/pictographs, emoticons
            (0x1F680, 0x1F6FF), // Transport
            (0x1F900, 0x1F9FF), // Supplemental symbols/pictographs
            (0x1FA70, 0x1FAFF), // Symbols and Pictographs Extended-A
            (0x20000, 0x2FFFD), // CJK Extensions B-F
            (0x30000, 0x3FFFD), // CJK Extension G+
        ];

        WideStarts = new int[ranges.Length];
        WideEnds = new int[ranges.Length];
        for (int i = 0; i < ranges.Length; i++)
        {
            WideStarts[i] = ranges[i].Start;
            WideEnds[i] = ranges[i].End;
        }
    }

    private static bool IsWide(int codePoint)
    {
        if (codePoint < 0x1100) return false;

        int index = Array.BinarySearch(WideStarts, codePoint);
        if (index >= 0) return true;

        // BinarySearch returns complement of the first element greater than codePoint;
        // the candidate interval is the one before it.
        int candidate = ~index - 1;
        return candidate >= 0 && codePoint <= WideEnds[candidate];
    }
}
