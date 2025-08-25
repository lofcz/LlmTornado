using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.ChatRuntime;

/// <summary>
/// Agent designed for runtime configurations such as handoff or orchestration for streaming.
/// </summary>
public class RuntimeAgent : TornadoAgent
{
    /// <summary>
    /// Should the agent response be streamed.
    /// </summary>
    public bool Streaming { get; set; } = false;
    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeAgent"/> class with the specified configuration parameters.
    /// </summary>
    /// <param name="client">The <see cref="TornadoApi"/> client used for communication with the backend API.</param>
    /// <param name="model">The chat model to be used by the agent for generating responses.</param>
    /// <param name="name">The name of the agent. Defaults to "Handoff Agent" if not specified.</param>
    /// <param name="instructions">The initial instructions or system prompt for the agent. Defaults to "You are a helpful assistant" if not
    /// specified.</param>
    /// <param name="outputSchema">An optional schema defining the expected structure of the agent's output. Can be <see langword="null"/> if no
    /// schema is required.</param>
    /// <param name="tools">An optional list of delegate tools that the agent can use to perform specific tasks. Can be <see
    /// langword="null"/> if no tools are provided.</param>
    /// <param name="mcpServers">An optional list of <see cref="MCPServer"/> instances for multi-channel processing. Can be <see
    /// langword="null"/> if no servers are specified.</param>
    /// <param name="streaming">A value indicating whether the agent should stream responses. <see langword="true"/> to enable streaming;
    /// otherwise, <see langword="false"/>.</param>
    public RuntimeAgent(TornadoApi client,
            Chat.Models.ChatModel model,
            string name = "Handoff Agent",
            string instructions = "You are a helpful assistant",
            Type? outputSchema = null,
            List<Delegate>? tools = null,
            List<MCPServer>? mcpServers = null,
            bool streaming = false) : base(client, model, name, instructions, outputSchema, tools, mcpServers)
    {
        Streaming = streaming;
    }
}
