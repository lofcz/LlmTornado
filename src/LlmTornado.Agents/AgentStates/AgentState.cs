using LlmTornado.Chat;
using LlmTornado.StateMachines;
using System.Text.Json;
using System.Text.RegularExpressions;
using LlmTornado.Agents.DataModels;
using static LlmTornado.Agents.TornadoRunner;

namespace LlmTornado.Agents.AgentStates;

public interface IAgentState
{
    /// <summary>
    /// Callbacks from the runner operations for verbose output.
    /// </summary>
    public RunnerVerboseCallbacks? RunnerVerboseCallbacks { get; set; }
    /// <summary>
    /// Call backs from the agent state for streaming channels
    /// </summary>
    public StreamingCallbacks? StreamingCallbacks { get; set; }
    /// <summary>
    /// Control Agent of the state
    /// </summary>
    public TornadoAgent StateAgent { get; set; }

    /// <summary>
    /// Verbose event callback for recieving verbose messages.
    /// </summary>
    public event Action<string>? RunningVerboseCallback;

    /// <summary>
    /// Occurs when a streaming operation is running and provides updates.
    /// </summary>
    /// <remarks>This event is triggered during the execution of a streaming operation, passing a
    /// string parameter that contains the current status or data update. Subscribers can use this event to receive
    /// real-time updates.</remarks>
    public event ModelStreamingEvent? RunningStreamingCallback;

    /// <summary>
    /// Gets or sets the <see cref="CancellationTokenSource"/> used to signal cancellation requests.
    /// </summary>
    public CancellationTokenSource CancelTokenSource { get; set; }  
}

public abstract class AgentState<TInput, TOutput> : BaseState<TInput, TOutput>, IAgentState
{
    public RunnerVerboseCallbacks? RunnerVerboseCallbacks { get; set; }
    public StreamingCallbacks? StreamingCallbacks { get; set; }
    public TornadoAgent StateAgent { get; set; }
    public event Action<string>? RunningVerboseCallback;

    public event ModelStreamingEvent? RunningStreamingCallback;

    public CancellationTokenSource CancelTokenSource { get; set; } = new CancellationTokenSource();

    /// <summary>
    /// Initializes the state agent, preparing it for operation.
    /// </summary>
    /// <remarks>This method must be called before any other operations on the state agent are
    /// performed. Failure to initialize may result in undefined behavior.</remarks>
    public abstract TornadoAgent InitializeStateAgent();

    public override async Task<TOutput> Invoke(TInput input)
    {
        throw new NotImplementedException();
    }

    public AgentState(StateMachine stateMachine)
    {
        CurrentStateMachine = stateMachine;
        CurrentStateMachine.States.Add(this); //Keep States alive in the StateMachine
        StateAgent = InitializeStateAgent(); // Initialize the agent state, which sets up the agent and its properties

        RunnerVerboseCallbacks += ReceiveVerbose; //Setup the Verbose channel to trigger the RunningVerboseCallback event
        StreamingCallbacks += ReceiveStreaming; //Setup the Streaming channel to trigger the RunningStreamingCallback event

        StateAgent.Options.CancellationToken = CancelTokenSource.Token; // Set the cancellation token source for the agent client
    }

    /// <summary>
    /// Invokes the callback to process a verbose message.
    /// </summary>
    /// <remarks>This method triggers the <see cref="RunningVerboseCallback"/> delegate, if it is set,
    /// passing the provided message for further handling. Ensure that the callback is assigned  before calling this
    /// method to avoid a null reference exception.</remarks>
    /// <param name="message">The verbose message to be processed. Cannot be null.</param>
    public ValueTask ReceiveVerbose(string message)
    {
        RunningVerboseCallback?.Invoke(message);
        return default;
    }

    /// <summary>
    /// Processes a streaming message by invoking the associated callback.
    /// </summary>
    /// <remarks>This method triggers the <see cref="RunningStreamingCallback"/> delegate with the
    /// provided message. Ensure that <see cref="RunningStreamingCallback"/> is not null before calling this method
    /// to avoid a <see cref="NullReferenceException"/>.</remarks>
    /// <param name="message">The message received from the stream. Cannot be null.</param>
    public ValueTask ReceiveStreaming(ModelStreamingEvents message)
    {
        RunningStreamingCallback?.Invoke(message);
        return default;
    }

