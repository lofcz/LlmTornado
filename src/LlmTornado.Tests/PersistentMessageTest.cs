using LlmTornado.Agents;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;

namespace LlmTornado.Tests;


public class PersistentMessageTest
{
    PersistentConversation _persistentConversation { get; set; }
    Conversation conversation;
    TornadoApi client;
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _persistentConversation = new PersistentConversation("test_conversation.json");
        client = new TornadoApi(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
    }

    [SetUp]
    public void SetUp()
    {
        conversation = client.Chat.CreateConversation(ChatModel.OpenAi.Gpt41.V41Mini);
    }

    [TearDown]
    public void TearDown()
    {

    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (File.Exists("test_conversation.json"))
        {
            File.Delete("test_conversation.json");
        }
    }

    [Test]
    public void TestChatConversion()
    {
        ChatMessage userMessage = new ChatMessage(Code.ChatMessageRoles.User, "Hello, how are you?");
        ChatMessage assistantMessage = new ChatMessage(Code.ChatMessageRoles.Assistant, "I'm good, thank you!");

        var persistentUserMessage = ConversationIOUtility.ConvertChatMessageToPersistent(userMessage);
        var persistentAssistantMessage = ConversationIOUtility.ConvertChatMessageToPersistent(assistantMessage);
        
        Assert.That(persistentAssistantMessage.Role, Is.EqualTo(ChatMessageRoles.Assistant));
        Assert.That(persistentAssistantMessage.Content, Is.EqualTo("I'm good, thank you!"));
        Assert.That(persistentUserMessage.Role, Is.EqualTo(ChatMessageRoles.User));
        Assert.That(persistentUserMessage.Content, Is.EqualTo("Hello, how are you?"));


        var restoredUserMessage = ConversationIOUtility.ConvertPersistantToChatMessage(persistentUserMessage);
        var restoredAssistantMessage = ConversationIOUtility.ConvertPersistantToChatMessage(persistentAssistantMessage);
        Assert.That(restoredAssistantMessage.Role, Is.EqualTo(ChatMessageRoles.Assistant));
        Assert.That(restoredAssistantMessage.Content, Is.EqualTo("I'm good, thank you!"));
        Assert.That(restoredUserMessage.Role, Is.EqualTo(ChatMessageRoles.User));
        Assert.That(restoredUserMessage.Content, Is.EqualTo("Hello, how are you?"));
    }

    [Test]
    public async Task TestPersistentConversationAppendAndLoad()
    {
        ChatMessage userMessage = new ChatMessage(Code.ChatMessageRoles.User, "Hello, how are you?");
        ChatMessage assistantMessage = new ChatMessage(Code.ChatMessageRoles.Assistant, "I'm good, thank you!");
        _persistentConversation.AppendMessage(userMessage);
        _persistentConversation.AppendMessage(assistantMessage);
        _persistentConversation.SaveChanges();

        _persistentConversation.Clear();

        List<ChatMessage> loadedMessages = new List<ChatMessage>();
        await loadedMessages.LoadMessagesAsync("test_conversation.json");

        Assert.That(loadedMessages.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(loadedMessages[^2].Role, Is.EqualTo(ChatMessageRoles.User));
        Assert.That(loadedMessages[^1].Role, Is.EqualTo(ChatMessageRoles.Assistant));

        conversation.LoadConversation(loadedMessages);

        Assert.That(conversation.Messages.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(conversation.Messages[^2].Role, Is.EqualTo(ChatMessageRoles.User));
        Assert.That(conversation.Messages[^1].Role, Is.EqualTo(ChatMessageRoles.Assistant));
    }
}
