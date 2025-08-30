using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Demo.ExampleAgents.CSCodingAgent;
using LlmTornado.Responses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LlmTornado.Demo.ExampleAgents.MagenticOneAgent.MagenticOneConfiguration;

namespace LlmTornado.Demo.ExampleAgents.MagenticOneAgent;

#region Data Models
public struct TaskRequest
{
    public string Task { get; set; }
    public string Context { get; set; }
    public TaskRequest(string task, string context = "")
    {
        Task = task;
        Context = context;
    }
}

public struct TaskPlan
{
    public string OriginalTask { get; set; }
    public string[] RequiredAgents { get; set; }
    public string ExecutionPlan { get; set; }
    public string[] StepDescriptions { get; set; }
    public TaskPlan(string originalTask, string[] requiredAgents, string executionPlan, string[] stepDescriptions)
    {
        OriginalTask = originalTask;
        RequiredAgents = requiredAgents;
        ExecutionPlan = executionPlan;
        StepDescriptions = stepDescriptions;
    }
}

public struct AgentAction
{
    public string AgentType { get; set; }
    public string Action { get; set; }
    public string Reasoning { get; set; }
    public string Parameters { get; set; }
    public AgentAction(string agentType, string action, string reasoning, string parameters = "")
    {
        AgentType = agentType;
        Action = action;
        Reasoning = reasoning;
        Parameters = parameters;
    }
}

public struct WebSearchResult
{
    public string Query { get; set; }
    public string Results { get; set; }
    public string Summary { get; set; }
    public WebSearchResult(string query, string results, string summary)
    {
        Query = query;
        Results = results;
        Summary = summary;
    }
}

public struct FileOperationResult
{
    public string Operation { get; set; }
    public string FilePath { get; set; }
    public string Content { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public FileOperationResult(string operation, string filePath, string content, bool success, string errorMessage = "")
    {
        Operation = operation;
        FilePath = filePath;
        Content = content;
        Success = success;
        ErrorMessage = errorMessage;
    }
}

public struct CodeExecutionResult
{
    public string Code { get; set; }
    public string Language { get; set; }
    public string Output { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
    public CodeExecutionResult(string code, string language, string output, bool success, string errorMessage = "")
    {
        Code = code;
        Language = language;
        Output = output;
        Success = success;
        ErrorMessage = errorMessage;
    }
}

public struct TerminalCommandResult
{
    public string Command { get; set; }
    public string Output { get; set; }
    public int ExitCode { get; set; }
    public bool Success { get; set; }
    public TerminalCommandResult(string command, string output, int exitCode, bool success)
    {
        Command = command;
        Output = output;
        ExitCode = exitCode;
        Success = success;
    }
}

public struct AgentExecutionResults
{
    public string OriginalTask { get; set; }
    public string WebSearchResults { get; set; }
    public string FileOperationResults { get; set; }
    public string CodeExecutionResults { get; set; }
    public string TerminalResults { get; set; }
    public string[] ActionsPerformed { get; set; }
    public AgentExecutionResults(string originalTask, string webResults, string fileResults, string codeResults, string terminalResults, string[] actionsPerformed)
    {
        OriginalTask = originalTask;
        WebSearchResults = webResults;
        FileOperationResults = fileResults;
        CodeExecutionResults = codeResults;
        TerminalResults = terminalResults;
        ActionsPerformed = actionsPerformed;
    }
}

public struct MagenticOneResult
{
    public string TaskResult { get; set; }
    public string Summary { get; set; }
    public string[] ActionsPerformed { get; set; }
    public MagenticOneResult(string taskResult, string summary, string[] actionsPerformed)
    {
        TaskResult = taskResult;
        Summary = summary;
        ActionsPerformed = actionsPerformed;
    }

    public override string ToString()
    {
        return $@"
Task Result:
{TaskResult}

Summary:
{Summary}

Actions Performed:
{string.Join("\n", ActionsPerformed)}
";
    }
}
#endregion

public static class MagenticOneTools
{
    // Tool methods for FileSurfer
    [Description("Read the contents of a file")]
    public static string ReadFileTool([Description("Path to the file to read")] string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return $"File not found: {filePath}";
            
            return File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            return $"Error reading file: {ex.Message}";
        }
    }

