using System.Net;
using LlmTornado.Chat;
using LlmTornado.Code.Vendor;
using LlmTornado.Common;

namespace LlmTornado.Demo;

public class SerializeDemo : DemoBase
{
    [TornadoTest]
    public static void Issue43()
    {
        string str = """
                     {
                         "id": "msg_01RkbugKNALp3sdVPuMhzmPN",
                         "type": "message",
                         "role": "assistant",
                         "model": "claude-sonnet-4-20250514",
                         "content": [
                             {
                                 "type": "text",
                                 "text": "just plain message"
                             },
                             {
                                 "type": "tool_use",
                                 "id": "toolu_013sBasdmuZ5ZQ1QBGW2MMXa",
                                 "name": "TodoWrite",
                                 "input": {
                                     "todos": [
                                         {
                                             "id": "1",
                                             "content": "plain string",
                                             "status": "pending",
                                             "priority": "high"
                                         },
                                         {
                                             "id": "2",
                                             "content": "unstructured arbitrary string",
                                             "status": "pending",
                                             "priority": "high"
                                         }
                                     ]
                                 }
                             }
                         ],
                         "stop_reason": "tool_use",
                         "stop_sequence": null,
                         "usage": {
                             "input_tokens": 14199,
                             "cache_creation_input_tokens": 0,
                             "cache_read_input_tokens": 0,
                             "output_tokens": 337,
                             "service_tier": "standard"
                         }
                     }
                     """;
        
        ChatResult? result = new AnthropicEndpointProvider().InboundMessage<ChatResult>(str, null);
        int z = 0;
    }
}