using A2A;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Chat;

namespace LlmTornado.A2A.AgentServer;

/// <summary>
/// Wraps Llm Runtime agents to handle Travel related tasks
/// </summary>
public class A2ATornadoRuntimeConfiguration : BaseA2ATornadoRuntimeConfiguration
{
    /// <summary>
    /// Initializes a new instance of the A2ATornadoRuntimeService
    /// </summary>
    public A2ATornadoRuntimeConfiguration(IRuntimeConfiguration runtimeConfig, string name, string version) : base(runtimeConfig, name, version) { }

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
            Tags = ["chat", "websearch", "llm-tornado"],
            Examples =
            [
                "Hello, what's up?",
            "What is the weather like in boston?",
        ],
        };

        return new AgentCard()
        {
            Name = "Tornado Agent",
            Description = "Agent to chat with and search the web",
            Url = agentUrl, // Placeholder URL
            Version = "1.0.0",
            DefaultInputModes = ["text"],
            DefaultOutputModes = ["text"],
            Capabilities = capabilities,
            Skills = [chattingSkill],
        };
    }
}
