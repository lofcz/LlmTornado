using System.Collections.Generic;
using System.Linq;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Responses;
using Newtonsoft.Json;

namespace LlmTornado.Tokenize.Vendors;

internal class VendorOpenAiTokenizeRequest
{
    [JsonProperty("model")]
    public string? Model { get; set; }

    [JsonProperty("input")]
    public object? Input { get; set; }

    [JsonProperty("instructions")]
    public string? Instructions { get; set; }

    [JsonProperty("tools")]
    public List<ResponseTool>? Tools { get; set; }

    public VendorOpenAiTokenizeRequest(TokenizeRequest request, IEndpointProvider provider)
    {
        Model = request.Model?.Name;

        if (request.Messages is { Count: > 0 })
        {
            ChatMessage? sysMsg = request.Messages.FirstOrDefault(x => x is { Role: ChatMessageRoles.System });

            if (sysMsg is not null)
            {
                if (sysMsg.Content?.Length > 0)
                {
                    Instructions = sysMsg.Content;
                }
                else if (sysMsg.Parts?.Count > 0)
                {
                    Instructions = string.Join("\n", sysMsg.Parts.Where(x => x.Type is ChatMessageTypes.Text).Select(x => x.Text));
                }
            }

            Input = ResponseHelpers.ToResponseInputItems(request.Messages);
        }
        else if (request.Text is not null)
        {
            Input = request.Text;
        }

        if (request.Tools is { Count: > 0 })
        {
            Tools = ResponseHelpers.ConvertTools(request.Tools);
        }
    }
}