    [Description("Write content to a file")]
    public static string WriteFileTool([Description("Path to the file to write")] string filePath, 
                                      [Description("Content to write to the file")] string content)
    {
        try
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            File.WriteAllText(filePath, content);
            return $"Successfully wrote to file: {filePath}";
        }
        catch (Exception ex)
        {
            return $"Error writing file: {ex.Message}";
        }
    }

    [Description("List contents of a directory")]
    public static string ListDirectoryTool([Description("Path to the directory to list")] string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
                return $"Directory not found: {directoryPath}";
            
            var files = Directory.GetFiles(directoryPath);
            var directories = Directory.GetDirectories(directoryPath);
            
            StringBuilder result = new StringBuilder();
            result.AppendLine($"Contents of {directoryPath}:");
            result.AppendLine("Directories:");
            foreach (var dir in directories)
                result.AppendLine($"  {Path.GetFileName(dir)}/");
            result.AppendLine("Files:");
            foreach (var file in files)
                result.AppendLine($"  {Path.GetFileName(file)}");
            
            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"Error listing directory: {ex.Message}";
        }
    }

    // Tool methods for Coder
    [Description("Execute code and return the result")]
    public static string ExecuteCodeTool([Description("Code to execute")] string code, 
                                        [Description("Programming language")] string language = "csharp")
    {
        try
        {
            return $"Code execution simulated for {language}:\n{code}\n\nOutput: [Simulated execution completed successfully]";
        }
        catch (Exception ex)
        {
            return $"Error executing code: {ex.Message}";
        }
    }

    // Tool methods for Terminal
    [Description("Execute a command in the terminal")]
    public static string ExecuteCommandTool([Description("Command to execute")] string command)
    {
        try
        {
            if (command.StartsWith("echo "))
            {
                return command.Substring(5);
            }
            else if (command == "pwd")
            {
                return Directory.GetCurrentDirectory();
            }
            else if (command == "ls" || command == "dir")
            {
                var files = Directory.GetFiles(Directory.GetCurrentDirectory());
                var dirs = Directory.GetDirectories(Directory.GetCurrentDirectory());
                return string.Join("\n", dirs.Concat(files).Select(Path.GetFileName));
            }
            else
            {
                return $"Command '{command}' executed (simulated for security)";
            }
        }
        catch (Exception ex)
        {
            return $"Error executing command: {ex.Message}";
        }
    }
}

public class MagenticOneConfiguration : OrchestrationRuntimeConfiguration
{
    PlanningRunnable planner;
    WebSurferRunnable webSurfer;
    FileSurferRunnable fileSurfer;
    CoderRunnable coder;
    TerminalRunnable terminal;
    OrchestratorRunnable orchestrator;
    DirectOrchestratorRunnable directOrchestrator;
    ExitRunnable exit;

