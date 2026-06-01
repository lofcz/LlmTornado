using System.Collections.Generic;
using System.Text;
using LlmTornado.Chat.Vendors.Google;
using LlmTornado.Code;
using LlmTornado.Webhooks;
using Newtonsoft.Json;

namespace LlmTornado.Batch.Vendors.Google;

/// <summary>
/// Google/Gemini-specific JSONL batch request line.
/// </summary>
internal class VendorGoogleBatchRequestLine
{
    [JsonProperty("key")]
    public string Key { get; set; } = string.Empty;

    [JsonProperty("request")]
    public VendorGoogleChatRequest Request { get; set; }

    public VendorGoogleBatchRequestLine(BatchRequestItem item, IEndpointProvider provider)
    {
        Key = item.CustomId;
        Request = new VendorGoogleChatRequest(item.Params, provider);
    }
}

/// <summary>
/// Input configuration for the batch.
/// </summary>
internal class VendorGoogleBatchInputConfig
{
    [JsonProperty("file_name")]
    public string FileName { get; set; }

    public VendorGoogleBatchInputConfig(string fileName)
    {
        FileName = fileName;
    }
}

/// <summary>
/// Batch configuration object.
/// </summary>
internal class VendorGoogleBatchConfig
{
    [JsonProperty("display_name")]
    public string? DisplayName { get; set; }
    
    [JsonProperty("input_config")]
    public VendorGoogleBatchInputConfig InputConfig { get; set; }
    
    [JsonProperty("priority")]
    public string? Priority { get; set; }

    [JsonProperty("webhook_config")]
    public GeminiWebhookConfig? WebhookConfig { get; set; }

    public VendorGoogleBatchConfig(VendorGoogleBatchInputConfig inputConfig)
    {
        InputConfig = inputConfig;
    }
}

/// <summary>
/// Google/Gemini batch creation request body.
/// </summary>
internal class VendorGoogleBatchRequest
{
    [JsonProperty("batch")]
    public VendorGoogleBatchConfig Batch { get; set; }

    public VendorGoogleBatchRequest(BatchRequest request, string inputFileId)
    {
        Batch = new VendorGoogleBatchConfig(new VendorGoogleBatchInputConfig(inputFileId));

        // Set display name from vendor extensions if provided
        Batch.DisplayName = request.VendorExtensions?.Google?.DisplayName ?? $"batch_{System.Guid.NewGuid():N}";

        // Set priority if provided
        if (request.VendorExtensions?.Google?.Priority is not null)
        {
            Batch.Priority = request.VendorExtensions.Google.Priority.Value.ToString();
        }

        if (request.VendorExtensions?.Google?.WebhookConfig is not null)
        {
            Batch.WebhookConfig = request.VendorExtensions.Google.WebhookConfig;
        }
    }
    
    /// <summary>
    /// Serializes the request to JSON.
    /// </summary>
    public string Serialize()
    {
        return JsonConvert.SerializeObject(this, EndpointBase.NullSettings);
    }

    /// <summary>
    /// Serializes batch request items to JSONL format for Gemini file uploads.
    /// </summary>
    public static string SerializeToJsonl(BatchRequest request, IEndpointProvider provider)
    {
        StringBuilder sb = new StringBuilder();

        foreach (BatchRequestItem item in request.Requests)
        {
            VendorGoogleBatchRequestLine line = new VendorGoogleBatchRequestLine(item, provider);
            string json = JsonConvert.SerializeObject(line, EndpointBase.NullSettings);
            sb.AppendLine(json);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Serializes batch request items to JSONL bytes for Gemini file uploads.
    /// </summary>
    public static byte[] SerializeToJsonlBytes(BatchRequest request, IEndpointProvider provider)
    {
        return Encoding.UTF8.GetBytes(SerializeToJsonl(request, provider));
    }
}
