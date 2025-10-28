using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LlmTornado.Internal.Press.DataModels;

namespace LlmTornado.Internal.Press.Services;

public static class MemeService
{
    private static readonly HttpClient _httpClient = new HttpClient();

    /// <summary>
    /// Download image from URL to local path
    /// </summary>
    public static async Task<string> DownloadImageFromUrlAsync(string url, string outputDirectory, string fileName = "")
    {
        try
        {
            // Ensure output directory exists
            Directory.CreateDirectory(outputDirectory);

            // Generate filename if not provided
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = $"meme_{Guid.NewGuid():N}.jpg";
            }

            var outputPath = Path.Combine(outputDirectory, fileName);

            // Download image
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(outputPath, imageBytes);

            Console.WriteLine($"  [MemeService] Downloaded meme to: {outputPath}");
            return outputPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [MemeService] Download failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Identify potential insertion points in markdown content
    /// AVOIDS code blocks, lists, and other special markdown structures
    /// </summary>
    public static List<MemeInsertionPoint> IdentifyInsertionPoints(string markdown)
    {
        var insertionPoints = new List<MemeInsertionPoint>();
        var lines = markdown.Split('\n');
        bool inCodeBlock = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();

            // Track code block state
            if (trimmed.StartsWith("```"))
            {
                inCodeBlock = !inCodeBlock;
                continue;
            }

            // Skip if we're inside a code block
            if (inCodeBlock)
                continue;

            // After H2/H3 headings (but not inside code blocks)
            if (trimmed.StartsWith("## ") || trimmed.StartsWith("### "))
            {
                // Find next non-empty line after heading
                int insertLine = i + 1;
                while (insertLine < lines.Length && string.IsNullOrWhiteSpace(lines[insertLine]))
                {
                    insertLine++;
                }

                if (insertLine < lines.Length && !IsInsideSpecialBlock(lines, insertLine))
                {
                    insertionPoints.Add(new MemeInsertionPoint
                    {
                        LineNumber = insertLine,
                        Context = trimmed,
                        SurroundingText = GetSurroundingText(lines, insertLine, 3)
                    });
                }
            }

            // Between paragraphs (after empty line, before regular text)
            if (i > 0 && string.IsNullOrWhiteSpace(line) && i + 1 < lines.Length)
            {
                var nextLine = lines[i + 1].TrimStart();
                
                // Only insert before regular text paragraphs
                if (!string.IsNullOrWhiteSpace(nextLine) &&
                    !nextLine.StartsWith("#") &&      // Not a heading
                    !nextLine.StartsWith("```") &&    // Not code block
                    !nextLine.StartsWith("-") &&      // Not list
                    !nextLine.StartsWith("*") &&      // Not list
                    !nextLine.StartsWith("1.") &&     // Not numbered list
                    !nextLine.StartsWith(">") &&      // Not blockquote
                    !nextLine.StartsWith("![") &&     // Not existing image
                    !IsInsideSpecialBlock(lines, i + 1))
                {
                    insertionPoints.Add(new MemeInsertionPoint
                    {
                        LineNumber = i + 1,
                        Context = "Between paragraphs",
                        SurroundingText = GetSurroundingText(lines, i + 1, 3)
                    });
                }
            }
        }

        return insertionPoints;
    }

    /// <summary>
    /// Check if a line is inside a code block or other special markdown structure
    /// </summary>
    private static bool IsInsideSpecialBlock(string[] lines, int lineIndex)
    {
        // Look backwards to see if we're in a code block
        bool inCodeBlock = false;
        for (int i = 0; i < lineIndex; i++)
        {
            if (lines[i].TrimStart().StartsWith("```"))
            {
                inCodeBlock = !inCodeBlock;
            }
        }
        return inCodeBlock;
    }

    /// <summary>
    /// Insert meme markdown at specified line number
    /// </summary>
    public static string InsertMemeAtLine(string markdown, int lineNumber, string memeMarkdown)
    {
        var lines = markdown.Split('\n').ToList();

        if (lineNumber < 0 || lineNumber > lines.Count)
        {
            Console.WriteLine($"  [MemeService] Invalid line number: {lineNumber}");
            return markdown;
        }

        // Insert with blank lines around it for proper markdown formatting
        lines.Insert(lineNumber, "");
        lines.Insert(lineNumber + 1, memeMarkdown);
        lines.Insert(lineNumber + 2, "");

        return string.Join('\n', lines);
    }

    /// <summary>
    /// Create markdown for meme image
    /// </summary>
    public static string CreateMemeMarkdown(string imagePath, string caption)
    {
        // Use relative path from output directory
        var fileName = Path.GetFileName(imagePath);
        return $"![{caption}](memes/{fileName})";
    }

    /// <summary>
    /// Creates markdown for a meme using a URL (for uploaded memes)
    /// </summary>
    public static string CreateMemeMarkdownFromUrl(string imageUrl, string caption)
    {
        return $"![{caption}]({imageUrl})";
    }

    /// <summary>
    /// Get surrounding text context for a line
    /// </summary>
    private static string GetSurroundingText(string[] lines, int lineNumber, int radius)
    {
        int start = Math.Max(0, lineNumber - radius);
        int end = Math.Min(lines.Length - 1, lineNumber + radius);

        var surroundingLines = new List<string>();
        for (int i = start; i <= end; i++)
        {
            surroundingLines.Add(lines[i]);
        }

        return string.Join('\n', surroundingLines);
    }

    /// <summary>
    /// Extract article topics/keywords for meme generation
    /// </summary>
    public static string[] ExtractTopics(ArticleOutput article)
    {
        var topics = new List<string>();

        // Add tags
        if (article.Tags != null && article.Tags.Length > 0)
        {
            topics.AddRange(article.Tags);
        }

        // Extract from title (split by common separators)
        var titleWords = article.Title
            .Split(new[] { ' ', ':', '-', '—', '–' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3)
            .Take(5);
        topics.AddRange(titleWords);

        return topics.Distinct().ToArray();
    }

    /// <summary>
    /// Calculate article length category for meme decision
    /// </summary>
    public static string GetArticleLengthCategory(int wordCount)
    {
        if (wordCount < 500) return "short";
        if (wordCount < 1500) return "medium";
        if (wordCount < 3000) return "long";
        return "very-long";
    }

    /// <summary>
    /// Parse meme URL from agent conversation
    /// </summary>
    public static string? ExtractMemeUrlFromContent(string content)
    {
        if (string.IsNullOrEmpty(content))
            return null;

        // Look for localhost URLs
        var urlPattern = @"(http://localhost:\d+/[^\s\)""']+)";
        var match = Regex.Match(content, urlPattern);
        
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return null;
    }

    /// <summary>
    /// Validate that output directory exists and is writable
    /// </summary>
    public static void EnsureOutputDirectory(string directory)
    {
        try
        {
            Directory.CreateDirectory(directory);
            
            // Test write permission
            var testFile = Path.Combine(directory, ".test");
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Cannot write to meme output directory: {directory}", ex);
        }
    }
}

