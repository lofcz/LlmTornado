using System.Collections.Generic;
using System.Threading.Tasks;
using LlmTornado.Chat.Models;
using LlmTornado.Common;

namespace LlmTornado.Infra;

internal static class ChatPluginCompiler
{
    private static readonly HashSet<ChatModel> ModelsSupportingStrictFunctions = [ 
        ChatModel.OpenAi.Gpt41.V41, ChatModel.OpenAi.Gpt41.V41Mini, ChatModel.OpenAi.Gpt41.V41Nano,
        ChatModel.OpenAi.Gpt4.O, ChatModel.OpenAi.Gpt4.O240806, ChatModel.OpenAi.Gpt4.O241120, ChatModel.OpenAi.Gpt4.O240513,
        ChatModel.Google.Gemini.Gemini15Flash, ChatModel.Google.Gemini.Gemini2Flash001, ChatModel.Google.Gemini.Gemini15Pro, ChatModel.Google.Gemini.Gemini15Pro002, ChatModel.Google.Gemini.Gemini15Pro001, ChatModel.Google.Gemini.Gemini15ProLatest,
        ChatModel.Google.Gemini.Gemini2FlashLatest, ChatModel.Google.GeminiPreview.Gemini25FlashPreview0417, ChatModel.Google.GeminiPreview.Gemini25ProPreview0325, ChatModel.Google.GeminiPreview.Gemini25ProPreview0506
    ];

    public static ToolFunction Compile(Tool function, ToolMeta meta)
    {
        function.Params ??= [];
        
        ToolParamObject root = new ToolParamObject(function.Params);
        object obj = root.Compile(function, meta);
        return new ToolFunction(function.Name, function.Description, obj);
    }
}