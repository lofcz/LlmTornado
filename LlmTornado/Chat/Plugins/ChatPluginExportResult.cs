using System.Collections.Generic;

namespace LlmTornado.Chat.Plugins;

public class ChatPluginExportResult
{
    public List<ChatPluginFunction> Functions { get; set; }

    public ChatPluginExportResult(List<ChatPluginFunction> functions)
    {
        Functions = functions;
    }
}