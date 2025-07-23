
using LlmTornado.Responses;

namespace LlmTornado.Agents
{
    /// <summary>
    /// Options to modify the response setup
    /// </summary>
    public class ModelResponseOptions
    {
        /// <summary>
        /// Previous Response ID for response API only
        /// </summary>
        public string? PreviousResponseId {  get; set; }

        /// <summary>
        /// Model being used
        /// </summary>
        public string Model { get; set; }

        /// <summary>
        /// Instructions for the model to run
        /// </summary>
        public string Instructions { get; set; }

        /// <summary>
        /// Tools to use during the run
        /// (required uniter for response tool and function tool)
        /// </summary>
        public List<BaseTool> Tools { get; set; } = new List<BaseTool>();

        /// <summary>
        /// Structured Output of type being used
        /// </summary>
        public ModelOutputFormat OutputFormat { get; set; }

        /// <summary>
        /// Reasoning Options
        /// </summary>
        public ReasoningConfiguration ReasoningOptions { get; set; }

        public ModelResponseOptions() { }
    }
}
