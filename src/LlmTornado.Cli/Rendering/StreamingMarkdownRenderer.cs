using System.Text;

namespace LlmTornado.Cli.Rendering;

/// <summary>
/// Renders markdown incrementally as it streams in, one character at a time.
/// Styling decisions never need unbounded lookahead: block type is classified from a small
/// buffered line prefix, inline markers (**, *, `) use toggle semantics, and word wrapping
/// holds back only the current word. Chunk boundaries are therefore invisible — feeding the
/// same text in any split produces identical output.
/// Not thread-safe; callers synchronize externally.
/// </summary>
internal sealed class StreamingMarkdownRenderer
{
    private readonly IStyledWriter _writer;
    private readonly MarkdownTheme _theme;
    private readonly Func<int> _widthProvider;

    // ── Line-start classification ──
    private bool _classifying = true;
    private readonly StringBuilder _lineStart = new();

    // ── Block context (per source line, except fences) ──
    private TextStyle _blockStyle;
    private readonly List<(string Text, TextStyle Style)> _hangSegments = [];
    private int _hangWidth;
    private bool _inFence;
    private int _fenceLen;
    private bool _prevLineBlank = true;

    // ── Inline state ──
    private bool _bold, _italic, _code;
    private char _pendingDelim;
    private int _pendingCount;
    private char _prevContentChar = '\0';

    // ── Wrapping ──
    private int _column;
    private readonly List<(string Text, TextStyle Style)> _word = [];
    private int _wordWidth;
    private int _pendingSpaces;

    /// <summary>Cap on how long a line prefix can stay ambiguous before defaulting to paragraph text.</summary>
    private const int ClassifyCap = 200;

    public StreamingMarkdownRenderer(IStyledWriter writer, MarkdownTheme theme, Func<int> widthProvider)
    {
        _writer = writer;
        _theme = theme;
        _widthProvider = widthProvider;
        _blockStyle = theme.Body;
    }

    /// <summary>True when nothing is pending and the cursor sits at column 0.</summary>
    public bool AtLineStart => _classifying && _lineStart.Length == 0 && _column == 0;

    /// <summary>Feeds a chunk of streamed markdown. Chunk boundaries carry no meaning.</summary>
    public void Push(string chunk)
    {
        foreach (char c in chunk)
        {
            if (c == '\r') continue;
            ProcessChar(c);
        }
    }

    /// <summary>Emits everything held back (partial word, unresolved markers, unclassified prefix).</summary>
    public void Flush()
    {
        if (_classifying && _lineStart.Length > 0)
        {
            string buffered = _lineStart.ToString();
            _lineStart.Clear();
            if (_inFence) BeginFenceBodyLine();
            else BeginParagraphContent();
            foreach (char c in buffered) ContentChar(c);
        }
        ResolvePendingLiteral();
        FlushWord();
    }

    /// <summary>Flushes and, if the cursor is mid-line, ends the physical line.</summary>
    public void CompleteLine()
    {
        Flush();
        if (_column > 0 || !_classifying)
        {
            EndLine(hadContent: _column > 0);
        }
    }

    /// <summary>Clears all state for a fresh message. Does not move the cursor.</summary>
    public void Reset()
    {
        _classifying = true;
        _lineStart.Clear();
        _blockStyle = _theme.Body;
        _hangSegments.Clear();
        _hangWidth = 0;
        _inFence = false;
        _fenceLen = 0;
        _prevLineBlank = true;
        _bold = _italic = _code = false;
        _pendingCount = 0;
        _prevContentChar = '\0';
        _column = 0;
        _pendingSpaces = 0;
        _word.Clear();
        _wordWidth = 0;
    }

    // ─────────────────────────────── Character dispatch ───────────────────────────────

    private void ProcessChar(char c)
    {
        if (_classifying)
        {
            LineStartChar(c);
        }
        else
        {
            ContentChar(c);
        }
    }

    // ─────────────────────────────── Line-start classification ───────────────────────────────

