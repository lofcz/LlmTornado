using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.ChatRuntime;

public class RuntimeAgent : TornadoAgent
{
    public bool Streaming { get; set; } = false;
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
