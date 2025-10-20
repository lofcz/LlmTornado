﻿using LlmTornado.Agents.DataModels;
using LlmTornado.Moderation;
using LlmTornado.Threads;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LlmTornado.Agents.ChatRuntime.Orchestration;

/// <summary>
/// Represents a process that manages the execution state and input for a state-based operation.
/// </summary>
/// <remarks>The <see cref="StateProcess"/> class provides functionality to manage the state of an
/// operation, including the ability to rerun the operation a specified number of times. It also allows for the
/// creation of state results and the conversion to a typed state process.</remarks>
public class RunnableProcess
{
    /// <summary>
    /// Max alled reruns for this process.
    /// </summary>
    [JsonIgnore]
    public int MaxReruns { get; set; } = 3;
    /// <summary>
    /// Current rerun attempts for this process.
    /// </summary>
    /// [SerializationRequired]
    [JsonPropertyName("attempts")]
    public int RerunAttempts { get; set; } = 0;
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    /// [SerializationRequired]
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    /// <summary>
    /// Get the State for this process.
    /// </summary>
    [JsonIgnore]
    public OrchestrationRunnableBase Runner { get; set; }

    /// <summary>
    /// Gets or sets the input object to process.
    /// </summary>
    /// [SerializationRequired]
    [JsonPropertyName("input")]
    public object BaseInput { get; set; } = new object();

    /// <summary>
    /// Gets or sets the base result of the operation.
    /// </summary>
    /// [SerializationRequired]
    [JsonPropertyName("result")]
    public object? BaseResult { get; set; }

    /// <summary>
    /// Time Execution process has Started
    /// </summary>
    /// [SerializationRequired]
    [JsonPropertyName("startTime")]
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Execution process time.
    /// </summary>
    /// [SerializationRequired]
    [JsonPropertyName("executionTime")]
    public TimeSpan RunnableExecutionTime { get; set; }

    /// <summary>
    /// Token usage for this process
    /// </summary>
    /// [SerializationRequired]
    [JsonPropertyName("tokenUsage")]
    public int TokenUsage { get; set; } = 0;

    /// <summary>
    /// Check if the process is completed.
    /// </summary>
    public bool IsCompleted => BaseResult != null;

    //public object Result { get; set; }
    public RunnableProcess() { }

    [JsonIgnore]
    public List<TornadoAgent> RegisteredAgents { get; set; } = new List<TornadoAgent>();
    /// <summary>
    /// Process had an error during execution.
    /// </summary>
    /// [SerializationRequired]
    [JsonPropertyName("hadError")]
    public bool HadError { get; set; } = false;

    public void RegisterAgent(TornadoAgent agent)
    {
        RegisterAgentMetrics(agent);
    }

    internal void UnregisterAgents()
    {
        UnregisterAgentsMetrics();
    }

    public void SetupProcess()
    {
        RunnableExecutionTime = TimeSpan.Zero;
        StartTime = DateTime.Now;
    }

    public void FinalizeProcess()
    {
        RunnableExecutionTime = DateTime.Now - StartTime!.Value;
        UnregisterAgents();
    }

    private void RegisterAgentMetrics(TornadoAgent agent)
    {
        RegisteredAgents.Add(agent);

        agent.OnAgentRunnerEvent = (agentEvent) =>
        {
            if (agentEvent is AgentRunnerUsageReceivedEvent usageEvent)
            {
                TokenUsage += usageEvent.TokenUsageAmount;
            }

            return Threading.ValueTaskCompleted;
        };
    }

