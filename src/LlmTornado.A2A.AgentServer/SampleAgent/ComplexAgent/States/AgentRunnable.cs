using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Moderation;
using LlmTornado.Responses;

namespace LlmTornado.A2A.AgentServer.SampleAgent.States;

public class AgentRunnable : OrchestrationRunnable<ChatMessage, ChatMessage>
{
    TornadoAgent Agent;
    public Action<AgentRunnerEvents>? OnAgentRunnerEvent { get; set; }

    Conversation _conv;
    OrchestrationRuntimeConfiguration _runtimeConfiguration;
    public AgentRunnable(TornadoApi client, OrchestrationRuntimeConfiguration orchestrator, bool streaming = false) : base(orchestrator)
    {
        string instructions = @"You are a conversational chatbot, be engaging and creative to have a playful and interesting conversation with the user.
Given the following context will include Vector Search Memory, Websearch Results, and Entity Memory to keep track of real world things.";

        Agent = new TornadoAgent(
            client: client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Agent Runner",
            instructions: instructions,
            streaming: streaming);

        _conv = Agent.Client.Chat.CreateConversation(Agent.Options);

        Agent.ResponseOptions = new ResponseRequest() { Tools = [new ResponseWebSearchTool(), new ResponseLocalShellTool()] };
        _runtimeConfiguration = orchestrator;
    }


    public override async ValueTask<ChatMessage> Invoke(RunnableProcess<ChatMessage, ChatMessage> process)
    {
        process.RegisterAgent(Agent);

        _conv = await RunAgent();

        return _conv.Messages.Last();
    }

    public async Task<Conversation> RunAgent()
    {
        List<ChatMessage> messages = _runtimeConfiguration.GetMessages(); //Includes latest user message
        _conv = await Agent.RunAsync(appendMessages: messages);
        return _conv; 
    }
}