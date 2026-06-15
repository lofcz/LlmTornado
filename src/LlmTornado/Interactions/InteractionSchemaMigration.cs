using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Interactions;

internal static class InteractionSchemaMigration
{
    internal static void Normalize(Interaction interaction)
    {
        NormalizeUsage(interaction.Usage);

        if (interaction.Steps?.Count > 0 || interaction.LegacyOutputs is null || interaction.LegacyOutputs.Count == 0)
        {
            return;
        }

        interaction.Steps = ConvertLegacyOutputs(interaction.LegacyOutputs);
        interaction.UsesLegacyResponseSchema = true;
    }

    internal static void NormalizeUsage(InteractionUsage? usage)
    {
        if (usage is null)
        {
            return;
        }

        if (usage.TotalThoughtTokens is null && usage.LegacyTotalReasoningTokens is not null)
        {
            usage.TotalThoughtTokens = usage.LegacyTotalReasoningTokens;
        }
    }

    internal static List<InteractionStep> ConvertLegacyOutputs(List<InteractionLegacyOutput> outputs)
    {
        List<InteractionStep> steps = [];
        List<InteractionContent> pendingText = [];

        void FlushText()
        {
            if (pendingText.Count == 0)
            {
                return;
            }

            steps.Add(new InteractionStep
            {
                Type = "model_output",
                Content = [..pendingText]
            });
            pendingText.Clear();
        }

        foreach (InteractionLegacyOutput output in outputs)
        {
            switch (output.Type)
            {
                case "text":
                    pendingText.Add(InteractionContent.AsText(output.Text ?? string.Empty));
                    break;
                case "thought":
                    FlushText();
                    steps.Add(new InteractionStep
                    {
                        Type = "thought",
                        Signature = output.Signature,
                        Summary = output.Text is not null ? [InteractionContent.AsText(output.Text)] : null
                    });
                    break;
                case "function_call":
                    FlushText();
                    steps.Add(new InteractionStep
                    {
                        Type = "function_call",
                        Id = output.Id,
                        Name = output.Name,
                        Arguments = output.Arguments
                    });
                    break;
                case "function_result":
                    FlushText();
                    steps.Add(new InteractionStep
                    {
                        Type = "function_result",
                        CallId = output.CallId,
                        Result = output.Result,
                        IsError = output.IsError
                    });
                    break;
                case "google_search_call":
                case "google_search_result":
                case "code_execution_call":
                case "code_execution_result":
                    FlushText();
                    steps.Add(new InteractionStep
                    {
                        Type = output.Type,
                        Id = output.Id,
                        CallId = output.CallId,
                        Name = output.Name,
                        Arguments = output.Arguments,
                        Result = output.Result,
                        Signature = output.Signature
                    });
                    break;
                default:
                    if (output.Text is not null)
                    {
                        pendingText.Add(InteractionContent.AsText(output.Text));
                    }

                    break;
            }
        }

        FlushText();
        return steps;
    }

    internal static object? BuildResponseFormatProperty(InteractionCreateRequest request)
    {
        if (request.ApiRevision is InteractionSchemaRevision.LegacyMay2026)
        {
            if (request.ResponseFormat?.Schema is not null && request.ResponseFormat.Type is null)
            {
                return request.ResponseFormat.Schema;
            }

            if (request.LegacyJsonSchema is not null)
            {
                return request.LegacyJsonSchema;
            }

            return null;
        }

        if (request.ResponseFormats?.Count > 0)
        {
            return request.ResponseFormats.Count == 1 ? request.ResponseFormats[0] : request.ResponseFormats;
        }

        if (request.ResponseFormat is not null && request.ResponseFormat.Type is not null)
        {
            return request.ResponseFormat;
        }

        if (request.LegacyJsonSchema is not null)
        {
            return InteractionResponseFormat.Json(request.LegacyJsonSchema);
        }

        return null;
    }

    internal static InteractionGenerationConfig? BuildGenerationConfig(InteractionCreateRequest request)
    {
        InteractionGenerationConfig? config = request.GenerationConfig;
        if (request.ApiRevision is not InteractionSchemaRevision.LegacyMay2026 && config?.ImageConfig is not null)
        {
            InteractionGenerationConfig clone = new()
            {
                Temperature = config.Temperature,
                TopP = config.TopP,
                MaxOutputTokens = config.MaxOutputTokens,
                StopSequences = config.StopSequences,
                ThinkingLevel = config.ThinkingLevel,
                ThinkingSummaries = config.ThinkingSummaries
            };
            return clone;
        }

        return config;
    }

    internal static InteractionResponseFormat? MigrateImageConfigToResponseFormat(InteractionCreateRequest request)
    {
        InteractionLegacyImageConfig? imageConfig = request.GenerationConfig?.ImageConfig;
        if (imageConfig is null || request.ApiRevision is InteractionSchemaRevision.LegacyMay2026)
        {
            return null;
        }

        return InteractionResponseFormat.Image(aspectRatio: imageConfig.AspectRatio, imageSize: imageConfig.ImageSize);
    }
}

/// <summary>
/// Legacy flat output item from pre-May-2026 Interactions responses.
/// </summary>
public class InteractionLegacyOutput
{
    [JsonProperty("type")]
    public string? Type { get; set; }

    [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
    public string? Text { get; set; }

    [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
    public string? Id { get; set; }

    [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
    public string? Name { get; set; }

    [JsonProperty("arguments", NullValueHandling = NullValueHandling.Ignore)]
    public JObject? Arguments { get; set; }

    [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
    public object? Result { get; set; }

    [JsonProperty("call_id", NullValueHandling = NullValueHandling.Ignore)]
    public string? CallId { get; set; }

    [JsonProperty("signature", NullValueHandling = NullValueHandling.Ignore)]
    public string? Signature { get; set; }

    [JsonProperty("is_error", NullValueHandling = NullValueHandling.Ignore)]
    public bool? IsError { get; set; }
}
