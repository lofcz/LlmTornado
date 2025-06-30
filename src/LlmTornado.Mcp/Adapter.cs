using LlmTornado.Common;
using ModelContextProtocol.Client;

namespace LlmTornado.Mcp;

public static class McpExtensions
{
    public static async ValueTask<List<Tool>> ListTornadoToolsAsync(this IMcpClient client)
    {
        IList<McpClientTool> tools = await client.ListToolsAsync();
        List<Tool> converted = tools.Select(x => x.ToTornadoTool()).ToList();
        return converted;
    }

    public static Tool ToTornadoTool(this McpClientTool tool)
    {
        return new Tool(new ToolFunction(tool.Name, tool.Description, tool.JsonSchema));
    }
}