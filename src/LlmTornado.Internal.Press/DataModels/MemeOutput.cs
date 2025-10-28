using System;

namespace LlmTornado.Internal.Press.DataModels;

/// <summary>
/// Output from meme generation process including validation results
/// </summary>
public class MemeGenerationOutput
{
    public string Url { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public double ValidationScore { get; set; }
    public string[] Feedback { get; set; } = [];
    public int IterationCount { get; set; }
    public bool Approved { get; set; }
    public string Topic { get; set; } = string.Empty;
}

/// <summary>
/// Result from vision model validation of a meme
/// </summary>
public class MemeValidationResult
{
    public bool Approved { get; set; }
    public string[] Issues { get; set; } = [];
    public string[] Suggestions { get; set; } = [];
    public double Score { get; set; }
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Decision about whether to generate memes for an article
/// </summary>
public class MemeDecision
{
    public bool ShouldGenerateMemes { get; set; }
    public int MemeCount { get; set; }
    public string[] Topics { get; set; } = [];
    public string Reasoning { get; set; } = string.Empty;
}

/// <summary>
/// Represents a point in the article where a meme should be inserted
/// </summary>
public class MemeInsertionPoint
{
    public int LineNumber { get; set; }
    public string Context { get; set; } = string.Empty;
    public string MemeUrl { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string SurroundingText { get; set; } = string.Empty;
}

/// <summary>
/// Combined output from meme decision and generation
/// </summary>
public class MemeCollectionOutput
{
    public MemeGenerationOutput[] Memes { get; set; } = [];
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}


