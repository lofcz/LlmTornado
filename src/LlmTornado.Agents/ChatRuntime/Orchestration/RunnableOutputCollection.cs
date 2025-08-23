using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.ChatRuntime.Orchestration
{
    /// <summary>
    /// Represents a collection of output results from a run, along with an index indicating the position of the input array.
    /// </summary>
    /// <remarks>This class is used to store and manage the results of a run operation, providing both the
    /// results and the index of the run. It can be used to track multiple runs and their respective outputs.</remarks>
    /// <typeparam name="TOutput">The type of the output results contained in the collection.</typeparam>
    public class RunnableOutputCollection<TOutput>
    {
        /// <summary>
        /// Index of the input array this result corresponds to.
        /// </summary>
        public int Index { get; set; } = 0;

        /// <summary>
        /// Results from the run operation.
        /// </summary>
        public List<TOutput> Results { get; set; }

        public RunnableOutputCollection() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index">Index of the runnable</param>
        /// <param name="results">Results from the run operation</param>
        public RunnableOutputCollection(int index, List<TOutput> results)
        {
            Index = index;
            Results = results;
        }
    }
}
