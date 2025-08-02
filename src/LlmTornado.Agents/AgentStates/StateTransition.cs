namespace LlmTornado.Agents.AgentStates
{
    /// <summary>
    /// Delegate for a transition event that takes an input of type T and returns a boolean indicating whether the transition should occur.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="input"></param>
    /// <returns></returns>
    public delegate bool TransitionEvent<T>(T input);

    /// <summary>
    /// Represents a method that converts an input of type <typeparamref name="TInput"/> to an output of type
    /// <typeparamref name="TOutput"/>.
    /// </summary>
    /// <typeparam name="TInput">The type of the input parameter to the conversion method.</typeparam>
    /// <typeparam name="TOutput">The type of the return value from the conversion method.</typeparam>
    /// <param name="input"></param>
    /// <returns></returns>
    public delegate TOutput ConversionMethod<TInput, TOutput>(TInput input);

    /// <summary>
    /// Base class for the State Transition used to define the conditions to transition to the next state in a state machine.
    /// </summary>
    public class StateTransition
    {
        /// <summary>
        /// Gets or sets the next state in the state transition process.
        /// </summary>
        public BaseState NextState { get; set; }

        /// <summary>
        /// Gets or sets the result of the converter method.
        /// </summary>
        public object? _ConverterMethodResult { get => _converterMethodResult; set => _converterMethodResult = value; }

        /// <summary>
        /// Represents the type of the state transition.
        /// </summary>
        public string type = "base";
        private object? _converterMethodResult;
    }

    /// <summary>
    /// State transition class that defines a transition with a method to invoke for evaluation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class StateTransition<T> : StateTransition
    {
        /// <summary>
        /// Gets or sets the method to be invoked to determine if it can transition.
        /// </summary>
        public TransitionEvent<T> InvokeMethod { get; set; }
        public StateTransition() { }
        public StateTransition(TransitionEvent<T> methodToInvoke, BaseState nextState)
        {
            type = "out";
            // Validate the next state input type against the type of T
            if (nextState.GetInputType().IsAssignableTo(typeof(T)) || typeof(T).IsSubclassOf(nextState.GetInputType()))
            {
                this.NextState = nextState;
                this.InvokeMethod = methodToInvoke;
            }
            else
            {
                throw new InvalidOperationException($"Next State with input type of {nextState.GetInputType()} requires Input type assignable to type of {typeof(T)}");
            }

        }

        /// <summary>
        /// Evaluates the specified result using a dynamically invoked method.
        /// </summary>
        /// <remarks>The method uses dynamic invocation to evaluate the result, which may have performance
        /// implications. Ensure that the invoked method is compatible with the expected input type.</remarks>
        /// <param name="result">The result to be evaluated. Can be null.</param>
        /// <returns><see langword="true"/> if the dynamically invoked method returns a non-null and true value; otherwise, <see
        /// langword="false"/>.</returns>
        public virtual bool Evaluate(T? result)
        {
            return (bool?)InvokeMethod.DynamicInvoke(result) ?? false;
        }
    }

    /// <summary>
    /// State Transition class that defines a transition with a method to invoke and a conversion method to convert the input type to new output type.
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TOutput"></typeparam>
    public class StateTransition<TInput, TOutput> : StateTransition<TInput>
    {
        /// <summary>
        /// Gets or sets the method used to convert an input of type <typeparamref name="TInput"/>          to an output
        /// of type <typeparamref name="TOutput"/>.
        /// </summary>
        public ConversionMethod<TInput, TOutput> ConverterMethod { get; set; }

        /// <summary>
        /// Gets or sets the result of the converter method.
        /// </summary>
        public TOutput ConverterMethodResult
        {
            get
            {
                if (ConverterMethodResult == null)
                {
                    throw new InvalidOperationException("Converter method result is not set. Ensure the converter method has been invoked.");
                }
                return (TOutput)_ConverterMethodResult;
            }
            set => ConverterMethodResult = value;
        }

        public StateTransition(TransitionEvent<TInput> methodToInvoke, ConversionMethod<TInput, TOutput> converter, BaseState nextState)
        {
            // Validate the next state input type against the type of TOutput
            if (nextState.GetInputType().IsAssignableTo(typeof(TOutput)) || typeof(TOutput).IsSubclassOf(nextState.GetInputType()))
            {
                type = "in_out";
                this.ConverterMethod = converter;
                this.NextState = nextState;
                this.InvokeMethod = methodToInvoke;
            }
            else
            {
                throw new InvalidOperationException($"Next State with input type of {nextState.GetInputType()} requires Input type assignable to type of {typeof(TOutput)}");
            }
        }

        public override bool Evaluate(TInput? result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result), "Input cannot be null.");
            }
            
            if((bool?)InvokeMethod.DynamicInvoke(result) ?? false)
            {
                _ConverterMethodResult = ConverterMethod.Invoke(result);
                return true;
            }

            return false;
        }
    }
}
