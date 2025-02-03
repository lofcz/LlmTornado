namespace LlmTornado.Demo;
// ReSharper disable UnusedType.Global // used by reflection

[DemoEnum(demoType: typeof(CachingDemo))]
public enum CachingDemos
{
    [Method(nameof(CachingDemo.Create))]
    Create
}

[DemoEnum(demoType: typeof(FilesDemo))]
public enum FileDemos
{
    [Method(nameof(FilesDemo.Upload))]
    FilesUpload,
    [Method(nameof(FilesDemo.UploadGoogle))]
    GoogleUpload,
    [Method(nameof(FilesDemo.GetAllFilesGoogle))]
    GoogleList,
    [Method(nameof(FilesDemo.GetAllFilesOpenAi))]
    FilesOpenAiList,
    [Method(nameof(FilesDemo.DeleteFileGoogle))]
    GoogleDelete,
    [Method(nameof(FilesDemo.DeleteFileOpenAi))]
    OpenAiDelete
}

[DemoEnum(demoType: typeof(ChatDemo))]
public enum ChatDemos
{
    [Method(nameof(ChatDemo.Completion))]
    Completion,
    [Method(nameof(ChatDemo.StreamWithFunctions))]
    StreamWithFunctions,
    [Method(nameof(ChatDemo.Anthropic))]
    Anthropic,
    [Method(nameof(ChatDemo.AnthropicStreaming))]
    AnthropicStreaming,
    [Method(nameof(ChatDemo.Azure))]
    Azure,
    [Method(nameof(ChatDemo.OpenAiFunctions))]
    OpenAiFunctions,
    [Method(nameof(ChatDemo.AnthropicStreamingFunctions))]
    AnthropicFunctions,
    [Method(nameof(ChatDemo.AnthropicFailFunctions))]
    AnthropicFailFunctions,
    [Method(nameof(ChatDemo.Cohere))]
    Cohere,
    [Method(nameof(ChatDemo.CohereStreaming))]
    CohereStreaming,
    [Flaky("covered by other tests, takes a long time to finish")]
    [Method(nameof(ChatDemo.AllChatVendors))]
    AllChatVendors,
    [Method(nameof(ChatDemo.ChatFunctionRequired))]
    FunctionRequired,
    [Method(nameof(ChatDemo.CohereWebSearch))]
    CohereWebSearch,
    [Method(nameof(ChatDemo.CohereWebSearchStreaming))]
    CohereWebSearchStreaming,
    [Flaky("interactive demo")]
    [Method(nameof(ChatDemo.OpenAiFunctionsStreamingInteractive))]
    OpenAiFunctionsStreamingInteractive,
    [Method(nameof(ChatDemo.AnthropicFunctionsParallel))]
    AnthropicParallelFunctions,
    [Flaky("interactive demo")]
    [Method(nameof(ChatDemo.AnthropicFunctionsStreamingInteractive))]
    AnthropicFunctionsStreamingInteractive,
    [Flaky("interactive demo")]
    [Method(nameof(ChatDemo.CohereFunctionsStreamingInteractive))]
    CohereFunctionsStreamingInteractive,
    [Flaky("interactive demo")]
    [Method(nameof(ChatDemo.CrossVendorFunctionsStreamingInteractive))]
    CrossVendorFunctionsStreamingInteractive,
    [Method(nameof(ChatDemo.OpenAiDisableParallelFunctions))]
    DisableParallelTools,
    [Method(nameof(ChatDemo.Google))]
    Google,
    [Method(nameof(ChatDemo.GoogleFunctions))]
    GoogleFunctions,
    [Method(nameof(ChatDemo.GoogleStream))]
    GoogleStream,
    [Method(nameof(ChatDemo.Completion4Mini))]
    Chat4OMini,
    [Method(nameof(ChatDemo.CompletionGroq))]
    ChatGroq,
    [Method(nameof(ChatDemo.GroqStreaming))]
    GroqStreaming,
    [Method(nameof(ChatDemo.Completion4OStructuredJson))]
    Chat4OStructuredJson,
    [Method(nameof(ChatDemo.Cohere2408))]
    Cohere2408,
    [Method(nameof(ChatDemo.OpenAiO3))]
    OpenAiO3,
    [Method(nameof(ChatDemo.Haiku35))]
    Haiku35,
    [Method(nameof(ChatDemo.AudioInWav))]
    AudioWav,
    [Method(nameof(ChatDemo.AudioInMp3))]
    AudioMp3,
    [Method(nameof(ChatDemo.AudioInAudioOutWav))]
    AudioInAudioOut,
    [Method(nameof(ChatDemo.AudioInAudioOutMultiturn))]
    AudioMultiturn,
    [Method(nameof(ChatDemo.AudioInWavStreaming))]
    AudioWavStreaming,
    [Method(nameof(ChatDemo.AudioInAudioOutWavStreaming))]
    AudioInAudioOutWavStreaming,
    [Method(nameof(ChatDemo.ChatFunctionGemini))]
    ToolsGemini,
    [Method(nameof(ChatDemo.ChatFunctionGeminiStrict))]
    ToolsGeminiStrict,
    [Method(nameof(ChatDemo.CompletionO1Developer))]
    CompletionO1Developer,
    [Method(nameof(ChatDemo.AnthropicCaching))]
    AnthropicCaching,
    [Flaky("interactive")]
    [Method(nameof(ChatDemo.AnthropicCachingChat))]
    AnthropicCachingInteractive,
    [Method(nameof(ChatDemo.GoogleStreamFileInput))]
    GoogleFile
}

