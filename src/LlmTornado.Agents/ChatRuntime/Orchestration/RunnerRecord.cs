using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.ChatRuntime.Orchestration
{
    public class AdvancementRecord
    {
        public string AdvancedFrom { get; set; } = "";
        public string AdvancedTo { get; set; } = "";
        public object Output { get; set; }
        public AdvancementRecord(string advancedFrom, string advancedTo, object output)
        {
            AdvancedFrom = advancedFrom;
            AdvancedTo = advancedTo;
            Output = output;
        }
    }

    public class RunnerRecord
    {
        public string ProcessId { get; set; }
        public string RunnerName { get; set; } = "";
        public List<AdvancementRecord> TransitionRecords { get; set; } = new List<AdvancementRecord>();
        public int UsageTokens { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public TimeSpan ExecutionTime { get; set; } = TimeSpan.Zero;

        public object Input { get; set; }


        public RunnerRecord(string processId, string runnerName, int usageTokens, DateTime createdAt, TimeSpan executionTime, AdvancementRecord[]? transitions = null, object? input = null)
        {
            ProcessId = processId;
            RunnerName = runnerName;
            UsageTokens = usageTokens;
            CreatedAt = createdAt;
            ExecutionTime = executionTime;
            Input = input;

            if (transitions != null)
            {
                TransitionRecords.AddRange(transitions);
            }
        }


    }
}
