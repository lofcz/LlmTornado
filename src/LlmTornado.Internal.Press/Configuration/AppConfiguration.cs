using Newtonsoft.Json;
using System;
using System.IO;

namespace LlmTornado.Internal.Press.Configuration;

public class AppConfiguration
{
    public string Objective { get; set; } = string.Empty;
    public ApiKeysConfiguration ApiKeys { get; set; } = new();
    public ModelsConfiguration Models { get; set; } = new();
    public OutputConfiguration Output { get; set; } = new();
    public ImageGenerationConfiguration ImageGeneration { get; set; } = new();
    public ReviewLoopConfiguration ReviewLoop { get; set; } = new();
    public ArticleGenerationConfiguration ArticleGeneration { get; set; } = new();
    public TrendAnalysisConfiguration TrendAnalysis { get; set; } = new();
    public TavilyConfiguration Tavily { get; set; } = new();
    public CodebaseAccessConfiguration CodebaseAccess { get; set; } = new();

    public static AppConfiguration Load(string configPath = "appCfg.json")
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found: {configPath}");
        }

        string json = File.ReadAllText(configPath);
        var config = JsonConvert.DeserializeObject<AppConfiguration>(json);

        if (config == null)
        {
            throw new InvalidOperationException("Failed to deserialize configuration");
        }

        config.Validate();
        return config;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Objective))
        {
            throw new InvalidOperationException("Objective must be specified in configuration");
        }

        if (string.IsNullOrWhiteSpace(ApiKeys.Tavily))
        {
            throw new InvalidOperationException("Tavily API key is required");
        }

        if (string.IsNullOrWhiteSpace(Output.Directory))
        {
            throw new InvalidOperationException("Output directory must be specified");
        }
    }

    public void Save(string configPath = "appCfg.json")
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(configPath, json);
    }
}

public class ApiKeysConfiguration
{
    [JsonProperty("openAi")]
    public string OpenAi { get; set; } = string.Empty;

    [JsonProperty("anthropic")]
    public string Anthropic { get; set; } = string.Empty;

    [JsonProperty("tavily")]
    public string Tavily { get; set; } = string.Empty;

    [JsonProperty("google")]
    public string Google { get; set; } = string.Empty;

    [JsonProperty("groq")]
    public string Groq { get; set; } = string.Empty;
}

public class ModelsConfiguration
{
    [JsonProperty("ideation")]
    public string Ideation { get; set; } = "gpt-4o-mini";

    [JsonProperty("trendAnalysis")]
    public string TrendAnalysis { get; set; } = "gpt-4o-mini";

    [JsonProperty("research")]
    public string Research { get; set; } = "gpt-4o";

    [JsonProperty("writing")]
    public string Writing { get; set; } = "gpt-4o";

    [JsonProperty("review")]
    public string Review { get; set; } = "gpt-4o";

    [JsonProperty("imagePrompt")]
    public string ImagePrompt { get; set; } = "gpt-4o-mini";
}

public class OutputConfiguration
{
    [JsonProperty("directory")]
    public string Directory { get; set; } = "./output/articles";

    [JsonProperty("exportMarkdown")]
    public bool ExportMarkdown { get; set; } = true;

    [JsonProperty("exportJson")]
    public bool ExportJson { get; set; } = true;

    [JsonProperty("includeMetadata")]
    public bool IncludeMetadata { get; set; } = true;
}

public class ImageGenerationConfiguration
{
    [JsonProperty("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonProperty("model")]
    public string Model { get; set; } = "dall-e-3";
}

public class ReviewLoopConfiguration
{
    [JsonProperty("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonProperty("maxIterations")]
    public int MaxIterations { get; set; } = 3;

    [JsonProperty("qualityThresholds")]
    public QualityThresholds QualityThresholds { get; set; } = new();

    [JsonProperty("improvementCriteria")]
    public List<string> ImprovementCriteria { get; set; } = new();
}

public class QualityThresholds
{
    [JsonProperty("minWordCount")]
    public int MinWordCount { get; set; } = 800;

    [JsonProperty("minReadabilityScore")]
    public int MinReadabilityScore { get; set; } = 60;

    [JsonProperty("requireSources")]
    public bool RequireSources { get; set; } = true;

    [JsonProperty("minSeoScore")]
    public int MinSeoScore { get; set; } = 70;
}

public class ArticleGenerationConfiguration
{
    [JsonProperty("maxConcurrent")]
    public int MaxConcurrent { get; set; } = 3;

    [JsonProperty("delayBetweenArticles")]
    public int DelayBetweenArticles { get; set; } = 5000;

    [JsonProperty("retryAttempts")]
    public int RetryAttempts { get; set; } = 2;
}

public class TrendAnalysisConfiguration
{
    [JsonProperty("maxTopics")]
    public int MaxTopics { get; set; } = 10;

    [JsonProperty("relevanceThreshold")]
    public double RelevanceThreshold { get; set; } = 0.6;

    [JsonProperty("updateFrequency")]
    public string UpdateFrequency { get; set; } = "daily";
}

public class TavilyConfiguration
{
    [JsonProperty("maxResults")]
    public int MaxResults { get; set; } = 5;

    [JsonProperty("searchDepth")]
    public string SearchDepth { get; set; } = "advanced";

    [JsonProperty("includeDomains")]
    public List<string> IncludeDomains { get; set; } = new();

    [JsonProperty("excludeDomains")]
    public List<string> ExcludeDomains { get; set; } = new();
}

public class CodebaseAccessConfiguration
{
    [JsonProperty("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonProperty("repositoryPath")]
    public string RepositoryPath { get; set; } = "C:\\Users\\lordo\\Documents\\GitHub\\OpenAiNg";

    [JsonProperty("allowedTools")]
    public List<string> AllowedTools { get; set; } = new List<string> 
    { 
        "read_file", 
        "list_directory", 
        "search_files", 
        "get_file_info" 
    };

    [JsonProperty("maxFilesPerSession")]
    public int MaxFilesPerSession { get; set; } = 10;
}

