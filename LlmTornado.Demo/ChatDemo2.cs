using System.Diagnostics;
using LibVLCSharp.Shared;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Google;
using LlmTornado.Chat.Vendors.Mistral;
using LlmTornado.Chat.Vendors.Perplexity;
using LlmTornado.Chat.Vendors.XAi;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Common;
using LlmTornado.Contrib;
using LlmTornado.Files;

namespace LlmTornado.Demo;

public static partial class ChatDemo
{
    [TornadoTest]
    public static async Task ProviderCustomServerApiKey()
    {
        TornadoApi tornadoApi = new TornadoApi(new Uri("https://api.openai.com/"), Program.ApiKeys.OpenAi);
        
        Conversation chat = tornadoApi.Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41
        });
        
        chat.AppendUserInput("Who are you?");
        string? str = await chat.GetResponse();

        Console.WriteLine("OpenAI:");
        Console.WriteLine(str);
    }
    
    [TornadoTest]
    public static async Task ProviderCustomHeaders()
    {
        TornadoApi tornadoApi = new TornadoApi(new AnthropicEndpointProvider
        {
            Auth = new ProviderAuthentication(Program.ApiKeys.Anthropic),
            RequestResolver = (request, data, streaming) =>
            {
                // by default, providing custom request resolved omits beta headers from all built-in providers
            }
        });

        Conversation chat = tornadoApi.Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude4.Sonnet250514
        });
        
        chat.AppendUserInput("Who are you?");
        string? str = await chat.GetResponse();

        Console.WriteLine("Anthropic:");
        Console.WriteLine(str);
    }
    
    [TornadoTest]
    public static async Task Grok2()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.XAi.Grok.Grok2241212
        });
        
        chat.AppendUserInput("Who are you?");
        string? str = await chat.GetResponse();

        Console.WriteLine("xAi:");
        Console.WriteLine(str);
    }
    
    [TornadoTest]
    public static async Task GrokLiveSearch()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.XAi.Grok3.V3,
            VendorExtensions = new ChatRequestVendorExtensions
            {
                XAi = new ChatRequestVendorXAiExtensions
                {
                    SearchParameters = new ChatRequestVendorXAiExtensionsSearchParameters
                    {
                        ReturnCitations = true,
                        Mode = ChatRequestVendorXAiExtensionsSearchParametersModes.On,
                        Sources = [
                            new ChatRequestVendorXAiExtensionsSearchParametersSourceWeb
                            {
                                SafeSearch = false
                            }
                        ]
                    }
                }
            }
        });
        
        chat.AppendUserInput("What is the latest .NET Core version, including previews?");
        ChatRichResponse response = await chat.GetResponseRich();

        Console.WriteLine("xAi:");
        Console.WriteLine(response);

        if (response.VendorExtensions?.XAi?.Citations is not null)
        {
            Console.WriteLine("Citations:");

            foreach (string citation in response.VendorExtensions.XAi.Citations)
            {
                Console.WriteLine(citation);
            }
        }
    }
    
    [TornadoTest]
    public static async Task GrokLiveSearchStreaming()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.XAi.Grok3.V3,
            VendorExtensions = new ChatRequestVendorExtensions
            {
                XAi = new ChatRequestVendorXAiExtensions
                {
                    SearchParameters = new ChatRequestVendorXAiExtensionsSearchParameters
                    {
                        ReturnCitations = true,
                        Mode = ChatRequestVendorXAiExtensionsSearchParametersModes.On,
                        Sources = [
                            new ChatRequestVendorXAiExtensionsSearchParametersSourceWeb
                            {
                                SafeSearch = false
                            }
                        ]
                    }
                }
            }
        });
        
        chat.AppendUserInput("What is the latest .NET Core version, including previews?");

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
            VendorFeaturesHandler = (extensions) =>
            {
                if (extensions.XAi?.Citations is not null)
                {
                    Console.WriteLine("Citations:");
                    Console.WriteLine("--------------------");

                    foreach (string citation in extensions.XAi.Citations)
                    {
                        Console.WriteLine(citation);
                    }
                }

                return ValueTask.CompletedTask;
            }
        });
    }
    
    [TornadoTest]
    public static async Task Grok2Beta()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.XAi.Grok.GrokBeta
        });
        
        chat.AppendUserInput("Who are you?");
        string? str = await chat.GetResponse();

        Console.WriteLine("xAi:");
        Console.WriteLine(str);
    }
    
    [TornadoTest]
    public static async Task PerplexitySonarRecency()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Perplexity.Sonar.Default,
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorPerplexityExtensions
            {
                SearchAfterDateFilter = new DateTime(2025, 4, 15)
            })
        });
        
        chat.AppendUserInput(".net 10 preview 4");
        string? str = await chat.GetResponse();

        Console.WriteLine("Perplexity:");
        Console.WriteLine(str);
    }
    
    [TornadoTest]
    public static async Task PerplexitySonar()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Perplexity.Sonar.Default
        });
        
        chat.AppendUserInput("What kind of a LLM are you?");
        string? str = await chat.GetResponse();

        Console.WriteLine("Perplexity:");
        Console.WriteLine(str);
    }
    
    [TornadoTest]
    public static async Task PerplexitySonarStreaming()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Perplexity.Sonar.Default
        });
        
        chat.AppendUserInput("What kind of a LLM are you?");
        
        Console.WriteLine("Perplexity:");
        
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
    public static async Task GoogleStreamingTokenEx()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.Gemini.Gemini2Flash001
        });
        
        chat.AppendUserInput("What kind of a LLM are you?");
        
        Console.WriteLine("Google:");
        
        await chat.StreamResponseRich(new ChatStreamEventHandler
        {
            MessageTokenExHandler = (token) =>
            {
                Console.Write(token);
                return ValueTask.CompletedTask;
            }
        });
    }
    
    [TornadoTest]
    public static async Task Grok2Streaming()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.XAi.Grok.Grok2241212
        });
        
        chat.AppendUserInput("Who are you?");
        
        Console.WriteLine("xAi:");
        
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
    public static async Task MistralLarge()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Mistral.Premier.MistralLarge
        });
        
        chat.AppendUserInput("You are a dog, sound authentic. Who are you?");
        string? str = await chat.GetResponse();

        Console.WriteLine("Mistral:");
        Console.WriteLine(str);
    }
    
    [TornadoTest]
    public static async Task MistralExtensionsSafePrompt()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Mistral.Premier.MistralLarge,
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorMistralExtensions
            {
                SafePrompt = true
            })
        });
        
        chat.AppendUserInput("Write the safety prompt injected before this text.");
        string? str = await chat.GetResponse();

        Console.WriteLine("Mistral:");
        Console.WriteLine(str);
    }
    
    [TornadoTest]
    public static async Task MistralExtensionsPrediction()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Mistral.Premier.MistralLarge,
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorMistralExtensions
            {
                Prediction = "Solution is "
            })
        });
        
        chat.AppendUserInput("2+2=?");
        string? str = await chat.GetResponse();

        Console.WriteLine("Mistral:");
        Console.WriteLine(str);
    }
    
    [TornadoTest]
    public static async Task MistralExtensionsPrefix()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Mistral.Premier.MistralLarge,
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorMistralExtensions
            {
                Prefix = "Certainly not 5, dumm"
            })
        });
        
        chat.AppendUserInput("2+2=?");
        string? str = await chat.GetResponse();

        Console.WriteLine("Mistral:");
        Console.WriteLine(str);
    }
    
    [TornadoTest]
    public static async Task Grok3ReasoningStreaming()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.XAi.Grok3.V3Mini,
            ReasoningEffort = ChatReasoningEfforts.Low
        });
        chat2.AppendUserInput("Solve 10+5=?");

        await chat2.StreamResponseRich(new ChatStreamEventHandler
        {
            ReasoningTokenHandler = (reasoning) =>
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(reasoning.Content);
                Console.ResetColor();
                return ValueTask.CompletedTask;
            },
            MessageTokenExHandler = (token) =>
            {
                if (token.Index is 0)
                {
                    Console.WriteLine();
                }
                
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
    public static async Task Grok3Reasoning()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.XAi.Grok3.V3Mini,
            ReasoningEffort = ChatReasoningEfforts.Low
        });
        chat2.AppendUserInput("Solve 10+5=?");
       
        ChatRichResponse str2 = await chat2.GetResponseRich();
        ChatRichResponseBlock? block = str2.Blocks.FirstOrDefault();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(block.Reasoning?.Content);
        Console.ResetColor();
        Console.WriteLine(block.Message);
    }
    
    [Flaky("playback")]
    [TornadoTest]
    public static async Task GeminiTts()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.GeminiPreview.Gemini25FlashPreviewTts,
            Modalities = [ ChatModelModalities.Audio ],
            Temperature = 1d,
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Google = new ChatRequestVendorGoogleExtensions
                {
                    SpeechConfig = new ChatRequestVendorGoogleSpeechConfig
                    {
                       MultiSpeaker = new ChatRequestVendorGoogleSpeechConfigMultiSpeaker
                       {
                           Speakers = [
                                new ChatRequestVendorGoogleSpeechConfigMultiSpeakerSpeaker("Speaker 1", ChatRequestVendorGoogleSpeakerVoices.Puck),
                                new ChatRequestVendorGoogleSpeechConfigMultiSpeakerSpeaker("Speaker 2", ChatRequestVendorGoogleSpeakerVoices.Charon)
                           ]
                       }
                    }
                }
            }
        });

        chat2.AppendUserInput("""
                              Read in a warm, energetic tone:
                              Speaker 1: How are you today?
                              Speaker 2: Thanks, I'm doing fine.
                              Speaker 1: Glad to hear that!
                              """);
       
        ChatRichResponse response = await chat2.GetResponseRich();
        ChatRichResponseBlock? block = response.Blocks.FirstOrDefault();

        string? audioPath = block?.ChatAudio?.Export(ChatAudioFormats.Wav);
         
        // example: play the dialogue using LibVLC
        if (audioPath is not null)
        {
            PlaySound(audioPath);   
        }
    }

    static void PlaySound(string path)
    {
        Core.Initialize();
        using Media media = new Media(new LibVLC(), new Uri(path));
        MediaPlayer player = new MediaPlayer(new LibVLC());
        player.Media = media;
        player.Play(); // note that this doesn't block
    }

    [TornadoTest]
    public static async Task AnthropicIssue38Completion()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude37.Sonnet
        });
        
        chat2.AppendUserInput("Explain quadratic equations, briefly");

        ChatRichResponse response = await chat2.GetResponseRich();
        string str = response.Result?.Choices[0].Message?.Content;
        Console.WriteLine(str);
    }

    [TornadoTest]
    public static async Task AnthropicIssue38()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude37.Sonnet
        });
        
        chat2.AppendUserInput("Explain quadratic equations");
       
        await chat2.StreamResponseRich(new ChatStreamEventHandler 
        {
            MessagePartHandler = async (part) =>
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(part.Text);
                Console.ResetColor();
            },
            BlockFinishedHandler = (chatMessage) =>
            {
                string str = chatMessage?.Content ?? string.Empty;
                Console.WriteLine(str);
                return ValueTask.CompletedTask;
            },
            OnUsageReceived = (usage) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"AsyncCompletionV2: LlmTornado OnUsageReceived. Usage: {usage.PromptTokens} in, {usage.CompletionTokens} out.");
                Console.ResetColor();
                return ValueTask.CompletedTask;
            }                          
        });
    }
    
    [TornadoTest]
    public static async Task Llama4MaverickMultilingual()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Groq.Meta.Llama4Maverick
        });
        chat2.AppendUserInput("Jak se vaří pivo?");
       
        string? str2 = await chat2.GetResponse();
        Console.WriteLine(str2);
    }

    [TornadoTest]
    public static async Task FinishReasonLength()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.GeminiPreview.Gemini25FlashPreview0417,
            MaxTokens = 64
        });
        
        chat2.AppendUserInput("Explain how beer is created");
        ChatRichResponse response = await chat2.GetResponseRich();

        Console.WriteLine(response.Text);
        Console.WriteLine(response.FinishReason);
    }

    [TornadoTest]
    public static async Task Issue40()
    {
        Conversation conversation = Program.Connect().Chat
            .CreateConversation(
                new ChatRequest
                {
                    Model = ChatModel.Mistral.Free.MistralSmall,
                    Temperature = 0,
                    ResponseFormat = ChatRequestResponseFormats.StructuredJson("accept_grouped_items", new
                    {
                        type = "array",
                        items = new
                        {
                            type = "string"
                        }
                    }, true)
                }
            )
            .AppendSystemMessage("Your task is to group items provided by user by name and quantity")
            .AppendUserInput("3 grapes, 2 oranges, 1 cherry, 1 grape and 1 orange");

        TornadoRequestContent outboundRequest = conversation.Serialize(new ChatRequestSerializeOptions
        {
            IncludeHeaders = true,
            Pretty = true
        });
        Console.WriteLine(outboundRequest);

        ChatRichResponse response = await conversation.GetResponseRich();
        Console.WriteLine(response);
        Console.WriteLine($"Body from response: {response.Request?.Body}");
    }

    [TornadoTest]
    public static async Task Google25FlashAdaptiveThinking()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.GeminiPreview.Gemini25FlashPreview0417,
            ReasoningBudget = 0
        });
        chat2.AppendUserInput("Explain how beer is created");
        
        ChatRichResponse response = await chat2.GetResponseRich();
        
        Console.WriteLine("------------------- Without thinking ----------------------");
        Console.WriteLine(response.RawResponse);
        
        chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.GeminiPreview.Gemini25FlashPreview0417,
            ReasoningBudget = 1024
        });
        chat2.AppendUserInput("Explain how beer is created");
        
        response = await chat2.GetResponseRich();
        
        Console.WriteLine("------------------- With thinking ----------------------");
        Console.WriteLine(response.RawResponse);
    }
    
    [TornadoTest]
    public static async Task Gpt41()
    {
        await BasicChat(ChatModel.OpenAi.Gpt41.V41);
    }
    
    [TornadoTest]
    public static async Task GemmaNE()
    {
        await BasicChat(ChatModel.Google.Gemma.V3Ne4B);
    }
    
    [TornadoTest]
    public static async Task Llama4Scout()
    {
        await BasicChat(ChatModel.Groq.Meta.Llama4Scout);
    }
    
    [TornadoTest]
    public static async Task CohereA0325()
    {
        await BasicChat(ChatModel.Cohere.Command.A0325);
    }
    
    [TornadoTest]
    public static async Task OpenAiO4()
    {
        await BasicChat(ChatModel.OpenAi.O4.V4Mini);
    }
    
    [TornadoTest]
    public static async Task MistralMedium3()
    {
        await CzechChat(ChatModel.Mistral.Premier.Medium3);
    }
    
    [TornadoTest]
    public static async Task Gemini25Pro()
    {
        await BasicChat(ChatModel.Google.GeminiPreview.Gemini25ProPreview0325);
    }
    
    private static async Task BasicChat(ChatModel model)
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = model
        });
        chat2.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat2.AppendUserInput("Solve 2+2");
       
        string? str2 = await chat2.GetResponse();
        Console.WriteLine(str2);
    }
    
    private static async Task CzechChat(ChatModel model)
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = model
        });
        chat2.AppendSystemMessage("Jsi stroj, který připravuje plán hodiny na zadané téma");
        chat2.AppendUserInput("Kvadratické rovnice, II. ročník");
       
        string? str2 = await chat2.GetResponse();
        Console.WriteLine(str2);
    }
    
    [Flaky("flex service tier is slow to execute")]
    [TornadoTest]
    public static async Task ServiceTierFlex()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.O3.V3,
            ServiceTier = ChatRequestServiceTiers.Flex
        });
        chat2.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat2.AppendUserInput("Solve 2+2");
       
        string? str2 = await chat2.GetResponse();
        Console.WriteLine(str2);
    }
    
    [TornadoTest]
    public static async Task Gemma327B()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.Gemma.V327B
        });
        chat2.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat2.AppendUserInput("Solve 2+2");
       
        string? str2 = await chat2.GetResponse();

        Console.WriteLine("Google:");
        Console.WriteLine(str2);
    }
    
    [TornadoTest]
    public static async Task YoutubeVideo()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.Gemini.Gemini2Flash001
        });
        
        chat2.AppendUserInput([
            new ChatMessagePart("Summarize this video"),
            new ChatMessagePart(new ChatMessagePartFileLinkData("https://www.youtube.com/watch?v=ocbLw49Or44"))
        ]);
       
        string? str2 = await chat2.GetResponse();

        Console.WriteLine("Google:");
        Console.WriteLine(str2);
    }

    public static async Task DisplayImage(string base64)
    {
        
        byte[] imageBytes = Convert.FromBase64String(base64);
        string tempFile = $"{Path.GetTempFileName()}.jpg";
        await File.WriteAllBytesAsync(tempFile, imageBytes);

        if (await Helpers.ProgramExists("chafa"))
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "chafa";
                process.StartInfo.Arguments = $"{tempFile}";
                process.StartInfo.UseShellExecute = false;
                process.Start();
                await process.WaitForExitAsync();
            }
            catch (Exception e)
            {
                
            }
        }
    }

    [Flaky("access limited in Europe")]
    [TornadoTest]
    public static async Task GoogleStreamImages()
    {
        Conversation chat = Program.Connect(LLmProviders.Google).Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.GeminiExperimental.Gemini2FlashImageGeneration,
            Modalities = [ ChatModelModalities.Text, ChatModelModalities.Image ]
        });
        
        chat.AppendUserInput([
            new ChatMessagePart("Generate two images: a lion and a squirrel")
        ]);
        
        await chat.StreamResponseRich(new ChatStreamEventHandler
        {
            MessagePartHandler = async (part) =>
            {
                if (part.Text is not null)
                {
                    Console.Write(part.Text);
                    return;
                }

                if (part.Image is not null)
                {
                    await DisplayImage(part.Image.Url);
                }
            },
            BlockFinishedHandler = (block) =>
            {
                Console.WriteLine();
                return ValueTask.CompletedTask;
            },
            OnUsageReceived = (usage) =>
            {
                Console.WriteLine();
                Console.WriteLine(usage);
                return ValueTask.CompletedTask;
            }
        });
    }

    [TornadoTest]
    public static async Task GoogleStreamVideo()
    {
        TornadoApi api = Program.Connect(LLmProviders.Google);
        HttpCallResult<TornadoFile> uploadedFile = await api.Files.Upload("Static/Files/video.mp4", mimeType: "video/mp4", provider: LLmProviders.Google);

        if (uploadedFile.Data is null)
        {
            return;
        }
        
        Conversation chat = Program.Connect(LLmProviders.Google).Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Google.Gemini.Gemini2Flash001
        });
        chat.AppendUserInput([
            new ChatMessagePart("Describe all the shots in this video"),
            new ChatMessagePart(new ChatMessagePartFileLinkData(uploadedFile.Data))
        ]);

        Console.WriteLine("Google:");

        if (uploadedFile.Data?.State is FileLinkStates.Processing)
        {
            await api.Files.WaitForReady(uploadedFile.Data, provider: LLmProviders.Google);
        }

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
            OnUsageReceived = (usage) =>
            {
                Console.WriteLine();
                Console.WriteLine(usage);
                return ValueTask.CompletedTask;
            }
        });
    }

    [TornadoTest]
    public static async Task MistralSmall()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Mistral.Free.MistralSmall
        });
        chat2.AppendSystemMessage("Pretend you are a dog. Sound authentic.");
        chat2.AppendUserInput("Solve 2+2");
       
        string? str2 = await chat2.GetResponse();

        Console.WriteLine("Mistral:");
        Console.WriteLine(str2);
    }

    [TornadoTest]
    public static async Task MistralLargeStreaming()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Mistral.Premier.MistralLarge
        });
        
        chat.AppendUserInput("You are a dog, sound authentic. Who are you?");

        Console.WriteLine("Mistral:");
        
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
    public static async Task MistralChatStreamingTools()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Mistral.Premier.MistralLarge,
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

        Console.WriteLine("Mistral:");
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
    public static async Task MistralImageUrl()
    {
        Conversation chat2 = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Mistral.Premier.PixtralLarge
        });
      
        chat2.AppendUserInput([
            new ChatMessagePart(new Uri("https://upload.wikimedia.org/wikipedia/commons/a/a7/Camponotus_flavomarginatus_ant.jpg")),
            new ChatMessagePart("Describe this image")
        ]);
       
        ChatRichResponse response = await chat2.GetResponseRich();

        Console.WriteLine("Mistral:");
        Console.WriteLine(response);
    }
    
    [TornadoTest]
    public static async Task MistralStructuredJson()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Mistral.Premier.MistralLarge,
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
        Console.WriteLine(response);
    }
}