using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Common;

namespace LlmTornado.Agents.Samples.ContextController;

public interface IContextService
{
    public Task<AgentContext> GetAgentContext();
}

public interface IToolContextService
{
    public Task<List<Tool>>? GetToolContext();
}

public interface IModelContextService
{
    public Task<ChatModel> GetModelContext();
}

public interface IMessageContextService
{
    public Task<List<ChatMessage>> GetChatContext();
}

public interface IInstructionsContextService
{
    public Task<string> GetInstructionsContext();
}

