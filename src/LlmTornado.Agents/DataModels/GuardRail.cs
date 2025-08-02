namespace LlmTornado.Agents
{
    /// <summary>
    /// Helper Delegate to define Input output requirements of the function
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public delegate Task<GuardRailFunctionOutput> GuardRailFunction(string? input = "");

    /// <summary>
    /// Used to check the input or output of a message to see if it meets certain criteria.
    /// Triggers the runner to stop processing if the criteria are not met and tripwire is triggered.
    /// </summary>
    public class GuardRailFunctionOutput
    {
        /// <summary>
        /// Summary of why the guardrail was triggered
        /// </summary>
        public string OutputInfo { get; set; }

        /// <summary>
        /// tripwire is triggered on failed Input context
        /// </summary>
        public bool TripwireTriggered { get; set; }

        public GuardRailFunctionOutput(string outputInfo = "", bool tripwireTriggered = false)
        {
            OutputInfo = outputInfo;
            TripwireTriggered = tripwireTriggered;
        }
    }

    /// <summary>
    /// Custom Exception for Unit Testing
    /// </summary>
    public class GuardRailTriggerException : Exception
    {
        public GuardRailTriggerException(string message): base(message) { }
    }
}
