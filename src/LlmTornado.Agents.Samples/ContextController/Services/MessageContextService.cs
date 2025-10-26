using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.VectorDatabases;

namespace LlmTornado.Agents.Samples.ContextController;

public class MessageContextService : IMessageContextService
{
    private TornadoApi _client { get; set; }
    private ContextContainer _contextContainer { get; set; }

    //private MessageProviderService _messageProvider { get; set; }

    private MessageCompressionService _messageCompressor { get; set; }

    public MessageContextService(TornadoApi api, ContextContainer contextContainer)
    {
        _client = api;
        _contextContainer = contextContainer;
        //_messageProvider = new MessageProviderService(_client, _contextContainer);
        _messageCompressor = new MessageCompressionService(_client, _contextContainer);
    }

    public async Task<List<ChatMessage>> GetChatContext()
    {
        return await _messageCompressor.Invoke();
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
    private MessageMetadataStore _metadataStore { get; set; }

    public CompressedContextStore CompressedContextStore { get; set; } = new CompressedContextStore();

    IMessagesSummarizer _summarizer;
    IMessagesCompressionStrategy _compressionStrategy;

    public MessageCompressionService(TornadoApi api, ContextContainer contextContainer, IVectorDatabase? longTermMemory = null, IMessagesSummarizer? summarizer = null, IMessagesCompressionStrategy? compressionStrategy = null)
    {
        _client = api;
        _contextContainer = contextContainer;
        _metadataStore = new MessageMetadataStore();
        var model = _contextContainer.CurrentModel;

        var _compressionOptions = new ContextWindowCompressionOptions
        {
            TargetUtilization = 0.40,
            UncompressedCompressionThreshold = 0.30,
            CompressedReCompressionThreshold = 0.80,
            ReCompressionTarget = 0.20,
            LargeMessageThreshold = 10000,
            SummaryModel = ChatModel.OpenAi.Gpt35.Turbo,
            MaxSummaryTokens = 1000,
        };

        _compressionStrategy =  compressionStrategy ?? new ContextWindowCompressionStrategy(
            model,
            _metadataStore,
            _compressionOptions
        );

        _summarizer =  summarizer ?? new ContextWindowMessageSummarizer(
            _client,
            model,
            _metadataStore,
            _compressionOptions);

        // Track existing messages
        foreach (var message in _contextContainer.ChatMessages)
        {
            _metadataStore.Track(message);
        }
    }

    public async Task<List<ChatMessage>> Invoke()
    {
        if (_compressionStrategy.ShouldCompress(_contextContainer.ChatMessages))
        {
            var compressedMessages = await _summarizer.SummarizeMessages(
                _contextContainer.ChatMessages,
                _compressionStrategy.GetCompressionOptions(_contextContainer.ChatMessages));
            
            // Rebuild the ContextContainer.ChatMessages with the compressed result
            _contextContainer.ChatMessages = compressedMessages;
        }
        
        return _contextContainer.ChatMessages;
    }
    
    public void TrackNewMessage(ChatMessage message)
    {
        _metadataStore.Track(message);
    }
    
    public ContextWindowAnalysis GetAnalysis()
    {
        var strategy = new ContextWindowCompressionStrategy(
            _contextContainer.CurrentModel,
            _metadataStore,
            new ContextWindowCompressionOptions());
        
        return strategy.AnalyzeMessages(_contextContainer.ChatMessages);
    }
}
