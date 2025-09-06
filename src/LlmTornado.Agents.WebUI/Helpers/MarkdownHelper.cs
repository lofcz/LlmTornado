using Markdig;

namespace LlmTornado.Chat.Web.Helpers;

/// <summary>
/// Helper class for converting markdown to HTML
/// </summary>
public static class MarkdownHelper
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    /// <summary>
    /// Converts markdown text to HTML
    /// </summary>
    /// <param name="markdown">The markdown text to convert</param>
    /// <returns>HTML string</returns>
    public static string ToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        return Markdown.ToHtml(markdown, Pipeline);
    }

    /// <summary>
    /// Checks if text contains markdown syntax
    /// </summary>
    /// <param name="text">Text to check</param>
    /// <returns>True if text appears to contain markdown</returns>
    public static bool ContainsMarkdown(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        // Simple heuristics to detect common markdown patterns
        return text.Contains("**") ||       // Bold
               text.Contains("*") ||        // Italic (single asterisk)
               text.Contains("_") ||        // Italic/bold (underscore)
               text.Contains("```") ||      // Code blocks
               text.Contains("`") ||        // Inline code
               text.Contains("##") ||       // Headers
               text.Contains("#") ||        // Headers
               text.Contains("[") ||        // Links
               text.Contains("](") ||       // Links
               text.Contains("- ") ||       // Lists
               text.Contains("* ") ||       // Lists
               text.Contains("1. ") ||      // Numbered lists
               text.Contains("> ") ||       // Blockquotes
               text.Contains("---") ||      // Horizontal rules
               text.Contains("***") ||      // Horizontal rules
               text.Contains("| ");         // Tables
    }
}