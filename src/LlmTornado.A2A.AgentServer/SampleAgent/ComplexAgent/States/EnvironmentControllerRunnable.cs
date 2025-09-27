using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Responses;
using System.Diagnostics;

namespace LlmTornado.A2A.AgentServer.SampleAgent.ComplexAgent.States
{
    public class EnvironmentControllerRunnable : OrchestrationRunnable<ChatMessage, ChatMessage>
    {
        TornadoAgent Agent;

        OrchestrationRuntimeConfiguration _runtimeConfiguration;

        GitHubTool gitHubTool = new GitHubTool();

        public EnvironmentControllerRunnable(TornadoApi client, OrchestrationRuntimeConfiguration orchestrator, bool streaming = false) : base(orchestrator)
        {
            string instructions = @"
Your Job is to setup the current system  environmental based on the current context. 
If a Git repository is mentioned please clone it, make a new branch, and setup the environment.
If no repository is mentioned please create a new git repository and setup the environment for the project.
Please install any required packages in order to satisfy repository requirements. 
Please Make sure to test you can run and commit to the following project
You are in a docker container sandbox so feel free to explore the environment and do what you need. 
when complete please report back what was setup and how to use the environment eg (conda activate, folders, ect.),
";

            Agent = new TornadoAgent(
                client: client,
                model: ChatModel.OpenAi.Gpt5.V5,
                name: "Agent Runner",
                instructions: instructions,
                streaming: streaming,
                tools: [
                    ComplexAgentTools.ExecuteLinuxCommand,
                    gitHubTool.CloneGithubRepository,
                    gitHubTool.CreateGitRepository,
                    gitHubTool.CreateBranchOfRepo,
                    gitHubTool.LocalCommitAndPushChanges ]);

            Agent.ResponseOptions = new ResponseRequest() { Tools = [new ResponseWebSearchTool()] };

            _runtimeConfiguration = orchestrator;
        }

        public override async ValueTask<ChatMessage> Invoke(RunnableProcess<ChatMessage, ChatMessage> process)
        {
            process.RegisterAgent(Agent);
            return (await RunAgent()).Messages.Last();
        }

        public async Task<Conversation> RunAgent()
        {
            List<ChatMessage> messages = _runtimeConfiguration.GetMessages(); //Includes latest user message
            return await Agent.RunAsync(appendMessages: messages, maxTurns: 50, onAgentRunnerEvent: MonitorToolEvents);
        }

        private ValueTask MonitorToolEvents(AgentRunnerEvents agentEvent)
        {
            if (agentEvent.EventType == AgentRunnerEventTypes.ToolInvoked)
            {
                if (agentEvent is AgentRunnerToolInvokedEvent toolInvoked)
                {
                    Debug.WriteLine($"Tool Invoked: {toolInvoked.ToolCalled.Name} with input: {toolInvoked.ToolCalled.Arguments}");
                    Console.WriteLine($"Tool Invoked: {toolInvoked.ToolCalled.Name} with input: {toolInvoked.ToolCalled.Arguments}");
                }
            }
            else if(agentEvent.EventType == AgentRunnerEventTypes.ToolCompleted)
            {
                if (agentEvent is AgentRunnerToolCompletedEvent toolCompleted)
                {
                    Debug.WriteLine($"Tool Completed: {toolCompleted.ToolResult.Name} with output: {toolCompleted.ToolResult.Content}");
                    Console.WriteLine($"Tool Completed: {toolCompleted.ToolResult.Name} with output: {toolCompleted.ToolResult.Content}");
                }
            }
                return ValueTask.CompletedTask;
        }
    
    }
}
