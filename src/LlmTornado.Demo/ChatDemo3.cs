using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http.Headers;
using LibVLCSharp.Shared;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Chat.Vendors.Google;
using LlmTornado.Chat.Vendors.Mistral;
using LlmTornado.Chat.Vendors.Perplexity;
using LlmTornado.Chat.Vendors.XAi;
using LlmTornado.Chat.Vendors.Zai;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Common;
using LlmTornado.Contrib;
using LlmTornado.Files;
using LlmTornado.Responses;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Demo;

public partial class ChatDemo : DemoBase
{
    [TornadoTest]
    public static async Task ZaiWebSearch()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.Zai.Glm.Glm46,
            Messages = [
                new ChatMessage(ChatMessageRoles.User, "Use web search to find the latest release of NodeJS")
            ],
            VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorZaiExtensions
            {
                BuiltInTools = [
                    new VendorZaiWebSearchTool
                    {
                        WebSearch = new VendorZaiWebSearchObject
                        {
                            Enable = true,
                            SearchEngine = VendorZaiSearchEngine.SearchProJina
                        }
                    }
                ]
            })
        });

        ChatRichResponse response = await chat.GetResponseRich();

        Console.WriteLine("ZAi:");
        Console.WriteLine(response);
    }
    
    [TornadoTest]
    public static async Task ZaiGlm()
    {
        await BasicChat(ChatModel.Zai.Glm.Glm46);
    }
}