namespace LlmTornado.Agents.ChatRuntime.Orchestration;

/// <summary>
/// Delegate for a transition event that takes an input of type T and returns a boolean indicating whether the transition should occur.
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="input"></param>
/// <returns></returns>
public delegate bool AdvancementRequirement<in T>(T input);

/// <summary>
/// Represents a method that converts an input of type <typeparamref name="TInput"/> to an output of type
/// <typeparamref name="TOutput"/>.
/// </summary>
/// <typeparam name="TInput">The type of the input parameter to the conversion method.</typeparam>
/// <typeparam name="TOutput">The type of the return value from the conversion method.</typeparam>
/// <param name="input"></param>
/// <returns></returns>
public delegate TOutput AdvancementResultConverter<in TInput, out TOutput>(TInput input);

/// <summary>
/// Base class for the State Transition used to define the conditions to transition to the next state in a state machine.
/// </summary>
public class OrchestrationAdvancer
{
    /// <summary>
    /// Gets or sets the next state in the state transition process.
    /// </summary>
    public OrchestrationRunnableBase NextRunnable { get; private set; }

    /// <summary>
    /// Gets or sets the method to be invoked to determine if it can transition.
    /// </summary>
    public Delegate InvokeMethod { get; set; }

    /// <summary>
    /// Gets or sets the method used to convert an input of type <typeparamref name="TInput"/>          to an output
    /// of type <typeparamref name="TOutput"/>.
    /// </summary>
    public Delegate ConverterMethod { get; set; }

    /// <summary>
    /// Gets or sets the result of the converter method.
    /// </summary>
    internal object? ConverterMethodResult { get; set; }

    /// <summary>
    /// Represents the type of the state transition.
    /// </summary>
    public string type = "base";
    public OrchestrationAdvancer(OrchestrationRunnableBase runnable)
    {
        NextRunnable = runnable ?? throw new ArgumentNullException(nameof(runnable), "Next runner cannot be null.");
    }

    public OrchestrationAdvancer(Delegate methodToInvoke, OrchestrationRunnableBase nextRunnable) 
    {
        type = "out";

        // Validate the next state input type against the type of T
        if (!typeof(object).IsAssignableFrom(nextRunnable.GetInputType()))
        {
            throw new InvalidOperationException($"Next State with input type of {nextRunnable.GetInputType()} requires Input type assignable to type of {typeof(object)}");
        }

        InvokeMethod = methodToInvoke;
        NextRunnable = nextRunnable ?? throw new ArgumentNullException(nameof(nextRunnable), "Next runner cannot be null.");
    }

    public OrchestrationAdvancer(Delegate methodToInvoke, Delegate converter, OrchestrationRunnableBase nextRunnable)
    {
        type = "out";

        // Validate the next state input type against the type of T
        if (!typeof(object).IsAssignableFrom(nextRunnable.GetInputType()))
        {
            throw new InvalidOperationException($"Next State with input type of {nextRunnable.GetInputType()} requires Input type assignable to type of {typeof(object)}");
        }

        InvokeMethod = methodToInvoke;
        ConverterMethod = converter;
        NextRunnable = nextRunnable ?? throw new ArgumentNullException(nameof(nextRunnable), "Next runner cannot be null.");
    }
}

/// <summary>
/// State transition class that defines a transition with a method to invoke for evaluation.
/// </summary>
/// <typeparam name="T"> T being the Type of Input for the next State</typeparam>
public class OrchestrationAdvancer<T> : OrchestrationAdvancer
{
    public OrchestrationAdvancer(OrchestrationRunnableBase nextRunnable):base(nextRunnable) 
    {
        type = "out";

        // Validate the next state input type against the type of T
        if (!typeof(T).IsAssignableFrom(nextRunnable.GetInputType()))
        {
            throw new InvalidOperationException($"{NextRunnable.RunnableName} with input type of {nextRunnable.GetInputType()} requires Input type assignable to type of {typeof(T)}");
        }

        InvokeMethod = new AdvancementRequirement<T>((T input) => true);
    }

    public OrchestrationAdvancer(AdvancementRequirement<T> methodToInvoke, OrchestrationRunnableBase nextRunnable) : base(nextRunnable)
    {
        type = "out";

        // Validate the next state input type against the type of T
        if (!typeof(T).IsAssignableFrom(nextRunnable.GetInputType()))
        {
            throw new InvalidOperationException($"Next State with input type of {nextRunnable.GetInputType()} requires Input type assignable to type of {typeof(T)}");
        }

        InvokeMethod = methodToInvoke;
    }

    /// <summary>
    /// Evaluates the specified value using a dynamically invoked method.
    /// </summary>
    /// <remarks>The method uses dynamic invocation to evaluate the result, which may have performance
    /// implications. Ensure that the invoked method is compatible with the expected input type.</remarks>
    /// <param name="value">The value to be evaluated. Can be null.</param>
    /// <returns><see langword="true"/> if the dynamically invoked method returns a non-null and true value; otherwise, <see
    /// langword="false"/>.</returns>
    internal virtual bool CanAdvance(T? value)
    {
        return (bool?)InvokeMethod.DynamicInvoke(value) ?? false;
    }
}

/// <summary>
/// State Transition class that defines a transition with a method to invoke and a conversion method to convert the input type to new output type.
/// </summary>
/// <typeparam name="TInput"> TInput being the Output of the State you wish to convert</typeparam>
/// <typeparam name="TOutput">TOutput being the type you wish to convert to and is input type of the next state</typeparam>
public class OrchestrationAdvancer<TInput, TOutput> : OrchestrationAdvancer<TInput>
{
    /// <summary>
    /// Gets or sets the result of the converter method.
    /// </summary>
    public TOutput ConverterResult
    {
        get
        {
            if (ConverterMethodResult == null)
            {
                throw new InvalidOperationException("Converter method result is not set. Ensure the converter method has been invoked.");
            }
            return (TOutput)ConverterMethodResult;
        }
    }

    public OrchestrationAdvancer(AdvancementRequirement<TInput> methodToInvoke, AdvancementResultConverter<TInput, TOutput> converter, OrchestrationRunnableBase nextRunnable) : base(nextRunnable)
    {
        // Validate the next state input type against the type of TOutput
        if (!typeof(TOutput).IsAssignableFrom(nextRunnable.GetInputType()))
        {
            throw new InvalidOperationException($"Next runner with input type of {nextRunnable.GetInputType()} requires Input type assignable to type of {typeof(TOutput)}");
        }
        type = "in_out";
        ConverterMethod = converter;
        InvokeMethod = methodToInvoke;
    }

    internal override bool CanAdvance(TInput? value)
    {
        try
        {
            if ((bool?)InvokeMethod.DynamicInvoke(value) ?? false)
            {
                ConverterMethodResult = ConverterMethod.DynamicInvoke(value);
                return true;
            }
        }
        catch (Exception ex)
        {

        }

        if (ConverterMethodResult is null)
        {
            throw new ArgumentNullException(nameof(value), "Input cannot be null.");
        }

        return false;
    }
}