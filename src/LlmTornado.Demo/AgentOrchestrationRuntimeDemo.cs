using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Responses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LlmTornado.Demo.AgentOrchestrationRuntimeDemo.CodeUtility;
using static LlmTornado.Demo.AgentOrchestrationRuntimeDemo.ResearchAgentConfiguration;

namespace LlmTornado.Demo;

public class AgentOrchestrationRuntimeDemo : DemoBase
{
    #region ResearchAgent
    [TornadoTest]
    public static async Task ResearchAgentRuntimeDemo()
    {
        ResearchAgentConfiguration RuntimeConfiguration = new ResearchAgentConfiguration();

        ChatRuntime runtime = new ChatRuntime(RuntimeConfiguration);

        Console.WriteLine("[Assistant]: What do you want to research?");
        Console.Write("[User]: ");
        string topic = Console.ReadLine();
        ChatMessage report = await runtime.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, topic ?? "Write a report about the benefits of using AI agents."));

        Console.WriteLine(report.Parts.Last().Text);
    }

    [TornadoTest]
    public static async Task BasicOrchestrationRuntimeStreamingDemo()
    {
        ResearchAgentConfiguration RuntimeConfiguration = new ResearchAgentConfiguration();

        ChatRuntime runtime = new ChatRuntime(RuntimeConfiguration);

        RuntimeConfiguration.OnRuntimeEvent = async (evt) =>
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
        };

        ChatMessage report = await runtime.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, "Write a report about the benefits of using AI agents."));
    }

    public class ResearchAgentConfiguration : OrchestrationRuntimeConfiguration
    {
        PlannerRunnable planner;
        ResearchRunnable researcher;
        ReportingRunnable reporter;
        ExitRunnable exit;

        public ResearchAgentConfiguration() 
        {
            //Create the Runnables
            planner = new PlannerRunnable();
            researcher = new ResearchRunnable();
            reporter = new ReportingRunnable();
            exit = new ExitRunnable() { AllowDeadEnd = true }; //Set deadend to disable reattempts and finish the execution

            //Setup the orchestration flow
            planner.AddAdvancer((plan) => plan.items.Length > 0, researcher);
            researcher.AddAdvancer((research) => !string.IsNullOrEmpty(research), reporter);
            reporter.AddAdvancer((report) => !string.IsNullOrEmpty(report.FinalReport), exit);

            //Configure the Orchestration entry and exit points
            SetEntryRunnable(planner);
            SetRunnableWithResult(exit);

            reporter.OnAgentRunnerEvent += (sEvent) =>
            {
                this.OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent));
            };
        }

        public class PlannerRunnable : OrchestrationRunnable<ChatMessage, WebSearchPlan>
        {
            RuntimeAgent Agent;

            public PlannerRunnable()
            {
                string instructions = """
                    You are a helpful research assistant. Given a query, come up with a set of web searches, 
                    to perform to best answer the query. Output between 5 and 10 terms to query for. 
                    """;

                Agent = new RuntimeAgent(
                    client: Program.Connect(),
                    model: ChatModel.OpenAi.Gpt5.V5Mini,
                    name: "Research Agent",
                    outputSchema: typeof(WebSearchPlan),
                    instructions: instructions);
            }

            public override async ValueTask<WebSearchPlan> Invoke(ChatMessage input)
            {
                Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { input });

                WebSearchPlan? plan = await conv.Messages.Last().Content?.SmartParseJsonAsync<WebSearchPlan>(Agent);

                if (plan is null || plan?.items is null || plan?.items.Length == 0)
                {
                    return new WebSearchPlan([]);
                }
                else
                {
                    return plan.Value;
                }
            }
        }

        public class ResearchRunnable : OrchestrationRunnable<WebSearchPlan, string>
        {
            public int MaxDegreeOfParallelism { get; set; } = 4;
            public int MaxItemsToProcess { get; set; } = 10;

            public ResearchRunnable() { }

            public override async ValueTask<string> Invoke(WebSearchPlan plan)
            {
                return await InvokeThreadedAsync(plan); 
            }

            public async Task<string> InvokeThreadedAsync(WebSearchPlan plan)
            {
                SemaphoreSlim semaphore = new SemaphoreSlim(MaxDegreeOfParallelism);

                List<Task<RunnerResult<string>>> researchTasks = 
                    plan.items
                        .Take(MaxItemsToProcess)
                        .Select(item => Task.Run(async () =>
                        {
                            await semaphore.WaitAsync();
                            try
                            {
                                return await RunResearchAgent(item);
                            }
                            finally
                            {
                                semaphore.Release();
                            }
                        }))
                        .ToList();

                var researchResults = await Task.WhenAll(researchTasks);

                return string.Join("[RESEARCH RESULT]\n\n\n", researchResults.ToList().Select(result => result.Result));
            }

            private async ValueTask<RunnerResult<string>> RunResearchAgent(WebSearchItem item)
            {
                string instructions = """
                    You are a research assistant. Given a search term, you search the web for that term and
                    produce a concise summary of the results. The summary must be 2-3 paragraphs and less than 300 
                    words. Capture the main points. Write succinctly, no need to have complete sentences or good
                    grammar. This will be consumed by someone synthesizing a report, so its vital you capture the 
                    essence and ignore any fluff. Do not include any additional commentary other than the summary itself.
                    """;

                RuntimeAgent Agent = new RuntimeAgent(
                    client: Program.Connect(),
                    model: ChatModel.OpenAi.Gpt5.V5Mini,
                    name: "Research Agent",
                    instructions: instructions);

                Agent.ResponseOptions = new ResponseRequest() { Tools = [new ResponseWebSearchTool()] };

                ChatMessage userMessage = new ChatMessage(Code.ChatMessageRoles.User, item.query);

                Conversation conv = await Agent.RunAsync(appendMessages: new List<ChatMessage> { userMessage });

                return new RunnerResult<string>(item.query, conv.Messages.Last().Content ?? string.Empty);
            }
        }

        public class ReportingRunnable : OrchestrationRunnable<string, ReportData>
        {
            RuntimeAgent Agent;

            public Action<AgentRunnerEvents>? OnAgentRunnerEvent { get; set; }

            public ReportingRunnable() 
            {
                string instructions = """
                    You are a senior researcher tasked with writing a cohesive report for a research query.
                    you will be provided with the original query, and some initial research done by a research assistant.

                    you should first come up with an outline for the report that describes the structure and flow of the report. 
                    Then, generate the report and return that as your final output.

                    The final output should be in markdown format, and it should be lengthy and detailed. Aim for 5-10 pages of content, at least 1000 words.
                    """;

                Agent = new RuntimeAgent(
                    client: Program.Connect(),
                    model: ChatModel.OpenAi.Gpt5.V5,
                    name: "Report Agent",
                    instructions: instructions,
                    outputSchema:typeof(ReportData),
                    streaming:true);
            }

            public override async ValueTask<ReportData> Invoke(string research)
            {
                Conversation conv = await Agent.RunAsync(
                    appendMessages: new List<ChatMessage> { new ChatMessage(Code.ChatMessageRoles.User, research) }, 
                    streaming: Agent.Streaming, 
                    onAgentRunnerEvent: (sEvent) =>
                    {
                        OnAgentRunnerEvent?.Invoke(sEvent);
                        return ValueTask.CompletedTask;
                    });

                ReportData? report = await conv.Messages.Last().Content?.SmartParseJsonAsync<ReportData>(Agent)!;

                if (report is null || string.IsNullOrWhiteSpace(report?.FinalReport))
                {
                    throw new Exception("No report generated");
                }

                return report.Value;
            }
        }

        public class ExitRunnable : OrchestrationRunnable<ReportData, ChatMessage> { 
            public override ValueTask<ChatMessage> Invoke(ReportData input)
            {
                this.Orchestrator?.HasCompletedSuccessfully(); //Signal the orchestration has completed successfully
                return ValueTask.FromResult(new ChatMessage(Code.ChatMessageRoles.Assistant, input.ToString()));
            }
        }

        public struct WebSearchPlan
        {
            public WebSearchItem[] items { get; set; }
            public WebSearchPlan(WebSearchItem[] items)
            {
                this.items = items;
            }
        }

        public struct WebSearchItem
        {
            public string reason { get; set; }
            public string query { get; set; }

            public WebSearchItem(string reason, string query)
            {
                this.reason = reason;
                this.query = query;
            }
        }

        public struct ReportData
        {
            public string ShortSummary { get; set; }
            public string FinalReport { get; set; }
            public string[] FollowUpQuestions { get; set; }
            public ReportData(string shortSummary, string finalReport, string[] followUpQuestions)
            {
                this.ShortSummary = shortSummary;
                this.FinalReport = finalReport;
                this.FollowUpQuestions = followUpQuestions;
            }

            public override string ToString()
            {
                return $@"
Summary: 
{ShortSummary}

Final Report: 
{FinalReport}


Follow Up Questions: 
{string.Join("\n", FollowUpQuestions)}
";
            }
        }
    }
    #endregion

    #region CodingAgent

    [TornadoTest]
    public static async Task CodingAgentRuntimeDemo()
    {

        CodingAgentConfiguration RuntimeConfiguration = new CodingAgentConfiguration();

        ChatRuntime runtime = new ChatRuntime(RuntimeConfiguration);

        Console.WriteLine("[Assistant]: What do you want to code?");
        Console.Write("[User]: ");
        string topic = Console.ReadLine();
        ChatMessage report = await runtime.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, topic ?? "Write a hello world script"));

        Console.WriteLine(report.Content);
    }

    public class CodingAgentConfiguration : OrchestrationRuntimeConfiguration
    {
        public static string ProjectBuildPath = Directory.GetCurrentDirectory();
        public static string ProjectName = "ConsoleDemo_" + Guid.NewGuid().ToString().Substring(0,6);

        CodingAgentRunnable codeState = new CodingAgentRunnable();//Program a solution
        ProjectBuilderRunnable buildState = new ProjectBuilderRunnable(); //Execute a solution 
        CodeReviewRunnable reviewState = new CodeReviewRunnable();//How to fix the code
        ProjectSummaryRunnable summaryState = new ProjectSummaryRunnable() { AllowDeadEnd = true }; //Summarize the code

        public CodingAgentConfiguration()
        {
            FileIOUtility.SafeWorkingDirectory = Path.Combine(ProjectBuildPath, ProjectName);
            
            CreateNewProject(ProjectName);

            Console.WriteLine($"Created new project at {FileIOUtility.SafeWorkingDirectory}");
            SetupOrchestration();
        }


        private void SetupOrchestration()
        {
            //Setup the orchestration flow
            codeState.AddAdvancer(CheckIfCodeGenerated, buildState);

            buildState.AddAdvancer(CheckIfProgramFailed, reviewState);
            buildState.AddAdvancer(CheckIfProgramWorked, summaryState);

            reviewState.AddAdvancer(codeState); //Loop back to coding agent to fix issues

            //Configure the Orchestration entry and exit points
            SetEntryRunnable(codeState);
            SetRunnableWithResult(summaryState);
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
        public static string ReadFileTool([Description("file path of the file you wish to read.")]  string filePath)
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
            public override ValueTask<ChatMessage> Invoke(CodeBuildInfoOutput input)
            {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("The project was built successfully! Here are the results:");
                stringBuilder.AppendLine($"Build Completed: {input.BuildInfo.BuildResult.BuildCompleted}");
                this.Orchestrator?.HasCompletedSuccessfully(); //Signal the orchestration has completed successfully
                return ValueTask.FromResult(new ChatMessage(Code.ChatMessageRoles.Assistant, stringBuilder.ToString()));
            }
        }

        class ProjectBuilderRunnable : OrchestrationRunnable<ProgramResultOutput, CodeBuildInfoOutput>
        {
            public override ValueTask<CodeBuildInfoOutput> Invoke(ProgramResultOutput programResult)
            {
                //Need a file path to a solution you don't care about or has git control
                if (!Directory.Exists(FileIOUtility.SafeWorkingDirectory))
                {
                    throw new Exception("Need to set a directory to a c# project");
                }

                //Write over files in project
                foreach (CodeItem script in programResult.Result.items)
                {
                    FileIOUtility.WriteFile(script.filePath, script.code);
                }

                //build the project code
                //In theory here i could setup a lot of different code to build
                CodeBuildInfo codeInfo = BuildAndRunProject(FileIOUtility.SafeWorkingDirectory, "netcoreapp3.1");

                //Report the results of the build
                return ValueTask.FromResult(new CodeBuildInfoOutput(codeInfo, programResult));
            }
        }

        class CodeReviewRunnable : OrchestrationRunnable<CodeBuildInfoOutput, ChatMessage>
        {
            RuntimeAgent Agent;

            public CodeReviewRunnable()
            {
                string instructions = """
                    You are an expert programmer for c#. Given the generated C# project errors help the coding agent by finding all the files with errors 
                    and suggestions on how to fix them.
                    
                    Original Program Request was: 
                    """;

                Agent = new RuntimeAgent(
                    client: Program.Connect(),
                    model: ChatModel.OpenAi.Gpt5.V5Mini,
                    name: "Research Agent",
                    tools: [ReadFileTool, GetFilesTool],
                    outputSchema: typeof(CodeReview),
                    instructions: instructions);
            }
        
            public override async ValueTask<ChatMessage> Invoke(CodeBuildInfoOutput codeBuildInfo)
            {
                string updatedInstructions = $"""
                    You are an expert programmer for c#. Given the generated C# project errors help the coding agent by finding all the files with errors 
                    and suggestions on how to fix them.

                    Original Program Request was: 

                    {codeBuildInfo.ProgramResult.ProgramRequest}
                    """;

                Agent.Instructions = updatedInstructions;

                Conversation result = await Agent.RunAsync($"Errors Generated {codeBuildInfo.BuildInfo.BuildResult.Error}");

                CodeReview review = result.Messages.Last().Content.ParseJson<CodeReview>();

                return new ChatMessage(Code.ChatMessageRoles.User, review.ToString());
            }
        }

        class CodingAgentRunnable : OrchestrationRunnable<ChatMessage, ProgramResultOutput>
        {
            RuntimeAgent Agent;

            public CodingAgentRunnable()
            {
                string instructions = """
                    You are an expert C# programmer. Your task is to write detailed and working code for the following function based on the context provided.
                    """;

                Agent = new RuntimeAgent(
                    client: Program.Connect(),
                    model: ChatModel.OpenAi.Gpt5.V5Mini,
                    name: "Research Agent",
                    tools: [ReadFileTool, GetFilesTool],
                    outputSchema: typeof(ProgramResult),
                    instructions: instructions);
            }

            public override async ValueTask<ProgramResultOutput> Invoke(ChatMessage input)
            {
                string prompt = @"You are an expert C# programmer. Your task is to write detailed and working code for the following function based on the context provided. 
                    The C# Console Program will be Built and executed later from a .exe and will use the input args to get the function to work.
                    Make sure Program code is the main entry to utilize .net8.0 args structure when executing exe
                    Do not provide placeholder code, but rather do your best like you are the best senior engineer in the world and provide the best code possible. DO NOT PROVIDE PLACEHOLDER CODE.
                    Overall context:
                    {0}";

                Conversation result = await Agent.RunAsync(string.Format(prompt, input.Content), appendMessages: [input]);

                ProgramResult program = result.Messages.Last().Content.ParseJson<ProgramResult>();

                return new ProgramResultOutput(program, input.Content);
            }

        }

        #region DataModels
        public struct ProgramResult
        {
            public CodeItem[] items { get; set; }
            public ProgramResult(CodeItem[] items)
            {
                this.items = items;
            }
        }

        public struct CodeItem
        {
            public string filePath { get; set; }
            public string code { get; set; }

            public CodeItem(string path, string code)
            {
                filePath = path;
                this.code = code;
            }
        }

        public struct ProgramResultOutput
        {
            public ProgramResult Result { get; set; }
            public string ProgramRequest { get; set; }
            public ProgramResultOutput(ProgramResult result, string request)
            {
                Result = result;
                ProgramRequest = request;
            }
        }

        public struct CodeBuildInfoOutput
        {
            public CodeBuildInfo BuildInfo { get; set; }
            public ProgramResultOutput ProgramResult { get; set; }

            public CodeBuildInfoOutput() { }
            public CodeBuildInfoOutput(CodeBuildInfo info, ProgramResultOutput codeResult)
            {
                BuildInfo = info;
                ProgramResult = codeResult;
            }
        }

        public struct CodeReview
        {
            public string ReviewSummary { get; set; }
            public CodeReviewItem[] Items { get; set; }

            public CodeReview(CodeReviewItem[] item)
            {
                Items = item;
            }
            public override string ToString()
            {
                return $""""
                From Code Review Summary:
                {ReviewSummary}

                Items to fix:

                {string.Join("\n\n", Items)}

                """";
            }
        }

        public struct CodeReviewItem
        {
            public string CodePath { get; set; }
            public string CodeError { get; set; }
            public string SuggestedFix { get; set; }
            public CodeReviewItem(string codePath, string codeError, string suggestedFix)
            {
                CodePath = codePath;
                CodeError = codeError;
                SuggestedFix = suggestedFix;
            }

            public override string ToString()
            {
                return $"""

                 File: {CodePath}
                 
                 Had Error: 
                 {CodeError}

                 Suggested Fix:
                 {SuggestedFix}

                 """;
            }
        }

        #endregion

    }

    #region Utilities
    #region FileIOUtility
    public class FileIOUtility
    {
        public static string? SafeWorkingDirectory { get; set; }

        public static string ReadFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(FileIOUtility.SafeWorkingDirectory))
                {
                    throw new InvalidOperationException("SafeWorkingDirectory is not set. Please set SafeWorkingDirectory before reading directories.");
                }

                filePath = filePath.Trim();

                Path.GetInvalidFileNameChars().ToList().ForEach(c =>
                {
                    if (Path.GetFileName(filePath).Contains(c))
                    {
                        throw new ArgumentException($"File name cannot contain {c}");
                    }
                }
                );
                // Validate the file path
                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty.");
                }
                if (filePath.StartsWith("..") || filePath.StartsWith("/"))
                {
                    throw new ArgumentException("File path cannot contain relative paths like '..', or '/'.");
                }
                if (filePath.StartsWith("\\"))
                {
                    filePath = filePath.TrimStart('\\');
                }
                string fixedPath = Path.Combine(FileIOUtility.SafeWorkingDirectory, filePath.Trim());

                return File.ReadAllText(fixedPath);
            }
            catch (Exception ex)
            {
                return $"Error reading file -> {ex.Message}"; // Return empty string or handle as needed
            }
        }

        public static void WriteFile(string filePath, string content)
        {

            if (string.IsNullOrEmpty(SafeWorkingDirectory))
            {
                throw new InvalidOperationException("SafeWorkingDirectory is not set. Please set SafeWorkingDirectory before reading directories.");
            }

            string FixedPath = Path.Combine(SafeWorkingDirectory, filePath);

            string? directoryPath = Path.GetDirectoryName(FixedPath);

            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(FixedPath, content);
        }

        public static string GetAllPaths(string directory)
        {
            if (string.IsNullOrEmpty(SafeWorkingDirectory))
            {
                throw new InvalidOperationException("SafeWorkingDirectory is not set. Please set SafeWorkingDirectory before reading directories.");
            }

            List<string> allPaths = new List<string>();

            GetPaths(directory, allPaths);

            for (int i = 0; i < allPaths.Count; i++)
            {
                allPaths[i] = allPaths[i].Replace(SafeWorkingDirectory, "");
            }

            foreach (string path in allPaths)
            {
                Console.WriteLine(path);
            }

            return string.Join(Environment.NewLine, allPaths);
        }

        public static void GetPaths(string directory, List<string> paths)
        {
            if (string.IsNullOrEmpty(SafeWorkingDirectory))
            {
                throw new InvalidOperationException("SafeWorkingDirectory is not set. Please set SafeWorkingDirectory before reading directories.");
            }

            string SafePath = Path.Combine(SafeWorkingDirectory, directory);

            try
            {
                // Add files in the current directory
                paths.AddRange(Directory.GetFiles(SafePath));

                // Add subdirectories in the current directory and recurse
                string[] subdirectories = Directory.GetDirectories(SafePath);
                //paths.AddRange(subdirectories);
                foreach (string subdirectory in subdirectories)
                {
                    if (subdirectory.Contains("bin")) continue;
                    if (subdirectory.Contains("obj")) continue;
                    GetPaths(subdirectory, paths);
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access denied to: {ex.Message}"); // Handle exceptions gracefully
            }
            catch (DirectoryNotFoundException ex)
            {
                Console.WriteLine($"Directory not found: {ex.Message}"); // Handle exceptions gracefully
            }
        }
    }
    #endregion

    #region CodeBuildInfo
    public class CodeBuildInfo
    {

        public string BuildPath { get; set; }

        public string ProjectName { get; set; }
        public ExecutableOutputResult? ExecutableResult { get; set; }

        public BuildOutputResult? BuildResult { get; set; }

        public CodeBuildInfo() { }

        public CodeBuildInfo(string buildPath, string projectName)
        {
            BuildPath = buildPath;
            ProjectName = projectName;
        }
    }

    public class CodeUtility
    {
        public static CodeBuildInfo BuildAndRunProject(string pathToSolution, string? framework = "", bool runProject = false)
        {
            CodeBuildInfo codeBuildInfo = new CodeBuildInfo(pathToSolution, "FunctionApplication");
            // Path to the target project's directory
            string projectPath = pathToSolution;

            // ... (Code for programmatically building the project as shown in the previous example) ...

            // After the build is successful:
            codeBuildInfo.BuildResult = BuildProject(FileIOUtility.SafeWorkingDirectory);

            if (codeBuildInfo.BuildResult.BuildCompleted)
            {
                Console.WriteLine("Build successful! Now running the built project...");
                // Find the executable file
                string executablePath = FindExecutable(projectPath, "FunctionApplication", framework);

                if (!string.IsNullOrEmpty(executablePath))
                {
                    if (runProject)
                    {
                        // Run the executable and capture its output
                        codeBuildInfo.ExecutableResult = RunExecutableAndCaptureOutput(executablePath);
                    }
                }
                else
                {
                    Console.WriteLine("Could not find the executable file.");
                }
            }
            else
            {
                Console.WriteLine("Build failed. Cannot run the built project.");
            }

            return codeBuildInfo;
        }

        // Function to find the executable file after build
        public static string FindExecutable(string projectPath, string projectName, string framework)
        {
            // Assuming a typical .NET Core/5+ project structure
            // You might need to adjust this based on your project setup
            string binPath = Path.Combine(projectPath, projectName, "bin", "Debug", framework); // Adjust framework if needed
            string executableName = $"{projectName}.exe"; // Replace with your executable name
            string executablePath = Path.Combine(binPath, executableName);

            if (File.Exists(executablePath))
            {
                return executablePath;
            }

            // Check the Release folder as well
            binPath = Path.Combine(projectPath, projectName, "bin", "Release", framework); // Adjust framework if needed
            executablePath = Path.Combine(binPath, executableName);

            if (File.Exists(executablePath))
            {
                return executablePath;
            }

            return null; // Executable not found
        }

        public class ExecutableOutputResult
        {
            private bool executionCompleted = false;

            public string Output { get; set; }
            public string Error { get; set; }
            public bool ExecutionCompleted { get => executionCompleted; set => executionCompleted = value; }

            public ExecutableOutputResult() { }

            public ExecutableOutputResult(string output, string error, bool completed)
            {
                Output = output;
                Error = error;
                ExecutionCompleted = completed;
            }
        }


        // Function to run the executable and capture its output
        public static ExecutableOutputResult RunExecutableAndCaptureOutput(string executablePath, string? arguments = "")
        {
            Process process = new Process();
            process.StartInfo.FileName = executablePath;
            process.StartInfo.Arguments = arguments; // Pass any arguments to the executable here
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            ExecutableOutputResult result = new ExecutableOutputResult();

            try
            {
                process.Start();

                // Read the output and error streams
                result.Output = process.StandardOutput.ReadToEnd();
                result.Error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine("Executable Output:");
                    Console.WriteLine(result.Output);
                }
                else
                {
                    Console.WriteLine("Executable Error:");
                    Console.WriteLine(result.Error);
                }
                result.ExecutionCompleted = true;
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running executable: {ex.Message}");
                result.ExecutionCompleted = false;
                return result;
            }
        }

        public class BuildOutputResult
        {
            public string Output { get; set; }
            public string Error { get; set; }
            public bool BuildCompleted { get; set; }

            public BuildOutputResult() { }

            public BuildOutputResult(string output, string error, bool completed)
            {
                Output = output;
                Error = error;
                BuildCompleted = completed;
            }
        }

        public static bool CreateNewProject(string projectName)
        {
            string zipPath = "Static//Files//FunctionApplicationTemplate.zip";
            string extractPath = FileIOUtility.SafeWorkingDirectory + "_TEMP";
            string finalPath = FileIOUtility.SafeWorkingDirectory;

            if (Directory.Exists(extractPath) || Directory.Exists(finalPath))
            {
                return false;
            }

            ZipFile.ExtractToDirectory(zipPath, extractPath); //Extract zip
            Directory.Move(Path.Combine(extractPath, "FunctionApplication"), finalPath); // Rename folder
            Directory.Delete(extractPath, true); //Delete temp folder

            return true;
        }

        // Function to build the project using dotnet CLI
        public static BuildOutputResult BuildProject(string path)
        {
            // Path to the target project's directory
            string projectPath = path;

            // Create a new process to run the dotnet build command
            Process process = new Process();
            process.StartInfo.FileName = "dotnet"; // Use "dotnet" command
            process.StartInfo.Arguments = $"build \"{projectPath}\""; // Arguments to build the project
            process.StartInfo.UseShellExecute = false; // Don't use the OS shell
            process.StartInfo.RedirectStandardOutput = true; // Redirect standard output to capture build output
            process.StartInfo.RedirectStandardError = true; // Redirect standard error to capture error messages
            process.StartInfo.CreateNoWindow = true; // Don't create a new window for the process

            BuildOutputResult result = new BuildOutputResult();

            try
            {
                // Start the process
                process.Start();

                // Read the build output (optional)
                result.Output = process.StandardOutput.ReadToEnd();
                result.Error = process.StandardError.ReadToEnd();

                // Wait for the process to exit
                process.WaitForExit();

                // Check the exit code to determine if the build was successful
                if (process.ExitCode == 0)
                {
                    Console.WriteLine("Build successful!");
                    Console.WriteLine(result.Output);
                    result.BuildCompleted = true;
                }
                else
                {
                    Console.WriteLine("Build failed!");
                    Console.WriteLine(result.Error);
                    result.BuildCompleted = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error occurred: {ex.Message}");
                result.BuildCompleted = false;
            }

            return result;
        }
    }
    #endregion
    #endregion
    #endregion
}