    /// <summary>
    /// Initiates an asynchronous operation to process the specified input using the given agent.
    /// </summary>
    /// <remarks>Use this to receive the first text from the Run, and processes with another agent other than the state Agent</remarks>
    /// <param name="agent">The agent responsible for processing the input.</param>
    /// <param name="input">The input data to be processed by the agent.</param>
    /// <param name="streaming">A value indicating whether the operation should use streaming. Defaults to <see langword="false"/>.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the processed text output.</returns>
    public async Task<Conversation> BeginRunnerAsync(TornadoAgent agent, string input, bool streaming = false)
    {
        return (await RunAsync(agent, input, verboseCallback: RunnerVerboseCallbacks,streamingCallback:StreamingCallbacks ,streaming: streaming, cancellationToken: CancelTokenSource));
    }

    /// <summary>
    /// Initiates an asynchronous operation to process the specified input using the given agent.
    /// </summary>
    /// <remarks>Use this automatically parse the output result, and processes with another agent other than the state Agent</remarks>
    /// <param name="agent">The agent responsible for processing the input.</param>
    /// <param name="input">The input data to be processed by the agent.</param>
    /// <param name="streaming">A value indicating whether the operation should use streaming. Defaults to <see langword="false"/>.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the processed text output.</returns>
    //public async Task<T> BeginRunnerAsync<T>(Agent agent, string input, bool streaming = false)
    //{
    //    return (await Runner.RunAsync(agent, input, verboseCallback: RunnerVerboseCallbacks, streamingCallback: StreamingCallbacks, streaming: streaming, cancellationToken: CancelTokenSource)).ParseJson<T>();
    //}

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
        return (await RunAsync(StateAgent, input, verboseCallback: RunnerVerboseCallbacks, streamingCallback: StreamingCallbacks, streaming: streaming, cancellationToken: CancelTokenSource));
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
            Conversation result = await RunAsync(StateAgent, input,
                verboseCallback: RunnerVerboseCallbacks,
                streamingCallback: StreamingCallbacks,
                streaming: streaming,
                cancellationToken: CancelTokenSource);

            // First try standard parsing
            if (result.Messages.Last().Content.TryParseJson<T>(out T? parsedResult))
            {
                return parsedResult;
            }

            // Attempt JSON repair if standard parsing fails
            string? repairedJson = await RepairJsonAsync(result.Messages.Last().Content, typeof(T));
            if (repairedJson != null && TryParseJson(repairedJson, out parsedResult))
            {
                RunningVerboseCallback?.Invoke($"JSON repaired and parsed successfully on attempt {attempt + 1}");
                return parsedResult;
            }

            // If not the last attempt, try again with improved prompt
            if (attempt < maxRetries)
            {
                string retryPrompt = $"The previous response couldn't be parsed as valid JSON for type {typeof(T).Name}. " +
                                     "Please provide a properly formatted JSON response that matches this C# class structure. " +
                                     "Previous response: {result.Text}";

                RunningVerboseCallback?.Invoke($"Retry attempt {attempt + 1}: Requesting properly formatted JSON");
                input = retryPrompt;
            }
        }

        throw new InvalidOperationException($"Failed to parse the result into {typeof(T).Name} after {maxRetries + 1} attempts.");
    }

    private async Task<string> RepairJsonAsync(string possibleJson, Type targetType)
    {
        try
        {
            // Basic cleanup - remove Markdown code fences and leading/trailing whitespace
            string cleaned = possibleJson.Trim();
            cleaned = Regex.Replace(cleaned, @"^```json\s*|```$", "", RegexOptions.Multiline);

            // Check if it's valid JSON already
            try
            {
                JsonDocument.Parse(cleaned);
                return cleaned; // It's valid, return as is
            }
            catch (JsonException) { /* Continue with repair attempts */ }

            // If basic cleaning didn't work, we can use the LLM itself to repair the JSON
            string repairPrompt = $"Fix this invalid JSON to match the C# type {targetType.Name}. " +
                                  $"Return ONLY the fixed JSON with no explanations or markdown:\n{cleaned}";

            Conversation repairResult = await RunAsync(StateAgent, repairPrompt,
                cancellationToken: CancelTokenSource);

            // Clean the repair result
            string repairedJson = repairResult.Messages.Last().Content?.Trim() ?? "";
            repairedJson = Regex.Replace(repairedJson, @"^```json\s*|```$", "", RegexOptions.Multiline);

            // Validate the repaired JSON
            try
            {
                JsonDocument.Parse(repairedJson);
                return repairedJson;
            }
            catch (JsonException)
            {
                RunningVerboseCallback?.Invoke("JSON repair attempt failed");
                return null;
            }
        }
        catch (Exception ex)
        {
            RunningVerboseCallback?.Invoke($"Error during JSON repair: {ex.Message}");
            return null;
        }
    }

    private static bool TryParseJson<T>(string json, out T result)
    {
        try
        {
            result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            });
            return result != null;
        }
        catch
        {
            result = default;
            return false;
        }
    }
}