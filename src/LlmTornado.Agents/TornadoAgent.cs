using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Common;
using LlmTornado.Responses;
using System;

namespace LlmTornado.Agents;

/// <summary>
/// Base Class to define agent behavior 
/// </summary>
public class TornadoAgent
{
    /// <summary>
    /// Which provider client to use
    /// </summary>
    public TornadoApi Client { get; set; }

    /// <summary>
    /// Gets or sets the chat model used for processing messages.
    /// </summary>
    public ChatModel Model { get; set; }

    /// <summary>
    /// chat options for the run
    /// </summary>
    public ChatRequest Options { get; set; } = new ChatRequest();

    /// <summary>
    /// Gets or sets the options used to configure the response behavior of the request.
    /// </summary>
    public ResponseRequest? ResponseOptions
    {
        get => Options.ResponseRequestParameters;
        set => Options.ResponseRequestParameters = value;
    }
    /// <summary>
    /// Name of the agent
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Instructions on how to process prompts
    /// </summary>
    public string Instructions { get; set; }

    /// <summary>
    /// Gets the unique identifier for this instance.
    /// </summary>
    public string Id { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Data Type to Format response output as
    /// </summary>
    public Type? OutputSchema { get; private set; }

    /// <summary>
    /// Tools available to the agent
    /// </summary>
    public List<Delegate>? Tools { get; set; } = new List<Delegate>();

    /// <summary>
    /// Gets or sets the permissions for tools, represented as a dictionary where the key is the tool name and the
    /// value indicates whether the tool requires permission to be used.
    /// </summary>
    public Dictionary<string, bool> ToolPermissionRequired = new Dictionary<string, bool>();

    /// <summary>
    /// Map of function tools to their methods
    /// </summary>
    public Dictionary<string, Tool> ToolList = new Dictionary<string, Tool>();

    /// <summary>
    /// Map of agent tools to their agents
    /// </summary>
    public Dictionary<string, TornadoAgentTool> AgentTools = new Dictionary<string, TornadoAgentTool>();

    /// <summary>
    /// MCP tols mapped to their servers
    /// </summary>
    public Dictionary<string, Tool> McpTools = new Dictionary<string, Tool>();


    /// <summary>
    /// Get agent runner events
    /// </summary>
    public Func<AgentRunnerEvents, ValueTask>? OnAgentRunnerEvent { private get; set; }

    /// <summary>
    /// Should the agent response be streamed.
    /// </summary>
    public bool Streaming { get; set; } = false;


    /// <summary>
    /// Initializes a new instance of the <see cref="TornadoAgent"/> class, which represents an AI agent capable of
    /// interacting with a Tornado API client and executing tasks based on provided instructions, tools, and an optional
    /// output schema.
    /// </summary>
    /// <remarks>This constructor sets up the agent with the specified configuration, including the AI model,
    /// optional instructions, and tools. If an output schema is provided, the agent will configure its response format
    /// accordingly. If tools are provided, they will be initialized for use by the agent.</remarks>
    /// <param name="client">The <see cref="TornadoApi"/> client used to communicate with the Tornado API.</param>
    /// <param name="model">The <see cref="ChatModel"/> that defines the AI model to be used by the agent.</param>
    /// <param name="instructions">Optional. A string containing the initial instructions for the agent. If not provided or empty, defaults to "You
    /// are a helpful assistant."</param>
    /// <param name="name">Optional. A string representing the name of the agent. Defaults to "Assistant" if not provided.</param>
    /// <param name="outputSchema">Optional. A <see cref="Type"/> representing the schema of the expected output. If provided, the agent will
    /// format its responses according to the specified schema.</param>
    /// <param name="tools">Optional. A list of <see cref="Delegate"/> instances representing tools or functions that the agent can use to
    /// perform specific tasks. If not provided, the agent will use its default tools.</param>
    /// <param name="mcpServers">A list of <see cref="MCPServer"/> instances for MCP Server tools.</param>
    /// <exception cref="ArgumentNullException">Thrown when client or model is null.</exception>
    public TornadoAgent(
        TornadoApi client,
        ChatModel model,
        string name = "Assistant",
        string instructions = "You are a helpful assistant",
        Type? outputSchema = null,
        List<Delegate>? tools = null,
        bool streaming = false,
        Dictionary<string, bool>? toolPermissionRequired = null)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));
        Model = model ?? throw new ArgumentNullException(nameof(model));

        Instructions = string.IsNullOrEmpty(instructions) ? "You are a helpful assistant" : instructions;
        OutputSchema = outputSchema;
        Tools = tools ?? Tools;
        Options.Model = model;
        Name = string.IsNullOrEmpty(name) ? "Assistant" : name;
        Streaming = streaming;
        ToolPermissionRequired = toolPermissionRequired ?? new Dictionary<string, bool>();

        if (OutputSchema != null)
        {
            Options.ResponseFormat = OutputSchema.CreateJsonSchemaFormatFromType();
        }

        //Setup tools and agent tools
        AutoSetupTools(Tools);
    }

    /// <summary>
    /// Update the output schema and response format
    /// </summary>
    /// <param name="newSchema"></param>
    public void UpdateOutputSchema(Type? newSchema, bool setStrict = true)
    {
        if (newSchema != null && newSchema != OutputSchema)
        {
            OutputSchema = newSchema;
            Options.ResponseFormat = OutputSchema.CreateJsonSchemaFormatFromType(setStrict);
        }
        else
        {
            OutputSchema = null;
            Options.ResponseFormat = null;
        }
    }

    /// <summary>
    /// Set up the provided methods as tools
    /// </summary>
    /// <param name="tools"></param>
    /// <exception cref="ArgumentNullException">Thrown when tools is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when tool setup fails.</exception>
    private void AutoSetupTools(List<Delegate>? tools)
    {
        if (tools == null || Tools?.Count == 0) return;

        Options.Tools ??= new List<Tool>();

        foreach (Delegate fun in tools)
        {
            //Convert Agent to tool
            if (fun.Method.Name.Equals("AsTool"))
            {
                GetAgentTool(fun);
            }
            else
            {
                GetTornadoTool(fun);
            }
        }
    }

    /// <summary>
    /// Use this to properly add a Tornado tool to both the agent's Options tool list and the global agent tools list.
    /// </summary>
    /// <param name="tool"></param>
    public void AddTornadoTool(Tool tool)
    {
        if (tool.Delegate != null)
        {
            if(ToolList.ContainsKey(tool.ToolName ?? tool.Function.Name)) return;
            SetDefaultToolPermission(tool);
            ToolList.Add(tool.ToolName ?? tool.Function.Name, tool);
            Options.Tools?.Add(tool);
        }
    }

    /// <summary>
    /// Use this to Properly add an agent tool to both the agent's Options tool list and the global agent tools list.
    /// </summary>
    /// <param name="tool"></param>
    public void AddAgentTool(TornadoAgentTool tool)
    {
        if (tool != null)
        {
            if(AgentTools.ContainsKey(tool.ToolAgent.Id)) return;
            SetDefaultToolPermission(tool.Tool);
            AgentTools.Add(tool.ToolAgent.Id, tool);
            Options.Tools?.Add(tool.Tool);
        }
    }

    /// <summary>
    ///  Adds a Model Context Protocol (MCP) tool to the agent's tool list.
    /// </summary>
    /// <param name="tool">MCP client tool</param>
    /// <param name="server">MCP Server where tool lives</param>
    public void AddMcpTools(Tool[] tools)
    {
        Options.Tools ??= new List<Tool>();

        if (tools.Length > 0)
        {
            foreach (var tool in tools)
            {
                string? name = tool.ToolName ?? tool.Function.Name ?? throw new InvalidOperationException("Tool name is required");
                if (McpTools.ContainsKey(name)) continue;
                SetDefaultToolPermission(tool);
                McpTools.Add(name, tool);
                ToolList.Add(name, tool);
                Options.Tools?.Add(tool);
            }
        }
    }

    public void ClearTools()
    {
        Options.Tools?.Clear();
        Options.ResponseRequestParameters?.Tools?.Clear();
        ToolList.Clear();
        AgentTools.Clear();
        McpTools.Clear();
    }

    private void GetTornadoTool(Delegate methodAsTool)
    {
        //Convert Method to tool
        Tool tool = methodAsTool.ConvertFunctionToTornadoTool();

        if (tool.Delegate != null) AddTornadoTool(tool);
    }

    private void GetAgentTool(Delegate agentAsTool)
    {
        //Creates the Chat tool for the agents running as tools and adds them to global list
        TornadoAgentTool? tool = (TornadoAgentTool?)agentAsTool.DynamicInvoke(); 
        
        if (tool != null)
        {
            AddAgentTool(tool);
        }
    }

    private void SetDefaultToolPermission(Tool tool)
    {
        if (tool.ToolName == null) return;
        if (!ToolPermissionRequired.ContainsKey(tool.ToolName))
        {
            ToolPermissionRequired.Add(tool.ToolName, false); //Default all tools to false
        }
    }

    /// <summary>
    /// Executes the conversation flow asynchronously, processing the input and managing interactions with the agent.
    /// </summary>
    /// <remarks>This method orchestrates the conversation flow by invoking the underlying runner with the
    /// provided parameters. It supports optional streaming, guardrail validation, and event handling for advanced
    /// scenarios.</remarks>
    /// <param name="input">The initial input message to start the conversation. Defaults to an empty string if not provided.</param>
    /// <param name="appendMessages">A list of additional chat messages to append to the conversation context. Can be <see langword="null"/>.</param>
    /// <param name="inputGuardRailFunction">An optional guardrail function to validate or modify the input before processing. Can be <see langword="null"/>.</param>
    /// <param name="streaming">A value indicating whether the response should be streamed. <see langword="true"/> to enable streaming;
    /// otherwise, <see langword="false"/>.</param>
    /// <param name="onAgentRunnerEvent">An optional callback function to handle agent runner events during execution. Can be <see langword="null"/>.</param>
    /// <param name="maxTurns">The maximum number of turns allowed in the conversation. Must be a positive integer. Defaults to 10.</param>
    /// <param name="responseId">An optional identifier for the response, used for response API chat. Defaults to an empty string.</param>
    /// <param name="toolPermissionHandle">An optional callback function to handle tool permission requests. Returns a <see langword="true"/> or <see
    /// langword="false"/> value indicating whether the tool is permitted. Can be <see langword="null"/>.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. Defaults to <see cref="CancellationToken.None"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation. The task result contains the updated <see
    /// cref="Conversation"/> object after processing.</returns>
    public async Task<Conversation> RunAsync(
        string input = "", 
        List<ChatMessage>? appendMessages = null, 
        GuardRailFunction? inputGuardRailFunction = null,
        bool? streaming = null,
        Func<AgentRunnerEvents, ValueTask>? onAgentRunnerEvent = null, 
        int maxTurns = 10, 
        string responseId = "",
        Func<string, ValueTask<bool>>? toolPermissionHandle = null, 
        bool singleTurn = false,
        TornadoRunnerOptions? runnerOptions = null,
        CancellationToken cancellationToken = default)
    {
        onAgentRunnerEvent += OnAgentRunnerEvent;
        return await TornadoRunner.RunAsync(this, 
            input: input, 
            messagesToAppend: appendMessages, 
            guardRail: inputGuardRailFunction, 
            singleTurn: singleTurn,
            cancellationToken: cancellationToken, 
            streaming: streaming ?? Streaming,
            runnerCallback: onAgentRunnerEvent, 
            maxTurns: maxTurns, 
            responseId: responseId, 
            runnerOptions: runnerOptions,
            toolPermissionHandle: toolPermissionHandle);
    }
}