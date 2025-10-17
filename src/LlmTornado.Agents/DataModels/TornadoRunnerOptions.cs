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
}