    public MagenticOneConfiguration() 
    {
        TornadoApi client = new TornadoApi(Environment.GetEnvironmentVariable("OPENAI_API_KEY"), LLmProviders.OpenAi);
        RecordSteps = true;

        // Create all the specialized runnables
        planner = new PlanningRunnable(client, this);
        webSurfer = new WebSurferRunnable(client, this);
        fileSurfer = new FileSurferRunnable(client, this);
        coder = new CoderRunnable(client, this);
        terminal = new TerminalRunnable(client, this);
        orchestrator = new OrchestratorRunnable(client, this);
        directOrchestrator = new DirectOrchestratorRunnable(client, this);
        exit = new ExitRunnable(this) { AllowDeadEnd = true };

        // Setup orchestration flow: Planning -> specialized agents -> orchestrator for final summary -> exit
        planner.AddAdvancer((plan) => plan.RequiredAgents.Contains("WebSurfer") ||
            plan.OriginalTask.ToLower().Contains("search") ||
            plan.OriginalTask.ToLower().Contains("web") ||
            plan.OriginalTask.ToLower().Contains("research"), webSurfer);

        planner.AddAdvancer((plan) => plan.RequiredAgents.Contains("FileSurfer") ||
            plan.OriginalTask.ToLower().Contains("file") ||
            plan.OriginalTask.ToLower().Contains("document") ||
            plan.OriginalTask.ToLower().Contains("write"), fileSurfer);

        planner.AddAdvancer((plan) => plan.RequiredAgents.Contains("Coder") ||
            plan.OriginalTask.ToLower().Contains("code") ||
            plan.OriginalTask.ToLower().Contains("program") ||
            plan.OriginalTask.ToLower().Contains("script"), coder);

        planner.AddAdvancer((plan) => plan.RequiredAgents.Contains("Terminal") ||
            plan.OriginalTask.ToLower().Contains("command") ||
            plan.OriginalTask.ToLower().Contains("terminal") ||
            plan.OriginalTask.ToLower().Contains("run"), terminal);

        // If no specific agents are needed, go directly to directOrchestrator
        planner.AddAdvancer((plan) => !RequiresSpecializedAgent(plan), directOrchestrator);

        // All specialized agents flow to orchestrator for final summary
        webSurfer.AddAdvancer(orchestrator);
        fileSurfer.AddAdvancer(orchestrator);
        coder.AddAdvancer(orchestrator);
        terminal.AddAdvancer(orchestrator);

        // Both orchestrators produce final result and exit
        orchestrator.AddAdvancer((result) => !string.IsNullOrWhiteSpace(result.TaskResult), exit);
        directOrchestrator.AddAdvancer((result) => !string.IsNullOrWhiteSpace(result.TaskResult), exit);

        // Configure entry and exit points
        SetEntryRunnable(planner);
        SetRunnableWithResult(exit);
    }

    public MagenticOneConfiguration(TornadoApi client)
    {
        RecordSteps = true;
        
        // Create all the specialized runnables
        planner = new PlanningRunnable(client, this);
        webSurfer = new WebSurferRunnable(client, this);
        fileSurfer = new FileSurferRunnable(client, this);
        coder = new CoderRunnable(client, this);
        terminal = new TerminalRunnable(client, this);
        orchestrator = new OrchestratorRunnable(client, this);
        directOrchestrator = new DirectOrchestratorRunnable(client, this);
        exit = new ExitRunnable(this) { AllowDeadEnd = true };

        // Setup orchestration flow: Planning -> specialized agents -> orchestrator for final summary -> exit
        planner.AddAdvancer((plan) => plan.RequiredAgents.Contains("WebSurfer") || 
            plan.OriginalTask.ToLower().Contains("search") || 
            plan.OriginalTask.ToLower().Contains("web") || 
            plan.OriginalTask.ToLower().Contains("research"), webSurfer);
            
        planner.AddAdvancer((plan) => plan.RequiredAgents.Contains("FileSurfer") || 
            plan.OriginalTask.ToLower().Contains("file") || 
            plan.OriginalTask.ToLower().Contains("document") || 
            plan.OriginalTask.ToLower().Contains("write"), fileSurfer);
            
        planner.AddAdvancer((plan) => plan.RequiredAgents.Contains("Coder") || 
            plan.OriginalTask.ToLower().Contains("code") || 
            plan.OriginalTask.ToLower().Contains("program") || 
            plan.OriginalTask.ToLower().Contains("script"), coder);
            
        planner.AddAdvancer((plan) => plan.RequiredAgents.Contains("Terminal") || 
            plan.OriginalTask.ToLower().Contains("command") || 
            plan.OriginalTask.ToLower().Contains("terminal") || 
            plan.OriginalTask.ToLower().Contains("run"), terminal);
            
        // If no specific agents are needed, go directly to directOrchestrator
        planner.AddAdvancer((plan) => !RequiresSpecializedAgent(plan), directOrchestrator);

        // All specialized agents flow to orchestrator for final summary
        webSurfer.AddAdvancer(orchestrator);
        fileSurfer.AddAdvancer(orchestrator);
        coder.AddAdvancer(orchestrator);
        terminal.AddAdvancer(orchestrator);
        
        // Both orchestrators produce final result and exit
        orchestrator.AddAdvancer((result) => !string.IsNullOrWhiteSpace(result.TaskResult), exit);
        directOrchestrator.AddAdvancer((result) => !string.IsNullOrWhiteSpace(result.TaskResult), exit);

        // Configure entry and exit points
        SetEntryRunnable(planner);
        SetRunnableWithResult(exit);
    }

