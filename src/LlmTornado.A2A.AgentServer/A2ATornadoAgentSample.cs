using A2A;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Chat.Models;

namespace LlmTornado.A2A.AgentServer;

public class A2ATornadoAgentSample 
{
    TornadoAgent Agent;
    TornadoApi Client;

    public A2ATornadoAgentSample()
    {
        Client = new TornadoApi(LlmTornado.Code.LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "");

        string instructions = @"
        You are an expert assistant designed to help users with a variety of tasks.
        You can perform tasks such as answering questions, providing recommendations, and assisting with problem-solving.
        You should always strive to provide accurate and helpful information to the user.
        ";

        Agent = new TornadoAgent(
            client: Client,
            model: ChatModel.OpenAi.Gpt5.V5,
            name: "Assistant",
            instructions: instructions,
            streaming: true);
    }

    public BaseA2ATornadoRuntimeConfiguration Build()
    {
        IRuntimeConfiguration runtimeConfig = new SingletonRuntimeConfiguration(Agent); //Add your Runtime Configuration here

        return new SampleRuntimeConfiguration(
            runtimeConfig: runtimeConfig,
            name: "LlmTornado.A2A.AgentServer",  //Name of your agent server
            version: "1.0.0" //Version of your agent server
            );
    }
}

/// <summary>
/// Define the Agent Capabilities here
/// </summary>
public class SampleRuntimeConfiguration : BaseA2ATornadoRuntimeConfiguration
{
    /// <summary>
    /// Initializes a new instance of the A2ATornadoRuntimeService
    /// </summary>
    public SampleRuntimeConfiguration(IRuntimeConfiguration runtimeConfig, string name, string version) : base(runtimeConfig, name, version) { }

    /// <summary>
    /// Defines a static Agent Card for the agent
    /// </summary>
    /// <returns></returns>
    public override AgentCard DescribeAgentCard(string agentUrl)
    {
        AgentCapabilities capabilities = new AgentCapabilities()
        {
            Streaming = true,
            PushNotifications = false,
        };

        AgentSkill chattingSkill = new AgentSkill()
        {
            Id = "chatting_skill",
            Name = "Chatting feature",
            Description = "Agent to chat with and search the web.",
            Tags = ["chat", "llm-tornado"],
            Examples =
            [
                "Hello, what's up?",
            ],
        };

        return new AgentCard()
        {
            Name = AgentName,
            Description = "Agent to chat with and search the web",
            Url = agentUrl, // Placeholder URL
            Version = AgentVersion,
            DefaultInputModes = ["text"],
            DefaultOutputModes = ["text"],
            Capabilities = capabilities,
            Skills = [chattingSkill],
        };
    }
}
