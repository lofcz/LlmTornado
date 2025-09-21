using System.Text;
using LlmTornado.Code;
using LlmTornado.Responses;

namespace LlmTornado.Demo;

public class ResponsesConversationDemo : DemoBase
{
    public static string LongText(double megabytes)
    {
        const string loremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum. Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt explicabo. Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt. Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet, consectetur, adipisci velit, sed quia non numquam eius modi tempora incidunt ut labore et dolore magnam aliquam quaerat voluptatem. ";
    
        int targetBytes = (int)(megabytes * 1024 * 1024);
        StringBuilder sb = new StringBuilder(targetBytes);
        
        while (sb.Length < targetBytes)
        {
            int remainingBytes = targetBytes - sb.Length;
            sb.Append(remainingBytes >= loremIpsum.Length ? loremIpsum : loremIpsum[..remainingBytes]);
        }
    
        return sb.ToString();
    }
    
    [TornadoTest]
    public static async Task ResponsesConversationCreate()
    {
        ConversationResult result = await Program.Connect().ResponsesConversation.CreateConversation(new ResponsesConversationRequest
        {
            Items = [
                new ResponseInputMessage(ChatMessageRoles.System, "You are a helpful assistant"),
                new ResponseInputMessage(ChatMessageRoles.User, [
                    new ResponseInputContentText("How are you?")
                ]),
                new ResponseOutputMessageItem
                {
                    Role = ChatMessageRoles.Assistant,
                    Content = [
                        new ResponseOutputTextContent
                        {
                            Text = LongText(0.1)
                        }
                    ]
                }
            ]
        });
        
        Assert.That(result.Id.Length, Is.GreaterThan(0));
        int z = 0;
    }
}