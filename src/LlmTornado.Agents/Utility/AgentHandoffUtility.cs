using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LlmTornado.Agents
{
    public class AgentHandoffUtility
    {
        public static ChatRequestResponseFormats CreateHandoffResponseFormat(TornadoAgent[] handoffs)
        {
            if (handoffs == null || handoffs.Length == 0) throw new ArgumentException("Handoffs cannot be null or empty", nameof(handoffs));

            List<string> agentNames = handoffs.Select(h => h.Id).ToList();
            agentNames.Add("CurrentAgent"); // Add the current agent as an option

            dynamic? responseFormat = ConvertObjectDictionaryToDynamic(CreateHandoffObjectSchemaFormat(agentNames.ToArray()));

            if (responseFormat == null)
            {
                throw new InvalidOperationException("Failed to convert handoff object schema to dynamic format.");
            }

            return ChatRequestResponseFormats.StructuredJson(
                "handoff",
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

        private static Dictionary<string, object> CreateHandoffObjectSchemaFormat(string[] agentNames)
        {
            Dictionary<string, object> propSchema = new Dictionary<string, object>
                    {
                        { "reason", new Dictionary<string, object>
                            {
                                { "type", "string" },
                                { "description", "Reason for the handoff" }
                            }
                        },
                        { "agent", new Dictionary<string, object>
                            {
                                { "type", "string" },
                                { "description", "The Agent to select" },
                                { "enum",  agentNames }
                            }
                        }
                    };

            string[] requiredProperties = ["reason", "agent"];
            Dictionary<string, object> objectSchema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object> { { "agents", propSchema } }
                ["required"] = requiredProperties,
                ["additionalProperties"] = false
            };

            string[] requiredArrayProperties = ["agents"];

            return new Dictionary<string, object>
            {
                ["type"] = "array",
                ["items"] = objectSchema,
                ["required"] = requiredArrayProperties,
                ["additionalProperties"] = false
            };
        }

        public static List<string> ParseHandoffResponse(string response)
        {
            List<string> selectedAgents = new();
            if (string.IsNullOrEmpty(response))
            {
                throw new ArgumentException("Response cannot be null or empty", nameof(response));
            }
            try
            {
                using JsonDocument doc = JsonDocument.Parse(response);
                if(doc.RootElement.TryGetProperty("agents", out JsonElement array))
                {
                    List<JsonElement> agentArray = array.EnumerateArray().ToList();
                    if(agentArray.Count == 0)
                    {
                        selectedAgents.Add("CurrentAgent"); // No agents specified, return current agent
                        return selectedAgents;
                    }
                    
                    foreach (var agent in agentArray)
                    {
                        if (agent.TryGetProperty("agent", out JsonElement agentNameElement))
                        {
                            string? agentName = agentNameElement.GetString();
                            if (agentName is not null && !string.IsNullOrEmpty(agentName))
                            {
                                selectedAgents.Add(agentName);
                            }
                        }
                        else
                        {
                            throw new FormatException("Response does not contain required properties 'Reason' and 'Agent'.");
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                throw new FormatException("Response is not in the expected JSON format.", ex);
            }

            return selectedAgents;
        }
    }
}