    private bool RequiresSpecializedAgent(TaskPlan plan)
    {
        var task = plan.OriginalTask.ToLower();
        return plan.RequiredAgents.Any(agent => agent != "Orchestrator") ||
               task.Contains("search") || task.Contains("web") || task.Contains("research") ||
               task.Contains("file") || task.Contains("document") || task.Contains("write") ||
               task.Contains("code") || task.Contains("program") || task.Contains("script") ||
               task.Contains("command") || task.Contains("terminal") || task.Contains("run");
    }

    public override void OnRuntimeInitialized()
    {
        base.OnRuntimeInitialized();
    }
}

public class PlanningRunnable : OrchestrationRunnable<ChatMessage, TaskPlan>
{
    TornadoAgent Agent;

    public PlanningRunnable(TornadoApi client, Orchestration orchestrator) : base(orchestrator)
    {
        string instructions = """
                You are the Planning Agent in a Magentic-One multi-agent system. Your role is to analyze incoming tasks and create execution plans.

                Available specialized agents:
                - WebSurfer: For web browsing, searching, and gathering information from websites
                - FileSurfer: For file operations like reading, writing, creating, and managing files  
                - Coder: For writing, reviewing, and executing code in various programming languages
                - Terminal: For running command-line operations and system commands

                Analyze the given task and determine:
                1. Which specialized agents are needed
                2. The execution plan and sequence
                3. Step-by-step descriptions of what each agent should do
                """;

        Agent = new TornadoAgent(
            client: client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Planning Agent",
            outputSchema: typeof(TaskPlan),
            instructions: instructions);
    }

    public override async ValueTask<TaskPlan> Invoke(RunnableProcess<ChatMessage, TaskPlan> process)
    {
        process.RegisterAgent(agent: Agent);

        string planningPrompt = $"""
            Task to analyze: {process.Input.Content}
            
            Please analyze this task and create a detailed execution plan. Determine which specialized agents are needed and provide a clear sequence of steps.
            """;

        Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { 
            new ChatMessage(Code.ChatMessageRoles.User, planningPrompt) 
        });

        TaskPlan? plan = await conv.Messages.Last().Content?.SmartParseJsonAsync<TaskPlan>(Agent);

        if (plan is null || plan?.RequiredAgents is null || plan?.RequiredAgents.Length == 0)
        {
            return new TaskPlan(
                process.Input.Content ?? "Unknown task",
                new[] { "Orchestrator" },
                "Direct execution by orchestrator",
                new[] { "Complete the task directly" }
            );
        }

        return plan.Value;
    }
}

public class WebSurferRunnable : OrchestrationRunnable<TaskPlan, AgentExecutionResults>
{
    TornadoAgent Agent;

    public WebSurferRunnable(TornadoApi client, Orchestration orchestrator) : base(orchestrator)
    {
        Agent = new TornadoAgent(
            client: client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "WebSurfer Agent",
            instructions: "You are a web browsing specialist. Search for information and browse websites as needed to gather comprehensive information for the given task.");
        Agent.ResponseOptions = new ResponseRequest() { Tools = [new ResponseWebSearchTool()] };
    }

    public override async ValueTask<AgentExecutionResults> Invoke(RunnableProcess<TaskPlan, AgentExecutionResults> process)
    {
        process.RegisterAgent(agent: Agent);

        string webPrompt = $"""
            Original Task: {process.Input.OriginalTask}
            
            Execution Plan: {process.Input.ExecutionPlan}
            
            As the WebSurfer agent, perform web research and information gathering to support this task. Provide comprehensive results that will be used by the Orchestrator to create the final deliverable.
            """;

        Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { 
            new ChatMessage(Code.ChatMessageRoles.User, webPrompt) 
        });

