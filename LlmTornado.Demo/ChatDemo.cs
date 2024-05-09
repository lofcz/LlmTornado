using System.Text;
using Newtonsoft.Json;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Plugins;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Common;

namespace LlmTornado.Demo;

public static class ChatDemo
{
    public static async Task<ChatResult?> Completion()
    {
        ChatResult? result = await Program.Connect().Chat.CreateChatCompletionAsync(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Turbo,
            ResponseFormat = ChatRequestResponseFormats.Json,
            Messages = [
                new ChatMessage(ChatMessageRoles.System, "Solve the math problem given by user, respond in JSON format."),
                new ChatMessage(ChatMessageRoles.User, "2+2=?")
            ]
        });

        Console.WriteLine(result?.Choices?.Count > 0 ? result.Choices?[0].Message?.Content : "no response");
        
        return result;
    }
    
    public static async Task<bool> ChatFunctionRequired()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Turbo,
            Tools = new List<Tool>
            {
                new Tool
                {
                    Function = new ToolFunction("get_weather", "gets the current weather")
                }
            },
            ToolChoice = new OutboundToolChoice(OutboundToolChoiceModes.Required)
        });
        chat.AppendUserInput("Who are you?"); // user asks something unrelated, but we force the model to use the tool

        ChatRichResponse response = await chat.GetResponseRich();

        if (response.Blocks.Any(x => x.Type is ChatRichResponseBlockTypes.Function))
        {
            return true;
        }

        return false;
    }
    
    public static async Task CohereWebSearch()
    {
        Conversation chat = Program.Connect(LLmProviders.Cohere).Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Cohere.CommandRPlus,
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorCohereExtensions([
                ChatVendorCohereExtensionConnector.WebConnector
            ]))
        });
        
        chat.AppendSystemMessage("You are a helpful assistant connected to the internet tasked with fetching the latest information as requested by the user.");
        chat.AppendUserInput("Search for the latest version of .net core, including preview version. Respond with the latest version number and date of release.");

        ChatRichResponse response = await chat.GetResponseRich();
        List<VendorCohereCitationBlock>? blocks = response.VendorExtensions?.Cohere?.ParseCitations();
        
        Console.WriteLine("Raw:");
        Console.WriteLine(response.Text);

        if (blocks is not null)
        {
            Console.WriteLine("Structured:");
            
            foreach (VendorCohereCitationBlock block in blocks)
            {
                if (block.Citation is not null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                
                Console.Write(block.Text);
                Console.ResetColor();
            }
        }
    }

    public static async Task OpenAiFunctionsStreamingInteractive()
    {
        // 1. set up a sample tool using strongly typed model
        ChatPluginCompiler compiler = new ChatPluginCompiler();
        compiler.SetFunctions([
            new ChatPluginFunction("get_weather", "gets the current weather in a given city", [
                new ChatFunctionParam("city_name", "name of the city", ChatPluginFunctionAtomicParamTypes.String)
            ])
        ]);
        
        // 2. in this scenario, the conversation starts with the user asking for the current weather in two of the supported cities.
        // we can try asking for the weather in the third supported city (Paris) later.
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Turbo,
            Tools = compiler.GetFunctions()
        }).AppendUserInput("Please call functions get_weather for Prague and Bratislava (two function calls).");

        // 3. repl
        while (true)
        {
            // 3.1 stream the response from llm
            await StreamResponse();

            // 3.2 read input
            while (true)
            {
                Console.WriteLine();
                Console.Write("> ");
                string? input = Console.ReadLine();

                if (input?.ToLowerInvariant() is "q" or "quit")
                {
                    return;
                }
                
                if (!string.IsNullOrWhiteSpace(input))
                {
                    chat.AppendUserInput(input);
                    break;
                }
            }
        }

        async Task StreamResponse()
        {
            await chat.StreamResponseRich(new ChatStreamEventHandler
            {
                MessageTokenHandler = async (token) =>
                {
                    Console.Write(token);
                },
                FunctionCallHandler = async (fnCalls) =>
                {
                    foreach (FunctionCall x in fnCalls)
                    {
                        if (!x.TryGetArgument("city_name", out string? cityName))
                        {
                            x.Result = new FunctionResult(x, new
                            {
                                result = "error",
                                message = "expected city_name argument"
                            }, null, true);
                            continue;
                        }

                        x.Result = new FunctionResult(x, new
                        {
                            result = "ok",
                            weather = cityName.ToLowerInvariant() is "prague" ? "A mild rain" : cityName.ToLowerInvariant() is "paris" ? "Foggy, cloudy" : "A sunny day"
                        }, null, true);
                    }
                },
                AfterFunctionCallsResolvedHandler = async (fnResults, handler) =>
                {
                    await chat.StreamResponseRich(handler);
                }
            });
        }
    }
    
    public static async Task CohereWebSearchStreaming()
    {
        Conversation chat = Program.Connect(LLmProviders.Cohere).Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Cohere.CommandRPlus,
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorCohereExtensions([
                ChatVendorCohereExtensionConnector.WebConnector
            ]))
        });
        
        chat.AppendSystemMessage("You are a helpful assistant connected to the internet tasked with fetching the latest information as requested by the user.");
        chat.AppendUserInput("Search for the latest version of .net core, including preview version. Respond with the latest version number and date of release.");

        StringBuilder sb = new StringBuilder();
        
        await chat.StreamResponseRich((str) =>
        {
            sb.Append(str);
            Console.Write(str);
            return Task.CompletedTask;
        }, null, null, null, (ext) =>
        {
            if (ext.Cohere?.Citations is not null)
            {
                string str = sb.ToString();
                List<VendorCohereCitationBlock> blocks = ext.Cohere.ParseCitations(str);

                foreach (VendorCohereCitationBlock block in blocks)
                {
                    if (block.Citation is not null)
                    {
                        str = str.Replace(block.Text, $"<span>{block.Text}</span>");   
                    }
                }

                sb.Clear();
                sb.Append(str);

                try
                {
                    Console.Clear();
                }
                catch (Exception e)
                {
                    
                }
                
                Console.WriteLine(str);
            }
            
            return Task.CompletedTask;
        });
    }

    public static async Task Cohere()
    {
        Conversation chat = Program.Connect(LLmProviders.Cohere).Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Cohere.CommandRPlus
        });
        chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat.AppendUserInput("Who are you?");

        string? str = await chat.GetResponse();

        Console.WriteLine("Cohere:");
        Console.WriteLine(str);
    }

    public static async Task AllChatVendors()
    {
        TornadoApi api = new TornadoApi(new List<ProviderAuthentication>
        {
            new ProviderAuthentication(LLmProviders.OpenAi, Program.ApiKeys.OpenAi),
            new ProviderAuthentication(LLmProviders.Anthropic, Program.ApiKeys.Anthropic),
            new ProviderAuthentication(LLmProviders.Cohere, Program.ApiKeys.Cohere)
        });

        List<ChatModel> models =
        [
            ChatModel.OpenAi.Gpt4.Turbo,
            ChatModel.Anthropic.Claude3.Sonnet,
            ChatModel.Cohere.CommandRPlus
        ];
        
        foreach (ChatModel model in models)
        {
            string? response = await api.Chat.CreateConversation(model)
                .AppendSystemMessage("You are a fortune teller.")
                .AppendUserInput("What will my future bring?")
                .GetResponse();
            
            Console.WriteLine(response);
        }
    }
    
    public static async Task CohereStreaming()
    {
        Conversation chat = Program.Connect(LLmProviders.Cohere).Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Cohere.CommandRPlus
        });
        
        chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat.AppendUserInput("Bark 10 times. Each bark should look like: [BARK {{i}}]: {{random text here}}");

        Console.WriteLine("Cohere:");
        await chat.StreamResponse(Console.Write);
        Console.WriteLine();
    }

    public static async Task Anthropic()
    {
       Conversation chat = Program.Connect(LLmProviders.Anthropic).Chat.CreateConversation(new ChatRequest
       {
           Model = ChatModel.Anthropic.Claude3.Sonnet
       });
       chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
       chat.AppendUserInput("Who are you?");

       string? str = await chat.GetResponse();
       
       Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
       {
           Model = ChatModel.OpenAi.Gpt4.Turbo
       });
       chat2.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
       chat2.AppendUserInput("Who are you?");
       
       string? str2 = await chat2.GetResponse();

       Console.WriteLine("Anthropic:");
       Console.WriteLine(str);
       Console.WriteLine("OpenAI:");
       Console.WriteLine(str2);
    }
    
    public static async Task OpenAiFunctions()
    {
        StringBuilder sb = new StringBuilder();

        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Turbo,
            Tools = [
                new Tool(new ToolFunction("get_weather", "gets the current weather", new
                {
                    type = "object",
                    properties = new
                    {
                        location = new
                        {
                            type = "string",
                            description = "The location for which the weather information is required."
                        }
                    },
                    required = new List<string> { "location" }
                }))
            ]
        });

        chat.OnAfterToolsCall = async (result) =>
        {
            string? str = await chat.GetResponse();

            if (str is not null)
            {
                sb.Append(str);
            }
        };
        
        chat.AppendMessage(ChatMessageRoles.System, "You are a helpful assistant");
        Guid msgId = Guid.NewGuid();
        chat.AppendMessage(ChatMessageRoles.User, "What is the weather like today in Prague?", msgId);

        await chat.StreamResponseRich(msgId, (x) =>
        {
            sb.Append(x);
            return Task.CompletedTask;
        }, functions =>
        {
            List<FunctionResult> results = functions.Select(fn => new FunctionResult(fn.Name, "A mild rain is expected around noon.")).ToList();
            return Task.FromResult(results);
        }, null);


        string response = sb.ToString();
        Console.WriteLine(response);
    }
    
    public static async Task AnthropicFunctions()
    {
        StringBuilder sb = new StringBuilder();

        Conversation chat = Program.Connect(LLmProviders.Anthropic).Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude3.Sonnet,
            Tools = [
                new Tool(new ToolFunction("get_weather", "gets the current weather", new
                {
                    type = "object",
                    properties = new
                    {
                        location = new
                        {
                            type = "string",
                            description = "The location for which the weather information is required."
                        }
                    },
                    required = new List<string> { "location" }
                }))
            ]
        });

        chat.OnAfterToolsCall = async (result) =>
        {
            string? str = await chat.GetResponse();

            if (str is not null)
            {
                sb.Append(str);
            }
        };
        
        chat.AppendMessage(ChatMessageRoles.System, "You are a helpful assistant");
        Guid msgId = Guid.NewGuid();
        chat.AppendMessage(ChatMessageRoles.User, "What is the weather like today in Prague?", msgId);

        await chat.StreamResponseRich(msgId, (x) =>
        {
            sb.Append(x);
            return Task.CompletedTask;
        }, functions =>
        {
            List<FunctionResult> results = [];

            foreach (FunctionCall fn in functions)
            {
                results.Add(new FunctionResult(fn.Name, "A mild rain is expected around noon."));
            }

            return Task.FromResult(results);
        }, null, null);


        string response = sb.ToString();
        Console.WriteLine(response);
    }

    public static async Task AnthropicFailFunctions()
    {
        StringBuilder sb = new StringBuilder();

        Conversation chat = Program.Connect(LLmProviders.Anthropic).Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude3.Sonnet,
            Tools = [
                new Tool(new ToolFunction("get_weather", "gets the current weather", new
                {
                    type = "object",
                    properties = new
                    {
                        location = new
                        {
                            type = "string",
                            description = "The location for which the weather information is required."
                        }
                    },
                    required = new List<string> { "location" }
                }))
            ]
        });

        chat.OnAfterToolsCall = async (result) =>
        {
            string? str = await chat.GetResponse();

            if (str is not null)
            {
                sb.Append(str);
            }
        };
        
        chat.AppendMessage(ChatMessageRoles.System, "You are a helpful assistant");
        Guid msgId = Guid.NewGuid();
        chat.AppendMessage(ChatMessageRoles.User, "What is the weather like today in Prague?", msgId);

        await chat.StreamResponseRich(msgId, (x) =>
        {
            sb.Append(x);
            return Task.CompletedTask;
        }, functions =>
        {
            List<FunctionResult> results = [];

            foreach (FunctionCall fn in functions)
            {
                results.Add(new FunctionResult(fn, "Service not available.", null, false));
            }

            return Task.FromResult(results);
        }, null, null);


        string response = sb.ToString();
        Console.WriteLine(response);
    }

    
    public static async Task Azure()
    {
        Conversation chat = Program.Connect(LLmProviders.AzureOpenAi).Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Default
        });
        
        chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat.AppendUserInput("Who are you?");
        
        string? str = await chat.GetResponse();

        Console.WriteLine("Azure OpenAI:");
        Console.WriteLine(str);
    }

    public static async Task AnthropicStreaming()
    {
        Conversation chat = Program.Connect(LLmProviders.Anthropic).Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude3.Sonnet
        });
        chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat.AppendUserInput("Who are you?");

        Console.WriteLine("Anthropic:");
        await chat.StreamResponse(Console.Write);
        Console.WriteLine();
       
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Turbo
        });
        chat2.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat2.AppendUserInput("Who are you?");
       
        Console.WriteLine("OpenAI:");
        await chat2.StreamResponse(Console.Write);
        Console.WriteLine();
    }


    public static async Task<string> StreamWithFunctions()
    {
        StringBuilder sb = new StringBuilder();
        
        Conversation chat = Program.Connect().Chat.CreateConversation();
        chat.RequestParameters.Tools = [
            new Tool(new ToolFunction("get_weather", "gets the current weather", new
            {
                type = "object",
                properties = new
                {
                    arg1 = new
                    {
                        type = "string",
                        description = "argument 1 description"
                    }
                },
                required = new List<string> { "arg1" }
            }))
        ];
        chat.OnAfterToolsCall = async (result) =>
        {
            string? str = await chat.GetResponse();

            if (str is not null)
            {
                sb.Append(str);
            }
        };
        chat.AppendMessage(ChatMessageRoles.System, "You are a helpful assistant");
        
        Guid msgId = Guid.NewGuid(); 
        chat.AppendMessage(ChatMessageRoles.User, "What is the weather like today?", msgId);
        
        await chat.StreamResponseRich(msgId, (x) =>
        {
            sb.Append(x);
            return Task.CompletedTask;
        }, functions =>
        {
            List<FunctionResult> results = [];
            
            foreach (FunctionCall fn in functions)
            {
                results.Add(new FunctionResult(fn.Name, new
                {
                    result = "ok",
                    weather = "A mild rain is expected around noon."
                }));
            }

            return Task.FromResult(results);
        }, null, null);


        string response = sb.ToString();
        return response;
    }
}