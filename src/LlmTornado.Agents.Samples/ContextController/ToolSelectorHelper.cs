using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Samples.ContextController;

public class ToolSelectorHelper
{
    /// <summary>
    /// Creates a response format for handing off to another agent.
    /// </summary>
    /// <param name="tools">Agents to add to enum list of selectable agents</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static ChatRequestResponseFormats CreateResponseFormat(Tool[] tools)
    {
        if (tools == null || tools.Length == 0) throw new ArgumentException("Tools cannot be null or empty", nameof(tools));

        List<string> toolNames = tools.Where(t=>t.ToolName is not null)?.Select(t => t.ToolName)!.ToList<string>() ?? new List<string>();

        dynamic? responseFormat = ConvertObjectDictionaryToDynamic(CreateObjectSchemaFormat(toolNames.ToArray()));

        if (responseFormat == null)
        {
            throw new InvalidOperationException("Failed to convert tools object schema to dynamic format.");
        }

        return ChatRequestResponseFormats.StructuredJson(
            "ToolsSelection",
            responseFormat,
            "A list of available Tools",
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

    private static Dictionary<string, object> CreateObjectSchemaFormat(string[] toolNames)
    {

        string[] requiredProperties = ["reason", "tool"];
        string[] arrayPropertiesRequired = ["tools"];

        Dictionary<string, object> arraySchema = new Dictionary<string, object>
        {
            ["type"] = "array",
            ["items"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["reason"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "Reason for the tool selection"
                    },
                    ["tool"] = new Dictionary<string, object>
                    {
                        ["type"] = "string",
                        ["description"] = "The tool to select",
                        ["enum"] = toolNames
                    }
                },
                ["required"] = requiredProperties,
                ["additionalProperties"] = false
            }
        };

        Dictionary<string, object> toolsSchema = new Dictionary<string, object>
        {
            ["tools"] = arraySchema,
        };

        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = toolsSchema,
            ["required"] = arrayPropertiesRequired,
            ["additionalProperties"] = false
        };
    }

    public static List<string> ParseResponse(string response)
    {
        List<string> selectedTools = new();
        if (string.IsNullOrEmpty(response))
        {
            throw new ArgumentException("Response cannot be null or empty", nameof(response));
        }
        try
        {
            using JsonDocument doc = JsonDocument.Parse(response);
            if (doc.RootElement.TryGetProperty("tools", out JsonElement array))
            {
                List<JsonElement> toolArray = array.EnumerateArray().ToList();
                if (toolArray.Count == 0)
                {
                    return selectedTools;
                }

                foreach (var tool in toolArray)
                {
                    if (tool.TryGetProperty("tool", out JsonElement toolNameElement))
                    {
                        string? toolName = toolNameElement.GetString();
                        if (toolName is not null && !string.IsNullOrEmpty(toolName))
                        {
                            selectedTools.Add(toolName);
                        }
                    }
                    else
                    {
                        throw new FormatException("Response does not contain required properties 'Reason' and 'Tool'.");
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            throw new FormatException("Response is not in the expected JSON format.", ex);
        }

        return selectedTools;
    }
}
