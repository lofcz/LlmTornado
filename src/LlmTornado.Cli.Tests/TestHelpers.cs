using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Memory;
using LlmTornado.Code;

namespace LlmTornado.Cli.Tests;

/// <summary>
/// Shared test helpers and constants.
/// </summary>
internal static class TestHelpers
{
    /// <summary>
    /// Create a ChatMessage for testing. Uses user role by default.
    /// </summary>
    public static ChatMessage MakeMessage(string content, ChatMessageRoles? role = null)
    {
        return new ChatMessage(role ?? ChatMessageRoles.User, content);
    }

    /// <summary>
    /// Create N messages of approximately the given character length.
    /// </summary>
    public static List<ChatMessage> MakeMessages(int count, int charsEach = 100)
    {
        List<ChatMessage> msgs = [];
        for (int i = 0; i < count; i++)
        {
            ChatMessageRoles role = i % 2 == 0 ? ChatMessageRoles.User : ChatMessageRoles.Assistant;
            msgs.Add(new ChatMessage(role, new string('x', charsEach)));
        }
        return msgs;
    }

    /// <summary>
    /// Create a temporary directory and ensure it's cleaned up.
    /// </summary>
    public static string CreateTempDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "llmtornado_test_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>
    /// Recursively delete a temp directory. Silently fails.
    /// </summary>
    public static void CleanupTempDir(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
        catch { /* best effort */ }
    }

    /// <summary>
    /// Get OpenAI API key from env, or null if not set.
    /// </summary>
    public static string? GetOpenAiKey() => Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    /// <summary>
    /// A cheap model for live tests (minimal token usage).
    /// </summary>
    public static ChatModel CheapModel => ChatModel.OpenAi.Gpt41.V41Nano;

    /// <summary>
    /// Create a TornadoApi with just OpenAI for live tests.
    /// </summary>
    public static TornadoApi? CreateOpenAiApi()
    {
        string? key = GetOpenAiKey();
        if (key is null) return null;
        return new TornadoApi(key);
    }
}
