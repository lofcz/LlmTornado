using LlmTornado.Chat;
using LlmTornado.Cli.Core.Memory;
using LlmTornado.Cli.Core.Storage;
using LlmTornado.Code;
using LlmTornado.Images;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class SqliteConversationStoreTests
{
    private string _tempDir = null!;
    private string _dbPath = null!;
    private SqliteConversationStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = TestHelpers.CreateTempDir();
        _dbPath = Path.Combine(_tempDir, "test.db");
        _store = new SqliteConversationStore(_dbPath, Path.Combine(_tempDir, "attachments"));
    }

    [TearDown]
    public void TearDown()
    {
        _store.Dispose();
        TestHelpers.CleanupTempDir(_tempDir);
    }

    #region Save & Load — Basic

    [TestFixture]
    public class BasicSaveLoad
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
        public void Save_And_Load_RoundTrips_TextMessages()
        {
            List<ChatMessage> messages =
            [
                new(ChatMessageRoles.User, "Hello, world!"),
                new(ChatMessageRoles.Assistant, "Hi there!")
            ];

            string id = _store.Save(messages, "gpt-4", null, "test-convo");

            List<ChatMessage>? loaded = _store.Load(id);
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!, Has.Count.EqualTo(2));
            Assert.That(loaded![0].Content, Is.EqualTo("Hello, world!"));
            Assert.That(loaded[0].Role, Is.EqualTo(ChatMessageRoles.User));
            Assert.That(loaded[1].Content, Is.EqualTo("Hi there!"));
            Assert.That(loaded[1].Role, Is.EqualTo(ChatMessageRoles.Assistant));
        }

        [Test]
        public void Save_Returns_Id_With_Label_Slug()
        {
            List<ChatMessage> messages = [new(ChatMessageRoles.User, "test")];
            string id = _store.Save(messages, "gpt-4", null, "My Test Conversation");
            Assert.That(id, Does.Contain("my-test-conversation"));
        }

        [Test]
        public void Save_Generates_Id_Without_Label()
        {
            List<ChatMessage> messages = [new(ChatMessageRoles.User, "test")];
            string id = _store.Save(messages, "gpt-4", null);
            Assert.That(id, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Load_Returns_Null_For_Nonexistent_Id()
        {
            List<ChatMessage>? loaded = _store.Load("nonexistent");
            Assert.That(loaded, Is.Null);
        }

        [Test]
        public void Save_Preserves_Message_Count()
        {
            List<ChatMessage> messages = TestHelpers.MakeMessages(5);
            string id = _store.Save(messages, "gpt-4", null);

            List<ConversationMetadata> list = _store.List();
            Assert.That(list, Has.Count.EqualTo(1));
            Assert.That(list[0].MessageCount, Is.EqualTo(5));
        }

        [Test]
        public void Save_Preserves_First_User_Message_Preview()
        {
            List<ChatMessage> messages =
            [
                new(ChatMessageRoles.System, "You are a helpful assistant."),
                new(ChatMessageRoles.User, "What is the meaning of life?"),
                new(ChatMessageRoles.Assistant, "42"),
            ];

            string id = _store.Save(messages, "gpt-4", null);
            List<ConversationMetadata> list = _store.List();
            Assert.That(list[0].FirstMessagePreview, Is.EqualTo("What is the meaning of life?"));
        }

        [Test]
        public void Save_Truncates_Long_Preview()
        {
            string longMsg = new string('x', 200);
            List<ChatMessage> messages = [new(ChatMessageRoles.User, longMsg)];

            string id = _store.Save(messages, "gpt-4", null);
            List<ConversationMetadata> list = _store.List();
            Assert.That(list[0].FirstMessagePreview!.Length, Is.LessThanOrEqualTo(104)); // 100 + "..."
        }
    }

    #endregion

    #region Save & Load — Update Existing

    [TestFixture]
    public class UpdateExisting
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
        public void Save_With_ExistingId_Overwrites_Messages()
        {
            List<ChatMessage> original = [new(ChatMessageRoles.User, "first")];
            string id = _store.Save(original, "gpt-4", null, "convo");

            List<ChatMessage> updated =
            [
                new(ChatMessageRoles.User, "first"),
                new(ChatMessageRoles.Assistant, "response"),
                new(ChatMessageRoles.User, "second")
            ];
            _store.Save(updated, "gpt-4", null, existingId: id);

            List<ChatMessage>? loaded = _store.Load(id);
            Assert.That(loaded, Has.Count.EqualTo(3));
            Assert.That(loaded![2].Content, Is.EqualTo("second"));
        }

        [Test]
        public void Save_With_ExistingId_Preserves_Hidden_Raw_History()
        {
            ChatMessage oldTurn = new(ChatMessageRoles.User, "raw old turn");
            string id = _store.Save([oldTurn], "gpt-4", null, "convo");

            ChatMessage summary = new(ChatMessageRoles.User, "[Conversation Summary]\n- raw old turn happened");
            ChatMessage recent = new(ChatMessageRoles.Assistant, "recent visible answer");
            _store.Save([summary, recent], "gpt-4", null, existingId: id);

            List<ChatMessage>? visible = _store.Load(id);
            List<ChatMessage>? full = _store.LoadFull(id);

            Assert.That(visible, Has.Count.EqualTo(2));
            Assert.That(visible!.Select(m => m.Content), Does.Not.Contain("raw old turn"));
            Assert.That(full, Is.Not.Null);
            Assert.That(full!.Select(m => m.Content), Does.Contain("raw old turn"));
            Assert.That(full.Select(m => m.Content), Does.Contain("[Conversation Summary]\n- raw old turn happened"));
        }

        [Test]
        public void Save_With_ExistingId_Preserves_CreatedAt()
        {
            List<ChatMessage> messages = [new(ChatMessageRoles.User, "test")];
            string id = _store.Save(messages, "gpt-4", null, "convo");

            DateTime createdBefore = _store.List().First(c => c.Id == id).CreatedAt;

            // Re-save after a tiny delay
            Thread.Sleep(50);
            _store.Save(messages, "gpt-4", null, existingId: id);

            DateTime createdAfter = _store.List().First(c => c.Id == id).CreatedAt;
            Assert.That(createdAfter, Is.EqualTo(createdBefore));
        }

        [Test]
        public void Save_With_ExistingId_Preserves_Label_When_Not_Set()
        {
            List<ChatMessage> messages = [new(ChatMessageRoles.User, "test")];
            string id = _store.Save(messages, "gpt-4", null, "Original Label");

            _store.Save(messages, "gpt-4", null, existingId: id);

            ConversationMetadata meta = _store.List().First(c => c.Id == id);
            Assert.That(meta.Label, Is.EqualTo("Original Label"));
        }
    }

    #endregion

    #region AppendMessage

    [Test]
    public void AppendMessage_Adds_To_Existing_Conversation()
    {
        List<ChatMessage> messages = [new(ChatMessageRoles.User, "first")];
        string id = _store.Save(messages, "gpt-4", null);

        _store.AppendMessage(id, new ChatMessage(ChatMessageRoles.Assistant, "appended"), 1);

        List<ChatMessage>? loaded = _store.Load(id);
        Assert.That(loaded, Has.Count.EqualTo(2));
        Assert.That(loaded![1].Content, Is.EqualTo("appended"));
    }

    [Test]
    public void AppendMessage_Updates_MessageCount()
    {
        List<ChatMessage> messages = [new(ChatMessageRoles.User, "first")];
        string id = _store.Save(messages, "gpt-4", null);

        _store.AppendMessage(id, new ChatMessage(ChatMessageRoles.Assistant, "second"), 1);
        _store.AppendMessage(id, new ChatMessage(ChatMessageRoles.User, "third"), 2);

        int count = _store.GetMessageCount(id);
        Assert.That(count, Is.EqualTo(3));
    }

    #endregion

    #region List & Delete

    [Test]
    public void List_Returns_Empty_When_No_Conversations()
    {
        List<ConversationMetadata> list = _store.List();
        Assert.That(list, Is.Empty);
    }

    [Test]
    public void List_Returns_All_Conversations_Ordered_By_Updated()
    {
        _store.Save([new(ChatMessageRoles.User, "first")], "gpt-4", null, "A");
        Thread.Sleep(50);
        _store.Save([new(ChatMessageRoles.User, "second")], "gpt-4", null, "B");

        List<ConversationMetadata> list = _store.List();
        Assert.That(list, Has.Count.EqualTo(2));
        Assert.That(list[0].Label, Is.EqualTo("B")); // Most recent first
        Assert.That(list[1].Label, Is.EqualTo("A"));
    }

    [Test]
    public void List_Includes_Model_And_Skills()
    {
        _store.Save(
            [new(ChatMessageRoles.User, "test")],
            "gpt-4o",
            ["web-search", "code-interpreter"],
            "convo");

        ConversationMetadata meta = _store.List().First();
        Assert.That(meta.Model, Is.EqualTo("gpt-4o"));
        Assert.That(meta.ActiveSkills, Has.Count.EqualTo(2));
        Assert.That(meta.ActiveSkills, Does.Contain("web-search"));
    }

    [Test]
    public void Delete_Removes_Conversation()
    {
        string id = _store.Save([new(ChatMessageRoles.User, "test")], "gpt-4", null);

        bool deleted = _store.Delete(id);
        Assert.That(deleted, Is.True);
        Assert.That(_store.Load(id), Is.Null);
        Assert.That(_store.List(), Is.Empty);
    }

    [Test]
    public void Delete_Returns_False_For_Nonexistent()
    {
        bool deleted = _store.Delete("nonexistent");
        Assert.That(deleted, Is.False);
    }

    #endregion

    #region LoadFull — Includes Hidden Messages

    [Test]
    public void LoadFull_Returns_All_Messages_Including_Compressed()
    {
        List<ChatMessage> messages = TestHelpers.MakeMessages(10);
        string id = _store.Save(messages, "gpt-4", null);

        // Hide first 5 messages via compression
        _store.MarkMessagesCompressed(id, 4);

        List<ChatMessage>? visible = _store.Load(id);
        List<ChatMessage>? full = _store.LoadFull(id);

        Assert.That(visible, Has.Count.EqualTo(5));
        Assert.That(full, Has.Count.EqualTo(10));
    }

    #endregion

    #region Multipart Messages — Images

    [Test]
    public void Save_Load_Image_Message_Extracts_And_Resolves_Attachment()
    {
        // Build a message with an inline base64 image
        byte[] fakeImage = new byte[64];
        Random.Shared.NextBytes(fakeImage);
        string base64 = Convert.ToBase64String(fakeImage);
        string dataUri = $"data:image/png;base64,{base64}";

        ChatMessagePart imagePart = new(dataUri, ImageDetail.Auto, "image/png");
        ChatMessagePart textPart = new("Describe this image");
        ChatMessage msg = new(ChatMessageRoles.User, [imagePart, textPart]);

        string id = _store.Save([msg], "gpt-4", null);

        // Lightweight load — attachments are not resolved
        List<ChatMessage>? lightweight = _store.Load(id);
        Assert.That(lightweight, Has.Count.EqualTo(1));
        Assert.That(lightweight![0].Parts, Has.Count.EqualTo(2));

        // Full load with attachments resolved
        List<ChatMessage>? resolved = _store.LoadWithAttachments(id);
        Assert.That(resolved, Has.Count.EqualTo(1));
        Assert.That(resolved![0].Parts, Has.Count.EqualTo(2));

        // Find the image part in resolved message
        ChatMessagePart? resolvedImage = resolved[0].Parts!.FirstOrDefault(p => p.Type == ChatMessageTypes.Image);
        Assert.That(resolvedImage, Is.Not.Null);
        Assert.That(resolvedImage!.Image, Is.Not.Null);
    }

    #endregion

    #region Multipart Messages — Reasoning/Thinking

    [Test]
    public void Save_Load_Reasoning_Message_Preserves_Content_And_Signature()
    {
        ChatMessageReasoningData reasoning = new()
        {
            Content = "Let me think about this step by step...",
            Signature = "abc123signature"
        };
        ChatMessagePart reasoningPart = new(reasoning);
        ChatMessagePart textPart = new("The answer is 42.");

        ChatMessage msg = new(ChatMessageRoles.Assistant, [reasoningPart, textPart]);
        string id = _store.Save([msg], "gpt-4", null);

        List<ChatMessage>? loaded = _store.Load(id);
        Assert.That(loaded, Has.Count.EqualTo(1));

        ChatMessage loadedMsg = loaded![0];
        Assert.That(loadedMsg.Parts, Has.Count.EqualTo(2));

        ChatMessagePart? loadedReasoning = loadedMsg.Parts!.FirstOrDefault(p => p.Type == ChatMessageTypes.Reasoning);
        Assert.That(loadedReasoning, Is.Not.Null);
        Assert.That(loadedReasoning!.Reasoning, Is.Not.Null);
        Assert.That(loadedReasoning.Reasoning!.Content, Is.EqualTo("Let me think about this step by step..."));
        Assert.That(loadedReasoning.Reasoning.Signature, Is.EqualTo("abc123signature"));

        ChatMessagePart? loadedText = loadedMsg.Parts!.FirstOrDefault(p => p.Type == ChatMessageTypes.Text);
        Assert.That(loadedText, Is.Not.Null);
        Assert.That(loadedText!.Text, Is.EqualTo("The answer is 42."));
    }

    [Test]
    public void Save_Load_Reasoning_With_Null_Content_Preserves_Signature()
    {
        // Redacted reasoning (like Anthropic/xAI) — signature but no content
        ChatMessageReasoningData reasoning = new()
        {
            Content = null,
            Signature = "opaque-redacted-token"
        };
        ChatMessagePart reasoningPart = new(reasoning);
        ChatMessage msg = new(ChatMessageRoles.Assistant, [reasoningPart, new ChatMessagePart("Final answer.")]);
        string id = _store.Save([msg], "gpt-4", null);

        List<ChatMessage>? loaded = _store.Load(id);
        ChatMessagePart? loadedReasoning = loaded![0].Parts!.First(p => p.Type == ChatMessageTypes.Reasoning);
        Assert.That(loadedReasoning.Reasoning!.Content, Is.Null);
        Assert.That(loadedReasoning.Reasoning.Signature, Is.EqualTo("opaque-redacted-token"));
    }

    #endregion

    #region Summaries

    [Test]
    public void SaveSummary_And_GetLatestSummary_RoundTrips()
    {
        string id = _store.Save([new(ChatMessageRoles.User, "test")], "gpt-4", null);

        long summaryId = _store.SaveSummary(id, "Summary of first 5 messages.", 4, 50);
        Assert.That(summaryId, Is.GreaterThan(0));

        var latest = _store.GetLatestSummary(id);
        Assert.That(latest, Is.Not.Null);
        Assert.That(latest!.Value.text, Is.EqualTo("Summary of first 5 messages."));
        Assert.That(latest.Value.coversThrough, Is.EqualTo(4));
    }

    [Test]
    public void GetLatestSummary_Returns_Most_Recent()
    {
        string id = _store.Save([new(ChatMessageRoles.User, "test")], "gpt-4", null);

        _store.SaveSummary(id, "First summary", 2, 30);
        _store.SaveSummary(id, "Second summary", 5, 60);

        var latest = _store.GetLatestSummary(id);
        Assert.That(latest!.Value.text, Is.EqualTo("Second summary"));
        Assert.That(latest.Value.coversThrough, Is.EqualTo(5));
    }

    [Test]
    public void GetLatestSummary_Returns_Null_When_None()
    {
        string id = _store.Save([new(ChatMessageRoles.User, "test")], "gpt-4", null);
        var latest = _store.GetLatestSummary(id);
        Assert.That(latest, Is.Null);
    }

    #endregion

    #region MarkMessagesCompressed

    [Test]
    public void MarkMessagesCompressed_Hides_Messages_From_Load()
    {
        List<ChatMessage> messages = TestHelpers.MakeMessages(6);
        string id = _store.Save(messages, "gpt-4", null);

        _store.MarkMessagesCompressed(id, 2); // Hide seq 0, 1, 2

        List<ChatMessage>? visible = _store.Load(id);
        Assert.That(visible, Has.Count.EqualTo(3)); // Only seq 3, 4, 5
    }

    [Test]
    public void MarkMessagesCompressed_Does_Not_Downgrade_State()
    {
        List<ChatMessage> messages = TestHelpers.MakeMessages(4);
        string id = _store.Save(messages, "gpt-4", null);

        // Compress first, then try to mark as lower state — should not change
        _store.MarkMessagesCompressed(id, 1, MessageCompressionState.ReCompressed);
        _store.MarkMessagesCompressed(id, 1, MessageCompressionState.Compressed);

        // Messages should still be hidden (ReCompressed > Compressed)
        List<ChatMessage>? visible = _store.Load(id);
        Assert.That(visible, Has.Count.EqualTo(2));
    }

    #endregion

    #region Snapshots

    [Test]
    public void CreateSnapshot_And_ListSnapshots()
    {
        List<ChatMessage> messages = TestHelpers.MakeMessages(5);
        string id = _store.Save(messages, "gpt-4", null);

        long snapId = _store.CreateSnapshot(id, "before-summary");

        List<SnapshotMetadata> snaps = _store.ListSnapshots(id);
        Assert.That(snaps, Has.Count.EqualTo(1));
        Assert.That(snaps[0].Id, Is.EqualTo(snapId));
        Assert.That(snaps[0].Label, Is.EqualTo("before-summary"));
        Assert.That(snaps[0].MessageCount, Is.EqualTo(5));
    }

    [Test]
    public void RestoreSnapshot_Resets_Visibility()
    {
        List<ChatMessage> messages = TestHelpers.MakeMessages(6);
        string id = _store.Save(messages, "gpt-4", null);

        // Take snapshot before compression
        long snapId = _store.CreateSnapshot(id);

        // Compress first 3 messages
        _store.MarkMessagesCompressed(id, 2);
        List<ChatMessage>? afterCompress = _store.Load(id);
        Assert.That(afterCompress, Has.Count.EqualTo(3));

        // Restore snapshot
        bool restored = _store.RestoreSnapshot(id, snapId);
        Assert.That(restored, Is.True);

        List<ChatMessage>? afterRestore = _store.Load(id);
        Assert.That(afterRestore, Has.Count.EqualTo(6));
    }

    [Test]
    public void RestoreSnapshot_Returns_False_For_Invalid()
    {
        string id = _store.Save([new(ChatMessageRoles.User, "test")], "gpt-4", null);
        bool restored = _store.RestoreSnapshot(id, 99999);
        Assert.That(restored, Is.False);
    }

    #endregion

    #region EnsureConversation & GetMessageCount

    [Test]
    public void EnsureConversation_Creates_New_Row()
    {
        _store.EnsureConversation("my-conv", "gpt-4");

        List<ConversationMetadata> list = _store.List();
        Assert.That(list, Has.Count.EqualTo(1));
        Assert.That(list[0].Id, Is.EqualTo("my-conv"));
    }

    [Test]
    public void EnsureConversation_Is_Idempotent()
    {
        _store.EnsureConversation("my-conv", "gpt-4");
        _store.EnsureConversation("my-conv", "gpt-4");

        List<ConversationMetadata> list = _store.List();
        Assert.That(list, Has.Count.EqualTo(1));
    }

    [Test]
    public void GetMessageCount_Returns_Zero_For_Empty_Conversation()
    {
        _store.EnsureConversation("empty");
        Assert.That(_store.GetMessageCount("empty"), Is.EqualTo(0));
    }

    #endregion

    #region MessageSerializer — Direct Tests

    [TestFixture]
    public class SerializerTests
    {
        [Test]
        public void Serialize_TextOnly_Message_Has_No_Attachments()
        {
            ChatMessage msg = new(ChatMessageRoles.User, "Hello");
            SerializedMessage result = MessageSerializer.Serialize(msg);

            Assert.That(result.Role, Is.EqualTo("user"));
            Assert.That(result.Content, Is.EqualTo("Hello"));
            Assert.That(result.Attachments, Is.Empty);
            Assert.That(result.PartsJson, Is.Null);
        }

        [Test]
        public void Serialize_Multipart_Extracts_Image_Attachment()
        {
            byte[] imageBytes = new byte[32];
            Random.Shared.NextBytes(imageBytes);
            string dataUri = $"data:image/jpeg;base64,{Convert.ToBase64String(imageBytes)}";

            ChatMessagePart imagePart = new(dataUri, ImageDetail.Auto, "image/jpeg");
            ChatMessagePart textPart = new("caption");
            ChatMessage msg = new(ChatMessageRoles.User, [imagePart, textPart]);

            SerializedMessage result = MessageSerializer.Serialize(msg);
            Assert.That(result.Attachments, Has.Count.EqualTo(1));
            Assert.That(result.Attachments[0].MimeType, Is.EqualTo("image/jpeg"));
            Assert.That(result.Attachments[0].MediaType, Is.EqualTo(AttachmentMediaType.Image));
            Assert.That(result.PartsJson, Does.Contain("attachment:"));
        }

        [Test]
        public void Serialize_Reasoning_Part_Stores_Content_Inline()
        {
            ChatMessageReasoningData reasoning = new() { Content = "thinking...", Signature = "sig" };
            ChatMessage msg = new(ChatMessageRoles.Assistant,
                [new ChatMessagePart(reasoning), new ChatMessagePart("result")]);

            SerializedMessage result = MessageSerializer.Serialize(msg);
            Assert.That(result.Attachments, Is.Empty); // Reasoning is text, not binary
            Assert.That(result.PartsJson, Does.Contain("thinking..."));
            Assert.That(result.PartsJson, Does.Contain("sig"));
        }

        [Test]
        public void DeserializeLightweight_Reconstructs_Text_And_Reasoning()
        {
            ChatMessageReasoningData reasoning = new() { Content = "step 1, step 2", Signature = "s1" };
            ChatMessage original = new(ChatMessageRoles.Assistant,
                [new ChatMessagePart(reasoning), new ChatMessagePart("final answer")]);

            SerializedMessage serialized = MessageSerializer.Serialize(original);
            ChatMessage deserialized = MessageSerializer.DeserializeLightweight(
                serialized.Role, serialized.Content, serialized.PartsJson, Guid.NewGuid());

            Assert.That(deserialized.Parts, Has.Count.EqualTo(2));

            ChatMessagePart rPart = deserialized.Parts!.First(p => p.Type == ChatMessageTypes.Reasoning);
            Assert.That(rPart.Reasoning!.Content, Is.EqualTo("step 1, step 2"));
            Assert.That(rPart.Reasoning.Signature, Is.EqualTo("s1"));

            ChatMessagePart tPart = deserialized.Parts!.First(p => p.Type == ChatMessageTypes.Text);
            Assert.That(tPart.Text, Is.EqualTo("final answer"));
        }
    }

    #endregion

    #region AttachmentStore — Direct Tests

    [TestFixture]
    public class AttachmentStoreTests
    {
        private string _tempDir = null!;
        private AttachmentStore _attachments = null!;

        [SetUp]
        public void SetUp()
        {
            _tempDir = TestHelpers.CreateTempDir();
            _attachments = new AttachmentStore(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            TestHelpers.CleanupTempDir(_tempDir);
        }

        [Test]
        public void SaveAttachment_And_LoadAttachment_RoundTrips()
        {
            byte[] data = [1, 2, 3, 4, 5];
            string path = _attachments.SaveAttachment("conv1", "att1", data, ".png");

            byte[]? loaded = _attachments.LoadAttachment(path);
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded, Is.EqualTo(data));
        }

        [Test]
        public void DeleteConversationAttachments_Removes_Directory()
        {
            byte[] data = [1, 2, 3];
            _attachments.SaveAttachment("conv1", "att1", data, ".png");
            _attachments.SaveAttachment("conv1", "att2", data, ".jpg");

            _attachments.DeleteConversationAttachments("conv1");

            string convDir = Path.Combine(_tempDir, "conv1");
            Assert.That(Directory.Exists(convDir), Is.False);
        }

        [Test]
        public void LoadAttachment_Returns_Null_For_Missing_File()
        {
            byte[]? loaded = _attachments.LoadAttachment("nonexistent/path.png");
            Assert.That(loaded, Is.Null);
        }

        [Test]
        public void GetExtensionForMime_Returns_Known_Extensions()
        {
            Assert.That(AttachmentStore.GetExtensionForMime("image/png"), Is.EqualTo(".png"));
            Assert.That(AttachmentStore.GetExtensionForMime("image/jpeg"), Is.EqualTo(".jpg"));
            Assert.That(AttachmentStore.GetExtensionForMime("audio/wav"), Is.EqualTo(".wav"));
            Assert.That(AttachmentStore.GetExtensionForMime("application/pdf"), Is.EqualTo(".pdf"));
        }

        [Test]
        public void GetExtensionForMime_Returns_Bin_For_Unknown()
        {
            Assert.That(AttachmentStore.GetExtensionForMime("application/x-custom"), Is.EqualTo(".bin"));
        }
    }

    #endregion

    #region End-to-End — Full Conversation Lifecycle

    [Test]
    public void Full_Lifecycle_Save_Compress_Snapshot_Restore()
    {
        // 1. Save initial conversation
        List<ChatMessage> messages = TestHelpers.MakeMessages(10, 50);
        string id = _store.Save(messages, "gpt-4", ["web-search"]);

        // 2. Verify full load
        Assert.That(_store.LoadFull(id), Has.Count.EqualTo(10));
        Assert.That(_store.Load(id), Has.Count.EqualTo(10));

        // 3. Take snapshot before compression
        long snap1 = _store.CreateSnapshot(id, "pre-compression");

        // 4. Save a summary and compress messages 0-4
        _store.SaveSummary(id, "Summary: user and assistant exchanged 5 messages.", 4, 25);
        _store.MarkMessagesCompressed(id, 4);

        // 5. Visible view now has 5 messages, full has 10
        Assert.That(_store.Load(id), Has.Count.EqualTo(5));
        Assert.That(_store.LoadFull(id), Has.Count.EqualTo(10));

        // 6. Take another snapshot after compression
        long snap2 = _store.CreateSnapshot(id, "post-compression");

        // 7. List snapshots
        List<SnapshotMetadata> snaps = _store.ListSnapshots(id);
        Assert.That(snaps, Has.Count.EqualTo(2));
        Assert.That(snaps[0].Label, Is.EqualTo("post-compression")); // Most recent first

        // 8. Restore pre-compression snapshot
        _store.RestoreSnapshot(id, snap1);
        Assert.That(_store.Load(id), Has.Count.EqualTo(10));

        // 9. Verify summary still exists
        var summary = _store.GetLatestSummary(id);
        Assert.That(summary, Is.Not.Null);
    }

    [Test]
    public void Full_Lifecycle_Multiple_Conversations_Independent()
    {
        string id1 = _store.Save([new(ChatMessageRoles.User, "conv1 msg")], "gpt-4", null, "First");
        string id2 = _store.Save([new(ChatMessageRoles.User, "conv2 msg")], "gpt-4o", null, "Second");

        // Delete first, second is unaffected
        _store.Delete(id1);
        Assert.That(_store.List(), Has.Count.EqualTo(1));
        Assert.That(_store.Load(id2)![0].Content, Is.EqualTo("conv2 msg"));
    }

    #endregion
}
