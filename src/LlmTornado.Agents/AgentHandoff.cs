using LlmTornado.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LlmTornado.Agents
{
    public class AgentHandoff
    {
        public TornadoAgent Agent { get; set; }
        public string Name { get; set; }
        public string HandoffReason { get; set; } 

        public AgentHandoff(TornadoAgent agent, string name, string handoffReason)
        {
            Agent = agent ?? throw new ArgumentNullException(nameof(agent), "Agent cannot be null");
            HandoffReason = string.IsNullOrEmpty(handoffReason)? throw new ArgumentNullException(nameof(agent), "handoff Reason cannot be empty") : handoffReason;
            Name = string.IsNullOrEmpty(name) ? throw new ArgumentNullException(nameof(name), "Name cannot be empty") : name;
        }

        public static ChatRequestResponseFormats CreateHandoffResponseFormat(AgentHandoff[] handoffs)
        {
            if (handoffs == null || handoffs.Length == 0)
            {
                throw new ArgumentException("Handoffs cannot be null or empty", nameof(handoffs));
            }
            List<string> agentNames = handoffs.Select(h => h.Name).ToList();
            agentNames.Add("CurrentAgent"); // Add the current agent as an option
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
                                { "enum",  agentNames.ToArray() }
                            }
                        }
                    };
            
            string[] requiredProperties = ["reason", "agent"];
            Dictionary<string, object> schema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = propSchema,
                ["required"] = requiredProperties,
                ["additionalProperties"] = false
            };

            string json = JsonSerializer.Serialize(schema, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            dynamic? responseFormat = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);

            return ChatRequestResponseFormats.StructuredJson(
                "handoff",
                responseFormat,
                "I need you to decide if you need to handoff the conversation to another agent.",
                true
            );

        }

        public static string ParseHandoffResponse(string response, out string? reasoning)
        {
            if (string.IsNullOrEmpty(response))
            {
                throw new ArgumentException("Response cannot be null or empty", nameof(response));
            }
            try
            {
                using JsonDocument doc = JsonDocument.Parse(response);
                if (doc.RootElement.TryGetProperty("reason", out JsonElement reasonElement) &&
                    doc.RootElement.TryGetProperty("agent", out JsonElement agentElement))
                {
                    reasoning = reasonElement.GetString();
                    return agentElement.GetString();
                }
                else
                {
                    throw new FormatException("Response does not contain required properties 'Reason' and 'Agent'.");
                }
            }
            catch (JsonException ex)
            {
                throw new FormatException("Response is not in the expected JSON format.", ex);
            }
        }
    }
}
