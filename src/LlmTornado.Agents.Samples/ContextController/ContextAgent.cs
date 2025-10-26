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

    public ContextController contextManager { get; }

    public ContextAgent(TornadoApi api, ContextController controller)
    {
        Api = api;
        Agent = new TornadoAgent(Api, ChatModel.OpenAi.Gpt5.V5Nano);
        contextManager = controller;
    }

    public async Task<Conversation> RunAsync(ChatMessage userMessage)
    {
        AgentContext context = await contextManager.GetAgentContext();


        Console.WriteLine("Selected Model: " + context.Model);
        Console.WriteLine("Instructions: " + context.Instructions);
        Console.WriteLine("Tools: " + string.Join(", ", context.Tools?.Select(t => t.ToolName) ?? []));
        Console.WriteLine("Current Task: " + contextManager.Container.CurrentTask);
        Console.WriteLine("Chat Messages: " + string.Join("\n", context.ChatMessages?.Select(m => $"{m.Role}: {m.GetMessageContent()}") ?? []));

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

        bool hitTokenLimit = false;
        Conversation conv = await Agent.RunAsync(appendMessages: context.ChatMessages, 
            onAgentRunnerEvent: (e) =>
            {
                if(e.EventType == Agents.DataModels.AgentRunnerEventTypes.MaxTokensReached)
                {
                    hitTokenLimit = true;

                }
                return ValueTask.CompletedTask;
            },
            runnerOptions: new Agents.DataModels.TornadoRunnerOptions()
            {
                ThrowOnMaxTurnsExceeded = false,
                ThrowOnTokenLimitExceeded = false,
                TokenLimit = (int)((Agent.Model.ContextTokens ?? 32000)*.08f)
            });

        if(hitTokenLimit)
        {
            contextManager.Container.ChatMessages = conv.Messages.ToList();
            Console.WriteLine("Token limit hit, summarizing conversation to reduce token count.");
            conv = await RunAsync(userMessage);
        }

        return conv;
    }
}