    private void LineStartChar(char c)
    {
        if (c == '\n')
        {
            ResolveLineStartAtNewline();
            return;
        }

        _lineStart.Append(c);

        if (_inFence)
        {
            ClassifyFenceBody();
            return;
        }

        LineClass result = Analyze(_lineStart.ToString(), atNewline: false, out int consumed, out int data, out string marker);
        if (result == LineClass.Pending)
        {
            if (_lineStart.Length > ClassifyCap) ResolveBlock(LineClass.Paragraph, 0, 0, "");
            return;
        }
        ResolveBlock(result, consumed, data, marker);
    }

    /// <summary>
    /// Enters the classified block, emitting its marker/gutter, then replays any buffered
    /// characters beyond the consumed marker through inline processing.
    /// </summary>
    private void ResolveBlock(LineClass result, int consumed, int data, string marker)
    {
        string buffered = _lineStart.ToString();
        _lineStart.Clear();

        switch (result)
        {
            case LineClass.Header: BeginHeader(data); break;
            case LineClass.Bullet: BeginBullet(data); break;
            case LineClass.Ordered: BeginOrdered(data, marker); break;
            case LineClass.Quote: BeginQuote(); break;
            default: BeginParagraphContent(); consumed = 0; break;
        }

        foreach (char c in buffered[consumed..]) ContentChar(c);
    }

    private void ResolveLineStartAtNewline()
    {
        string buffered = _lineStart.ToString();
        _lineStart.Clear();

        if (_inFence)
        {
            if (IsFenceClose(buffered))
            {
                CloseFence();
                return;
            }
            BeginFenceBodyLine();
            foreach (char b in buffered) ContentChar(b);
            ContentChar('\n');
            return;
        }

        if (buffered.Length == 0)
        {
            _writer.WriteLine();
            _prevLineBlank = true;
            return;
        }

        LineClass result = Analyze(buffered, atNewline: true, out int consumed, out int data, out string marker);
        switch (result)
        {
            case LineClass.Hr:
                EmitHorizontalRule();
                return;
            case LineClass.FenceOpen:
                OpenFence(buffered[consumed..].Trim(), data);
                return;
            case LineClass.Header:
                BeginHeader(data);
                break;
            case LineClass.Bullet:
                BeginBullet(data);
                break;
            case LineClass.Ordered:
                BeginOrdered(data, marker);
                break;
            case LineClass.Quote:
                BeginQuote();
                break;
            default:
                BeginParagraphContent();
                consumed = 0;
                break;
        }

        foreach (char b in buffered[consumed..]) ContentChar(b);
        ContentChar('\n');
    }

    private enum LineClass
    {
        Pending,
        Paragraph,
        Header,
        Bullet,
        Ordered,
        Quote,
        FenceOpen,
        Hr,
    }

