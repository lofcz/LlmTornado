using LlmTornado.Internal.Press.Database.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
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
        var date = article.CreatedDate.ToString("yyyy-MM-dd");
        var slug = article.Slug ?? GenerateSlug(article.Title);
        var articleDir = Path.Combine(_outputDirectory, date, slug);

        Directory.CreateDirectory(articleDir);

        // Create memes subdirectory if article contains memes
        var memesDir = Path.Combine(articleDir, "memes");
        if (article.Body.Contains("](memes/"))
        {
            Directory.CreateDirectory(memesDir);
            await CopyMemeFilesAsync(article, memesDir);
        }

        // Build markdown content with frontmatter
        var markdown = BuildMarkdown(article);

        // Write markdown file
        var markdownPath = Path.Combine(articleDir, "article.md");
        await File.WriteAllTextAsync(markdownPath, markdown);

        return markdownPath;
    }

    private async Task CopyMemeFilesAsync(Article article, string memesDestDir)
    {
        // Extract meme references from markdown: ![caption](memes/filename.jpg)
        var memePattern = new System.Text.RegularExpressions.Regex(@"!\[.*?\]\(memes/([^)]+)\)");
        var matches = memePattern.Matches(article.Body);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                var fileName = match.Groups[1].Value;
                
                // Try to find the source meme file
                // Check common meme output directories
                var possibleSources = new[]
                {
                    Path.Combine("./output/memes", fileName),
                    Path.Combine(_outputDirectory, "../memes", fileName),
                    Path.Combine(Directory.GetCurrentDirectory(), "output", "memes", fileName)
                };

                foreach (var sourcePath in possibleSources)
                {
                    if (File.Exists(sourcePath))
                    {
                        var destPath = Path.Combine(memesDestDir, fileName);
                        
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
        var sb = new StringBuilder();
        
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
            var tags = JsonConvert.DeserializeObject<string[]>(article.Tags);
            if (tags != null && tags.Length > 0)
            {
                sb.AppendLine("tags:");
                foreach (var tag in tags)
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
        var cleanBody = StripPreamble(article.Body);
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
                var sources = JsonConvert.DeserializeObject<dynamic>(article.SourcesJson);
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

        var lines = body.Split('\n');
        
        // Find the first line that starts with #
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("#"))
            {
                // Return everything from this line onwards
                return string.Join('\n', lines[i..]);
            }
        }

        // If no heading found, return original body
        return body;
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

