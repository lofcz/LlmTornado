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

        // Build markdown content with frontmatter
        var markdown = BuildMarkdown(article);

        // Write markdown file
        var markdownPath = Path.Combine(articleDir, "article.md");
        await File.WriteAllTextAsync(markdownPath, markdown);

        return markdownPath;
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

        // Article body (pure markdown, no frontmatter)
        sb.AppendLine(article.Body);

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

