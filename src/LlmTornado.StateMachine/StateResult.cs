namespace LlmTornado.StateMachines;

/// <summary>
/// Result from a state machine process.
/// </summary>
public class StateResult
{
    /// <summary>
    /// Gets or sets the unique identifier for the process.
    /// </summary>
    public string ProcessId { get; set; }
    /// <summary>
    /// Result of the process.
    /// </summary>
    public object BaseResult { get; set; } = new object();

    //public object Result { get; set; }
    public StateResult() { }

    public StateResult(string processID, object result)
    {
        ProcessId = processID;
        BaseResult = result;
    }
    /// <summary>
    /// Retrieves the result of the current process as a <see cref="StateResult{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the result expected from the process.</typeparam>
    /// <returns>A <see cref="StateResult{T}"/> containing the process ID and the result cast to the specified type
    /// <typeparamref name="T"/>.</returns>
    public StateResult<T> GetResult<T>()
    {
        return new StateResult<T>(ProcessId, (T)BaseResult);
    }
}

/// <summary>
/// Represents the result of a process, including the process identifier and a typed result value.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
public class StateResult<T> : StateResult
{
    /// <summary>
    /// Gets or sets the result of the operation.
    /// </summary>
    public T Result { get => (T)BaseResult; set => BaseResult = value; }
    public StateResult(string process, T result)
    {
        ProcessId = process;
        Result = result!;
    }
}