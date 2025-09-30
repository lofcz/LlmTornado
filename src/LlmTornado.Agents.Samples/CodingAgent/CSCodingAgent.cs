using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Agents.Samples.Common;
using LlmTornado.Agents.Samples.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Samples.CodingAgent;


public class CodingAgentConfiguration : OrchestrationRuntimeConfiguration
{
    public static string ProjectBuildPath = Directory.GetCurrentDirectory();
    public static string ProjectName = "ConsoleDemo_" + Guid.NewGuid().ToString().Substring(0, 6);

    CodingAgentRunnable _codeState;
    ProjectBuilderRunnable _buildState;
    CodeReviewRunnable _reviewState;
    ProjectSummaryRunnable _summaryState;

    public CodingAgentConfiguration()
    {
        TornadoApi client = new TornadoApi(Environment.GetEnvironmentVariable("OPENAI_API_KEY"), Code.LLmProviders.OpenAi);
        FileIOUtility.SafeWorkingDirectory = Path.Combine(ProjectBuildPath, ProjectName);

        _codeState = new CodingAgentRunnable(client, this);//Program a solution
        _buildState = new ProjectBuilderRunnable(this); //Execute a solution 
        _reviewState = new CodeReviewRunnable(client, this);//How to fix the code
        _summaryState = new ProjectSummaryRunnable(this) { AllowDeadEnd = true }; //Summarize the code

        SetupOrchestration();
    }

    public CodingAgentConfiguration(TornadoApi client)
    {
        FileIOUtility.SafeWorkingDirectory = Path.Combine(ProjectBuildPath, ProjectName);

        _codeState = new CodingAgentRunnable(client, this);//Program a solution
        _buildState = new ProjectBuilderRunnable(this); //Execute a solution 
        _reviewState = new CodeReviewRunnable(client, this);//How to fix the code
        _summaryState = new ProjectSummaryRunnable(this) { AllowDeadEnd = true }; //Summarize the code

        SetupOrchestration();
    }

    public override void OnRuntimeInitialized()
    {
        CodeUtility.CreateNewProject(ProjectName);

        Console.WriteLine($"Created new project at {FileIOUtility.SafeWorkingDirectory}");
    }

    private void SetupOrchestration()
    {
        //Setup the orchestration flow
        _codeState.AddAdvancer(CheckIfCodeGenerated, _buildState);

        _buildState.AddAdvancer(CheckIfProgramFailed, _reviewState);
        _buildState.AddAdvancer(CheckIfProgramWorked, _summaryState);

        _reviewState.AddAdvancer(_codeState); //Loop back to coding agent to fix issues

        //Configure the Orchestration entry and exit points
        SetEntryRunnable(_codeState);
        SetRunnableWithResult(_summaryState);
    }

    public OrchestrationRuntimeConfiguration CreateWithBuilder()
    {
        return new OrchestrationBuilder(this)
            .SetEntryRunnable(_codeState)
            .WithOnRuntimeEvent(async (evt) =>
            {
                if (evt.EventType == ChatRuntimeEventTypes.AgentRunner)
                {
                    if (evt is ChatRuntimeAgentRunnerEvents runnerEvt)
                    {
                        if (runnerEvt.AgentRunnerEvent is AgentRunnerStreamingEvent streamEvt)
                        {
                            if (streamEvt.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                            {
                                Console.Write(deltaTextEvent.DeltaText);
                            }
                        }
                    }
                }
                await ValueTask.CompletedTask;
            })
            .AddAdvancer<ProgramResultOutput>(_codeState, CheckIfCodeGenerated, _buildState)
            .AddAdvancers(_buildState, 
                new OrchestrationAdvancer<CodeBuildInfoOutput>(CheckIfProgramFailed, _reviewState),
                new OrchestrationAdvancer<CodeBuildInfoOutput>(CheckIfProgramWorked, _summaryState)
            )
            .AddAdvancer<ChatMessage>(_reviewState, _codeState)
            .SetOutputRunnable(_summaryState)
            .Build();
    }

    public bool CheckIfCodeGenerated(ProgramResultOutput result)
    {
        return result.Result.items.Length > 0;
    }

    public bool CheckIfProgramFailed(CodeBuildInfoOutput result)
    {
        return !result.BuildInfo?.BuildResult.BuildCompleted ?? false;
    }

    public bool CheckIfProgramWorked(CodeBuildInfoOutput result)
    {
        return result.BuildInfo?.BuildResult.BuildCompleted ?? false;
    }

    [Description("Use this tool to read files already written")]
    public static string ReadFileTool([Description("file path of the file you wish to read.")] string filePath)
    {
        return FileIOUtility.ReadFile(filePath);
    }

    [Description("Use this tool to get all the file paths in the project")]
    public static string GetFilesTool()
    {
        return FileIOUtility.GetAllPaths(ProjectName);
    }

    class ProjectSummaryRunnable : OrchestrationRunnable<CodeBuildInfoOutput, ChatMessage>
    {
        public ProjectSummaryRunnable(Orchestration orchestrator) : base(orchestrator)
        {
        }

        public override ValueTask<ChatMessage> Invoke(RunnableProcess<CodeBuildInfoOutput, ChatMessage> input)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("The project was built successfully! Here are the results:");
            stringBuilder.AppendLine($"Build Completed: {input.Input.BuildInfo.BuildResult.BuildCompleted}");
            this.Orchestrator?.HasCompletedSuccessfully(); //Signal the orchestration has completed successfully
            return ValueTask.FromResult(new ChatMessage(Code.ChatMessageRoles.Assistant, stringBuilder.ToString()));
        }
    }

