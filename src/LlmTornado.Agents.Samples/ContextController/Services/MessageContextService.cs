using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.VectorDatabases;

namespace LlmTornado.Agents.Samples.ContextController;

public class MessageContextService : IMessageContextService
{
    private TornadoApi _client { get; set; }
    private ContextContainer _contextContainer { get; set; }

    public MessageContextService(TornadoApi api, ContextContainer contextContainer)
    {
        _client = api;
        _contextContainer = contextContainer;
    }

    public async Task<List<ChatMessage>> GetChatContext()
    {
        throw new NotImplementedException();
    }
}

public class MessageProviderService
{
    private TornadoApi _client { get; set; }
    private ContextContainer _contextContainer { get; set; }

    public IVectorDatabase LongTermMemory { get; set; }
    public IDocumentEmbeddingProvider EmbeddingProvider { get; set; }

    public MessageProviderService(TornadoApi api, ContextContainer contextContainer)
    {
        _client = api;
        _contextContainer = contextContainer;
    }
    public async Task<List<ChatMessage>> Invoke(string prompt)
    {
        string longTermMemoryContext = await RetrieveLongTermMemory(prompt);

        return new List<ChatMessage>();
    }

    public async Task<string> RetrieveLongTermMemory(string query)
    {
        var embedding = await EmbeddingProvider.Invoke(query);
        var results = await LongTermMemory.QueryByEmbeddingAsync(embedding, topK: 5, includeScore: true);
        return string.Join("\n", results.Select(r => r.Content));
    }
}

public class MessageCompressionService
{
    private TornadoApi _client { get; set; }
    private ContextContainer _contextContainer { get; set; }

    public CompressedContextStore CompressedContextStore { get; set; } = new CompressedContextStore();
    public MessageCompressionService(TornadoApi api, ContextContainer contextContainer, IVectorDatabase longTermMemory)
    {
        _client = api;
        _contextContainer = contextContainer;
    }

    public async Task<List<ChatMessage>> Invoke()
    {
        List<ChatMessage> context = new List<ChatMessage>();

        int totalChunkSize = _contextContainer.ChatMessages.Sum(m => m.GetMessageContent().Length);
        int chunkSize = 10000;

        if ( totalChunkSize >= _contextContainer.CurrentModel.ContextTokens * .60)
        {
            chunkSize = 20000;
        }

        var strat = new TornadoCompressionStrategy(options: new MessageCompressionOptions()
        {
            ChunkSize = 10000,
            SummaryModel = ChatModel.OpenAi.Gpt5.V5Nano,
            MaxSummaryTokens = 1000
        });

        if (strat.ShouldCompress(_contextContainer.ChatMessages))
        {
            var summerizer = new TornadoMessageSummarizer(_client);
            return await summerizer.SummarizeMessages(
                _contextContainer.ChatMessages,
                strat.GetCompressionOptions(_contextContainer.ChatMessages)
                );
        }

        return context;
    }
}
