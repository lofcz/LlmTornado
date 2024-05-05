using System.Threading.Tasks;

namespace LlmTornado.Chat.Plugins;

public interface IChatPlugin
{
    /// <summary>
    /// A unique vendor namespace to avoid collisions between function symbols cross plugins. Max 20 characters
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// A list o
    /// </summary>
    /// <returns></returns>
    public Task<ChatPluginExportResult> Export();
    
    ChatFunctionCallResult MissingParam(string name)
    {
        return new ChatFunctionCallResult(ChatFunctionCallResultParameterErrors.MissingRequiredParameter, name);
    }
    
    ChatFunctionCallResult MalformedParam(string name)
    {
        return new ChatFunctionCallResult(ChatFunctionCallResultParameterErrors.MalformedParam, name);
    }
}