    class ProjectBuilderRunnable : OrchestrationRunnable<ProgramResultOutput, CodeBuildInfoOutput>
    {
        public ProjectBuilderRunnable(Orchestration orchestrator) : base(orchestrator)
        {
        }

        public override ValueTask<CodeBuildInfoOutput> Invoke(RunnableProcess<ProgramResultOutput, CodeBuildInfoOutput> programResult)
        {
            //Need a file path to a solution you don't care about or has git control
            if (!Directory.Exists(FileIOUtility.SafeWorkingDirectory))
            {
                throw new Exception("Need to set a directory to a c# project");
            }

            //Write over files in project
            foreach (CodeItem script in programResult.Input.Result.items)
            {
                FileIOUtility.WriteFile(script.filePath, script.code);
            }

            //build the project code
            //In theory here i could setup a lot of different code to build
            CodeBuildInfo codeInfo = CodeUtility.BuildAndRunProject(FileIOUtility.SafeWorkingDirectory, "netcoreapp3.1");

            //Report the results of the build
            return ValueTask.FromResult(new CodeBuildInfoOutput(codeInfo, programResult.Input));
        }
    }

    class CodeReviewRunnable : OrchestrationRunnable<CodeBuildInfoOutput, ChatMessage>
    {
        TornadoAgent Agent;

        public CodeReviewRunnable(TornadoApi client, Orchestration orchestrator) : base(orchestrator)
        {
            string instructions = """
                You are an expert programmer for c#. Given the generated C# project errors help the coding agent by finding all the files with errors 
                and suggestions on how to fix them.
                
                Original Program Request was: 
                """;

            Agent = new TornadoAgent(
                client: client,
                model: ChatModel.OpenAi.Gpt5.V5Mini,
                name: "Research Agent",
                tools: [ReadFileTool, GetFilesTool],
                outputSchema: typeof(CodeReview),
                instructions: instructions);
        }

        public override async ValueTask<ChatMessage> Invoke(RunnableProcess<CodeBuildInfoOutput, ChatMessage> codeBuildInfo)
        {
            codeBuildInfo.RegisterAgent(Agent);
            string updatedInstructions = $"""
                You are an expert programmer for c#. Given the generated C# project errors help the coding agent by finding all the files with errors 
                and suggestions on how to fix them.

                Original Program Request was: 

                {codeBuildInfo.Input.ProgramResult.ProgramRequest}
                """;

            Agent.Instructions = updatedInstructions;

            Conversation result = await Agent.RunAsync($"Errors Generated {codeBuildInfo.Input.BuildInfo.BuildResult.Error}");

            CodeReview review = result.Messages.Last().Content.ParseJson<CodeReview>();

            return new ChatMessage(Code.ChatMessageRoles.User, review.ToString());
        }
    }

    class CodingAgentRunnable : OrchestrationRunnable<ChatMessage, ProgramResultOutput>
    {
        TornadoAgent Agent;

        public CodingAgentRunnable(TornadoApi client, Orchestration orchestrator) : base(orchestrator)
        {
            string instructions = """
                You are an expert C# programmer. Your task is to write detailed and working code for the following function based on the context provided.
                """;

            Agent = new TornadoAgent(
                client:client,
                model: ChatModel.OpenAi.Gpt5.V5Mini,
                name: "Research Agent",
                tools: [ReadFileTool, GetFilesTool],
                outputSchema: typeof(ProgramResult),
                instructions: instructions);
        }

        public override async ValueTask<ProgramResultOutput> Invoke(RunnableProcess<ChatMessage, ProgramResultOutput> input)
        {
            input.RegisterAgent(Agent);
            string prompt = @"You are an expert C# programmer. Your task is to write detailed and working code for the following function based on the context provided. 
                    The C# Console Program will be Built and executed later from a .exe and will use the input args to get the function to work.
                    Make sure Program code is the main entry to utilize .net8.0 args structure when executing exe
                    Do not provide placeholder code, but rather do your best like you are the best senior engineer in the world and provide the best code possible. DO NOT PROVIDE PLACEHOLDER CODE.
                    Overall context:
                    {0}";

            Conversation result = await Agent.RunAsync(string.Format(prompt, input.Input.Content), appendMessages: [input.Input]);

            ProgramResult program = result.Messages.Last().Content.ParseJson<ProgramResult>();

            return new ProgramResultOutput(program, input.Input.Content);
        }

    }
}

