using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
