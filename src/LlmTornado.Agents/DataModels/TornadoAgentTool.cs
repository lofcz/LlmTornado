using LlmTornado.Common;

namespace LlmTornado.Agents.DataModels;

public class TornadoAgentTool
{
    public TornadoAgent ToolAgent { get; set; }
    //Need to abstract this
    public Tool Tool { get; set; }

    public TornadoAgentTool(TornadoAgent agent, Tool tool)
    {
        ToolAgent = agent;
        Tool = tool;
    }
}