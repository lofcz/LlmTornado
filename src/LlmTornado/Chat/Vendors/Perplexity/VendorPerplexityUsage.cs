using LlmTornado.Code;
using LlmTornado.Responses;
using LlmTornado.Vendor.Anthropic;
using LlmTornado.Vendor.Google;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Perplexity;

/// <summary>
/// Usage reported by perplexity
/// </summary>
public class VendorPerplexityUsage : Usage, IChatUsage
{
   /// <summary>
   /// Number of tokens in the completion.
   /// </summary>
   [JsonProperty("completion_tokens")]
   public int CompletionTokens { get; set; }
    
   /// <summary>
   /// The search context size for the request.
   /// </summary>
   [JsonProperty("search_context_size")]
   public string? SearchContextSize { get; set; }

   /// <summary>
   /// Detailed cost information for the request.
   /// </summary>
   [JsonProperty("cost")]
   public VendorPerplexityUsageCost? Cost { get; set; }
}

/// <summary>
/// Detailed cost breakdown for a Perplexity API request.
/// </summary>
public class VendorPerplexityUsageCost
{
   /// <summary>
   /// Cost attributed to input tokens.
   /// </summary>
   [JsonProperty("input_tokens_cost")]
   public double InputTokensCost { get; set; }

   /// <summary>
   /// Cost attributed to output tokens.
   /// </summary>
   [JsonProperty("output_tokens_cost")]
   public double OutputTokensCost { get; set; }

   /// <summary>
   /// Fixed cost per request.
   /// </summary>
   [JsonProperty("request_cost")]
   public double RequestCost { get; set; }

   /// <summary>
   /// The total cost for this API call.
   /// </summary>
   [JsonProperty("total_cost")]
   public double TotalCost { get; set; }
}