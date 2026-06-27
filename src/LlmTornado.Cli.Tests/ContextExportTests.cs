using System.Text.Json;
using LlmTornado.Chat;
using LlmTornado.Cli.Commands;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Memory;
using LlmTornado.Cli.Core.Storage;
using LlmTornado.Code;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class ContextExportTests
{
    private string _tempDir = null!;
    private SqliteConversationStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = TestHelpers.CreateTempDir();
        _store = new SqliteConversationStore(
            Path.Combine(_tempDir, "test.db"),
            Path.Combine(_tempDir, "attachments"));
    }

    [TearDown]
    public void TearDown()
    {
        _store.Dispose();
        TestHelpers.CleanupTempDir(_tempDir);
    }

    [Test]
    public void Formatter_Markdown_Includes_ModelContext_And_Summary()
    {
        List<ChatMessage> messages =
        [
            new(ChatMessageRoles.User, "Earlier requirements"),
            new(ChatMessageRoles.Assistant, "Earlier answer"),
            new(ChatMessageRoles.User, "[Conversation Summary]\n- User wants context export"),
        ];

        ContextExportSnapshot snapshot = ContextExportFormatter.CreateSnapshot(
            messages,
            conversationId: "conv-1",
            latestSummary: "- User wants context export",
            latestSummaryCoversThrough: 1,
            snapshots:
            [
                new ContextSnapshotInfo
                {
                    Id = 7,
                    CreatedAt = new DateTime(2026, 6, 24, 12, 0, 0, DateTimeKind.Utc),
                    Label = "auto-summary",
                    MessageCount = 3,
                },
            ],
            exportedAt: new DateTimeOffset(2026, 6, 24, 12, 30, 0, TimeSpan.Zero));

        string markdown = ContextExportFormatter.ToMarkdown(snapshot);

        Assert.That(markdown, Does.Contain("# LlmTornado Context Export"));
        Assert.That(markdown, Does.Contain("conv-1"));
        Assert.That(markdown, Does.Contain("## Latest Stored Summary"));
        Assert.That(markdown, Does.Contain("[Conversation Summary]"));
        Assert.That(markdown, Does.Contain("Estimated model context tokens"));
        Assert.That(markdown, Does.Contain("auto-summary"));
    }

    [Test]
    public void Formatter_Json_Includes_MessageIds_And_TokenTotals()
    {
        ChatMessage msg = new(ChatMessageRoles.User, new string('x', 400));
        ContextExportSnapshot snapshot = ContextExportFormatter.CreateSnapshot([msg], conversationId: "conv-json");

        string json = ContextExportFormatter.ToJson(snapshot);
        using JsonDocument doc = JsonDocument.Parse(json);

        JsonElement root = doc.RootElement;
        Assert.That(root.GetProperty(nameof(ContextExportSnapshot.ConversationId)).GetString(), Is.EqualTo("conv-json"));
        Assert.That(root.GetProperty(nameof(ContextExportSnapshot.EstimatedModelContextTokens)).GetInt32(), Is.EqualTo(100));
        Assert.That(root.GetProperty(nameof(ContextExportSnapshot.ModelContext))[0].GetProperty(nameof(ContextMessageExport.Id)).GetGuid(), Is.EqualTo(msg.Id));
    }

    [Test]
    public async Task ContextCommand_Export_Writes_Markdown_File()
    {
        ConversationMemoryManager manager = NewManager();
        manager.EnsureActiveConversation();
        manager.SyncFrom(
        [
            new(ChatMessageRoles.User, "remember this"),
            new(ChatMessageRoles.Assistant, "noted"),
        ]);

        string exportDir = Path.Combine(_tempDir, "exports");
        ContextCommand command = new(manager, _store, exportDir);

        bool result = await command.ExecuteAsync(["export"]);

        Assert.That(result, Is.True);
        string[] files = Directory.GetFiles(exportDir, "context-*.md");
        Assert.That(files, Has.Length.EqualTo(1));
        string content = File.ReadAllText(files[0]);
        Assert.That(content, Does.Contain("remember this"));
        Assert.That(content, Does.Contain("Model Context Sent Next Turn"));
    }

    [Test]
    public async Task ContextCommand_Export_Full_Includes_Stored_History()
    {
        string id = _store.Save(
        [
            new(ChatMessageRoles.User, "hidden old turn"),
            new(ChatMessageRoles.Assistant, "old answer"),
            new(ChatMessageRoles.User, "visible new turn"),
        ], "gpt-test", null);
        _store.MarkMessagesCompressed(id, 1);

        ConversationMemoryManager manager = NewManager(conversationId: id);
        string outputPath = Path.Combine(_tempDir, "full-export.json");
        ContextCommand command = new(manager, _store, Path.Combine(_tempDir, "exports"));

        bool result = await command.ExecuteAsync(["export", outputPath, "--format", "json", "--full"]);

        Assert.That(result, Is.True);
        string json = File.ReadAllText(outputPath);
        Assert.That(json, Does.Contain("hidden old turn"));
        Assert.That(json, Does.Contain("visible new turn"));
        Assert.That(json, Does.Contain(nameof(ContextExportSnapshot.FullHistory)));
    }

    [Test]
    public async Task ContextCommand_Cap_Updates_Manager_And_Persists_Settings()
    {
        ConversationMemoryManager manager = NewManager();
        AgentSettings settings = new();
        TestSettingsPersistence persistence = new();
        ContextCommand command = new(manager, _store, Path.Combine(_tempDir, "exports"), settings, persistence);

        bool result = await command.ExecuteAsync(["cap", "8192"]);

        Assert.That(result, Is.True);
        Assert.That(manager.CompressionContextTokenCap, Is.EqualTo(8192));
        Assert.That(manager.EffectiveCompressionContextTokens, Is.EqualTo(8192));
        Assert.That(settings.CompressionContextTokenCap, Is.EqualTo(8192));
        Assert.That(persistence.SaveCalls, Is.EqualTo(1));
    }

    private ConversationMemoryManager NewManager(string? conversationId = null) =>
        new(new TornadoApi("fake-key"), TestHelpers.CheapModel, 128_000, _store, conversationId);

    private sealed class TestSettingsPersistence : ISettingsPersistence
    {
        public int SaveCalls { get; private set; }

        public void SaveSettings(AgentSettings settings)
        {
            SaveCalls++;
        }
    }
}
