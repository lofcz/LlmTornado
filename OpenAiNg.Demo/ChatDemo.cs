using System.Text;
using Newtonsoft.Json;
using OpenAiNg.Chat;
using OpenAiNg.ChatFunctions;
using OpenAiNg.Code;
using OpenAiNg.Common;

namespace OpenAiNg.Demo;

public static class ChatDemo
{
    public static async Task<ChatResult> Completion()
    {
        ChatResult result = await Program.Connect().Chat.CreateChatCompletionAsync(new ChatRequest
        {
            Model = Models.Model.GPT4_Turbo_Preview,
            ResponseFormat = ChatRequestResponseFormats.Json,
            Messages = [
                new ChatMessage(ChatMessageRole.System, "Solve the math problem given by user, respond in JSON format."),
                new ChatMessage(ChatMessageRole.User, "2+2=?")
            ]
        });

        return result;
    }


    public static async Task Anthropic()
    {
       Conversation chat = Program.Connect(LLmProviders.Anthropic).Chat.CreateConversation(new ChatRequest
       {
           Model = Models.Model.Claude3Sonnet
       });
       chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
       chat.AppendUserInput("Who are you?");

       string? str = await chat.GetResponseFromChatbotAsync();
       
       Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
       {
           Model = Models.Model.GPT4_Turbo_Preview
       });
       chat2.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
       chat2.AppendUserInput("Who are you?");
       
       string? str2 = await chat2.GetResponseFromChatbotAsync();

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
            Model = Models.Model.GPT4_Turbo,
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
            string? str = await chat.GetResponseFromChatbotAsync();

            if (str is not null)
            {
                sb.Append(str);
            }
        };
        
        chat.AppendMessage(ChatMessageRole.System, "You are a helpful assistant");
        Guid msgId = Guid.NewGuid();
        chat.AppendMessage(ChatMessageRole.User, "What is the weather like today in Prague?", msgId);

        await chat.StreamResponseEnumerableFromChatbotAsyncWithFunctions(msgId, (x) =>
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
            Model = Models.Model.Claude3Sonnet,
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
            string? str = await chat.GetResponseFromChatbotAsync();

            if (str is not null)
            {
                sb.Append(str);
            }
        };
        
        chat.AppendMessage(ChatMessageRole.System, "You are a helpful assistant");
        Guid msgId = Guid.NewGuid();
        chat.AppendMessage(ChatMessageRole.User, "What is the weather like today in Prague?", msgId);

        await chat.StreamResponseEnumerableFromChatbotAsyncWithFunctions(msgId, (x) =>
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
            Model = Models.Model.Claude3Sonnet,
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
            string? str = await chat.GetResponseFromChatbotAsync();

            if (str is not null)
            {
                sb.Append(str);
            }
        };
        
        chat.AppendMessage(ChatMessageRole.System, "You are a helpful assistant");
        Guid msgId = Guid.NewGuid();
        chat.AppendMessage(ChatMessageRole.User, "What is the weather like today in Prague?", msgId);

        await chat.StreamResponseEnumerableFromChatbotAsyncWithFunctions(msgId, (x) =>
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
            Model = Models.Model.GPT4
        });
        
        chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat.AppendUserInput("Who are you?");
        
        string? str = await chat.GetResponseFromChatbotAsync();

        Console.WriteLine("Azure OpenAI:");
        Console.WriteLine(str);
    }

    public static async Task AnthropicStreaming()
    {
        Conversation chat = Program.Connect(LLmProviders.Anthropic).Chat.CreateConversation(new ChatRequest
        {
            Model = Models.Model.Claude3Sonnet
        });
        chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat.AppendUserInput("Who are you?");

        Console.WriteLine("Anthropic:");
        await chat.StreamResponseFromChatbotAsync(Console.Write);
        Console.WriteLine();
       
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = Models.Model.GPT4_Turbo_Preview
        });
        chat2.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat2.AppendUserInput("Who are you?");
       
        Console.WriteLine("OpenAI:");
        await chat2.StreamResponseFromChatbotAsync(Console.Write);
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
            string? str = await chat.GetResponseFromChatbotAsync();

            if (str is not null)
            {
                sb.Append(str);
            }
        };
        chat.AppendMessage(ChatMessageRole.System, "You are a helpful assistant");
        
        Guid msgId = Guid.NewGuid(); 
        chat.AppendMessage(ChatMessageRole.User, "What is the weather like today?", msgId);
        
        await chat.StreamResponseEnumerableFromChatbotAsyncWithFunctions(msgId, (x) =>
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