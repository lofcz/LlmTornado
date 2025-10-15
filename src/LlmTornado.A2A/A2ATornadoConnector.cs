using A2A;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.A2A;

public class A2ATornadoConnector
{
    private List<string> a2AServersEndpoints { get; set; } = new List<string> ();
    public Dictionary<string, AgentCard> A2ACards { get; set; } = new Dictionary<string, AgentCard>();

    private A2AConnectorClient connectorClient { get; set; } = new A2AConnectorClient();

    /// <summary>
    /// This class serves as a connector to interact with multiple A2A agents and host the agent tool.
    /// </summary>
    /// <param name="agentEndpoints"></param>
    public A2ATornadoConnector(List<string> agentEndpoints)
    {
        a2AServersEndpoints = agentEndpoints;
        Task.Run(async () => await SetupClientInfo()).Wait();
    }

    private async Task SetupClientInfo() 
    {         
        foreach (var endpoint in a2AServersEndpoints)
        {
            try
            {
                var agentCard = await connectorClient.GetAgentCardAsync(endpoint);
                A2ACards[agentCard.Name] = agentCard;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get agent card from {endpoint}: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// (Agent Tool) Retrieves a formatted string containing a list of available agents and their details.
    /// </summary>
    /// <remarks>The returned string includes the name, description, and endpoint URL of each available agent.
    /// Each agent is listed on a new line in the format: "- [Name]: [Description] (Endpoint: [URL])".</remarks>
    /// <returns>A string containing the details of all available agents. If no agents are available, the string will indicate
    /// that no agents are listed.</returns>
    [Description("Get a list of available agents.")]
    public string GetAvailableAgentsTool()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Available Agents:");
        foreach (var agent in A2ACards.Values)
        {
            sb.AppendLine($"- {agent.Name}: {agent.Description} (Endpoint: {agent.Url})");
        }
        if (A2ACards.Count == 0)
        {
            sb.AppendLine("No agents available.");
        }
        return sb.ToString();
    }

    /// <summary>
    /// (Agent Tool) Sends a message to a specified agent and returns the agent's response.
    /// </summary>
    /// <param name="agentName">The name of the agent to send the message to.</param>
    /// <param name="message">The message to send to the agent.</param>
    /// <returns></returns>
    [Description("Send a message to a specified agent and get the response.")]
    public async Task<string> SendMessageTool(
        [Description("The name of the agent to send the message to.")] string agentName, 
        [Description("The message to send to the agent.")] string message)
    {
        if (!A2ACards.ContainsKey(agentName))
        {
            return $"Agent '{agentName}' not found.";
        }

        var agentCard = A2ACards[agentName];
        var parts = new List<Part> { new TextPart { Text = message } };
        A2AResponse response = await connectorClient.SendMessageAsync(agentCard.Url, parts);

        if (response is AgentMessage amessage)
        {
            var responseText = string.Join("\n", amessage.Parts.OfType<TextPart>().Select(p => p.Text));
            return responseText;
        }
        else if (response is AgentTask atask)
        {
            return $"Task created with ID: {atask.Id}, Status: {atask.Status}";
        }
        else
        {
            return "Unexpected response type.";
        }
    }

}
