// See https://aka.ms/new-console-template for more information
using LlmTornado.Agents.Samples.ContextController;
using LlmTornado.Code;
using LlmTornado;
using LlmTornado.Chat.Models;
using LlmTornado.Common;
using LlmTornado.Chat;
using System.ComponentModel;
using LlmTornado.Agents.DataModels;
using LlmTornado.Mcp;

var api = new TornadoApi([
                new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")),
                new ProviderAuthentication(LLmProviders.Anthropic, Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"))
                ]);

ContextContainer contextContainer = new ContextContainer();

ToolContextService toolContextService = new ToolContextService(api, contextContainer);

# region Tool Definitions
// Mock tools with mock results for performance and testing
var mcpFileSystemServer = MCPToolkits.FileSystemToolkit("C:\\Users\\johnl\\source\\repos\\Johnny2x2\\LlmTornado\\src",
    [
    "write_file",
    "read_multiple_files",
    "read_file",
    "list_directory",
    "directory_tree"]);

await mcpFileSystemServer.InitializeAsync();

var readFileTool = mcpFileSystemServer.AllowedTornadoTools.Find(t => t.ToolName == "read_file" || t.Function.Name == "read_file");
toolContextService.AddToolToLibrary(readFileTool.ToolName ?? readFileTool.Function.Name, readFileTool, readFileTool.ToolDescription);

var readMultipleFilesTool = mcpFileSystemServer.AllowedTornadoTools.Find(t => t.ToolName == "read_multiple_files" || t.Function.Name == "read_multiple_files");
toolContextService.AddToolToLibrary(readMultipleFilesTool.ToolName ?? readMultipleFilesTool.Function.Name, readMultipleFilesTool, readMultipleFilesTool.ToolDescription);

var listDirectoryTool = mcpFileSystemServer.AllowedTornadoTools.Find(t => t.ToolName == "list_directory" || t.Function.Name == "list_directory");
toolContextService.AddToolToLibrary(listDirectoryTool.ToolName ?? listDirectoryTool.Function.Name, listDirectoryTool, listDirectoryTool.ToolDescription);

var directoryTreeTool = mcpFileSystemServer.AllowedTornadoTools.Find(t => t.ToolName == "directory_tree" || t.Function.Name == "directory_tree");
toolContextService.AddToolToLibrary(directoryTreeTool.ToolName ?? directoryTreeTool.Function.Name, directoryTreeTool, directoryTreeTool.ToolDescription);

var writeFileTool = mcpFileSystemServer.AllowedTornadoTools.Find(t => t.ToolName == "write_file" || t.Function.Name == "write_file");
toolContextService.AddToolToLibrary(writeFileTool.ToolName ?? writeFileTool.Function.Name, writeFileTool, writeFileTool.ToolDescription);

//var searchFilesTool = mcpFileSystemServer.AllowedTornadoTools.Find(t => t.ToolName == "search_files" || t.Function.Name == "search_files");
//toolContextService.AddToolToLibrary(searchFilesTool.ToolName ?? searchFilesTool.Function.Name, searchFilesTool, searchFilesTool.ToolDescription);

//var listAllowedDirectoriesTool = mcpFileSystemServer.AllowedTornadoTools.Find(t => t.ToolName == "list_allowed_directories" || t.Function.Name == "list_allowed_directories");
//toolContextService.AddToolToLibrary(listAllowedDirectoriesTool.ToolName ?? listAllowedDirectoriesTool.Function.Name, listAllowedDirectoriesTool, listAllowedDirectoriesTool.ToolDescription);
#endregion

TaskContextService taskContextService = new TaskContextService(api, contextContainer);
ModelContextService modelContextService = new ModelContextService(api, contextContainer);

modelContextService.AddModelToLibrary("expensive", ChatModel.OpenAi.Gpt5.V5Pro, "Best for general purpose tasks with high accuracy.");
modelContextService.AddModelToLibrary("cheap", ChatModel.OpenAi.Gpt5.V5Nano, "Good for less complex tasks where cost is a concern.");
modelContextService.AddModelToLibrary("ethical", ChatModel.Anthropic.Claude45.Sonnet250929, "Useful for tasks requiring strong safety and ethical considerations.");
modelContextService.AddModelToLibrary("thinking", ChatModel.OpenAi.O4.V4MiniDeepResearch, "Well-rounded and powerful model across domains. It sets a new standard for math, science, coding, and visual reasoning tasks. It also excels at technical writing and instruction-following. Use it to think through multi-step problems that involve analysis across text, code, and images");

InstructionContextService instructionsContextService = new InstructionContextService(api, contextContainer);
MessageContextService messageContextService = new MessageContextService(api, contextContainer);

ContextController contextManager = new ContextController(
    taskContextService,
    contextContainer,
    instructionsContextService,
    toolContextService,
    modelContextService,
    messageContextService
);

//AgentContext context = await contextManager.GetAgentContext();


ContextAgent agent = new ContextAgent(api, contextManager);
string userPrompt = "do a deep research on LLMTornado located on my local drive and create a detailed readme on how to use it with all the most interesting features";
Console.WriteLine("User: ");
Console.Write(userPrompt);
Console.WriteLine("Agent is thinking...");
var response = await agent.RunAsync(new ChatMessage(ChatMessageRoles.User, userPrompt), (e) =>
    {
        if (e.EventType == AgentRunnerEventTypes.Streaming)
        {
            if (e is AgentRunnerStreamingEvent streamingEvent)
            {
                if (streamingEvent.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                {
                    Console.Write(deltaTextEvent.DeltaText); // Write the text delta directly
                }
            }
        }
        return ValueTask.CompletedTask;
    });

while (true)
{
    Console.WriteLine("User: ");
    userPrompt = Console.ReadLine() ?? "";
    if(userPrompt.ToLower() == "exit" || userPrompt.ToLower() == "quit")
    {
        break;
    }
    Console.WriteLine("Agent is thinking...");
    response = await agent.RunAsync(new ChatMessage(ChatMessageRoles.User,userPrompt), (e) =>
    {
        if(e.EventType == AgentRunnerEventTypes.Streaming)
        {
            if(e is AgentRunnerStreamingEvent streamingEvent)
            {
                if(streamingEvent.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                {
                    Console.Write(deltaTextEvent.DeltaText); // Write the text delta directly
                }
            }
        }
        return ValueTask.CompletedTask;
    });
}