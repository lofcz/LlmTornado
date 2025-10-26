using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.VectorDatabases.Faiss.Integrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Samples.ContextController;

public class ContextAgent
{
    public TornadoAgent Agent { get; set; }

    public TornadoApi Api { get; set; }

    public ContextContainer ContextContainer { get; }

    public ContextController contextManager { get; }

    public MessageMetadataStore MetadataStore { get; set; } = new MessageMetadataStore();

    public ContextAgent(TornadoApi api)
    {
        Agent = new TornadoAgent(api, ChatModel.OpenAi.Gpt5.V5Nano);

        Api = api;

        ContextContainer = new ContextContainer();
        ContextContainer.CurrentModel = ChatModel.OpenAi.Gpt5.V5Nano;

        MessageContextService messageContextService = new MessageContextService(api, ContextContainer);
        ToolContextService toolContextService = new ToolContextService(api, ContextContainer);
        TaskContextService taskContextService = new TaskContextService(api, ContextContainer);
        ModelContextService modelContextService = new ModelContextService(api, ContextContainer);
        InstructionContextService instructionsContextService = new InstructionContextService(api, ContextContainer);

        contextManager = new ContextController(
            taskContextService,
            ContextContainer,
            instructionsContextService,
            toolContextService,
            modelContextService,
            messageContextService
        );
    }

    public async Task<Conversation> RunAsync(ChatMessage userMessage)
    {
        AgentContext context = await contextManager.GetAgentContext();
        //Model selection
        Agent.Model = context.Model ?? ChatModel.OpenAi.Gpt5.V5Nano;
        //Instructions
        Agent.Instructions = context.Instructions ?? "You are a helpful AI assistant.";
        //Tools
        foreach (var tool in context.Tools ?? [])
        {
            if (tool is null) continue;
            if(tool.RemoteTool is not null)
            {
                Agent.AddMcpTools([tool]);
            }
            else
            {
                Agent.AddTornadoTool(tool);
            }
        }

        Conversation conv = await Agent.RunAsync(appendMessages: context.ChatMessages, runnerOptions: new Agents.DataModels.TornadoRunnerOptions()
        {
            ThrowOnMaxTurnsExceeded = false,
            ThrowOnTokenLimitExceeded = false,
            TokenLimit = 180000
        });

        return conv;
    }
}
