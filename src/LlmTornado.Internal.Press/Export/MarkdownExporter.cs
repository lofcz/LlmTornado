using LlmTornado.Internal.Press.Database.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LlmTornado.Internal.Press.Export;

public class MarkdownExporter
{
    private readonly string _outputDirectory;

    public MarkdownExporter(string outputDirectory)
    {
        _outputDirectory = outputDirectory;
    }

    public async Task<string> ExportArticleAsync(Article article)
    {
        // Create directory structure: {output}/{date}/{slug}/
        string date = article.CreatedDate.ToString("yyyy-MM-dd");
        string slug = article.Slug ?? GenerateSlug(article.Title);
        string articleDir = Path.Combine(_outputDirectory, date, slug);

        Directory.CreateDirectory(articleDir);

        // Create memes subdirectory if article contains memes
        string memesDir = Path.Combine(articleDir, "memes");
        if (article.Body.Contains("](memes/"))
        {
            Directory.CreateDirectory(memesDir);
            await CopyMemeFilesAsync(article, memesDir);
        }

        // this is the final article ready for publish - without frontmatter! don't call BuildMarkdown here and save the already clean body
        string markdown = article.Body; // BuildMarkdown(article);

        // Write markdown file
        string markdownPath = Path.Combine(articleDir, "article.md");
        await File.WriteAllTextAsync(markdownPath, markdown);

        return markdownPath;
    }

    private async Task CopyMemeFilesAsync(Article article, string memesDestDir)
    {
        // Extract meme references from markdown: ![caption](memes/filename.jpg)
        Regex memePattern = new System.Text.RegularExpressions.Regex(@"!\[.*?\]\(memes/([^)]+)\)");
        MatchCollection matches = memePattern.Matches(article.Body);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                string fileName = match.Groups[1].Value;
                
                // Try to find the source meme file
                // Check common meme output directories
                string[] possibleSources =
                [
                    Path.Combine("./output/memes", fileName),
                    Path.Combine(_outputDirectory, "../memes", fileName),
                    Path.Combine(Directory.GetCurrentDirectory(), "output", "memes", fileName)
                ];

                foreach (string sourcePath in possibleSources)
                {
                    if (File.Exists(sourcePath))
                    {
                        string destPath = Path.Combine(memesDestDir, fileName);
                        
                        try
                        {
                            File.Copy(sourcePath, destPath, overwrite: true);
                            Console.WriteLine($"  [MarkdownExporter] Copied meme: {fileName}");
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"  [MarkdownExporter] Failed to copy meme {fileName}: {ex.Message}");
                        }
                    }
                }
            }
        }
    }

    private string BuildMarkdown(Article article)
    {
        StringBuilder sb = new StringBuilder();
        
        // Add YAML frontmatter (body should never contain it)
        sb.AppendLine("---");
        sb.AppendLine($"title: \"{EscapeYaml(article.Title)}\"");
        sb.AppendLine($"description: \"{EscapeYaml(article.Description)}\"");
        sb.AppendLine($"date: {article.CreatedDate:yyyy-MM-dd}");
        
        if (article.PublishedDate.HasValue)
        {
            sb.AppendLine($"published: {article.PublishedDate.Value:yyyy-MM-dd}");
        }

        if (!string.IsNullOrEmpty(article.ImageUrl))
        {
            sb.AppendLine($"image: \"{article.ImageUrl}\"");
        }

        // Parse and add tags
        try
        {
            string[]? tags = JsonConvert.DeserializeObject<string[]>(article.Tags);
            if (tags != null && tags.Length > 0)
            {
                sb.AppendLine("tags:");
                foreach (string tag in tags)
                {
                    sb.AppendLine($"  - {tag}");
                }
            }
        }
        catch { }

        sb.AppendLine($"slug: \"{article.Slug}\"");
        sb.AppendLine($"status: \"{article.Status}\"");
        sb.AppendLine($"wordCount: {article.WordCount}");
        sb.AppendLine($"qualityScore: {article.QualityScore:F2}");
        sb.AppendLine("---");
        sb.AppendLine();

        // Article body - strip everything before first # heading
        string cleanBody = StripPreamble(article.Body);
        sb.AppendLine(cleanBody);

        // Optional: Add sources as footnotes
        if (!string.IsNullOrEmpty(article.SourcesJson))
        {
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("## Sources");
            sb.AppendLine();
            
            try
            {
                dynamic? sources = JsonConvert.DeserializeObject<dynamic>(article.SourcesJson);
                if (sources != null)
                {
                    sb.AppendLine("```json");
                    sb.AppendLine(JsonConvert.SerializeObject(sources, Formatting.Indented));
                    sb.AppendLine("```");
                }
            }
            catch { }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Strip everything before the first # heading (removes preamble/introductions)
    /// </summary>
    private string StripPreamble(string body)
    {
        if (string.IsNullOrEmpty(body))
            return body;

        string[] lines = body.Split('\n');
        
        // Find the first line that starts with # (markdown heading)
        // This will strip:
        // - Any preamble text
        // - HTML comments (<!-- -->)
        // - Empty lines before the first heading
        // - Any other content before the article starts
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("#") && !trimmed.StartsWith("#!"))  // Heading, but not shebang
            {
                // Return everything from this line onwards
                return string.Join('\n', lines[i..]);
            }
        }

        // If no heading found, return original body
        // But strip HTML comments and leading blank lines anyway
        int firstNonEmpty = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            string trimmed = lines[i].TrimStart();
            // Skip empty lines and HTML comments
            if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("<!--"))
            {
                firstNonEmpty = i;
                break;
            }
        }
        
        return firstNonEmpty > 0 ? string.Join('\n', lines[firstNonEmpty..]) : body;
    }

    private string EscapeYaml(string value)
    {
        return value.Replace("\"", "\\\"").Replace("\n", " ").Replace("\r", "");
    }

    private string GenerateSlug(string title)
    {
        return title
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace(":", "")
            .Replace(",", "")
            .Replace(".", "")
            .Replace("?", "")
            .Replace("!", "")
            .Replace("&", "and")
            .Trim('-')
            .Substring(0, Math.Min(100, title.Length));
    }
}

