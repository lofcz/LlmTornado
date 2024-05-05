using System.Text;
using Newtonsoft.Json;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Common;

namespace LlmTornado.Demo;

public static class ChatDemo
{
    public static async Task<ChatResult> Completion()
    {
        ChatResult result = await Program.Connect().Chat.CreateChatCompletionAsync(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Turbo,
            ResponseFormat = ChatRequestResponseFormats.Json,
            Messages = [
                new ChatMessage(ChatMessageRole.System, "Solve the math problem given by user, respond in JSON format."),
                new ChatMessage(ChatMessageRole.User, "2+2=?")
            ]
        });

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
        
        chat.AppendMessage(ChatMessageRole.System, "You are a helpful assistant");
        Guid msgId = Guid.NewGuid();
        chat.AppendMessage(ChatMessageRole.User, "What is the weather like today in Prague?", msgId);

        await chat.StreamResponseEnumerableWithFunctions(msgId, (x) =>
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
        
        chat.AppendMessage(ChatMessageRole.System, "You are a helpful assistant");
        Guid msgId = Guid.NewGuid();
        chat.AppendMessage(ChatMessageRole.User, "What is the weather like today in Prague?", msgId);

        await chat.StreamResponseEnumerableWithFunctions(msgId, (x) =>
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
        
        chat.AppendMessage(ChatMessageRole.System, "You are a helpful assistant");
        Guid msgId = Guid.NewGuid();
        chat.AppendMessage(ChatMessageRole.User, "What is the weather like today in Prague?", msgId);

        await chat.StreamResponseEnumerableWithFunctions(msgId, (x) =>
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
        chat.AppendMessage(ChatMessageRole.System, "You are a helpful assistant");
        
        Guid msgId = Guid.NewGuid(); 
        chat.AppendMessage(ChatMessageRole.User, "What is the weather like today?", msgId);
        
        await chat.StreamResponseEnumerableWithFunctions(msgId, (x) =>
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