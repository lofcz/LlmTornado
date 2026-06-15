namespace LlmTornado.Agents.DataModels;

public class TornadoRunnerOptions
{
    /// <summary>
    /// Throw an exception if the maximum number of turns is exceeded
    /// </summary>
    public bool ThrowOnMaxTurnsExceeded { get; set; } = false;

    /// <summary>
    /// Stop processing if the token limit is exceeded from context accumulation
    /// </summary>
    public int TokenLimit { get; set; } = 2000000;

    /// <summary>
    /// Gets or sets a value indicating whether an exception should be thrown when the token limit is exceeded.
    /// </summary>
    public bool ThrowOnTokenLimitExceeded { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether an exception should be thrown when the operation is canceled.
    /// </summary>
    public bool ThrowOnCancelled { get; set; } = false;

    /// <summary>
    /// Set the system message at the start of the conversation instead of the end. 
    /// </summary>
    public bool SystemMessageAtStart { get; set; } = false;

    /// <summary>
    /// Throw error on API expection or just log and continue. Default is false to avoid stopping the agent run due to transient API errors.
    /// </summary>
    public bool ThrowOnResponseError { get; set; } = false;

    /// <summary>
    /// Throw error on internal error during request processing or just log and continue. Default is false to avoid stopping the agent run due to transient errors.
    /// </summary>
    public bool ThrowOnRequestError { get; set; } = false;

    /// <summary>
    /// Throw error on tool failing or just log and continue. Default is false to avoid stopping the agent run due to transient tool errors.
    /// </summary>
    public bool ThrowOnToolError { get; set; } = false;
}