[DemoEnum(demoType: typeof(SpeechDemo))]
public enum SpeechDemos
{
   
}

[DemoEnum(demoType: typeof(ThreadsDemo))]
public enum ThreadsDemos
{
    [Flaky]
    [Method(nameof(ThreadsDemo.Create))]
    Create,
    [Flaky]
    [Method(nameof(ThreadsDemo.Retrieve))]
    Retrieve,
    [Flaky]
    [Method(nameof(ThreadsDemo.Modify))]
    Modify,
    [Flaky]
    [Method(nameof(ThreadsDemo.Delete))]
    Delete,
    [Flaky("only assistants v1 are supported")]
    [Method(nameof(ThreadsDemo.CreateMessage))]
    CreateMessage
}

[DemoEnum(demoType: typeof(VisionDemo))]
public enum VisionDemos
{
    [Flaky("Deprecated by OpenAI")]
    [Method(nameof(VisionDemo.VisionBase64))]
    VisionBase64,
    [Flaky("Deprecated by OpenAI")]
    [Method(nameof(VisionDemo.VisionBase64))]
    Vision
}

[DemoEnum(demoType: typeof(VectorStoreDemo))]
public enum VectorStoreDemos
{
    [Method(nameof(VectorStoreDemo.CreateVectorStore))]
    Create,
    [Method(nameof(VectorStoreDemo.RetrieveVectorStore))]
    Retrieve,
    [Method(nameof(VectorStoreDemo.ListVectorStores))]
    List,
    [Method(nameof(VectorStoreDemo.ModifyVectorStore))]
    Modify,
    [Method(nameof(VectorStoreDemo.CreateVectorStoreFile))]
    FilesCreate,
    [Method(nameof(VectorStoreDemo.CreateVectorStoreFileCustomChunkingStrategy))]
    FilesCreateCustomChunkingStrategy,
    [Method(nameof(VectorStoreDemo.ListVectorStoreFiles))]
    FilesList,
    [Method(nameof(VectorStoreDemo.RetrieveVectorStoreFile))]
    FilesRetrieve,
    [Method(nameof(VectorStoreDemo.DeleteVectorStoreFile))]
    FilesDelete,
    [Method(nameof(VectorStoreDemo.CreateVectorStoreFileBatch))]
    FileBatchCreate,
    [Method(nameof(VectorStoreDemo.ListVectorStoreBatchFiles))]
    BatchFileList,
    [Method(nameof(VectorStoreDemo.RetrieveVectorStoreFileBatch))]
    FileBatchRetrieve,
    [Method(nameof(VectorStoreDemo.CancelVectorStoreFileBatch))]
    FileBatchCancel,
    [Method(nameof(VectorStoreDemo.DeleteVectorStore))]
    Delete,
    [Method(nameof(VectorStoreDemo.DeleteAllDemoVectorStores))]
    DeleteAllDemo
}

[DemoEnum(demoType: typeof(ImagesDemo))]
public enum ImageDemos
{
    [Flaky]
    [Method(nameof(ImagesDemo.Generate))]
    Generate
}

[DemoEnum(demoType: typeof(CustomProviderDemo))]
public enum CustomProviderDemos
{
    [Flaky("requires ollama")]
    [Method(nameof(CustomProviderDemo.Ollama))]
    Ollama,
    [Flaky("requires ollama")]
    [Method(nameof(CustomProviderDemo.OllamaStreaming))]
    OllamaStreaming
}

[DemoEnum(demoType: typeof(EmbeddingDemo))]
public enum EmbeddingDemos
{
    [Method(nameof(EmbeddingDemo.Embed))]
    OpenAiScalar,
    [Method(nameof(EmbeddingDemo.EmbedVector))]
    OpenAiVector,
    [Method(nameof(EmbeddingDemo.EmbedCohere))]
    CohereScalar,
    [Method(nameof(EmbeddingDemo.EmbedCohereVector))]
    CohereVector,
    [Method(nameof(EmbeddingDemo.EmbedCohereExtensions))]
    CohereScalarExtensions
}

[DemoEnum(demoType: typeof(TranscriptionDemo))]
public enum TranscriptionDemos
{
    [Method(nameof(TranscriptionDemo.TranscribeFormatText))]
    WhisperV2Text,
    [Method(nameof(TranscriptionDemo.TranscribeFormatJson))]
    WhisperV2Json,
    [Method(nameof(TranscriptionDemo.TranscribeFormatSrt))]
    WhisperV2Srt,
    [Method(nameof(TranscriptionDemo.TranscribeFormatJsonVerbose))]
    WhisperV2JsonVerbose,
    [Method(nameof(TranscriptionDemo.TranscribeFormatJsonVerboseGroq))]
    WhisperV3TurboJsonVerbose
}