    private void UnregisterAgentsMetrics()
    {
        foreach (var agent in RegisteredAgents)
        {
            agent.OnAgentRunnerEvent = null;
        }
        RegisteredAgents.Clear();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="runner">Runnable instance associated with this process</param>
    /// <param name="inputValue">Input value for the process</param>
    /// <param name="maxReruns">Maximum number of reruns allowed</param>
    public RunnableProcess(OrchestrationRunnableBase runner , object inputValue, int maxReruns = 3)
    {
        Runner = runner;
        BaseInput = inputValue;
        MaxReruns = maxReruns;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="runner">Runnable instance associated with this process</param>
    /// <param name="inputValue">Input value for the process</param>
    /// <param name="maxReruns">Maximum number of reruns allowed</param>
    public RunnableProcess(OrchestrationRunnableBase runner, object inputValue, string id, int maxReruns = 3)
    {
        Runner = runner;
        BaseInput = inputValue;
        MaxReruns = maxReruns;
        Id = id;
    }

    public RunnableProcess(OrchestrationRunnableBase runner, object inputValue, object? resultValue, string id, int maxReruns = 3, int? runsAttempted = null)
    {
        Runner = runner;
        BaseInput = inputValue;
        BaseResult = resultValue;
        MaxReruns = maxReruns;
        Id = id;
        RerunAttempts = runsAttempted ?? RerunAttempts;
    }

    public virtual void Serialize()
    {
        // Implement serialization logic here
    }

    public virtual void Deserialize()
    {
        // Implement deserialization logic here
    }

    /// <summary>
    /// Determines whether another attempt can be made based on the current number of rerun attempts.
    /// </summary>
    /// <returns><see langword="true"/> if the number of rerun attempts is less than the maximum allowed reruns; otherwise,
    /// <see langword="false"/>.</returns>
    public bool CanReAttempt()
    {
        RerunAttempts++;
        return RerunAttempts < MaxReruns;
    }

    public static RunnableProcess CloneProcess(RunnableProcess process, string id = "")
    {
        var clone = process.CloneProcess<object, object>(id);
        return clone;
    }

    public RunnableProcess<TInput, TOutput> CloneProcess<TInput, TOutput>(string id = "")
    {
        RunnableProcess<TInput, TOutput> clone;
        string newId = string.IsNullOrWhiteSpace(id) ? Id : id;

        if (BaseResult is null)
        {
            try
            {
                TInput convertedResult = (TInput)BaseInput;
                clone =  new RunnableProcess<TInput, TOutput>(Runner, convertedResult, newId, MaxReruns);
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException($"Cannot cast BaseInput of type {BaseInput.GetType()} to {typeof(TInput)}");
            }
        }
        else
        {
            clone = new RunnableProcess<TInput, TOutput>(Runner, (TInput)BaseInput, (TOutput?)BaseResult, newId, MaxReruns);
        }

        clone.MemberwiseClone();
        clone.Id = newId;
        clone.RerunAttempts = RerunAttempts;
        clone.TokenUsage = TokenUsage;
        clone.StartTime = StartTime;
        clone.RunnableExecutionTime = RunnableExecutionTime;
        clone.MaxReruns = MaxReruns;
        clone.RegisteredAgents = RegisteredAgents;

        return clone;
    }
}


/// <summary>
/// Represents a process that operates on a specific state with a generic input type.
/// </summary>
/// <remarks>This class extends the <see cref="StateProcess"/> to handle operations with a specific input
/// type. It provides functionality to create state results with the specified type.</remarks>
/// <typeparam name="T">The type of the input and result associated with the state process.</typeparam>
public class RunnableProcess<TInput, TOutput> : RunnableProcess
{
    /// <summary>
    /// Gets or sets the input value of type <typeparamref name="T"/>.
    /// </summary>
    public TInput Input { get => (TInput)BaseInput; set => BaseInput = value!; }

    public TOutput? Result { get => (TOutput?)BaseResult; set => BaseResult = value!; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RunnableProcess{T}"/> class with the specified runnable, input, and
    /// maximum rerun count.
    /// </summary>
    /// <param name="runnable">The orchestration runnable that defines the process logic to be executed.</param>
    /// <param name="input">The input data of type <typeparamref name="T"/> required by the process. Cannot be <see langword="null"/>.</param>
    /// <param name="maxReruns">The maximum number of times the process can be rerun in case of failure. Defaults to 3.</param>
    public RunnableProcess(OrchestrationRunnableBase runnable, TInput input, string id, int maxReruns = 3) : base(runnable, input!, id, maxReruns)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RunnableProcess{T}"/> class with the specified runnable, input,
    /// identifier, and maximum rerun count.
    /// </summary>
    /// <param name="runnable">The orchestration runnable that defines the process logic to be executed.</param>
    /// <param name="input">The input data of type <typeparamref name="TInput"/> required by the process. Cannot be <see langword="null"/>.</param>
    /// <param name="result">The output data type <typeparamref name="TOutput"/> required by the process. Cannot be <see langword="null"/></param>
    /// <param name="id">A unique identifier for the process. Cannot be <see langword="null"/> or empty.</param>
    /// <param name="maxReruns">The maximum number of times the process can be rerun in case of failure. Defaults to 3.</param>
    public RunnableProcess(OrchestrationRunnableBase runnable, TInput input, TOutput? result, string id, int maxReruns = 3, int? rerunCount = null) : base(runnable, input!, result, id, maxReruns, rerunCount)
    {
        Result = result;
    }
}