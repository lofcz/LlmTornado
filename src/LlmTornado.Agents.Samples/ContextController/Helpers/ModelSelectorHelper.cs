using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Samples.ContextController;

public class ModelSelectorHelper
{
    /// <summary>
    /// Creates a response format for handing off to another agent.
    /// </summary>
    /// <param name="models">Agents to add to enum list of selectable agents</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static ChatRequestResponseFormats CreateResponseFormat(string[] models)
    {
        if (models == null || models.Length == 0) throw new ArgumentException("models cannot be null or empty", nameof(models));

        List<string> apiNames = models.ToList();

        dynamic? responseFormat = ConvertObjectDictionaryToDynamic(CreateObjectSchemaFormat(apiNames.ToArray()));

        if (responseFormat == null)
        {
            throw new InvalidOperationException("Failed to convert handoff object schema to dynamic format.");
        }

        return ChatRequestResponseFormats.StructuredJson(
            "Model Selector",
            responseFormat,
            "I need you to decide if you need to handoff the conversation to another agent.",
            true
        );

    }

    private static dynamic? ConvertObjectDictionaryToDynamic(Dictionary<string, object> dict)
    {
        string json = JsonSerializer.Serialize(dict, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);
    }

    private static Dictionary<string, object> CreateObjectSchemaFormat(string[] modelNames)
    {
        string[] requiredProperties = ["reason", "model"];

        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["reason"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Reason for the Model Selection"
                },
                ["model"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "The Model to select",
                    ["enum"] = modelNames
                }
            },
            ["required"] = requiredProperties,
            ["additionalProperties"] = false
        };
    }

    public static string? ParseModelSelectionResponse(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            throw new ArgumentException("Response cannot be null or empty", nameof(response));
        }
        try
        {
            using JsonDocument doc = JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("model", out JsonElement model))
            {
                string? modelName = model.GetString();
                if (modelName is not null && !string.IsNullOrEmpty(modelName))
                {
                    return modelName;
                }
            }
        }
        catch (JsonException ex)
        {
            throw new FormatException("Response is not in the expected JSON format.", ex);
        }

        return null;
    }
}
