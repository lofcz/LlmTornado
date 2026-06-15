using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace LlmTornado.Interactions;

internal static class VendorGoogleInteractionsJson
{
    internal static readonly JsonSerializerSettings Settings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        }
    };

    internal static Interaction? DeserializeInteraction(string json)
    {
        Interaction? interaction = JsonConvert.DeserializeObject<Interaction>(json, Settings);
        if (interaction is not null)
        {
            InteractionSchemaMigration.Normalize(interaction);
        }

        return interaction;
    }

    internal static string SerializeRequest(InteractionCreateRequest request)
    {
        ApplyDeepResearchDefaults(request);

        JObject obj = JObject.FromObject(request, JsonSerializer.Create(Settings));

        object? responseFormat = InteractionSchemaMigration.BuildResponseFormatProperty(request);
        if (responseFormat is not null)
        {
            if (request.ApiRevision is InteractionSchemaRevision.LegacyMay2026)
            {
                obj["response_format"] = responseFormat is JObject jo ? jo : JToken.FromObject(responseFormat, JsonSerializer.Create(Settings));
            }
            else
            {
                obj["response_format"] = JToken.FromObject(responseFormat, JsonSerializer.Create(Settings));
            }
        }

        if (request.ApiRevision is InteractionSchemaRevision.LegacyMay2026 && request.ResponseMimeType is not null)
        {
            obj["response_mime_type"] = request.ResponseMimeType;
        }

        InteractionGenerationConfig? generationConfig = InteractionSchemaMigration.BuildGenerationConfig(request);
        if (generationConfig is not null)
        {
            obj["generation_config"] = JObject.FromObject(generationConfig, JsonSerializer.Create(Settings));
        }

        InteractionResponseFormat? migratedImage = InteractionSchemaMigration.MigrateImageConfigToResponseFormat(request);
        if (migratedImage is not null && obj["response_format"] is null)
        {
            obj["response_format"] = JObject.FromObject(migratedImage, JsonSerializer.Create(Settings));
        }

        if (request.AgentConfig is not null)
        {
            obj["agent_config"] = JObject.FromObject(request.AgentConfig, JsonSerializer.Create(Settings));
        }

        return obj.ToString(Formatting.None);
    }

    private static void ApplyDeepResearchDefaults(InteractionCreateRequest request)
    {
        if (request.Agent is null || !request.Agent.Contains("deep-research", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (request.Background == true && request.Store is not true)
        {
            request.Store = true;
        }

        request.AgentConfig ??= InteractionDeepResearchAgentConfig.Default;
    }
}