        string webResults = conv.Messages.Last().Content ?? "";

        return new AgentExecutionResults(
            process.Input.OriginalTask,
            webResults,
            "",
            "",
            "",
            new[] { "WebSurfer: Performed web research and information gathering" }
        );
    }
}

public class FileSurferRunnable : OrchestrationRunnable<TaskPlan, AgentExecutionResults>
{
    TornadoAgent Agent;

    public FileSurferRunnable(TornadoApi client, Orchestration orchestrator) : base(orchestrator)
    {
        Agent = new TornadoAgent(
            client: client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "FileSurfer Agent",
            tools: [MagenticOneTools.ReadFileTool, MagenticOneTools.WriteFileTool, MagenticOneTools.ListDirectoryTool],
            instructions: "You are a file operations specialist. Handle reading, writing, and managing files and directories for the given task.");
    }

    public override async ValueTask<AgentExecutionResults> Invoke(RunnableProcess<TaskPlan, AgentExecutionResults> process)
    {
        process.RegisterAgent(agent: Agent);

        string filePrompt = $"""
            Original Task: {process.Input.OriginalTask}
            
            Execution Plan: {process.Input.ExecutionPlan}
            
            As the FileSurfer agent, handle any file operations needed for this task. Use the available tools to read, write, or manage files as required. Provide comprehensive results that will be used by the Orchestrator.
            """;

        Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { 
            new ChatMessage(Code.ChatMessageRoles.User, filePrompt) 
        });

        string fileResults = conv.Messages.Last().Content ?? "";

        return new AgentExecutionResults(
            process.Input.OriginalTask,
            "",
            fileResults,
            "",
            "",
            new[] { "FileSurfer: Handled file operations and document management" }
        );
    }
}

public class TerminalRunnable : OrchestrationRunnable<TaskPlan, AgentExecutionResults>
{
    TornadoAgent Agent;

    public TerminalRunnable(TornadoApi client, Orchestration orchestrator) : base(orchestrator)
    {
        Agent = new TornadoAgent(
            client: client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Terminal Agent",
            tools: [MagenticOneTools.ExecuteCommandTool],
            instructions: "You are a command-line specialist. Execute system commands and manage processes for the given task.");
    }

    public override async ValueTask<AgentExecutionResults> Invoke(RunnableProcess<TaskPlan, AgentExecutionResults> process)
    {
        process.RegisterAgent(agent: Agent);

        string terminalPrompt = $"""
            Original Task: {process.Input.OriginalTask}
            
            Execution Plan: {process.Input.ExecutionPlan}
            
            As the Terminal agent, execute any command-line operations needed for this task. Use the available tools safely and effectively. Provide comprehensive results that will be used by the Orchestrator.
            """;

        Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { 
            new ChatMessage(Code.ChatMessageRoles.User, terminalPrompt) 
        });

        string terminalResults = conv.Messages.Last().Content ?? "";

        return new AgentExecutionResults(
            process.Input.OriginalTask,
            "",
            "",
            "",
            terminalResults,
            new[] { "Terminal: Executed command-line operations" }
        );
    }
}


public class OrchestratorRunnable : OrchestrationRunnable<AgentExecutionResults, MagenticOneResult>
{
    TornadoAgent Agent;
    
    public OrchestratorRunnable(TornadoApi client, Orchestration orchestrator) : base(orchestrator)
    {
        string orchestratorInstructions = """
                You are the Orchestrator Agent in a Magentic-One multi-agent system. Your role is to synthesize results from specialized agents and create comprehensive final deliverables.

                You will receive results from specialized agents and need to combine them into a coherent final result for the user.
                """;

        Agent = new TornadoAgent(
            client: client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Orchestrator Agent",
            outputSchema: typeof(MagenticOneResult),
            instructions: orchestratorInstructions);
    }

    public override async ValueTask<MagenticOneResult> Invoke(RunnableProcess<AgentExecutionResults, MagenticOneResult> process)
    {
        process.RegisterAgent(agent: Agent);

        string taskDescription = process.Input.OriginalTask;
        var actionsPerformed = process.Input.ActionsPerformed.ToList();

        // Combine all results from specialized agents
        var combinedResults = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(process.Input.WebSearchResults))
        {
            combinedResults.Add($"Web Research Results:\n{process.Input.WebSearchResults}");
        }
        
