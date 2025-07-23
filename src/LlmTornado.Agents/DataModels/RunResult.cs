namespace LlmTornado.Agents 
{ 
    /// <summary>
    /// Result of the Runner Agent loop
    /// </summary>
    public class RunResult
    {
        /// <summary>
        /// Messages from the run
        /// </summary>
        private List<ModelItem> messages = new List<ModelItem>();

        /// <summary>
        /// Check if a guardrail was triggered during run
        /// </summary>
        private bool guardrailTriggered = false;

        /// <summary>
        /// Last response from the run
        /// </summary>
        public ModelResponse Response { get; set; } = new ModelResponse();

        /// <summary>
        /// Messages generated from the run including response output
        /// </summary>
        public List<ModelItem> Messages { get => messages; set => messages = value; }

        /// <summary>
        /// Check if a guardrail was triggered during run
        /// </summary>
        public bool GuardrailTriggered { get => guardrailTriggered; set => guardrailTriggered = value; }

        /// <summary>
        /// Text from the last message in the response
        /// </summary>
        public string? Text => TryGetText();

        /// <summary>
        /// Attempt to get Text if the last message is a text type
        /// </summary>
        /// <returns></returns>
        private string? TryGetText()
        {
            try
            {
                return ((ModelMessageItem?)Response.OutputItems?.LastOrDefault())?.Text ?? string.Empty;
            }
            catch
            {
                return null;
            }
        }

        public RunResult() { }
    }

}
