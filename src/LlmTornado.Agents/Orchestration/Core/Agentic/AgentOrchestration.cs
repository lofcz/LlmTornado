using LlmTornado.Agents.Orchestration.Core;
using LlmTornado.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Orchestration;

public interface IAgentOrchestrator: IOrchestrator
{
    public  List<ChatMessage> GetMessages();
}

public class AgentOrchestration : Core.Orchestration<ChatMessage, ChatMessage>, IAgentOrchestrator
{
    public virtual List<ChatMessage> GetMessages()
    {
        return new List<ChatMessage>();
    }
}
