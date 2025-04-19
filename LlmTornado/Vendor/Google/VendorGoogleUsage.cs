using System.Collections.Generic;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Vendor.Google;

internal class VendorGoogleUsage : IChatUsage
{
    /// <summary>
    /// Number of tokens in the prompt. When cachedContent is set, this is still the total effective prompt size meaning this includes the number of tokens in the cached content.
    /// </summary>
    [JsonProperty("promptTokenCount")]
    public int PromptTokenCount { get; set; }
    
    /// <summary>
    /// Number of tokens in the cached part of the prompt (the cached content)
    /// </summary>
    [JsonProperty("cachedContentTokenCount")]
    public int CachedContentTokenCount { get; set; }
    
    /// <summary>
    /// Total number of tokens across all the generated response candidates.
    /// </summary>
    [JsonProperty("candidatesTokenCount")]
    public int CandidatesTokenCount { get; set; }
    
    /// <summary>
    /// Output only. Number of tokens present in tool-use prompt(s).
    /// </summary>
    [JsonProperty("toolUsePromptTokenCount")]
    public int ToolUsePromptTokenCount { get; set; }
    
    /// <summary>
    /// Output only. Number of tokens of thoughts for thinking models.
    /// </summary>
    [JsonProperty("thoughtsTokenCount")]
    public int ThoughtsTokenCount { get; set; }
    
    /// <summary>
    /// Total token count for the generation request (prompt + response candidates).
    /// </summary>
    [JsonProperty("totalTokenCount")]
    public int TotalTokenCount { get; set; }
    
    /// <summary>
    /// Output only. List of modalities that were processed in the request input.
    /// </summary>
    [JsonProperty("promptTokensDetails")]
    public List<VendorGoogleUsageModalityDetail>? PromptTokensDetails { get; set; }
    
    /// <summary>
    /// Output only. List of modalities of the cached content in the request input.
    /// </summary>
    [JsonProperty("cacheTokensDetails")]
    public List<VendorGoogleUsageModalityDetail>? CacheTokensDetails { get; set; }
    
    /// <summary>
    /// Output only. List of modalities that were returned in the response.
    /// </summary>
    [JsonProperty("candidatesTokensDetails")]
    public List<VendorGoogleUsageModalityDetail>? CandidatesTokensDetails { get; set; }
    
    /// <summary>
    /// Output only. List of modalities that were processed for tool-use request inputs.
    /// </summary>
    [JsonProperty("toolUsePromptTokensDetails")]
    public List<VendorGoogleUsageModalityDetail>? ToolUsePromptTokensDetails { get; set; }
}

internal class VendorGoogleUsageModalityDetail
{
    /// <summary>
    /// The modality associated with this token count.
    /// MODALITY_UNSPECIFIED	Unspecified modality.
    /// TEXT	Plain text.
    /// IMAGE	Image.
    /// VIDEO	Video.
    /// AUDIO	Audio.
    /// DOCUMENT	Document, e.g. PDF.
    /// </summary>
    [JsonProperty("modality")]
    public string Modality { get; set; }
    
    [JsonProperty("tokenCount")]
    public int TokenCount { get; set; }
}

internal class VendorGooglePromptFeedback
{
    /// <summary>
    /// BLOCK_REASON_UNSPECIFIED	Default value. This value is unused.
    /// SAFETY	Prompt was blocked due to safety reasons. Inspect safetyRatings to understand which safety category blocked it.
    /// OTHER	Prompt was blocked due to unknown reasons.
    /// BLOCKLIST	Prompt was blocked due to the terms which are included from the terminology blocklist.
    /// PROHIBITED_CONTENT	Prompt was blocked due to prohibited content.
    /// </summary>
    [JsonProperty("blockReason")]
    public string? BlockReason { get; set; }

    /// <summary>
    /// List of ratings for the safety of a response candidate. There is at most one rating per category.
    /// </summary>
    [JsonProperty("safetyRatings")]
    public List<VendorGooglePromptFeedbackSafetyRating>? SafetyRatings { get; set; }
}

internal class VendorGooglePromptFeedbackSafetyRating
{
    /// <summary>
    ///    HARM_CATEGORY_UNSPECIFIED	Category is unspecified.
    ///    HARM_CATEGORY_DEROGATORY	PaLM - Negative or harmful comments targeting identity and/or protected attribute.
    ///    HARM_CATEGORY_TOXICITY	PaLM - Content that is rude, disrespectful, or profane.
    ///    HARM_CATEGORY_VIOLENCE	PaLM - Describes scenarios depicting violence against an individual or group, or general descriptions of gore.
    ///    HARM_CATEGORY_SEXUAL	PaLM - Contains references to sexual acts or other lewd content.
    ///    HARM_CATEGORY_MEDICAL	PaLM - Promotes unchecked medical advice.
    ///    HARM_CATEGORY_DANGEROUS	PaLM - Dangerous content that promotes, facilitates, or encourages harmful acts.
    ///    HARM_CATEGORY_HARASSMENT	Gemini - Harassment content.
    ///    HARM_CATEGORY_HATE_SPEECH	Gemini - Hate speech and content.
    ///    HARM_CATEGORY_SEXUALLY_EXPLICIT	Gemini - Sexually explicit content.
    ///    HARM_CATEGORY_DANGEROUS_CONTENT	Gemini - Dangerous content.
    ///    HARM_CATEGORY_CIVIC_INTEGRITY	Gemini - Content that may be used to harm civic integrity.
    /// </summary>
    [JsonProperty("category")]
    public string Category { get; set; }
    
    /// <summary>
    /// HARM_PROBABILITY_UNSPECIFIED	Probability is unspecified.
    /// NEGLIGIBLE	Content has a negligible chance of being unsafe.
    /// LOW	Content has a low chance of being unsafe.
    /// MEDIUM	Content has a medium chance of being unsafe.
    /// HIGH	Content has a high chance of being unsafe.
    /// </summary>
    [JsonProperty("probability")]
    public string Probability { get; set; }
   
    /// <summary>
    /// Was this content blocked because of this rating?
    /// </summary>
    [JsonProperty("blocked")]
    public bool Blocked { get; set; }
}