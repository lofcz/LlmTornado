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
    
    public static async Task<ChatResult?> Completion4Mini()
    {
        ChatResult? result = await Program.Connect().Chat.CreateChatCompletionAsync(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.OMini,
            ResponseFormat = ChatRequestResponseFormats.Json,
            Messages = [
                new ChatMessage(ChatMessageRoles.System, "Solve the math problem given by user, respond in JSON format."),
                new ChatMessage(ChatMessageRoles.User, "2+2=?")
            ]
        });

        Console.WriteLine(result?.Choices?.Count > 0 ? result.Choices?[0].Message?.Content : "no response");
        
        return result;
    }
    
    public static async Task<ChatResult?> CompletionGroq()
    {
        ChatResult? result = await Program.Connect().Chat.CreateChatCompletionAsync(new ChatRequest
        {
            Model =  ChatModel.Groq.Meta.Llama370B,
            ResponseFormat = ChatRequestResponseFormats.Json,
            Messages = [
                new ChatMessage(ChatMessageRoles.System, "Solve the math problem given by user, respond in JSON format."),
                new ChatMessage(ChatMessageRoles.User, "2+2=?")
            ]
        });

        Console.WriteLine(result?.Choices?.Count > 0 ? result.Choices?[0].Message?.Content : "no response");
        
        return result;
    }

    public static async Task Completion4OStructuredJson()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.O240806,
            ResponseFormat = ChatRequestResponseFormats.StructuredJson("get_weather", new
            {
                type = "object",
                properties = new
                {
                    city = new
                    {
                        type = "string"
                    }
                },
                required = new List<string> { "city" },
                additionalProperties = false
            }, true)
        });
        chat.AppendUserInput("what is 2+2, also what is the weather in prague"); // user asks something unrelated, but we force the model to use the tool

        ChatRichResponse response = await chat.GetResponseRich();
        int z = 1;
    }
    
    public static async Task<bool> ChatFunctionRequired()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.O240806,
            Tools = new List<Tool>
            {
                new Tool(new ToolFunction("get_weather", "gets the current weather"), true)
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
            Model = ChatModel.Cohere.Command.Default,
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
        await InternalFunctionsStreamingInteractive(LLmProviders.OpenAi, ChatModel.OpenAi.Gpt4.O);
    }
    
    public static async Task AnthropicFunctionsStreamingInteractive()
    {
        await InternalFunctionsStreamingInteractive(LLmProviders.Anthropic, ChatModel.Anthropic.Claude3.Sonnet);
    }
    
    public static async Task CohereFunctionsStreamingInteractive()
    {
        await InternalFunctionsStreamingInteractive(LLmProviders.Cohere, ChatModel.Cohere.Command.Default);
    }

    private static async Task InternalFunctionsStreamingInteractive(LLmProviders provider, ChatModel model)
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
        Conversation chat = Program.Connect(provider).Chat.CreateConversation(new ChatRequest
        {
            Model = model,
            Tools = compiler.GetFunctions(),
            StreamOptions = ChatStreamOptions.KnownOptionsIncludeUsage
        });
        
        Console.WriteLine("Try asking for weather in one of the supported cities: Prague, Bratislava, Paris. Try asking for multiple cities in one turn!");

        // 3. repl
        while (true)
        {
            // 3.1 read input
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
            
            // 3.2 stream the response from llm
            await StreamResponse();
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
                },
                OnUsageReceived = async (usage) =>
                {
                    Console.WriteLine($"[used tokens: input - {usage.PromptTokens}, output - {usage.CompletionTokens}, total - {usage.TotalTokens}]");
                }
            });
        }
    }
    
    public static async Task CrossVendorFunctionsStreamingInteractive()
    {
        ChatModel startModel = ChatModel.OpenAi.Gpt4.O;
        
        // 1. set up a sample tool using strongly typed model
        ChatPluginCompiler compiler = new ChatPluginCompiler();
        compiler.SetFunctions([
            new ChatPluginFunction("get_weather", "gets the current weather in one given city", [
                new ChatFunctionParam("city_name", "name of the city", ChatPluginFunctionAtomicParamTypes.String)
            ])
        ]);
        
        // 2. in this scenario, the conversation starts with the user asking for the current weather in two of the supported cities.
        // we can try asking for the weather in the third supported city (Paris) later.
        Conversation chat = Program.ConnectMulti().Chat.CreateConversation(new ChatRequest
        {
            Model = startModel,
            Tools = compiler.GetFunctions(),
            StreamOptions = ChatStreamOptions.KnownOptionsIncludeUsage
        });
        
        Console.WriteLine("Try asking for weather in one of the supported cities: Prague, Tokyo, Paris. Try asking for multiple cities in one turn!");
        Console.WriteLine("Special commands: q(uit) - exit REPL, openai - switch to OpenAI, anthropic - switch to Anthropic, cohere - switch to Cohere");
        Console.WriteLine($"Current model is: {startModel.Name}");

        // in real application this would be some async service
        Dictionary<string, string> mockCities = new Dictionary<string, string>
        {
            { "prague", "A mild rain" },
            { "tokyo", "Foggy, cloudy" },
            { "paris", "A sunny day" },
        };
        
        // 3. repl
        while (true)
        {
            // 3.1 read input
            while (true)
            {
                Console.WriteLine();
                Console.Write("> ");
                string? input = Console.ReadLine();
                string normalized = input?.ToLowerInvariant() ?? string.Empty;

                switch (normalized)
                {
                    case "q" or "quit":
                        return;
                    case "openai":
                        chat.Model = ChatModel.OpenAi.Gpt4.O;
                        Console.WriteLine($"Switched to model: {chat.Model.Name}");
                        continue;
                    case "cohere":
                        chat.Model = ChatModel.Cohere.Command.Default;
                        Console.WriteLine($"Switched to model: {chat.Model.Name}");
                        continue;
                    case "anthropic":
                        chat.Model = ChatModel.Anthropic.Claude3.Sonnet;
                        Console.WriteLine($"Switched to model: {chat.Model.Name}");
                        continue;
                }

                if (!string.IsNullOrWhiteSpace(input))
                {
                    chat.AppendUserInput(input);
                    break;
                }
            }
            
            // 3.2 stream the response from llm
            await StreamResponse();
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
                            weather = mockCities.GetValueOrDefault(cityName.ToLowerInvariant(), "A blizzard is expected")
                        }, null, true);
                    }
                },
                AfterFunctionCallsResolvedHandler = async (fnResults, handler) =>
                {
                    await chat.StreamResponseRich(handler);
                },
                OnUsageReceived = async (usage) =>
                {
                    Console.WriteLine($"[used tokens: input - {usage.PromptTokens}, output - {usage.CompletionTokens}, total - {usage.TotalTokens}]");
                },
                OutboundHttpRequestHandler = (call) =>
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine($"{call.Method} {call.Url}");
                    Console.ResetColor();
                    return Task.CompletedTask;
                }
            });
        }
    }
    
    public static async Task CohereWebSearchStreaming()
    {
        Conversation chat = Program.Connect(LLmProviders.Cohere).Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Cohere.Command.Default,
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
    
    public static async Task GoogleStream()
    {
        Conversation chat = Program.Connect(LLmProviders.Google).Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.Gemini.Gemini15Flash
        });
        chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat.AppendUserInput("Who are you?");

        Console.WriteLine("Google:");
        await chat.StreamResponse(Console.Write);
    }
    
    public static async Task Google()
    {
        Conversation chat = Program.Connect(LLmProviders.Google).Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.Gemini.Gemini15Flash
        });
        chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat.AppendUserInput("Who are you?");

        string? str = await chat.GetResponse();

        Console.WriteLine("Google:");
        Console.WriteLine(str);
    }
    
    public static async Task OpenAiO1()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.O1Mini,
            MaxTokens = 2000
        });
        chat.AppendUserInput("Who are you?");

        string? str = await chat.GetResponse();

        Console.WriteLine("OpenAI O1:");
        Console.WriteLine(str);
    }

    public static async Task Cohere()
    {
        Conversation chat = Program.Connect(LLmProviders.Cohere).Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Cohere.Command.Default
        });
        chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat.AppendUserInput("Who are you?");

        string? str = await chat.GetResponse();

        Console.WriteLine("Cohere:");
        Console.WriteLine(str);
    }
    
    public static async Task Cohere2408()
    {
        Conversation chat = Program.Connect(LLmProviders.Cohere).Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Cohere.Command.RPlus2408,
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Cohere = new ChatRequestVendorCohereExtensions
                {
                    SafetyMode = ChatVendorCohereExtensionSafetyMode.None
                }
            }
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
            ChatModel.Anthropic.Claude35.Sonnet241022,
            ChatModel.Cohere.Command.Default,
            ChatModel.Google.Gemini.Gemini15Flash
        ];
        
        foreach (ChatModel model in models)
        {
            Console.WriteLine($"{model.Name}:");
            
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
            Model = ChatModel.Cohere.Command.Default
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
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
            {
                Model = ChatModel.OpenAi.Gpt4.O,
                Tools =
                [
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
                ],
                MaxTokens = 256
            })
            .AppendSystemMessage("You are a helpful assistant")
            .AppendUserInput("What is the weather like today in Prague?");

        ChatStreamEventHandler handler = new ChatStreamEventHandler
        {
            MessageTokenHandler = (x) =>
            {
                Console.Write(x);
                return Task.CompletedTask;
            },
            FunctionCallHandler = (calls) =>
            {
                calls.ForEach(x => x.Result = new FunctionResult(x, "A mild rain is expected around noon.", null));
                return Task.CompletedTask;
            },
            AfterFunctionCallsResolvedHandler = async (results, handler) => { await chat.StreamResponseRich(handler); }
        };

        await chat.StreamResponseRich(handler);
    }
    
    public static async Task OpenAiDisableParallelFunctions()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
            {
                Model = ChatModel.OpenAi.Gpt4.O,
                Tools =
                [
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
                ],
                ParallelToolCalls = false
            })
            .AppendSystemMessage("You are a helpful assistant")
            .AppendUserInput("What is the weather like today in Prague and Paris?");

        ChatStreamEventHandler handler = new ChatStreamEventHandler
        {
            MessageTokenHandler = (x) =>
            {
                Console.Write(x);
                return Task.CompletedTask;
            },
            FunctionCallHandler = (calls) =>
            {
                calls.ForEach(x => x.Result = new FunctionResult(x, "A mild rain is expected around noon.", null));
                return Task.CompletedTask;
            },
            AfterFunctionCallsResolvedHandler = async (results, handler) => { await chat.StreamResponseRich(handler); }
        };

        await chat.StreamResponseRich(handler);
    }
    
    public static async Task AnthropicFunctionsParallel()
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
            ],
            ToolChoice = new OutboundToolChoice("get_weather")
        });

        ChatStreamEventHandler eventsHandler = new ChatStreamEventHandler
        {
            MessageTokenHandler = (x) =>
            {
                Console.Write(x);
                return Task.CompletedTask;
            },
            FunctionCallHandler = (functions) =>
            {
                foreach (FunctionCall fn in functions)
                {
                    if (fn.TryGetArgument("location", out string? str) && str.ToLowerInvariant() is "prague")
                    {
                        fn.Result = new FunctionResult(fn.Name, "A mild rain is expected around noon.");  
                    }
                    else
                    {
                        fn.Result = new FunctionResult(fn.Name, "A sunny, hot day is expected, 28 \u00b0C");
                    }
                }

                return Task.CompletedTask;
            }
        };

        chat.OnAfterToolsCall = async (result) =>
        {
            chat.RequestParameters.ToolChoice = null; // stop forcing the model to use the get_weather tool
            await chat.StreamResponseRich(eventsHandler);
        };
        
        chat.AppendMessage(ChatMessageRoles.System, "You are a helpful assistant");
        chat.AppendMessage(ChatMessageRoles.User, "What is the weather like today in Prague and Paris?");

        await chat.StreamResponseRich(eventsHandler);
    }
    
    public static async Task GoogleFunctions()
    {
        Conversation chat = Program.Connect(LLmProviders.Google).Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.Gemini.Gemini15Flash,
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
            ],
            ToolChoice = new OutboundToolChoice("get_weather")
        });

        chat.OnAfterToolsCall = async (result) =>
        {
            chat.RequestParameters.ToolChoice = null; // stop forcing the model to use the get_weather tool
            string? str = await chat.GetResponse();

            if (str is not null)
            {
                Console.WriteLine(str);
            }
        };
        
        chat.AppendMessage(ChatMessageRoles.System, "You are a helpful assistant");
        Guid msgId = Guid.NewGuid();
        chat.AppendMessage(ChatMessageRoles.User, "Fetch the weather information for Prague and Paris.", msgId);

        ChatRichResponse response = await chat.GetResponseRich(functions =>
        {
            foreach (FunctionCall fn in functions)
            {
                fn.Result = new FunctionResult(fn.Name, "A mild rain is expected around noon.");
            }

            return Task.CompletedTask;
        });

        string r = response.GetText();
        Console.WriteLine(r);
    }
    
    public static async Task AnthropicStreamingFunctions()
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
            ],
            ToolChoice = new OutboundToolChoice("get_weather")
        });

        chat.OnAfterToolsCall = async (result) =>
        {
            chat.RequestParameters.ToolChoice = null; // stop forcing the model to use the get_weather tool
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
            foreach (FunctionCall fn in functions)
            {
                fn.Result = new FunctionResult(fn.Name, "A mild rain is expected around noon.");
            }

            return Task.CompletedTask;
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
            return Task.CompletedTask;
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
    }

    public static async Task GroqStreaming()
    {
        foreach (IModel x in ChatModel.Groq.AllModels)
        {
            Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
            {
                Model = (ChatModel)x
            });
            chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
            chat.AppendUserInput("Who are you?");

            Console.WriteLine($"{x.Name} ({x.ApiName}):");
            await chat.StreamResponse(Console.Write);
            Console.WriteLine();   
            Console.WriteLine();
        }
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