using LlmTornado.Common;

namespace LlmTornado.Agents.DataModels;

public class TornadoAgentTool
{
    /// <summary>
    /// Tornado Agent to run on tool call
    /// </summary>
    public TornadoAgent ToolAgent { get; set; }

    /// <summary>
    /// Tool Generated from the agent
    /// </summary>
    public Tool Tool { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TornadoAgentTool"/> class with the specified agent and tool.
    /// </summary>
    /// <param name="agent">The <see cref="TornadoAgent"/> instance associated with this tool. Cannot be null.</param>
    /// <param name="tool">The <see cref="Tool"/> instance to be managed by this agent. Cannot be null.</param>
    public TornadoAgentTool(TornadoAgent agent, Tool tool)
    {
        ToolAgent = agent;
        Tool = tool;
    }
}