using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Common;
using LlmTornado.Responses;
using ModelContextProtocol.Client;

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
    public List<Delegate>? Tools { get; set; } = [];

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
    public List<AgentHandoff> HandoffAgents { get; set; }

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
    public TornadoAgent(
        TornadoApi client, 
        ChatModel model, 
        string instructions = "You are a helpful assistant", 
        Type? outputSchema = null, 
        List<Delegate>? tools = null, 
        List<AgentHandoff>? handoffs = null,
        List<MCPServer>? mcpServers = null)
    {
        Client = client;
        Instructions = instructions;
        OutputSchema = outputSchema;
        Tools = tools ?? Tools;
        Instructions = string.IsNullOrEmpty(instructions)? "You are a helpful assistant" : instructions;
        Model = model;
        Options.Model = model;
        McpServers = mcpServers ?? [];
        HandoffAgents = handoffs?.ToList() ?? [];

        if (OutputSchema != null)
        {
            Options.ResponseFormat = OutputSchema.CreateJsonSchemaFormatFromType(true);
        }

        //Setup tools and agent tools
        if (Tools?.Count > 0)
        {
            SetupTools(Tools);
        }
    }

    /// <summary>
    /// Set up the provided methods as tools
    /// </summary>
    /// <param name="tools"></param>
    private void SetupTools(List<Delegate> tools)
    {
        Options.Tools ??= [];
        
        foreach (Delegate fun in tools)
        {
            //Convert Agent to tool
            if (fun.Method.Name.Equals("AsTool"))
            {
                TornadoAgentTool? tool = (TornadoAgentTool?)fun.DynamicInvoke(); //Creates the Chat tool for the agents running as tools and adds them to global list
                //Add agent tool to context list
                if (tool != null)
                {
                    AgentTools.Add(tool.ToolAgent.Id, tool);
                    Options.Tools?.Add(tool.Tool);
                }
            }
            else
            {
                //Convert Method to tool
                Tool tool = fun.ConvertFunctionToTornadoTool();
                
                ToolList.Add(tool.Delegate.Method.Name, tool);
                Options.Tools?.Add(tool);
            }
        }

        foreach (MCPServer server in McpServers)
        {
            foreach (McpClientTool tool in server.Tools)
            {
                McpTools.Add(tool.Name, server);
                
                if (!ToolPermissionRequired.ContainsKey(tool.Name))
                {
                    ToolPermissionRequired.Add(tool.Name, false); //Default all mcp tools to false
                }
                
                Tool mcpTool = new Tool(new ToolFunction(tool.Name, tool.Description, tool.JsonSchema));
                ToolList.Add(tool.Name, mcpTool);
                Options.Tools?.Add(mcpTool);
            }
        }
    }
}