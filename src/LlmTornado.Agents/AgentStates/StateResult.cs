namespace LlmTornado.Agents.AgentStates
{
    /// <summary>
    /// Result from a state machine process.
    /// </summary>
    public class StateResult
    {
        private object result = new();
        /// <summary>
        /// Gets or sets the unique identifier for the process.
        /// </summary>
        public string ProcessID { get; set; }
        /// <summary>
        /// Result of the process.
        /// </summary>
        public object _Result { get => result; set => result = value; }
        //public object Result { get; set; }
        public StateResult() { }

        public StateResult(string processID, object result)
        {
            ProcessID = processID;
            _Result = result;
        }
        /// <summary>
        /// Retrieves the result of the current process as a <see cref="StateResult{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the result expected from the process.</typeparam>
        /// <returns>A <see cref="StateResult{T}"/> containing the process ID and the result cast to the specified type
        /// <typeparamref name="T"/>.</returns>
        public StateResult<T> GetResult<T>()
        {
            return new StateResult<T>(ProcessID, (T)_Result);
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
        public T Result { get => (T)_Result; set => _Result = value; }
        public StateResult(string process, T result)
        {
            ProcessID = process;
            Result = result!;
        }
    }
}
