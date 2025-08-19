using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LlmTornado.Agents.TornadoRunner;

namespace LlmTornado.Agents.Orchestration.Core;

public interface IAgentRunnable
{
    /// <summary>
    /// Control Agent of the state
    /// </summary>
    public TornadoAgent Agent { get; set; }

    /// <summary>
    /// Verbose event callback for recieving verbose messages.
    /// </summary>
    public Action<string>? OnVerboseEvent { get; }

    /// <summary>
    /// Occurs when a streaming operation is running and provides updates.
    /// </summary>
    /// <remarks>This event is triggered during the execution of a streaming operation, passing a
    /// string parameter that contains the current status or data update. Subscribers can use this event to receive
    /// real-time updates.</remarks>
    public Func<ModelStreamingEvents, ValueTask>? OnStreamingEvent { get; }

    /// <summary>
    /// Gets or sets the <see cref="CancellationTokenSource"/> used to signal cancellation requests.
    /// </summary>
    public CancellationTokenSource cts { get; set; }

    public void SubscribeStreamingChannel(StreamingCallbacks? streamingChannel);
    public void UnsubscribeStreamingChannel(StreamingCallbacks? streamingChannel);
}

public class RunnableAgent : OrchestrationRunnable<ChatMessage, ChatMessage>, IAgentRunnable
{
    public TornadoAgent Agent { get; set; }
    public Action<string>? OnVerboseEvent { get; }

    public Func<ModelStreamingEvents, ValueTask>? OnStreamingEvent { get; }

    public CancellationTokenSource cts { get; set; } = new CancellationTokenSource();

    public bool IsStreaming { get; set; } = false;

    public Conversation Conversation { get; set; }

    public RunnableAgent(TornadoAgent agent, bool streaming = false) 
    {
        Agent = agent; 
        IsStreaming = streaming;
    }

    public override async ValueTask<ChatMessage> Invoke(ChatMessage input)
    {

        if(Conversation != null)
        {
            Conversation = await RunAsync(Agent, conversation: Conversation, messages: [input], streamingCallback: ReceiveStreaming, streaming: IsStreaming, cancellationToken: cts.Token);
        }
        else
        {
            Conversation = await RunAsync(Agent, messages: [input], streamingCallback: ReceiveStreaming, streaming: IsStreaming, cancellationToken: cts.Token);
        }

        return Conversation.Messages.Last();
    }

    /// <summary>
    /// Initiates an asynchronous operation to process the specified input and returns the result as a string.
    /// </summary>
    ///  <remarks>Use this to automatically setup the verbose channels and cancellation within the system</remarks>
    /// <param name="input">The input data to be processed by the runner.</param>
    /// <param name="streaming">A boolean value indicating whether the operation should be performed in streaming mode. <see
    /// langword="true"/> to enable streaming; otherwise, <see langword="false"/>.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the processed output as a
    /// string. If the operation does not produce any output, an empty string is returned.</returns>
    public async Task<Conversation> BeginRunnerAsync(ChatMessage message, bool streaming = false)
    {
        return (await RunAsync(Agent, messages: [message], streamingCallback: ReceiveStreaming, streaming: streaming, cancellationToken: cts.Token));
    }

    /// <summary>
    /// Initiates an asynchronous operation to process the specified input and returns the result as a Type <see langword="T"/>.
    /// </summary>
    ///  <remarks>Use this to automatically setup the verbose channels and cancellation within the system</remarks>
    /// <param name="input">The input data to be processed by the runner.</param>
    /// <param name="streaming">A boolean value indicating whether the operation should be performed in streaming mode. <see
    /// langword="true"/> to enable streaming; otherwise, <see langword="false"/>.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the processed output as a
    /// string. If the operation does not produce any output, an empty string is returned.</returns>
    public async Task<T> BeginRunnerAsync<T>(ChatMessage message, bool streaming = false, int maxRetries = 2)
    {
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            Conversation result = await RunAsync(Agent, messages: [message],
                streamingCallback: ReceiveStreaming,
                streaming: streaming,
                cancellationToken: cts.Token);

            // First try standard parsing
            if (result.Messages.Last().Content.TryParseJson<T>(out T? parsedResult))
            {
                return parsedResult;
            }

            // Attempt JSON repair if standard parsing fails
            T? smartParsedResult = await JsonUtility.SmartParseJsonAsync<T>(Agent, result.Messages.Last().Content!);

            if (smartParsedResult != null)
            {
                OnVerboseEvent?.Invoke($"JSON repaired and parsed successfully on attempt {attempt + 1}");
                return smartParsedResult;
            }

            // If not the last attempt, try again with improved prompt
            if (attempt < maxRetries)
            {
                string retryPrompt = $"The previous response couldn't be parsed as valid JSON for type {typeof(T).Name}. " +
                                     "Please provide a properly formatted JSON response that matches this C# class structure. " +
                                     "Previous response: {result.Text}";

                OnVerboseEvent?.Invoke($"Retry attempt {attempt + 1}: Requesting properly formatted JSON");
                message.Parts.Last().Text = retryPrompt;
            }
        }

        throw new InvalidOperationException($"Failed to parse the result into {typeof(T).Name} after {maxRetries + 1} attempts.");
    }


    /// <summary>
    /// Processes a streaming message by invoking the associated callback.
    /// </summary>
    /// <remarks>This method triggers the <see cref="OnStreamingEvent"/> delegate with the
    /// provided message. Ensure that <see cref="OnStreamingEvent"/> is not null before calling this method
    /// to avoid a <see cref="NullReferenceException"/>.</remarks>
    /// <param name="message">The message received from the stream. Cannot be null.</param>
    internal ValueTask ReceiveStreaming(ModelStreamingEvents message)
    {
        OnStreamingEvent?.Invoke(message);
        return Threading.ValueTaskCompleted;
    }

    public void SubscribeStreamingChannel(StreamingCallbacks? streamingChannel)
    {
        streamingChannel += ReceiveStreaming; // Register the streaming channel to receive updates
    }


    public void UnsubscribeStreamingChannel(StreamingCallbacks? streamingChannel)
    {
        streamingChannel -= ReceiveStreaming; // Register the streaming channel to receive updates
    }
}