        if (!string.IsNullOrWhiteSpace(process.Input.FileOperationResults))
        {
            combinedResults.Add($"File Operation Results:\n{process.Input.FileOperationResults}");
        }
        
        if (!string.IsNullOrWhiteSpace(process.Input.CodeExecutionResults))
        {
            combinedResults.Add($"Code Execution Results:\n{process.Input.CodeExecutionResults}");
        }
        
        if (!string.IsNullOrWhiteSpace(process.Input.TerminalResults))
        {
            combinedResults.Add($"Terminal Command Results:\n{process.Input.TerminalResults}");
        }

        string allResults = string.Join("\n\n", combinedResults);

        // Create final summary
        string summaryPrompt = $"""
            Original Task: {taskDescription}
            
            Results from Specialized Agents:
            {allResults}
            
            As the Orchestrator, create a comprehensive final result that synthesizes all the work done by the specialized agents. 
            Provide a clear summary of what was accomplished and the final deliverable for the user's task.
            """;

        Conversation finalConv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { 
            new ChatMessage(Code.ChatMessageRoles.User, summaryPrompt) 
        });

        MagenticOneResult? finalResult = await finalConv.Messages.Last().Content?.SmartParseJsonAsync<MagenticOneResult>(Agent);

        if (finalResult is null)
        {
            return new MagenticOneResult(
                allResults.Length > 0 ? allResults : "Task completed by specialized agents",
                "Magentic-One agents collaborated to address the task",
                actionsPerformed.ToArray()
            );
        }

        return finalResult.Value;
    }
}

// Direct orchestration runnable for tasks that don't need specialized agents  
public class DirectOrchestratorRunnable : OrchestrationRunnable<TaskPlan, MagenticOneResult>
{
    TornadoAgent Agent;
    
    public DirectOrchestratorRunnable(TornadoApi client, Orchestration orchestrator) : base(orchestrator)
    {
        string orchestratorInstructions = """
                You are the Direct Orchestrator Agent in a Magentic-One multi-agent system. Your role is to handle tasks directly when no specialized agents are needed.
                """;

        Agent = new TornadoAgent(
            client: client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Direct Orchestrator Agent",
            outputSchema: typeof(MagenticOneResult),
            instructions: orchestratorInstructions);
    }

    public override async ValueTask<MagenticOneResult> Invoke(RunnableProcess<TaskPlan, MagenticOneResult> process)
    {
        process.RegisterAgent(agent: Agent);

        string taskDescription = process.Input.OriginalTask;

        // Handle task directly when no specialized agents are needed
        string directPrompt = $"""
            Task: {taskDescription}
            
            Execution Plan: {process.Input.ExecutionPlan}
            
            As the Direct Orchestrator, handle this task directly and provide a comprehensive result.
            """;

        Conversation finalConv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { 
            new ChatMessage(Code.ChatMessageRoles.User, directPrompt) 
        });

        MagenticOneResult? finalResult = await finalConv.Messages.Last().Content?.SmartParseJsonAsync<MagenticOneResult>(Agent);

        if (finalResult is null)
        {
            return new MagenticOneResult(
                process.Input.ExecutionPlan,
                "Direct Orchestrator handled the task without specialized agents",
                new[] { "Direct Orchestrator: Completed task directly" }
            );
        }

        return finalResult.Value;
    }
}



public class ExitRunnable : OrchestrationRunnable<MagenticOneResult, ChatMessage>
{
    public ExitRunnable(Orchestration orchestrator, string runnableName = "") : base(orchestrator, runnableName)
    {
    }

    public override async ValueTask<ChatMessage> Invoke(RunnableProcess<MagenticOneResult, ChatMessage> process)
    {
        this.Orchestrator?.HasCompletedSuccessfully();
        return new ChatMessage(Code.ChatMessageRoles.Assistant, process.Input.ToString());
    }
}