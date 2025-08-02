using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Responses;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;

namespace LlmTornado.Agents
{
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
        public ResponseRequest ResponseOptions { get; set; } = new ResponseRequest();


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
        public List<Delegate>? Tools { get; set; } = new();

        /// <summary>
        /// Gets or sets the permissions for tools, represented as a dictionary where the key is the tool name and the
        /// value indicates whether the tool requires permission to be used.
        /// </summary>
        public Dictionary<string, bool> ToolPermissionRequired = new();

        /// <summary>
        /// Map of function tools to their methods
        /// </summary>
        public Dictionary<string, Tool> ToolList = new();
        /// <summary>
        /// Map of agent tools to their agents
        /// </summary>
        public Dictionary<string, TornadoAgentTool> AgentTools = new();

        public Dictionary<string, MCPServer> McpTools = new();

        public List<MCPServer> McpServers = new();


        public static TornadoAgent DummyAgent()
        {
            TornadoApi client = new([new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);
            return new TornadoAgent(client, "");
        }
        public TornadoAgent(TornadoApi client, ChatModel model, string instructions = "", Type? outputSchema = null, List<Delegate>? tools = null)
        {
            Client = client;
            Instructions = string.IsNullOrEmpty(instructions) ? "You are a helpful assistant" : instructions;
            OutputSchema = outputSchema;
            Tools = tools ?? Tools;
            Instructions = instructions;
            Model = model;
            Options.Model = model;

            if (OutputSchema != null)
            {
                var format = OutputSchema.CreateJsonSchemaFormatFromType(true);
                dynamic? responseFormat = JsonConvert.DeserializeObject<dynamic>(format.JsonSchema.ToString());
                Options.ResponseFormat = ChatRequestResponseFormats.StructuredJson(format.JsonSchemaFormatName, responseFormat);
            }

            //Setup tools and agent tools
            if (Tools.Count > 0)
            {
                SetupTools(Tools);
            }
        }

        /// <summary>
        /// Setup the provided methods as tools
        /// </summary>
        /// <param name="Tools"></param>
        private void SetupTools(List<Delegate> Tools)
        {
            foreach (var fun in Tools)
            {
                //Convert Agent to tool
                if (fun.Method.Name.Equals("AsTool"))
                {
                    TornadoAgentTool? tool = (TornadoAgentTool?)fun.DynamicInvoke(); //Creates the Chat tool for the agents running as tools and adds them to global list
                                                                            //Add agent tool to context list
                    if (tool != null)
                    {
                        AgentTools.Add(tool.ToolAgent.Id, tool);
                        //Convert Tools here
                        if (Options.Tools == null) Options.Tools = new List<LlmTornado.Common.Tool>();
                        Options.Tools?.Add(tool.Tool);
                    }
                }
                else
                {
                    //Convert Method to tool
                    Tool? tool = fun.ConvertFunctionToTornadoTool();
                    if (tool != null)
                    {
                        ToolList.Add(tool.Delegate.Method.Name, tool);
                        if (Options.Tools == null) Options.Tools = new List<LlmTornado.Common.Tool>();
                        Options.Tools?.Add(tool);
                    }
                }
            }
        }


    }
}
