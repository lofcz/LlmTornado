using LlmTornado.Agents.DataModels;
using LlmTornado.Agents.Runtime;
using LlmTornado.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Orchestration;

public interface IAgentRunner
{
    /// <summary>
    /// Control Agent of the state
    /// </summary>
    public TornadoAgent Agent { get; set; }
    public void SubscribeVerboseChannel(RunnerVerboseCallbacks? verboseChannel);
    public void SubscribeStreamingChannel(StreamingCallbacks? streamingChannel);
    public void UnsubscribeVerboseChannel(RunnerVerboseCallbacks? verboseChannel);
    public void UnsubscribeStreamingChannel(StreamingCallbacks? streamingChannel);
}


public class AgentRunner
{
    public TornadoAgent Agent { get; set; }

    public Action<string>? OnVerboseEvent { get; }

    public Func<ModelStreamingEvents, ValueTask>? OnStreamingEvent { get; }
    private InternalAgentRunner<ChatMessage, ChatMessage> _agentRunnerInstance { get; }

    internal AgentRunner(ProcessRuntime runtime, TornadoAgent agent)
    {
        _agentRunnerInstance = new InternalAgentRunner<ChatMessage, ChatMessage>(runtime, agent, OnStreamingEvent, OnVerboseEvent);
        _agentRunnerInstance.ActiveRuntime = runtime;
        _agentRunnerInstance.ActiveRuntime.Runners.Add(_agentRunnerInstance); //Keep States alive in the StateMachine
        Agent = agent;
        Agent.Options.CancellationToken = _agentRunnerInstance.cts.Token; // Set the cancellation token source for the agent client
    }

    public async Task<Conversation> BeginRunnerAsync(string input, bool streaming = false)
    {
        return await _agentRunnerInstance.BeginRunnerAsync(input, streaming);
    }

    public async Task<T> BeginRunnerAsync<T>(string input, bool streaming = false, int maxRetries = 2)
    {
        return await _agentRunnerInstance.BeginRunnerAsync<T>(input, streaming, maxRetries);
    }
}

internal class InternalAgentRunner<TInput, TOutput> : Runner<TInput, TOutput>, IAgentRunner
{
    public TornadoAgent Agent { get; set; }

    public Action<string>? OnVerboseEvent { get; }

    private Func<ModelStreamingEvents, ValueTask>? OnStreamingEvent { get; }

    public CancellationTokenSource cts { get; set; } = new CancellationTokenSource();

    public override async Task<TOutput> Invoke(TInput input)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// State which uses a Agent within its invoking process.
    /// This class is designed to manage the lifecycle and operations of an agent state within a state machine.
    /// </summary>
    /// <param name="runtime"></param>
    internal InternalAgentRunner(ProcessRuntime runtime, TornadoAgent agent, Func<ModelStreamingEvents, ValueTask>? onStreamingEvent, Action<string>? onVerboseEvent)
    {
        ActiveRuntime = runtime;
        ActiveRuntime.Runners.Add(this); //Keep States alive in the StateMachine
        Agent = agent;
        Agent.Options.CancellationToken = cts.Token; // Set the cancellation token source for the agent client
        OnStreamingEvent = onStreamingEvent;
        OnVerboseEvent = onVerboseEvent;
    }

    /// <summary>
    /// Invokes the callback to process a verbose message.
    /// </summary>
    /// <remarks>This method triggers the <see cref="OnVerboseEvent"/> delegate, if it is set,
    /// passing the provided message for further handling. Ensure that the callback is assigned  before calling this
    /// method to avoid a null reference exception.</remarks>
    /// <param name="message">The verbose message to be processed. Cannot be null.</param>
    internal ValueTask ReceiveVerbose(string message)
    {
        OnVerboseEvent?.Invoke(message);
        return default;
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
        return default;
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
    public async Task<Conversation> BeginRunnerAsync(string input, bool streaming = false)
    {
        return (await TornadoRunner.RunAsync(Agent, input, verboseCallback: ReceiveVerbose, streamingCallback: ReceiveStreaming, streaming: streaming, cancellationToken: cts.Token));
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
    public async Task<T> BeginRunnerAsync<T>(string input, bool streaming = false, int maxRetries = 2)
    {
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            Conversation result = await TornadoRunner.RunAsync(Agent, input,
                verboseCallback: ReceiveVerbose,
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
                input = retryPrompt;
            }
        }

        throw new InvalidOperationException($"Failed to parse the result into {typeof(T).Name} after {maxRetries + 1} attempts.");
    }

    public void SubscribeVerboseChannel(RunnerVerboseCallbacks? verboseChannel)
    {
        verboseChannel += ReceiveVerbose; // Register the verbose channel to receive updates
    }

    public void SubscribeStreamingChannel(StreamingCallbacks? streamingChannel)
    {
        streamingChannel += ReceiveStreaming; // Register the streaming channel to receive updates
    }

    public void UnsubscribeVerboseChannel(RunnerVerboseCallbacks? verboseChannel)
    {
        verboseChannel -= ReceiveVerbose; // Register the verbose channel to receive updates
    }

    public void UnsubscribeStreamingChannel(StreamingCallbacks? streamingChannel)
    {
        streamingChannel -= ReceiveStreaming; // Register the streaming channel to receive updates
    }
}