    /// <summary>
    /// Classifies a buffered line prefix. <paramref name="consumed"/> is the marker length to strip;
    /// <paramref name="data"/> is the header level, list indent, or fence length depending on the class.
    /// Fence and horizontal-rule lines only resolve once the full line is known (<paramref name="atNewline"/>).
    /// </summary>
    private static LineClass Analyze(string s, bool atNewline, out int consumed, out int data, out string marker)
    {
        consumed = 0;
        data = 0;
        marker = "";

        int indent = 0;
        while (indent < s.Length && s[indent] == ' ') indent++;
        if (indent > 8) return LineClass.Paragraph;
        if (indent >= s.Length) return atNewline ? LineClass.Paragraph : LineClass.Pending;

        char c0 = s[indent];
        int i = indent;

        switch (c0)
        {
            case '#':
            {
                int hashes = 0;
                while (i < s.Length && s[i] == '#') { hashes++; i++; }
                if (hashes > 6) return LineClass.Paragraph;
                if (i >= s.Length) return atNewline ? LineClass.Paragraph : LineClass.Pending;
                if (s[i] != ' ') return LineClass.Paragraph;
                consumed = i + 1;
                data = hashes;
                return LineClass.Header;
            }

            case '>':
            {
                // Consume "> " (the space is optional; wait for the next char to know).
                if (i + 1 >= s.Length && !atNewline) return LineClass.Pending;
                consumed = i + 1 < s.Length && s[i + 1] == ' ' ? i + 2 : i + 1;
                return LineClass.Quote;
            }

            case '-':
            case '*':
            case '_':
            case '+':
            {
                if (i + 1 >= s.Length && !atNewline)
                {
                    return LineClass.Pending;
                }
                if (c0 is '-' or '*' or '+' && i + 1 < s.Length && s[i + 1] == ' ')
                {
                    // "- " could still be the start of "- - -" (an hr); the distinction is
                    // irrelevant visually often, but CommonMark says hr wins. Keep it simple:
                    // treat as a bullet — hr with spaced markers is rare in LLM output.
                    consumed = i + 2;
                    data = indent;
                    return LineClass.Bullet;
                }
                if (c0 == '+') return LineClass.Paragraph;

                // Candidate horizontal rule: the whole line must be this char (spaces allowed).
                int count = 0;
                for (int j = i; j < s.Length; j++)
                {
                    if (s[j] == c0) count++;
                    else if (s[j] != ' ') return LineClass.Paragraph;
                }
                if (atNewline) return count >= 3 ? LineClass.Hr : LineClass.Paragraph;
                return LineClass.Pending;
            }

            case >= '0' and <= '9':
            {
                int digits = 0;
                while (i < s.Length && char.IsAsciiDigit(s[i])) { digits++; i++; }
                if (digits > 3) return LineClass.Paragraph;
                if (i >= s.Length) return atNewline ? LineClass.Paragraph : LineClass.Pending;
                if (s[i] != '.' && s[i] != ')') return LineClass.Paragraph;
                i++;
                if (i >= s.Length) return atNewline ? LineClass.Paragraph : LineClass.Pending;
                if (s[i] != ' ') return LineClass.Paragraph;
                consumed = i + 1;
                data = indent;
                marker = s[indent..i];
                return LineClass.Ordered;
            }

            case '`':
            {
                int ticks = 0;
                while (i < s.Length && s[i] == '`') { ticks++; i++; }
                if (ticks < 3)
                {
                    return i >= s.Length && !atNewline ? LineClass.Pending : LineClass.Paragraph;
                }
                // A fence: the rest of the line is the info string, known only at newline.
                if (!atNewline) return LineClass.Pending;
                consumed = i;
                data = ticks;
                return LineClass.FenceOpen;
            }

            default:
                return LineClass.Paragraph;
        }
    }

    // ─────────────────────────────── Block transitions ───────────────────────────────

    private void BeginParagraphContent()
    {
        _classifying = false;
        _blockStyle = _theme.Body;
        _hangSegments.Clear();
        _hangWidth = 0;
    }

    private void BeginHeader(int level)
    {
        if (!_prevLineBlank)
        {
            _writer.WriteLine();
        }
        _classifying = false;
        _blockStyle = level <= 2 ? _theme.Header : _theme.Header with { Fg = null };
        _hangSegments.Clear();
        _hangWidth = 0;
    }

    private void BeginBullet(int indent)
    {
        _classifying = false;
        _blockStyle = _theme.Body;

        if (indent > 0) EmitDirect(new string(' ', indent), _theme.Body);
        EmitDirect("• ", _theme.ListMarker);

        _hangSegments.Clear();
        _hangSegments.Add((new string(' ', indent + 2), _theme.Body));
        _hangWidth = indent + 2;
    }

    private void BeginOrdered(int indent, string marker)
    {
        _classifying = false;
        _blockStyle = _theme.Body;

        if (indent > 0) EmitDirect(new string(' ', indent), _theme.Body);
        EmitDirect(marker + " ", _theme.ListMarker);

        int width = indent + DisplayWidth.Measure(marker) + 1;
        _hangSegments.Clear();
        _hangSegments.Add((new string(' ', width), _theme.Body));
        _hangWidth = width;
    }

    private void BeginQuote()
    {
        _classifying = false;
        _blockStyle = _theme.Quote;

        EmitDirect("│ ", _theme.QuoteGutter);

        _hangSegments.Clear();
        _hangSegments.Add(("│ ", _theme.QuoteGutter));
        _hangWidth = 2;
    }

