using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Demo;

namespace LlmTornado.Tests
{
    [TestFixture]
    public class ChatTests
    {
        [SetUp]
        public async Task Setup()
        {
            await Program.SetupApi();
        }
        
        [Test]
        public void StreamChatEnumerable_Cancelled_ThrowsTaskCanceledException()
        {
            // Arrange
            TornadoApi api = Program.Connect();
            
            CancellationTokenSource cts = new CancellationTokenSource();
            ChatRequest request = new ChatRequest
            {
                Model = ChatModel.OpenAi.Gpt5.V5Nano,
                Messages = [new ChatMessage(ChatMessageRoles.User, "Tell me a long story.")],
                Stream = true,
                CancellationToken = cts.Token
            };

            // Act & Assert
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                cts.CancelAfter(100);

                await foreach (ChatResult res in api.Chat.StreamChatEnumerable(request))
                {
                    Console.Write(res.Choices?.FirstOrDefault()?.Delta?.Content);
                }
            });
        }
    }
}
