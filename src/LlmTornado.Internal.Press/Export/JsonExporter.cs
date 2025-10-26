using LlmTornado.Internal.Press.Database.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LlmTornado.Internal.Press.Export;

public class JsonExporter
{
    private readonly string _outputDirectory;

    public JsonExporter(string outputDirectory)
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

        // Build article export object
        var exportData = new ArticleExportData
        {
            Id = article.Id,
            Title = article.Title,
            Description = article.Description,
            Body = article.Body,
            ImageUrl = article.ImageUrl,
            Tags = JsonConvert.DeserializeObject<string[]>(article.Tags) ?? Array.Empty<string>(),
            CreatedDate = article.CreatedDate,
            PublishedDate = article.PublishedDate,
            Objective = article.Objective,
            Status = article.Status,
            WordCount = article.WordCount,
            QualityScore = article.QualityScore,
            IterationCount = article.IterationCount,
            Slug = article.Slug,
            Sources = !string.IsNullOrEmpty(article.SourcesJson) 
                ? JsonConvert.DeserializeObject<dynamic>(article.SourcesJson) 
                : null,
            Metadata = !string.IsNullOrEmpty(article.MetadataJson)
                ? JsonConvert.DeserializeObject<dynamic>(article.MetadataJson)
                : null
        };

        // Serialize to JSON
        var json = JsonConvert.SerializeObject(exportData, Formatting.Indented);

        // Write JSON file
        var jsonPath = Path.Combine(articleDir, "article.json");
        await File.WriteAllTextAsync(jsonPath, json);

        return jsonPath;
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

public class ArticleExportData
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime CreatedDate { get; set; }
    public DateTime? PublishedDate { get; set; }
    public string Objective { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public double QualityScore { get; set; }
    public int IterationCount { get; set; }
    public string? Slug { get; set; }
    public dynamic? Sources { get; set; }
    public dynamic? Metadata { get; set; }
}

