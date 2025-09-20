using System.Text;
using Flurl.Http;
using LlmTornado.Caching;
using Newtonsoft.Json;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Chat.Vendors.Google;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Common;
using LlmTornado.Contrib;
using LlmTornado.Files;
using LlmTornado.Images;


namespace LlmTornado.Demo;

public partial class ChatDemo : DemoBase
{
    [TornadoTest]
    public static async Task<ChatResult?> Completion()
    {
        ChatResult? result = await Program.Connect().Chat.CreateChatCompletion(new ChatRequest
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
    
    [TornadoTest]
    public static async Task<ChatResult?> CompletionO1Developer()
    {
        ChatResult? result = await Program.Connect().Chat.CreateChatCompletion(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.O1241217,
            ReasoningEffort = ChatReasoningEfforts.Low,
            ResponseFormat = ChatRequestResponseFormats.Json,
            Messages = [
                new ChatMessage(ChatMessageRoles.System, "Solve the math problem given by user, respond in JSON format."),
                new ChatMessage(ChatMessageRoles.User, "2+2=?")
            ]
        });

        Console.WriteLine(result?.Choices?.Count > 0 ? result.Choices?[0].Message?.Content : "no response");
        
        return result;
    }
    
    [TornadoTest]
    public static async Task<ChatResult?> Completion4Mini()
    {
        ChatResult? result = await Program.Connect().Chat.CreateChatCompletion(new ChatRequest
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
    
    [TornadoTest]
    public static async Task<ChatResult?> CompletionGroq()
    {
        ChatResult? result = await Program.Connect().Chat.CreateChatCompletion(new ChatRequest
        {
            Model =  ChatModel.Groq.Meta.Llama3370BVersatile,
            ResponseFormat = ChatRequestResponseFormats.Json,
            Messages = [
                new ChatMessage(ChatMessageRoles.System, "Solve the math problem given by user, respond in JSON format."),
                new ChatMessage(ChatMessageRoles.User, "2+2=?")
            ]
        });

        Console.WriteLine(result?.Choices?.Count > 0 ? result.Choices?[0].Message?.Content : "no response");
        
        return result;
    }

    [TornadoTest]
    public static async Task<ChatResult?> CompletionMoonshotAi()
    {
        ChatResult? result = await Program.Connect().Chat.CreateChatCompletion(new ChatRequest
        {
            Model = ChatModel.MoonshotAi.Models.KimiK2Instruct,
            Messages = [
                new ChatMessage(ChatMessageRoles.System, "You are a helpful assistant that provides concise answers."),
                new ChatMessage(ChatMessageRoles.User, "What is the capital of China?")
            ]
        });

        Console.WriteLine(result?.Choices?.Count > 0 ? result.Choices?[0].Message?.Content : "no response");
        
        return result;
    }

    [TornadoTest]
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
            })
        });
        
        chat.AppendUserInput("what is 2+2, also what is the weather in prague"); // user asks something unrelated, but we force the model to use the tool
    
        ChatRichResponse response = await chat.GetResponseRich();
        Console.Write(response);
    }
    
    [TornadoTest]
    public static async Task<bool> ChatFunctionRequired()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.O241120,
            Tools = [new Tool(new ToolFunction("get_weather", "gets the current weather"), true)],
            ToolChoice = new OutboundToolChoice(OutboundToolChoiceModes.Required)
        });
        chat.AppendUserInput("Who are you?"); // user asks something unrelated, but we force the model to use the tool

        ChatRichResponse response = await chat.GetResponseRich();

        ChatRichResponseBlock? block = response.Blocks?.FirstOrDefault(x => x.Type is ChatRichResponseBlockTypes.Function);

        if (block is not null)
        {
            Console.WriteLine($"fn block found: {block.FunctionCall?.Name}");
            return true;
        }
        
        return false;
    }
    
    [TornadoTest]
    public static async Task<bool> CohereTool()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Cohere.Command.A0325,
            Tools = [new Tool(new ToolFunction("get_weather", "gets the current weather"), true)],
            ToolChoice = new OutboundToolChoice(OutboundToolChoiceModes.Required),
            Messages = [
                new ChatMessage(ChatMessageRoles.User, "What is the weather like today?")
            ]
        });
 
        ChatRichResponse response = await chat.GetResponseRich();

        ChatRichResponseBlock? block = response.Blocks?.FirstOrDefault(x => x.Type is ChatRichResponseBlockTypes.Function);

        if (block is not null)
        {
            Console.WriteLine($"fn block found: {block.FunctionCall?.Name}");
            return true;
        }
        
        return false;
    }
    
    [TornadoTest]
    public static async Task<bool> ChatFunctionGemini()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.Gemini.Gemini15FlashLatest,
            Tools =
            [
                new Tool(new ToolFunction("get_order_details", "Gets details of a given order", new
                {
                    type = "object",
                    properties = new
                    {
                        id = new
                        {
                            type = "string",
                            description = "ID of the order"
                        }
                    },
                    required = new List<string> { "id" }
                }))
            ]
        });
        chat.AppendUserInput("Contents of order with id A7GDX?");

        ChatRichResponse response = await chat.GetResponseRich();

        ChatRichResponseBlock? block = response.Blocks.FirstOrDefault(x => x.Type is ChatRichResponseBlockTypes.Function);
        
        if (block is not null)
        {
            Console.WriteLine(block.FunctionCall?.Name);
            Console.WriteLine(block.FunctionCall?.Arguments);
            return true;
        }

        return false;
    }
    
    [TornadoTest]
    public static async Task<bool> ChatFunctionGeminiStrict()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.Gemini.Gemini15FlashLatest,
            Tools =
            [
                new Tool(new ToolFunction("get_order_details", "Gets details of a given order", new
                {
                    type = "object",
                    properties = new
                    {
                        id = new
                        {
                            type = "string",
                            description = "ID of the order"
                        }
                    },
                    required = new List<string> { "id" }
                }), true)
            ],
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorGoogleExtensions
            {
                SafetyFilters = ChatRequestVendorGoogleSafetyFilters.Default
            })
        });
        chat.AppendUserInput("Contents of order with id A7GDX?");

        ChatRichResponse response = await chat.GetResponseRich();

        ChatRichResponseBlock? block = response.Blocks.FirstOrDefault(x => x.Type is ChatRichResponseBlockTypes.Function);
        
        if (block is not null)
        {
            Console.WriteLine(block.FunctionCall?.Name);
            Console.WriteLine(block.FunctionCall?.Arguments);
            return true;
        }

        return false;
    }

    [Flaky("interactive")]
    [TornadoTest]
    public static async Task AnthropicCachingChat()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude35.SonnetLatest,
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
                {
                    VendorExtensions = new ToolVendorExtensions(new AnthropicToolVendorExtensions
                    {
                        Cache = AnthropicCacheSettings.Ephemeral
                    })
                }
            ],
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorAnthropicExtensions
            {
                OutboundRequest = (sys, msgs, tools) =>
                {
                    // we need to mark the last user message and the second-last user message as cached (for hitting cache & setting it for the next turn)
                    int marked = 0;

                    for (int i = msgs.Count - 1; i >= 0; i--)
                    {
                        VendorAnthropicChatRequestMessageContent msg = msgs[i];

                        if (msg.Role is ChatMessageRoles.User)
                        {
                            if (msg.Parts.Count > 0)
                            {
                                msg.Parts[0].VendorExtensions = new ChatMessagePartAnthropicExtensions
                                {
                                    Cache = AnthropicCacheSettings.Ephemeral
                                };

                                marked++;

                                if (marked is 2)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
            })
        });

        chat.OnAfterToolsCall = async (result) =>
        {
            Console.WriteLine();
            await chat.StreamResponse(Console.Write);
        };

        chat.AppendUserInput("fetch me the weather in Paris");

        await chat.StreamResponseRich(new ChatStreamEventHandler
        {
            MessageTokenHandler = (token) =>
            {
                Console.Write(token);
                return ValueTask.CompletedTask;
            },
            FunctionCallHandler = (functions) =>
            {
                foreach (FunctionCall fn in functions)
                {
                    fn.Result = new FunctionResult(fn.Name, new
                    {
                        result = "ok",
                        weather = "A mild rain is expected around noon in Paris."
                    });
                }

                return ValueTask.CompletedTask;
            },
            OnUsageReceived = (usage) =>
            {
                return ValueTask.CompletedTask;
            }
        });
    }

    [TornadoTest]
    public static async Task AnthropicCaching()
    {
        string longPrompt = await File.ReadAllTextAsync("Static/Files/pride_and_prejudice.txt");
        
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude35.SonnetLatest
        });
        
        chat.AppendSystemMessage([ 
            new ChatMessagePart("You are an assistant answering queries about the following text"),
            new ChatMessagePart(longPrompt, new ChatMessagePartAnthropicExtensions
            {
                Cache = AnthropicCacheSettings.EphemeralWithTtl(AnthropicCacheTtlOptions.OneHour)
            }) 
        ]);
        
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("------- System:");
        Console.ResetColor();
        Console.WriteLine(longPrompt);
        
        string shortPrompt = "In the text above, who cries  \"I am sick of Mr. Bingley\"?";
        
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("------- User:");
        Console.ResetColor();
        Console.WriteLine(shortPrompt);
        chat.AppendUserInput(shortPrompt);
        
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("------- Assistant:");
        Console.ResetColor();
        await StreamResponse();

        string shortPrompt2 = "When Elizabeth replied \"He is also handsome\", who does she mean?";
        Console.WriteLine();
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("------- User:");
        Console.ResetColor();
        Console.WriteLine(shortPrompt2);
        chat.AppendUserInput(shortPrompt2);
        
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("------- Assistant:");
        Console.ResetColor();
        await StreamResponse();

        async Task StreamResponse()
        {
            await chat.StreamResponseRich(new ChatStreamEventHandler
            {
                MessageTokenHandler = (token) =>
                {
                    Console.Write(token);
                    return ValueTask.CompletedTask;
                },
                OnUsageReceived = (usage) =>
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Usage: {usage}");
                    Console.ResetColor();
                    return ValueTask.CompletedTask;
                }
            });
        }
    }
    
    [TornadoTest]
    public static async Task CohereWebSearch()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Cohere.Command.Default,
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorCohereExtensions([
                ChatVendorCohereExtensionConnector.WebConnector
            ]))
        });
        
        chat.AppendSystemMessage("You are a helpful assistant connected to the internet tasked with fetching the latest information as requested by the user.");
        chat.AppendUserInput("Search for the latest version of .NET Core, including preview version. Respond with the latest version number and date of release.");

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
    
    [TornadoTest]
    public static async Task CohereWebSearchStreaming()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
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
            return ValueTask.CompletedTask;
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
            
            return ValueTask.CompletedTask;
        });
    }
    
    [TornadoTest]
    public static async Task DeepSeekReasoner()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.DeepSeek.Models.Reasoner
        });
        
        chat.AppendUserInput([
            new ChatMessagePart("Solve this equation: 2+2=?"),
        ]);

        ChatRichResponse response = await chat.GetResponseRich();
        
        Console.WriteLine("DeepSeek:");
        Console.WriteLine(response);
    }
    
    [TornadoTest]
    public static async Task DeepSeekChat()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.DeepSeek.Models.Chat
        });
        
        chat.AppendUserInput([
            new ChatMessagePart("Solve this equation: 2+2=?"),
        ]);

        ChatRichResponse response = await chat.GetResponseRich();
        
        Console.WriteLine("DeepSeek:");
        Console.WriteLine(response.Text);
    }
    
    [TornadoTest]
    public static async Task DeepSeekChatStreaming()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.DeepSeek.Models.Chat
        });
        
        chat.AppendUserInput([
            new ChatMessagePart("Explain how beer is made, curtly")
        ]);

        Console.WriteLine("DeepSeek:");
        
        await chat.StreamResponseRich(new ChatStreamEventHandler
        {
            MessageTokenHandler = (token) =>
            {
                Console.Write(token);
                return ValueTask.CompletedTask;
            },
            BlockFinishedHandler = (block) =>
            {
                Console.WriteLine();
                return ValueTask.CompletedTask;
            }
        });
    }

    [TornadoTest]
    public static async Task OpenAiReasoningTemperatureAutofix()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.O3.Mini,
            Temperature = 0.5d
        });
        
        chat.AppendUserInput([
            new ChatMessagePart("Explain how beer is made, curtly")
        ]);
        
        Console.WriteLine("OpenAi:");
        
        await chat.StreamResponseRich(new ChatStreamEventHandler
        {
            MessageTokenHandler = (token) =>
            {
                Console.Write(token);
                return ValueTask.CompletedTask;
            },
            BlockFinishedHandler = (block) =>
            {
                Console.WriteLine();
                return ValueTask.CompletedTask;
            },
            OnFinished = (data) =>
            {
                Console.WriteLine();
                Console.WriteLine(data.Usage);
                Console.WriteLine(data.FinishReason);
                return ValueTask.CompletedTask;
            }
        });
    }

    [TornadoTest]
    public static async Task DeepSeekChatStreamingTools()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.DeepSeek.Models.Chat,
            Tools =
            [
                new Tool(new ToolFunction("get_weather", "gets the current weather in a given city", new
                {
                    type = "object",
                    properties = new
                    {
                        location = new
                        {
                            type = "string",
                            description = "The city for which the weather information is required."
                        }
                    },
                    required = new List<string> { "city" }
                }))
            ]
        });
        
        chat.OnAfterToolsCall = async (result) =>
        {
            Console.WriteLine();
            await GetNextResponse();
        };
        
        chat.AppendUserInput([
            new ChatMessagePart("Check the weather today in Paris")
        ]);

        Console.WriteLine("DeepSeek:");
        await GetNextResponse();

        async Task GetNextResponse()
        {
            await chat.StreamResponseRich(new ChatStreamEventHandler
            {
                FunctionCallHandler = (fns) =>
                {
                    foreach (FunctionCall fn in fns)
                    {
                        fn.Result = new FunctionResult(fn.Name, new
                        {
                            result = "ok",
                            weather = "A mild rain is expected around noon."
                        });
                    }
                
                    return ValueTask.CompletedTask;
                },
                MessageTokenHandler = (token) =>
                {
                    Console.Write(token);
                    return ValueTask.CompletedTask;
                },
                BlockFinishedHandler = (block) =>
                {
                    Console.WriteLine();
                    return ValueTask.CompletedTask;
                }
            });
        }
    }

    [TornadoTest]
    public static async Task GoogleFileInput()
    {
        TornadoApi api = Program.Connect();
        HttpCallResult<TornadoFile> uploadedFile = await api.Files.Upload("Static/Files/prezSample.pdf", mimeType: "application/pdf", provider: LLmProviders.Google);

        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.Gemini.Gemini2Flash001
        });
        chat.AppendUserInput([
            new ChatMessagePart("What is this file about?"),
            new ChatMessagePart(new ChatMessagePartFileLinkData(uploadedFile.Data.Uri))
        ]);

        ChatRichResponse response = await chat.GetResponseRich();
        Console.WriteLine(response.Result?.Usage?.TotalTokens);
    }

    [TornadoTest]
    public static async Task GoogleStreamFileInput()
    {
        TornadoApi api = Program.Connect();
        HttpCallResult<TornadoFile> uploadedFile = await api.Files.Upload("Static/Files/sample.pdf", mimeType: "application/pdf", provider: LLmProviders.Google);

        if (uploadedFile.Data is null)
        {
            return;
        }
        
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.Gemini.Gemini15Flash
        });
        chat.AppendUserInput([
            new ChatMessagePart("What is this file about?"),
            new ChatMessagePart(new ChatMessagePartFileLinkData(uploadedFile.Data.Uri))
        ]);

        Console.WriteLine("Google:");

        await chat.StreamResponseRich(new ChatStreamEventHandler
        {
            MessageTokenHandler = (token) =>
            {
                Console.Write(token);
                return ValueTask.CompletedTask;
            },
            OnFinished = (data) =>
            {
                Console.WriteLine();
                Console.WriteLine(data.Usage);
                Console.WriteLine(data.FinishReason);
                return ValueTask.CompletedTask;
            }
        });
    }
    
    [TornadoTest]
    public static async Task GoogleStream()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.Gemini.Gemini15Flash
        });
        chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat.AppendUserInput("Who are you?");

        Console.WriteLine("Google:");
        await chat.StreamResponse(Console.Write);
    }
    
    [TornadoTest]
    public static async Task Google()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.Gemini.Gemini15Flash
        });
        chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat.AppendUserInput("Who are you?");

        string? str = await chat.GetResponse();

        Console.WriteLine("Google:");
        Console.WriteLine(str);
    }

    [TornadoTest]
    public static async Task AudioInWav()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.AudioPreview241001,
            Modalities = [ ChatModelModalities.Text ],
            MaxTokens = 2000
        });

        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.wav");
        
        chat.AppendUserInput([
            new ChatMessagePart(audioData, ChatAudioFormats.Wav)
        ]);
        
        string? str = await chat.GetResponse();
        Console.WriteLine(str);
    }
    
    [TornadoTest]
    public static async Task AudioInWavStreaming()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.AudioPreview241001,
            Modalities = [ ChatModelModalities.Text ],
            MaxTokens = 2000
        });

        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.wav");
        
        chat.AppendUserInput([
            new ChatMessagePart(audioData, ChatAudioFormats.Wav)
        ]);
        
        await chat.StreamResponseRich(new ChatStreamEventHandler
        {
            MessageTokenHandler = (str) =>
            {
                Console.Write(str);
                return ValueTask.CompletedTask;
            }
        });

        int z = 0;
    }
    
    [TornadoTest]
    public static async Task AudioInAudioOutWav()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.AudioPreview241001,
            Modalities = [ ChatModelModalities.Text, ChatModelModalities.Audio ],
            Audio = new ChatRequestAudio(ChatAudioRequestKnownVoices.Ash, ChatRequestAudioFormats.Wav),
            MaxTokens = 2000
        });

        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.wav");
        
        chat.AppendUserInput([
            new ChatMessagePart(audioData, ChatAudioFormats.Wav)
        ]);
        
        ChatRichResponse data = await chat.GetResponseRich();

        if (data.Blocks is not null)
        {
            ChatRichResponseBlock? audioBlock = data.Blocks.FirstOrDefault(x => x.Type is ChatRichResponseBlockTypes.Audio);

            if (audioBlock is not null)
            {
                if (audioBlock.ChatAudio is not null)
                {
                    Console.WriteLine($"transcript: {audioBlock.ChatAudio.Transcript}");
                    
                    byte[] outAudioData = audioBlock.ChatAudio.ByteData;
                    await File.WriteAllBytesAsync("AudioInAudioOutWav.wav", outAudioData);
                }
            }
        }
    }
    
    [TornadoTest]
    public static async Task AudioInAudioOutWavStreaming()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.AudioPreview241001,
            Modalities = [ ChatModelModalities.Text, ChatModelModalities.Audio ],
            Audio = new ChatRequestAudio(ChatAudioRequestKnownVoices.Ash, ChatRequestAudioFormats.Pcm16),
            MaxTokens = 2000
        });

        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.wav");
        
        chat.AppendUserInput([
            new ChatMessagePart(audioData, ChatAudioFormats.Wav)
        ]);
        
        await chat.StreamResponseRich(new ChatStreamEventHandler
        {
            AudioTokenHandler = async (chunk) =>
            {
                if (chunk.Data is not null)
                {
                    
                }
                
                Console.Write(chunk.Transcript);
            }
        });
    }
    
    [TornadoTest]
    public static async Task AudioInAudioOutMultiturn()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.AudioPreview241217,
            Modalities = [ ChatModelModalities.Text, ChatModelModalities.Audio ],
            Audio = new ChatRequestAudio(ChatAudioRequestKnownVoices.Ballad, ChatRequestAudioFormats.Wav),
            MaxTokens = 2000
        });

        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.wav");
        
        chat.AppendUserInput([
            new ChatMessagePart(audioData, ChatAudioFormats.Wav)
        ]);
        
        ChatRichResponse data = await chat.GetResponseRich();

        int tokens = data.Result?.Usage?.TotalTokens ?? 0;
        
        if (data.Blocks is not null)
        {
            ChatRichResponseBlock? audioBlock = data.Blocks.FirstOrDefault(x => x.Type is ChatRichResponseBlockTypes.Audio);

            if (audioBlock is not null)
            {
                if (audioBlock.ChatAudio is not null)
                {
                    Console.WriteLine($"transcript: {audioBlock.ChatAudio.Transcript}");
                    
                    byte[] outAudioData = audioBlock.ChatAudio.ByteData;
                    await File.WriteAllBytesAsync("Multiturn1.wav", outAudioData);
                }
            }
        }
        
        byte[] audioResponseData = await File.ReadAllBytesAsync("Static/Audio/sample2.wav");
        
        chat.AppendUserInput([
            new ChatMessagePart(audioResponseData, ChatAudioFormats.Wav)
        ]);
        
        data = await chat.GetResponseRich();

        if (data.Blocks is not null)
        {
            ChatRichResponseBlock? audioBlock = data.Blocks.FirstOrDefault(x => x.Type is ChatRichResponseBlockTypes.Audio);

            if (audioBlock is not null)
            {
                if (audioBlock.ChatAudio is not null)
                {
                    Console.WriteLine($"transcript2: {audioBlock.ChatAudio.Transcript}");
                    
                    byte[] outAudioData = audioBlock.ChatAudio.ByteData;
                    await File.WriteAllBytesAsync("Multiturn2.wav", outAudioData);
                }
            }
        }
        
        tokens += data.Result?.Usage?.TotalTokens ?? 0;
        
        Console.WriteLine($"Tokens total: {tokens}");
    }
    
    [TornadoTest]
    public static async Task AudioInMp3()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.AudioPreview241001,
            Modalities = [ ChatModelModalities.Text ],
            MaxTokens = 2000
        });

        byte[] audioData = await File.ReadAllBytesAsync("Static/Audio/sample.mp3");
        
        chat.AppendUserInput([
            new ChatMessagePart(audioData, ChatAudioFormats.Mp3)
        ]);
        
        string? str = await chat.GetResponse();
        Console.WriteLine(str);
    }
    
    [TornadoTest]
    public static async Task Haiku35()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude35.Haiku,
            MaxTokens = 2000
        });
        chat.AppendUserInput("Who are you?");

        string? str = await chat.GetResponse();

        Console.WriteLine("Haiku35:");
        Console.WriteLine(str);
    }
    
    [TornadoTest]
    public static async Task OpenAiO3()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.O3.Mini,
            MaxTokens = 2000
        });
        chat.AppendUserInput("You are a dog, sound authentic. Who are you?");

        string? str = await chat.GetResponse();

        Console.WriteLine("OpenAI O3:");
        Console.WriteLine(str);
    }

    [TornadoTest]
    public static async Task Cohere()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Cohere.Command.R7B
        });
        chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat.AppendUserInput("Who are you?");

        string? str = await chat.GetResponse();

        Console.WriteLine("Cohere:");
        Console.WriteLine(str);
    }
    
    [TornadoTest]
    public static async Task CohereMessageParts()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Cohere.Command.R7B,
            Messages = [
                new ChatMessage(ChatMessageRoles.System, [
                    new ChatMessagePart("You are a helpful assistant")
                ]),
                new ChatMessage(ChatMessageRoles.User, [
                    new ChatMessagePart("Who are you?")
                ])
            ]
        });
        
        RestDataOrException<ChatRichResponse> response = await chat.GetResponseRichSafe();

        Console.WriteLine("Cohere:");
        Console.WriteLine(response.Data);
    }
    
    [TornadoTest]
    public static async Task Cohere2408()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
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

    [Flaky("covered by other tests, takes a long time to finish")]
    [TornadoTest]
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
    
    [TornadoTest]
    public static async Task CohereStreaming()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Cohere.Command.A0325
        });
        
        chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat.AppendUserInput("Bark 10 times. Each bark should look like: [BARK {{i}}]: {{random text here}}");

        Console.WriteLine("Cohere:");
        await chat.StreamResponse(Console.Write);
        Console.WriteLine();
    }
    
    [TornadoTest]
    public static async Task CohereStreamingRich()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Cohere.Command.A0325
        });
        
        chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat.AppendUserInput("Bark 10 times. Each bark should look like: [BARK {{i}}]: {{random text here}}");

        Console.WriteLine("Cohere:");
        await chat.StreamResponseRich(new ChatStreamEventHandler
        {
            MessagePartHandler = (part) =>
            {
                Console.Write(part.Text);
                return ValueTask.CompletedTask;
            },
            OnFinished = (data) =>
            {
                Console.WriteLine();
                Console.WriteLine(data.Usage);
                Console.WriteLine(data.FinishReason);
                return ValueTask.CompletedTask;
            }
        });
    }
    
    [TornadoTest]
    public static async Task AnthropicSonnet37()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude37.Sonnet
        });
        
        chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat.AppendUserInput("Who are you?");

        string? str = await chat.GetResponse();

        Console.WriteLine("Anthropic:");
        Console.WriteLine(str);
    }
    
    [TornadoTest]
    public static async Task AnthropicSonnet37Thinking()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude37.Sonnet,
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorAnthropicExtensions
            {
                Thinking = new AnthropicThinkingSettings
                {
                    BudgetTokens = 2_000,
                    Enabled = true
                }
            })
        });
        
        chat.AppendUserInput("Explain how to solve differential equations.");

        ChatRichResponse blocks = await chat.GetResponseRich();

        if (blocks.Blocks is not null)
        {
            foreach (ChatRichResponseBlock reasoning in blocks.Blocks.Where(x => x.Type is ChatRichResponseBlockTypes.Reasoning))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(reasoning.Reasoning?.Content);
                Console.ResetColor();
            }

            foreach (ChatRichResponseBlock reasoning in blocks.Blocks.Where(x => x.Type is ChatRichResponseBlockTypes.Message))
            {
                Console.WriteLine(reasoning.Message);
            }
        }
    }
    
    [TornadoTest]
    public static async Task AnthropicSonnet37ThinkingStreaming()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude37.Sonnet,
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorAnthropicExtensions
            {
                Thinking = new AnthropicThinkingSettings
                {
                    BudgetTokens = 2_000,
                    Enabled = true
                }
            })
        });
        
        chat.AppendUserInput("Explain how to solve differential equations.");

        await chat.StreamResponseRich(new ChatStreamEventHandler
        {
            ReasoningTokenHandler = (token) =>
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;

                if (token.IsRedacted ?? false)
                {
                    Console.Write($"[redacted COT block: {token.Signature}]");
                }
                else
                {
                    Console.Write(token.Content);    
                }
                
                Console.ResetColor();
                return ValueTask.CompletedTask;
            },
            MessageTokenHandler = (token) =>
            {
                Console.Write(token);
                return ValueTask.CompletedTask;
            },
            OnFinished = (data) =>
            {
                Console.WriteLine();
                Console.WriteLine(data.Usage);
                Console.WriteLine(data.FinishReason);
                return ValueTask.CompletedTask;
            }
        });
    }
    
    [TornadoTest]
    public static async Task AnthropicSonnet37ThinkingStreamingMultiturn()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude37.Sonnet,
            Stream = true,
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorAnthropicExtensions
            {
                Thinking = new AnthropicThinkingSettings
                {
                    BudgetTokens = 2_000,
                    Enabled = true
                }
            })
        });
        
        chat.AppendUserInput("Explain how to solve differential equations.");
        await StreamResponse();
        chat.AppendUserInput("Summarize your response");
        await StreamResponse();
        chat.AppendUserInput("Reflect upon your summary, inspect your thinking process.");
        await StreamResponse();
        async Task StreamResponse()
        {
            await chat.StreamResponseRich(new ChatStreamEventHandler
            {
                ReasoningTokenHandler = (token) =>
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;

                    if (token.IsRedacted ?? false)
                    {
                        Console.Write($"[redacted COT block: {token.Signature}]");
                    }
                    else
                    {
                        Console.Write(token.Content);    
                    }
                
                    Console.ResetColor();
                    return ValueTask.CompletedTask;
                },
                MessageTokenHandler = (token) =>
                {
                    Console.Write(token);
                    return ValueTask.CompletedTask;
                },
                BlockFinishedHandler = (block) =>
                {
                    Console.WriteLine();
                    return ValueTask.CompletedTask;
                }
            });
        }
    }

    [TornadoTest]
    public static async Task Anthropic()
    {
       Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
       {
           Model = ChatModel.Anthropic.Claude4.Sonnet250514
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
    
    [TornadoTest]
    public static async Task AnthropicImageUrl()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude37.Sonnet
        });
      
        chat2.AppendUserInput([
            new ChatMessagePart(new Uri("https://upload.wikimedia.org/wikipedia/commons/a/a7/Camponotus_flavomarginatus_ant.jpg")),
            new ChatMessagePart("Describe this image")
        ]);
       
        ChatRichResponse response = await chat2.GetResponseRich();

        Console.WriteLine("Anthropic:");
        Console.WriteLine(response);
    }
    
    [TornadoTest]
    public static async Task AnthropicImageBase64()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude37.Sonnet
        });

        byte[] bytes = await File.ReadAllBytesAsync("Static/Images/catBoi.jpg");
        string base64 = $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
        
        chat2.AppendUserInput([
            new ChatMessagePart(base64, ImageDetail.Auto, "image/jpeg"),
            new ChatMessagePart("Describe this image")
        ]);
       
        ChatRichResponse response = await chat2.GetResponseRich();

        Console.WriteLine("Anthropic:");
        Console.WriteLine(response);
    }
    
    [TornadoTest]
    public static async Task AnthropicPdfBase64()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude37.Sonnet
        });

        byte[] bytes = await File.ReadAllBytesAsync("Static/Files/sample.pdf");
        string base64 = Convert.ToBase64String(bytes);
        
        chat2.AppendUserInput([
            new ChatMessagePart(base64, DocumentLinkTypes.Base64),
            new ChatMessagePart("Summarize this file")
        ]);
       
        ChatRichResponse response = await chat2.GetResponseRich();

        Console.WriteLine("Anthropic:");
        Console.WriteLine(response);
    }
    
    [TornadoTest]
    public static async Task AnthropicPdfUrl()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude37.Sonnet
        });
        
        chat2.AppendUserInput([
            new ChatMessagePart("https://ontheline.trincoll.edu/images/bookdown/sample-local-pdf.pdf", DocumentLinkTypes.Url),
            new ChatMessagePart("Summarize this file")
        ]);
       
        ChatRichResponse response = await chat2.GetResponseRich();

        Console.WriteLine("Anthropic:");
        Console.WriteLine(response);
    }
    
    [TornadoTest]
    public static async Task R7BArabic()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Cohere.Command.R7BArabic2412
        });
        chat2.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat2.AppendUserInput("Who are you?");
       
        string? str2 = await chat2.GetResponse();

        Console.WriteLine("Cohere:");
        Console.WriteLine(str2);
    }
    
    [TornadoTest]
    public static async Task Aya32B()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Cohere.Aya.Expanse32B
        });
        chat2.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat2.AppendUserInput("Who are you?");
       
        string? str2 = await chat2.GetResponse();

        Console.WriteLine("Cohere:");
        Console.WriteLine(str2);
    }
    
    [TornadoTest]
    public static async Task Aya32BVision()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Cohere.Aya.Vision32B
        });
        chat2.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat2.AppendUserInput("Who are you?");
       
        string? str2 = await chat2.GetResponse();

        Console.WriteLine("Cohere:");
        Console.WriteLine(str2);
    }
    
    [TornadoTest]
    public static async Task Gpt5()
    {
        List<string> resolvedGames = [];
        
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt5.V5Mini,
            ReasoningEffort = ChatReasoningEfforts.Minimal,
            Tools = [
                new Tool(async (List<string> popularGames) =>
                {
                    resolvedGames = popularGames;
                }, "set_data")
            ],
            ToolChoice = "set_data",
            Verbosity = ChatRequestVerbosities.Medium
        });
        
        chat2.AppendUserInput("2+2=?");

        ChatRichResponse response = await chat2.GetResponseRich();
        
        Console.WriteLine("OpenAI:");
        Console.WriteLine(response);

        Assert.That(resolvedGames.Count, Is.GreaterThan(0));
    }
    
    [TornadoTest]
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
                return ValueTask.CompletedTask;
            },
            FunctionCallHandler = (calls) =>
            {
                calls.ForEach(x => x.Result = new FunctionResult(x, "A mild rain is expected around noon.", null));
                return ValueTask.CompletedTask;
            },
            AfterFunctionCallsResolvedHandler = async (results, handler) => { await chat.StreamResponseRich(handler); }
        };

        await chat.StreamResponseRich(handler);
    }
    
    [TornadoTest]
    public static async Task OpenAiFunctionsRichBlocks()
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
            .AppendUserInput("What is the weather like today in Prague? also include date for which your prediction is made and confidence level.");

        ChatStreamEventHandler handler = new ChatStreamEventHandler
        {
            MessageTokenHandler = (x) =>
            {
                Console.Write(x);
                return ValueTask.CompletedTask;
            },
            FunctionCallHandler = (calls) =>
            {
                calls.ForEach(x => x.Resolve([
                    new FunctionResultBlockText(new
                    {
                        prediction = "A mild rain is expected around noon.",
                        confidenceLevel = "high",
                        note = "data valid as of 7/13/2025 8:16 AM"
                    }),
                    new FunctionResultBlockText(new
                    {
                        moreDetails = "high chloric activity detected in rain due to the recent Janovský's store accident. While harmless, it is recommended to minimize the exposure."
                    })
                ]));
                return ValueTask.CompletedTask;
            },
            AfterFunctionCallsResolvedHandler = async (results, handler) =>
            {
                await chat.StreamResponseRich(handler);
            }
        };

        await chat.StreamResponseRich(handler);
    }
    
    [TornadoTest]
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
        
        await chat.StreamResponseRich(new ChatStreamEventHandler
        {
            MessageTokenHandler = (x) =>
            {
                Console.Write(x);
                return ValueTask.CompletedTask;
            },
            BlockFinishedHandler = (block) =>
            {
                Console.WriteLine();
                return ValueTask.CompletedTask;
            },
            FunctionCallHandler = (calls) =>
            {
                calls.ForEach(x => x.Result = new FunctionResult(x, "A mild rain is expected around noon.", null));
                return ValueTask.CompletedTask;
            },
            AfterFunctionCallsResolvedHandler = async (results, handler) =>
            {
                await chat.StreamResponseRich(handler);
            }
        });
    }
    
    [TornadoTest]
    public static async Task AnthropicToolsForceNone()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude4.Sonnet250514,
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
            ToolChoice = OutboundToolChoice.None
        });

        ChatStreamEventHandler eventsHandler = new ChatStreamEventHandler
        {
            MessageTokenHandler = (x) =>
            {
                Console.Write(x);
                return ValueTask.CompletedTask;
            },
            BlockFinishedHandler = (block) =>
            {
                Console.WriteLine();
                return ValueTask.CompletedTask;
            },
            FunctionCallHandler = (functions) =>
            {
                foreach (FunctionCall fn in functions)
                {
                    if (fn.Get("location", out string? str) && str.ToLowerInvariant() is "prague")
                    {
                        fn.Result = new FunctionResult(fn.Name, "A mild rain is expected around noon.");  
                    }
                    else
                    {
                        fn.Result = new FunctionResult(fn.Name, "A sunny, hot day is expected, 28 \u00b0C");
                    }
                }

                return ValueTask.CompletedTask;
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
    
    [TornadoTest]
    public static async Task AnthropicFunctionsParallel()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude4.Sonnet250514,
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
                return ValueTask.CompletedTask;
            },
            FunctionCallHandler = (functions) =>
            {
                foreach (FunctionCall fn in functions)
                {
                    if (fn.Get("location", out string? str) && str?.ToLowerInvariant() is "prague")
                    {
                        fn.Resolve([
                            new FunctionResultBlockText(new
                            {
                                summary = "A mild rain is expected around noon.",
                                note = "Low level of confidence on this prediction"
                            })
                        ]);
                    }
                    else
                    {
                        fn.Resolve([
                            new FunctionResultBlockText("A sunny, hot day is expected, 28 \u00b0C")
                        ]);
                    }
                }

                return ValueTask.CompletedTask;
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
    
    [TornadoTest]
    public static async Task GoogleFunctions()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
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
                }), true)
            ],
            ToolChoice = new OutboundToolChoice("get_weather")
        });

        chat.OnAfterToolsCall = async (result) =>
        {
            chat.RequestParameters.ToolChoice = null; // stop forcing the model to use the get_weather tool
            ChatRichResponse response = await chat.GetResponseRich(functions =>
            {
                foreach (FunctionCall fn in functions)
                {
                    fn.Result = new FunctionResult(fn.Name, "A mild rain is expected around noon.");
                }

                return ValueTask.CompletedTask;
            });
            
            Console.WriteLine(response);
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

            return ValueTask.CompletedTask;
        });

        string r = response.GetText();
        Console.WriteLine(r);
    }
    
    [TornadoTest]
    public static async Task GoogleFunctionsStrict()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.Gemini.Gemini25Flash,
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
            chat.RequestParameters.Tools = null;
            chat.RequestParameters.ToolChoice = null; // stop forcing the model to use the get_weather tool
            string? str = await chat.GetResponse();

            if (str is not null)
            {
                Console.WriteLine(str);
            }
        };
        
        chat.AppendMessage(ChatMessageRoles.System, "You are a helpful assistant");
        Guid msgId = Guid.NewGuid();
        chat.AppendMessage(ChatMessageRoles.User, "Fetch the weather information for Prague.", msgId);

        ChatRichResponse response = await chat.GetResponseRich(functions =>
        {
            foreach (FunctionCall fn in functions)
            {
                fn.Result = new FunctionResult(fn.Name, "A mild rain is expected around noon.");
            }

            return ValueTask.CompletedTask;
        });

        string r = response.GetText();
        Console.WriteLine(r);
    }
    
    [TornadoTest]
    public static async Task AnthropicStreamingFunctions()
    {
        StringBuilder sb = new StringBuilder();

        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude4.Sonnet250514,
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
            return ValueTask.CompletedTask;
        }, functions =>
        {
            foreach (FunctionCall fn in functions)
            {
                fn.Result = new FunctionResult(fn.Name, "A mild rain is expected around noon.");
            }

            return ValueTask.CompletedTask;
        }, null, null);


        string response = sb.ToString();
        Console.WriteLine(response);
    }

    [TornadoTest]
    public static async Task AnthropicFailFunctions()
    {
        StringBuilder sb = new StringBuilder();

        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude4.Sonnet250514,
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
            return ValueTask.CompletedTask;
        }, functions =>
        {
            return ValueTask.CompletedTask;
        }, null, null);


        string response = sb.ToString();
        Console.WriteLine(response);
    }

    [TornadoTest]
    public static async Task Azure()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Default
        });
        
        chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat.AppendUserInput("Who are you?");
        
        string? str = await chat.GetResponse();

        Console.WriteLine("Azure OpenAI:");
        Console.WriteLine(str);
    }

    [TornadoTest]
    public static async Task AnthropicStreaming()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude4.Sonnet250514
        });
        chat.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat.AppendUserInput("Who are you?");

        Console.WriteLine("Anthropic:");
        await chat.StreamResponse(Console.Write);
        Console.WriteLine();
    }

    [TornadoTest, Flaky("expensive")]
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

    [TornadoTest]
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
            return ValueTask.CompletedTask;
        }, functions =>
        {
            foreach (FunctionCall fn in functions)
            {
                fn.Result = new FunctionResult(fn.Name, new
                {
                    result = "ok",
                    weather = "A mild rain is expected around noon."
                });
            }
            
            return ValueTask.CompletedTask;
        }, null);


        string response = sb.ToString();
        Console.WriteLine(response);
        return response;
    }
    
    [TornadoTestCase("gemini-2.5-flash")]
    [TornadoTestCase("command-a-03-2025")]
    [TornadoTest]
    public static async Task StreamingFunctions(string model)
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = model,
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
            await chat.StreamResponse(Console.Write);
        };
        
        chat.AppendMessage(ChatMessageRoles.System, "You are a helpful assistant");
        Guid msgId = Guid.NewGuid();
        chat.AppendMessage(ChatMessageRoles.User, "1. Solve the following equation: 2+2=?\n2. What is the weather like today in Prague?", msgId);

        TornadoRequestContent z = chat.Serialize();
        
        await chat.StreamResponseRich(msgId, (x) =>
        {
            Console.Write(x);
            return ValueTask.CompletedTask;
        }, functions =>
        {
            foreach (FunctionCall fn in functions)
            {
                fn.Result = new FunctionResult(fn.Name, "A mild rain is expected around noon.");
            }

            return ValueTask.CompletedTask;
        }, null);
    }

    // note: CachedContent can not be used with GenerateContent request setting system_instruction, tools or tool_config.\n\nProposed fix: move those values to CachedContent from GenerateContent request.
    public static async Task GoogleCachedFileOnly()
    {
        HttpCallResult<TornadoFile> file = await Program.Connect().Files.Upload("Static/Files/sample.pdf", provider: LLmProviders.Google, mimeType: "application/pdf");
        
        HttpCallResult<CachedContentInformation> cachingResult = await Program.Connect().Caching.Create(new CreateCachedContentRequest(90, ChatModel.Google.Gemini.Gemini15Pro002, [
            new CachedContent([
                new ChatMessagePart(new ChatMessagePartFileLinkData(file.Data.Uri, "application/pdf"))
            ], CachedContentRoles.User)
        ], null, null, null));
        
        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.Gemini.Gemini15Pro002,
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorGoogleExtensions(cachingResult.Data)
            {
                
            })
        });

        conversation.AppendSystemMessage($"Jsi nápomocný stroj");
        conversation.AppendUserInput("O čem je ten soubor?");

        ChatRichResponse response = await conversation.GetResponseRich();
        Console.WriteLine(response);
    }

    [TornadoTest]
    public static async Task GoogleCached()
    {
        string text = await File.ReadAllTextAsync("Static/Files/a11.txt");

        Tool tool = new Tool(new ToolFunction("return_transcript", "returns transcript of a given entry", new
        {
            type = "object",
            properties = new
            {
                content = new
                {
                    type = "string",
                    description = "Content of the entry"
                },
                title = new
                {
                    type = "string",
                    description = "Title/headline of the entry"
                }
            },
            required = new List<string> { "content", "title" }
        }), true);
        
        HttpCallResult<CachedContentInformation> cachingResult = await Program.Connect().Caching.Create(new CreateCachedContentRequest(90, ChatModel.Google.Gemini.Gemini15Pro002, [
            new CachedContent([
                new ChatMessagePart(text)
            ], CachedContentRoles.User)
        ], new CachedContent([
            new ChatMessagePart($"You are a machine answering questions regarding Apollo 11 mission, use the transcript of the mission for precise answers")
        ]), [
            tool
        ], new OutboundToolChoice("return_transcript")));
        
        Console.WriteLine(cachingResult.Data.Name);
        Console.WriteLine(cachingResult.Data.ExpireTime);

        Conversation conversation = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.Gemini.Gemini15Pro002,
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorGoogleExtensions(cachingResult.Data)
            {
                ResponseSchema = tool
            })
        });
        
        conversation.AppendUserInput("Cite the exact wording of the entry labeled \"04 06 58 40 CMP (COLUMBIA)\", starting with \"Roger, ...\". Use the function return_transcript.");
        await GetNextResponse();
        conversation.AppendUserInput($"Now do the same for entry \"05 07 57 34 CDR (EAGLE)\"");
        await GetNextResponse();
       
        return;
        
        async Task GetNextResponse()
        {
            await conversation.StreamResponseRich(new ChatStreamEventHandler
            {
                MessageTokenHandler = (token) =>
                {
                    Console.Write(token);
                    return ValueTask.CompletedTask;
                },
                OnUsageReceived = (usage) =>
                {
                    Console.WriteLine();
                    Console.WriteLine(usage);
                    return ValueTask.CompletedTask;
                },
                FunctionCallHandler = (calls) =>
                {
                    calls.ForEach(x =>
                    {
                        x.Result = new FunctionResult(x, null);

                        if (x.Get("title", out string? title))
                        {
                            Console.WriteLine($"TITLE: {title}");
                        }
                        
                        if (x.Get("content", out string? content))
                        {
                            Console.WriteLine($"CONTENT: {content}");
                        }
                    });
                    
                    return ValueTask.CompletedTask;
                }
            });    
        }
    }
}