    private void OpenFence(string lang, int fenceLen)
    {
        _inFence = true;
        _fenceLen = fenceLen;

        EmitDirect("╭─", _theme.CodeGutter);
        if (lang.Length > 0)
        {
            EmitDirect(" " + lang, _theme.CodeFenceLabel);
        }
        EndLine(hadContent: true);
    }

    private bool IsFenceClose(string line)
    {
        int ticks = 0;
        int i = 0;
        while (i < line.Length && line[i] == '`') { ticks++; i++; }
        if (ticks < _fenceLen) return false;
        while (i < line.Length && line[i] == ' ') i++;
        return i >= line.Length;
    }

    private void CloseFence()
    {
        _inFence = false;
        EmitDirect("╰─", _theme.CodeGutter);
        EndLine(hadContent: true);
    }

    private void BeginFenceBodyLine()
    {
        _classifying = false;
        _blockStyle = _theme.CodeBlock;
        EmitDirect("│ ", _theme.CodeGutter);
        _hangSegments.Clear();
        _hangSegments.Add(("│ ", _theme.CodeGutter));
        _hangWidth = 2;
    }

    /// <summary>Fence body lines buffer until their newline so the closing fence can be recognized.</summary>
    private void ClassifyFenceBody()
    {
        // Fast path: as soon as the buffered prefix can no longer be a closing fence, emit it as code.
        string s = _lineStart.ToString();
        foreach (char c in s)
        {
            if (c != '`' && c != ' ')
            {
                _lineStart.Clear();
                BeginFenceBodyLine();
                foreach (char b in s) ContentChar(b);
                return;
            }
        }
        // Only backticks/spaces so far — keep buffering until the newline decides.
    }

    private void EmitHorizontalRule()
    {
        int width = Math.Min(Math.Max(10, UsableWidth()), 40);
        EmitDirect(new string('─', width), _theme.HorizontalRule);
        EndLine(hadContent: true);
    }

    // ─────────────────────────────── Inline content ───────────────────────────────

    private void ContentChar(char c)
    {
        if (c == '\n')
        {
            ResolvePendingAtLineEnd();
            FlushWord();
            EndLine(hadContent: _column > 0);
            return;
        }

        if (_inFence)
        {
            // Code is verbatim: no inline markers, hard-wrap only.
            AppendToWord(c, _blockStyle);
            FlushWord();
            _prevContentChar = c;
            return;
        }

        if (_pendingCount > 0)
        {
            if (c == _pendingDelim && _pendingCount < 3)
            {
                _pendingCount++;
                return;
            }
            ResolvePending(next: c);
        }

        if (c == '`')
        {
            _pendingDelim = '`';
            _pendingCount = 1;
            return;
        }

        if (!_code && (c == '*' || c == '_'))
        {
            _pendingDelim = c;
            _pendingCount = 1;
            return;
        }

        if (c == ' ')
        {
            FlushWord();
            WriteSpace();
            _prevContentChar = ' ';
            return;
        }

        AppendToWord(c, CurrentStyle());
        _prevContentChar = c;
    }

    /// <summary>Resolves a buffered delimiter run now that the following character is known.</summary>
    private void ResolvePending(char? next)
    {
        char d = _pendingDelim;
        int run = _pendingCount;
        _pendingCount = 0;

        if (d == '`')
        {
            _code = !_code;
            return;
        }

        if (_code)
        {
            EmitDelimiterLiteral(d, run);
            return;
        }

        if (d == '_')
        {
            // Underscores only toggle at word boundaries (no intraword emphasis).
            bool boundaryOk = _italic || _bold
                ? next is null || !char.IsLetterOrDigit(next.Value)
                : !char.IsLetterOrDigit(_prevContentChar);
            if (!boundaryOk)
            {
                EmitDelimiterLiteral(d, run);
                return;
            }
        }

        switch (run)
        {
            case 1: _italic = !_italic; break;
            case 2: _bold = !_bold; break;
            default: _bold = !_bold; _italic = !_italic; break;
        }
    }

