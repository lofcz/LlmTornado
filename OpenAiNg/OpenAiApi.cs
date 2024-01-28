using System;
using System.Threading.Tasks;
using OpenAiNg.Assistants;
using OpenAiNg.Audio;
using OpenAiNg.Chat;
using OpenAiNg.Completions;
using OpenAiNg.Embedding;
using OpenAiNg.Files;
using OpenAiNg.Images;
using OpenAiNg.Models;
using OpenAiNg.Moderation;
using OpenAiNg.Threads;

namespace OpenAiNg;

/// <summary>
///     Entry point to the OpenAPI API, handling auth and allowing access to the various API endpoints
/// </summary>
public class OpenAiApi : IOpenAiApi
{
    private IAssistantsEndpoint? _assistantsEndpoint;
    private IAudioEndpoint? _audioEndpoint;
    private IChatEndpoint? _chat;
    private ICompletionEndpoint? _completionEndpoint;
    private IEmbeddingEndpoint? _embedding;
    private IFilesEndpoint? _files;
    private IImageEditEndpoint? _imageEditEndpoint;
    private IImageGenerationEndpoint? _imageGenerationEndpoint;
    private IModelsEndpoint? _models;
    private IModerationEndpoint? _moderation;
    private IThreadsEndpoint? _threadsEndpoint;

    /// <summary>
    ///     Creates a new entry point to the OpenAPI API, handling auth and allowing access to the various API endpoints
    /// </summary>
    /// <param name="apiKeys">
    ///     The API authentication information to use for API calls, or <see langword="null" /> when using self-hosted provider
    ///     such as KoboldCpp
    /// </param>
    public OpenAiApi(ApiAuthentication? apiKeys)
    {
        Auth = apiKeys;
    }

    /// <summary>
    ///     Create a new OpenAiApi via API key, suitable for OpenAI as a provider
    /// </summary>
    /// <param name="apiKey">API key</param>
    public OpenAiApi(string apiKey)
    {
        Auth = new ApiAuthentication(apiKey);
    }

    /// <summary>
    ///     Create a new OpenAiApi via API key and organization key, suitable for Azure OpenAI
    /// </summary>
    /// <param name="apiKey">API key</param>
    /// <param name="organizationKey">Organization key</param>
    public OpenAiApi(string apiKey, string organizationKey)
    {
        Auth = new ApiAuthentication(apiKey, organizationKey);
    }

    /// <summary>
    ///     Interceptor
    /// </summary>
    public Func<ChatRequest, ChatResult?, Task>? ChatRequestInterceptor { get; set; }

    /// <summary>
    ///     The API lets you do operations with images. Given a prompt and an input image, the model will edit a new image.
    /// </summary>
    public IImageEditEndpoint ImageEdit => _imageEditEndpoint ??= new ImageEditEndpoint(this);

    /// <summary>
    ///     Manages audio operations such as transcipt and translate.
    /// </summary>
    public IAudioEndpoint Audio => _audioEndpoint ??= new AudioEndpoint(this);

    /// <summary>
    ///     Assistants are higher-level API than <see cref="ChatEndpoint" /> featuring automatic context management, code
    ///     interpreter and file based retrieval.
    /// </summary>
    public IAssistantsEndpoint Assistants => _assistantsEndpoint ??= new AssistantsEndpoint(this);

    /// <summary>
    ///     Assistants are higher-level API than <see cref="ChatEndpoint" /> featuring automatic context management, code
    ///     interpreter and file based retrieval.
    /// </summary>
    public IThreadsEndpoint Threads => _threadsEndpoint ??= new ThreadsEndpoint(this);

    /// <summary>
    ///     Base url for OpenAI
    ///     for OpenAI, should be "https://api.openai.com/{0}/{1}"
    ///     for Azure, should be
    ///     "https://(your-resource-name.openai.azure.com/openai/deployments/(deployment-id)/{1}?api-version={0}"
    ///     this will be formatted as {0} = <see cref="ApiVersion" />, {1} = <see cref="EndpointBase.Endpoint" />
    /// </summary>
    public string ApiUrlFormat { get; set; } = "https://api.openai.com/{0}/{1}";

    /// <summary>
    ///     Version of the Rest Api
    /// </summary>
    public string ApiVersion { get; set; } = "v1";

    /// <summary>
    ///     The API authentication information to use for API calls
    /// </summary>
    public ApiAuthentication? Auth { get; private set; }

    /// <summary>
    ///     Sets the API authentication information to use for API calls
    /// </summary>
    public void SetAuth(ApiAuthentication auth)
    {
        Auth = auth;
    }

    /// <summary>
    ///     Text generation is the core function of the API. You give the API a prompt, and it generates a completion. The way
    ///     you “program” the API to do a task is by simply describing the task in plain english or providing a few written
    ///     examples. This simple approach works for a wide range of use cases, including summarization, translation, grammar
    ///     correction, question answering, chatbots, composing emails, and much more (see the prompt library for inspiration).
    /// </summary>
    public ICompletionEndpoint Completions => _completionEndpoint ??= new CompletionEndpoint(this);

    /// <summary>
    ///     The API lets you transform text into a vector (list) of floating point numbers. The distance between two vectors
    ///     measures their relatedness. Small distances suggest high relatedness and large distances suggest low relatedness.
    /// </summary>
    public IEmbeddingEndpoint Embeddings => _embedding ??= new EmbeddingEndpoint(this);

    /// <summary>
    ///     Text generation in the form of chat messages. This interacts with the ChatGPT API.
    /// </summary>
    public IChatEndpoint Chat => _chat ??= new ChatEndpoint(this);

    /// <summary>
    ///     Classify text against the OpenAI Content Policy.
    /// </summary>
    public IModerationEndpoint Moderation => _moderation ??= new ModerationEndpoint(this);

    /// <summary>
    ///     The API endpoint for querying available Engines/models
    /// </summary>
    public IModelsEndpoint Models => _models ??= new ModelsEndpoint(this);

    /// <summary>
    ///     The API lets you do operations with files. You can upload, delete or retrieve files. Files can be used for
    ///     fine-tuning, search, etc.
    /// </summary>
    public IFilesEndpoint Files => _files ??= new FilesEndpoint(this);

    /// <summary>
    ///     The API lets you do operations with images. Given a prompt and/or an input image, the model will generate a new
    ///     image.
    /// </summary>
    public IImageGenerationEndpoint ImageGenerations => _imageGenerationEndpoint ??= new ImageGenerationEndpoint(this);
}