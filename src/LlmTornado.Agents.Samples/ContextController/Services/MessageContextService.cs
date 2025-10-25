using LlmTornado.Chat;
using LlmTornado.VectorDatabases;

namespace LlmTornado.Agents.Samples.ContextController;

public class MessageContextService : IMessageContextService
{
    public List<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    private List<ChatMessage> unsavedMessages = new List<ChatMessage>();
    public IVectorDatabase LongTermMemory { get; set; }
    public CompressedContextStore CompressedContextStore { get; set; } = new CompressedContextStore();

    public async Task<List<ChatMessage>> GetChatContext()
    {
        throw new NotImplementedException();
    }

    private async Task StoreNewMessages(List<ChatMessage> newMessages)
    {
        unsavedMessages.AddRange(newMessages);
    }

}
