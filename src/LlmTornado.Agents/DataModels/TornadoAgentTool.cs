using LlmTornado.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents
{
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
}
