using LlmTornado.Internal.Press.Database.Models;
using LlmTornado.Internal.Press.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LlmTornado.Internal.Press.Export;

public class JsonExporter
{
    private readonly string _outputDirectory;
    private readonly ImageVariationConfiguration? _variationConfig;

    public JsonExporter(string outputDirectory, ImageVariationConfiguration? variationConfig = null)
    {
        _outputDirectory = outputDirectory;
        _variationConfig = variationConfig;
    }

    public async Task<string> ExportArticleAsync(Article article)
    {
        // Create directory structure: {output}/{date}/{slug}/
        string date = article.CreatedDate.ToString("yyyy-MM-dd");
        string slug = article.Slug ?? GenerateSlug(article.Title);
        string articleDir = Path.Combine(_outputDirectory, date, slug);

        Directory.CreateDirectory(articleDir);

        // Parse image variations if available
        // Format stored: { "devto": "/path/to/image_1000_420.png", "og-image": "/path/..." }
        ImageVariationInfo[]? variations = null;
        if (!string.IsNullOrEmpty(article.ImageVariationsJson))
        {
            try
            {
                Dictionary<string, string>? variationPaths = JsonConvert.DeserializeObject<Dictionary<string, string>>(article.ImageVariationsJson);
                if (variationPaths != null && variationPaths.Count > 0)
                {
                    List<ImageVariationInfo> variationList = new List<ImageVariationInfo>();
                    
                    // Build dimension mapping from configuration
                    Dictionary<string, (int width, int height)> dimensionMap = new Dictionary<string, (int, int)>();
                    if (_variationConfig?.Formats != null)
                    {
                        foreach (ImageVariationFormat format in _variationConfig.Formats)
                        {
                            if (!string.IsNullOrEmpty(format.Name))
                            {
                                dimensionMap[format.Name] = (format.Width, format.Height);
                            }
                        }
                    }
                    
                    foreach (KeyValuePair<string, string> kvp in variationPaths)
                    {
                        // Try to get dimensions from configuration mapping first
                        int width = 0, height = 0;
                        if (dimensionMap.TryGetValue(kvp.Key, out (int w, int h) dims))
                        {
                            width = dims.w;
                            height = dims.h;
                        }
                        else
                        {
                            // Fallback: Extract width and height from filename pattern: "image_1000_420.png"
                            string filename = Path.GetFileNameWithoutExtension(kvp.Value);
                            string[] parts = filename.Split('_');
                            
                            // Try to find width_height pattern in filename
                            if (parts.Length >= 3 && 
                                int.TryParse(parts[parts.Length - 2], out width) && 
                                int.TryParse(parts[parts.Length - 1], out height))
                            {
                                // Found dimensions in filename
                            }
                        }
                        
                        variationList.Add(new ImageVariationInfo
                        {
                            Key = kvp.Key,  // e.g., "featured-seo"
                            Link = kvp.Value,  // URL or path
                            Width = width,
                            Height = height
                        });
                    }
                    if (variationList.Count > 0)
                    {
                        variations = variationList.ToArray();
                    }
                }
            }
            catch { /* Ignore parsing errors */ }
        }

        // Build article export object (body excluded - it's in the .md file)
        ArticleExportData exportData = new ArticleExportData
        {
            Id = article.Id,
            Title = article.Title,
            Description = article.Description,
            ImageUrl = article.ImageUrl,
            ImageVariations = variations,
            Tags = JsonConvert.DeserializeObject<string[]>(article.Tags) ?? [],
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
        string json = JsonConvert.SerializeObject(exportData, Formatting.Indented);

        // Write JSON file
        string jsonPath = Path.Combine(articleDir, "article.json");
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
    // Note: Body is not included - it's in the article.md file
    public string? ImageUrl { get; set; }
    public ImageVariationInfo[]? ImageVariations { get; set; }
    public string[] Tags { get; set; } = [];
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

public class ImageVariationInfo
{
    public string Key { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
}

