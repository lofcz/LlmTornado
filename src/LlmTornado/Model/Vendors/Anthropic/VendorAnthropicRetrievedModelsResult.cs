using System;
using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code;
using LlmTornado.Models;
using Newtonsoft.Json;

namespace LlmTornado.Models.Vendors.Anthropic;

internal class VendorAnthropicRetrievedModelsResult
{
    [JsonProperty("data")]
    public List<VendorAnthropicModelInfo>? Data { get; set; }

    [JsonProperty("first_id")]
    public string? FirstId { get; set; }

    [JsonProperty("last_id")]
    public string? LastId { get; set; }

    [JsonProperty("has_more")]
    public bool? HasMore { get; set; }

    public RetrievedModelsResult ToResult(string? postData)
    {
        return new RetrievedModelsResult
        {
            Data = Data?.Select(x => x.ToRetrievedModel()).ToList() ?? [],
            Obj = "list"
        };
    }
}

internal class VendorAnthropicModelInfo
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("display_name")]
    public string? DisplayName { get; set; }

    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("created_at")]
    public string? CreatedAt { get; set; }

    [JsonProperty("max_input_tokens")]
    public int? MaxInputTokens { get; set; }

    [JsonProperty("max_tokens")]
    public int? MaxTokens { get; set; }

    [JsonProperty("capabilities")]
    public RetrievedModelCapabilities? Capabilities { get; set; }

    public RetrievedModel ToRetrievedModel()
    {
        RetrievedModel model = new RetrievedModel
        {
            Id = Id,
            InternalDisplayName = DisplayName,
            Type = Type,
            MaxInputTokens = MaxInputTokens,
            MaxTokens = MaxTokens,
            Capabilities = Capabilities
        };

        if (CreatedAt is not null && DateTimeOffset.TryParse(CreatedAt, out DateTimeOffset createdAt))
        {
            model.CreatedUnixTime = createdAt.ToUnixTimeSeconds();
        }

        return model;
    }
}

internal static class VendorAnthropicRetrievedModelsDeserializer
{
    internal static RetrievedModelsResult? DeserializeList(string jsonData, string? postData)
    {
        return JsonConvert.DeserializeObject<VendorAnthropicRetrievedModelsResult>(jsonData)?.ToResult(postData);
    }

    internal static RetrievedModel? DeserializeModel(string jsonData, string? postData)
    {
        return JsonConvert.DeserializeObject<VendorAnthropicModelInfo>(jsonData)?.ToRetrievedModel();
    }
}
