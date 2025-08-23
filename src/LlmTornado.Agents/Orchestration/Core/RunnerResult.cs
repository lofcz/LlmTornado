namespace LlmTornado.Agents.Orchestration.Core;

/// <summary>
/// Result from a state machine process.
/// </summary>
public class RunnerResult
{
    /// <summary>
    /// Gets or sets the unique identifier for the process.
    /// </summary>
    public string ProcessId { get; set; }
    /// <summary>
    /// Result of the process.
    /// </summary>
    public object ResultObject { get; private set; } = new object();

    //public object Result { get; set; }
    public RunnerResult() { }

    public RunnerResult(string processID, object result)
    {
        ProcessId = processID;
        ResultObject = result;
    }

    internal void SetBaseResult(object? result)
    {
        ResultObject = result;
    }

    /// <summary>
    /// Retrieves the result of the current process as a <see cref="StateResult{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the result expected from the process.</typeparam>
    /// <returns>A <see cref="StateResult{T}"/> containing the process ID and the result cast to the specified type
    /// <typeparamref name="T"/>.</returns>
    public RunnerResult<T> GetResult<T>()
    {
        return new RunnerResult<T>(ProcessId, (T)ResultObject);
    }
}

/// <summary>
/// Represents the result of a process, including the process identifier and a typed result value.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
public class RunnerResult<T> : RunnerResult
{
    /// <summary>
    /// Gets the result of the operation.
    /// </summary>
    public T Result { get => (T)ResultObject; }
    public RunnerResult(string process, T result)
    {
        ProcessId = process;
        SetBaseResult(result);
    }
}