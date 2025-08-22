using LlmTornado.Agents.Orchestration;
using LlmTornado.Agents.Orchestration.Core;
using LlmTornado.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.ChatRuntime.RuntimeConfigurations
{
    internal class OrchestrationRuntimeConfiguration : AgentOrchestration
    {
        public OrchestrationRuntimeConfiguration()
        {

        }
        
        public override ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            MessageHistory.Add(message);

            //Add custom logic to handle orchestration-specific tasks here if needed

            return new ValueTask<ChatMessage>(MessageHistory.Last());
        }
    }
}
