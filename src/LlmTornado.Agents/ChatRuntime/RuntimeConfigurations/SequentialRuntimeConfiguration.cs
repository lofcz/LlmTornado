using LlmTornado.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.ChatRuntime.RuntimeConfigurations
{
    internal class SequentialRuntimeConfiguration : IRuntimeConfiguration
    {
        public CancellationTokenSource cts { get; set; } = new CancellationTokenSource();
        public Conversation? Conversation { get; set; }
        
        public List<TornadoAgent> Agents { get; set; } = new List<TornadoAgent>();

        public SequentialRuntimeConfiguration(TornadoAgent[] agents)
        {
            Agents.AddRange(agents);
        }

        public async ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            foreach (var agent in Agents)
            {
                if (Conversation == null)
                {
                    Conversation = await agent.RunAsync(appendMessages: [message], cancellationToken: cancellationToken);
                }
                else
                {
                    Conversation = await agent.RunAsync(overrideConversationWith:Conversation, appendMessages: [message], cancellationToken: cancellationToken);
                }
            }
            
            return Conversation?.Messages.LastOrDefault() ?? new ChatMessage();
        }

        public List<ChatMessage> GetMessages()
        {
            return Conversation?.Messages.ToList() ?? new List<ChatMessage>();
        }

        public void ClearMessages()
        {
            Conversation?.Clear();
        }
    }
}
