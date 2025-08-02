namespace LlmTornado.StateMachines
{
    /// <summary>
    /// Represents a process that manages the execution state and input for a state-based operation.
    /// </summary>
    /// <remarks>The <see cref="StateProcess"/> class provides functionality to manage the state of an
    /// operation, including the ability to rerun the operation a specified number of times. It also allows for the
    /// creation of state results and the conversion to a typed state process.</remarks>
    public class StateProcess
    {
        private object input = new();
        /// <summary>
        /// Max alled reruns for this process.
        /// </summary>
        public int MaxReruns { get; set; } = 3;
        /// <summary>
        /// Current rerun attempts for this process.
        /// </summary>
        private int rerunAttempts { get; set; } = 0;
        /// <summary>
        /// Gets or sets the unique identifier for the entity.
        /// </summary>
        public string ID { get; set; } = Guid.NewGuid().ToString();
        /// <summary>
        /// Get the State for this process.
        /// </summary>
        public BaseState State { get; set; }

        /// <summary>
        /// Gets or sets the input object to process.
        /// </summary>
        public object _Input { get => input; set => input = value; }
        //public object Result { get; set; }
        public StateProcess() { }

        public StateProcess(BaseState state, object input, int maxReruns = 3)
        {
            State = state;
            _Input = input;
            MaxReruns = maxReruns;
        }

        /// <summary>
        /// Determines whether another attempt can be made based on the current number of rerun attempts.
        /// </summary>
        /// <returns><see langword="true"/> if the number of rerun attempts is less than the maximum allowed reruns; otherwise,
        /// <see langword="false"/>.</returns>
        public bool CanReAttempt()
        {
            rerunAttempts++;
            return rerunAttempts < MaxReruns;
        }

        /// <summary>
        /// Creates a new <see cref="StateResult"/> instance associated with the current state.
        /// </summary>
        /// <param name="result">The result object to be encapsulated within the <see cref="StateResult"/>.</param>
        /// <returns>A <see cref="StateResult"/> containing the specified result object and the current state's identifier.</returns>
        public StateResult CreateStateResult(object result)
        {
            return new StateResult(ID, result);
        }

        /// <summary>
        /// Retrieves a new instance of <see cref="StateProcess{T}"/> initialized with the current state and input.
        /// </summary>
        /// <remarks>Used for Rerun generation</remarks>
        /// <typeparam name="T">The type of the input used to initialize the process.</typeparam>
        /// <returns>A <see cref="StateProcess{T}"/> object initialized with the current state and input of type <typeparamref
        /// name="T"/>.</returns>
        public StateProcess<T> GetProcess<T>()
        {
            return new StateProcess<T>(State, (T)input, ID);
        }
    }

    /// <summary>
    /// Represents a process that operates on a specific state with a generic input type.
    /// </summary>
    /// <remarks>This class extends the <see cref="StateProcess"/> to handle operations with a specific input
    /// type. It provides functionality to create state results with the specified type.</remarks>
    /// <typeparam name="T">The type of the input and result associated with the state process.</typeparam>
    public class StateProcess<T> : StateProcess
    {
        /// <summary>
        /// Gets or sets the input value of type <typeparamref name="T"/>.
        /// </summary>
        public T Input { get => (T)_Input; set => _Input = (object)value!; }

        public StateProcess(BaseState state, T input, int maxReruns = 3) : base(state, (object?)input!, maxReruns)
        {
            Input = input!;
        }

        public StateProcess(BaseState state, T input, string id, int maxReruns = 3) : base(state, (object?)input!, maxReruns)
        {
            Input = input!;
            ID = id;
        }

        /// <summary>
        /// Creates a new <see cref="StateResult{T}"/> instance with the specified result.
        /// </summary>
        /// <param name="result">The result value to be encapsulated within the <see cref="StateResult{T}"/>.</param>
        /// <returns>A <see cref="StateResult{T}"/> containing the specified result and the current state ID.</returns>
        public StateResult<T> CreateStateResult(T result)
        {
            return new StateResult<T>(ID, result);
        }
    }
}