    /// <summary>
    /// A delimiter run dangling at end of line: closing markers are consumed (styles reset at
    /// line end anyway), but markers that would only open a style are shown literally.
    /// </summary>
    private void ResolvePendingAtLineEnd()
    {
        if (_pendingCount == 0) return;

        char d = _pendingDelim;
        int run = _pendingCount;
        _pendingCount = 0;

        if (d == '`')
        {
            if (_code) _code = false;
            else EmitDelimiterLiteral(d, run);
            return;
        }

        if (_bold || _italic)
        {
            switch (run)
            {
                case 1: _italic = false; break;
                case 2: _bold = false; break;
                default: _bold = false; _italic = false; break;
            }
        }
        else
        {
            EmitDelimiterLiteral(d, run);
        }
    }

    private void ResolvePendingLiteral()
    {
        if (_pendingCount == 0) return;
        char d = _pendingDelim;
        int run = _pendingCount;
        _pendingCount = 0;
        EmitDelimiterLiteral(d, run);
    }

    private void EmitDelimiterLiteral(char d, int run)
    {
        for (int i = 0; i < run; i++)
        {
            AppendToWord(d, CurrentStyle());
        }
        _prevContentChar = d;
    }

    private TextStyle CurrentStyle()
    {
        TextStyle style = _code ? _theme.InlineCode : _blockStyle;
        if (_bold) style = style.With(StyleFlags.Bold);
        if (_italic) style = style.With(StyleFlags.Italic);
        return style;
    }

    // ─────────────────────────────── Wrapping / emission ───────────────────────────────

    private int UsableWidth() => Math.Max(4, _widthProvider() - 1);

    private void AppendToWord(char c, TextStyle style)
    {
        if (_word.Count > 0 && _word[^1].Style == style)
        {
            _word[^1] = (_word[^1].Text + c, style);
        }
        else
        {
            _word.Add((c.ToString(), style));
        }
        _wordWidth += DisplayWidth.Measure([c]);

        // A "word" that alone exceeds the writable span can never wrap cleanly — hard-break it.
        if (_wordWidth >= UsableWidth() - _hangWidth)
        {
            FlushWord();
        }
    }

    private void FlushWord()
    {
        if (_word.Count == 0) return;

        int usable = UsableWidth();
        if (_column > _hangWidth && _column + _pendingSpaces + _wordWidth > usable)
        {
            _pendingSpaces = 0; // the space that caused the wrap is swallowed
            WrapLine();
        }
        else if (_pendingSpaces > 0)
        {
            _writer.Write(new string(' ', _pendingSpaces), _blockStyle);
            _column += _pendingSpaces;
            _pendingSpaces = 0;
        }

        foreach ((string text, TextStyle style) in _word)
        {
            _writer.Write(text, style);
        }
        _column += _wordWidth;
        _word.Clear();
        _wordWidth = 0;
    }

    /// <summary>Spaces are deferred until the next word flush so wraps never leave trailing blanks.</summary>
    private void WriteSpace()
    {
        _pendingSpaces++;
    }

    /// <summary>Soft wrap: new physical line continuing the same source line, with hanging indent.</summary>
    private void WrapLine()
    {
        _writer.WriteLine();
        _column = 0;
        foreach ((string text, TextStyle style) in _hangSegments)
        {
            _writer.Write(text, style);
            _column += DisplayWidth.Measure(text);
        }
    }

    /// <summary>Hard line end: the source line is over; block and inline state reset.</summary>
    private void EndLine(bool hadContent)
    {
        _writer.WriteLine();
        _prevLineBlank = !hadContent;
        _column = 0;
        _pendingSpaces = 0;
        _bold = _italic = _code = false;
        _pendingCount = 0;
        _prevContentChar = '\0';
        _classifying = true;
        _blockStyle = _theme.Body;
        _hangSegments.Clear();
        _hangWidth = 0;
    }

    /// <summary>Writes pre-styled text (markers, gutters, rules) that bypasses word wrapping.</summary>
    private void EmitDirect(string text, TextStyle style)
    {
        _writer.Write(text, style);
        _column += DisplayWidth.Measure(text);
    }
}
