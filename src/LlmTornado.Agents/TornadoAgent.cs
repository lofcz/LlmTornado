using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Common;
using LlmTornado.Responses;
using ModelContextProtocol.Client;
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
    /// Instructions on how to process prompts
    /// </summary>
    public string Instructions { get; set; }

    /// <summary>
    /// Description of the agent's purpose or functionality for Orchestration purposes.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets the unique identifier for this instance.
    /// </summary>
    public string Id { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Data Type to Format response output as
    /// </summary>
    public Type? OutputSchema { get; set; }

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

    public Dictionary<string, MCPServer> McpTools = new Dictionary<string, MCPServer>();

    public List<MCPServer> McpServers;

    /// <summary>
    /// Agents that are handed off to be the controller 
    /// </summary>
    public List<TornadoAgent> HandoffAgents { get; set; }

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
    /// <param name="outputSchema">Optional. A <see cref="Type"/> representing the schema of the expected output. If provided, the agent will
    /// format its responses according to the specified schema.</param>
    /// <param name="tools">Optional. A list of <see cref="Delegate"/> instances representing tools or functions that the agent can use to
    /// perform specific tasks. If not provided, the agent will use its default tools.</param>
    /// <param name="handoffs">Optional. A list of <see cref="AgentHandoff"/> instances representing possible hand-offs to other agents.</param>
    /// <param name="mcpServers">A list of <see cref="MCPServer"/> instances for MCP Server tools.</param>
    /// <exception cref="ArgumentNullException">Thrown when client or model is null.</exception>
    public TornadoAgent(
        TornadoApi client,
        ChatModel model,
        string instructions = "You are a helpful assistant",
        string? description = "",
        Type? outputSchema = null,
        List<Delegate>? tools = null,
        List<TornadoAgent>? handoffs = null,
        List<MCPServer>? mcpServers = null)
    {
        Client = client ?? throw new ArgumentNullException(nameof(client));
        Model = model ?? throw new ArgumentNullException(nameof(model));

        Instructions = string.IsNullOrEmpty(instructions) ? "You are a helpful assistant" : instructions;
        OutputSchema = outputSchema;
        Tools = tools ?? Tools;
        Options.Model = model;
        McpServers = mcpServers ?? new List<MCPServer>();
        HandoffAgents = handoffs?.ToList() ?? new List<TornadoAgent>();
        Description = description ?? "No description provided";

        if (OutputSchema != null)
        {
            Options.ResponseFormat = OutputSchema.CreateJsonSchemaFormatFromType(true);
        }

        //Setup tools and agent tools
        AutoSetupTools(Tools);
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

        GetMcpTools();
    }

    /// <summary>
    /// Use this to properly add a Tornado tool to both the agent's Options tool list and the global agent tools list.
    /// </summary>
    /// <param name="tool"></param>
    public void AddTornadoTool(Tool tool)
    {
        if (tool.Delegate != null)
        {
            SetDefaultToolPermission(tool);
            ToolList.Add(tool.Delegate.Method.Name, tool);
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
    public void AddMcpTool(McpClientTool tool, MCPServer server)
    {
        if (tool != null && server != null)
        {
            McpTools.Add(tool.Name, server);
            AddTornadoTool(new Tool(new ToolFunction(tool.Name, tool.Description, tool.JsonSchema)));
        }
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

    private void GetMcpTools()
    {
        foreach (MCPServer server in McpServers)
        {
            try
            {
                server.Tools.ForEach(tool => AddMcpTool(tool, server));
            }
            catch (Exception ex)
            {
                //throw new InvalidOperationException($"Failed to setup MCP server {server.ServerLabel}: {ex.Message}", ex);
                Console.WriteLine($"Failed to setup MCP server {server.ServerLabel}: {ex.Message}");
                continue; // Skip this server and continue with others
            }
        }
    }



    public async Task<Conversation> RunAsync(
        string input, 
        Conversation? conversation = null, 
        GuardRailFunction? guardRailFunction = null,
        RunnerVerboseCallbacks? runnerVerboseCallbacks = null, 
        CancellationToken cancellationToken = default,
        bool streaming = false, 
        StreamingCallbacks? streamingCallback = null, 
        int maxTurns = 10, 
        string responseId = "",
        ToolPermissionRequest? toolPermissionRequest = null)
    {
        return await TornadoRunner.RunAsync(this, input: input, conversation: conversation, guardRail:guardRailFunction,verboseCallback: runnerVerboseCallbacks, cancellationToken: cancellationToken, streaming: streaming,
            streamingCallback: streamingCallback, maxTurns: maxTurns, responseId: responseId, toolPermissionRequest:toolPermissionRequest);
    }
}