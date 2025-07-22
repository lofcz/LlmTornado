
namespace LlmTornado.Agents
{
    /// <summary>
    /// Base class to define provider clients
    /// </summary>
    public abstract class ModelClient
    {
        /// <summary>
        /// Gets or sets the <see cref="CancellationTokenSource"/> used to signal cancellation requests.
        /// </summary>
        public CancellationTokenSource CancelTokenSource { get; set; } = new CancellationTokenSource();
        /// <summary>
        /// Model being ran
        /// </summary>
        public string Model { get; set; }

        public ModelClient() { }

        public ModelClient(string model)
        {
            Model = model;
        }
        /// <summary>
        /// Create a async streaming response (use StreamingCallbacks like Console.Write)
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="options"></param>
        /// <param name="streamingCallback"></param>
        /// <returns></returns>
        public async Task<ModelResponse> _CreateStreamingResponseAsync(List<ModelItem> messages, ModelResponseOptions options, StreamingCallbacks? streamingCallback = null)
        {
            return await CreateStreamingResponseAsync(messages, options, streamingCallback);
        }

        /// <summary>
        /// Create a custom provider streaming response
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="options"></param>
        /// <param name="streamingCallback"></param>
        /// <returns></returns>
        public virtual async Task<ModelResponse> CreateStreamingResponseAsync(List<ModelItem> messages, ModelResponseOptions options, StreamingCallbacks? streamingCallback = null)
        {
            return await CreateStreamingResponseAsync(messages, options, streamingCallback);
        }

        /// <summary>
        /// Internal use Create async Response
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<ModelResponse> _CreateResponseAsync(List<ModelItem> messages, ModelResponseOptions options) 
        {
            return await CreateResponseAsync(messages, options);
        }

        /// <summary>
        /// Create a custom async provider response
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public virtual async Task<ModelResponse> CreateResponseAsync(List<ModelItem> messages, ModelResponseOptions options)
        {
            return new ModelResponse();
        }

        /// <summary>
        /// Create a custom provider response
        /// </summary>
        /// <param name="messages"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public virtual ModelResponse CreateResponse(List<ModelItem> messages, ModelResponseOptions options)
        {
            return new ModelResponse();
        }

    }
}
