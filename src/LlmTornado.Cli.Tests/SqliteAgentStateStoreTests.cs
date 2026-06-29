using LlmTornado.Cli.Core.State;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class SqliteAgentStateStoreTests
{
    private string _tempDir = null!;
    private SqliteAgentStateStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = TestHelpers.CreateTempDir();
        _store = new SqliteAgentStateStore(Path.Combine(_tempDir, "test.db"));
    }

    [TearDown]
    public void TearDown()
    {
        _store.Dispose();
        TestHelpers.CleanupTempDir(_tempDir);
    }

    [Test]
    public void Memory_Crud_And_Search_RoundTrips()
    {
        AgentMemoryRecord stored = _store.StoreMemory(
            "favorite-color",
            "User prefers green dashboards.",
            ["preference", "ui"],
            "conv-1");

        Assert.That(stored.Id, Is.GreaterThan(0));
        Assert.That(stored.SourceConversationId, Is.EqualTo("conv-1"));

        IReadOnlyList<AgentMemoryRecord> search = _store.SearchMemories("green", null, 10);
        Assert.That(search, Has.Count.EqualTo(1));
        Assert.That(search[0].Key, Is.EqualTo("favorite-color"));

        IReadOnlyList<AgentMemoryRecord> tagged = _store.SearchMemories(null, "ui", 10);
        Assert.That(tagged, Has.Count.EqualTo(1));

        AgentMemoryRecord? loaded = _store.GetMemory(stored.Id);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Content, Is.EqualTo("User prefers green dashboards."));

        Assert.That(_store.DeleteMemory(stored.Id), Is.True);
        Assert.That(_store.GetMemory(stored.Id), Is.Null);
    }

    [Test]
    public void Memory_Recall_Uses_Local_Vector_Index_And_Token_Budget()
    {
        AgentMemoryRecord ui = _store.StoreMemory(
            "ui-preference",
            "User prefers compact green dashboards with dense tables.",
            ["preference", "ui"],
            "conv-1");
        _store.StoreMemory(
            "backend-note",
            "The service retries failed jobs with exponential backoff.",
            ["backend"],
            "conv-2");

        IReadOnlyList<AgentMemoryRecallRecord> recalled = _store.RecallMemories(
            "green dashboard layout preferences",
            "ui",
            limit: 5,
            maxTokens: 200);

        Assert.That(recalled, Is.Not.Empty);
        Assert.That(recalled[0].Id, Is.EqualTo(ui.Id));
        Assert.That(recalled[0].VectorScore, Is.GreaterThan(0));
        Assert.That(recalled[0].Score, Is.GreaterThan(0));
        Assert.That(recalled[0].MatchReason, Is.Not.Empty);
    }

    [Test]
    public void Memory_Reindex_Backfills_Vector_Rows()
    {
        _store.StoreMemory("one", "Remember the project uses SQLite memory.", ["project"], null);
        _store.StoreMemory("two", "User likes concise context budget displays.", ["preference"], null);

        int reindexed = _store.ReindexMemoryVectors();

        Assert.That(reindexed, Is.EqualTo(2));
        Assert.That(_store.RecallMemories("context budget", null, 5, 500), Is.Not.Empty);
    }

    [Test]
    public void State_Set_Get_List_Delete_RoundTrips()
    {
        AgentStateRecord first = _store.SetState("task.phase", "planning", "text/plain");
        _store.SetState("task.owner", "agent", null);
        AgentStateRecord updated = _store.SetState("task.phase", "implementation", "text/plain");

        Assert.That(first.CreatedAt, Is.EqualTo(updated.CreatedAt));
        Assert.That(updated.Value, Is.EqualTo("implementation"));
        Assert.That(_store.GetState("task.phase")!.Value, Is.EqualTo("implementation"));

        IReadOnlyList<AgentStateRecord> listed = _store.ListState("task.", 10);
        Assert.That(listed.Select(s => s.Key), Is.EquivalentTo(new[] { "task.phase", "task.owner" }));

        Assert.That(_store.DeleteState("task.owner"), Is.True);
        Assert.That(_store.GetState("task.owner"), Is.Null);
    }

    [Test]
    public void StateSnapshot_Create_Get_Restore_RoundTrips()
    {
        _store.SetState("task.phase", "planning", null);
        _store.SetState("task.owner", "agent", null);

        AgentStateSnapshotRecord snapshot = _store.CreateSnapshot("before-change");

        _store.SetState("task.phase", "implementation", null);
        _store.DeleteState("task.owner");

        Assert.That(_store.RestoreSnapshot(snapshot.Id), Is.True);
        Assert.That(_store.GetState("task.phase")!.Value, Is.EqualTo("planning"));
        Assert.That(_store.GetState("task.owner")!.Value, Is.EqualTo("agent"));

        AgentStateSnapshotRecord? loaded = _store.GetSnapshot(snapshot.Id);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Label, Is.EqualTo("before-change"));
        Assert.That(loaded.State, Has.Count.EqualTo(2));

        IReadOnlyList<AgentStateSnapshotRecord> snapshots = _store.ListSnapshots(10);
        Assert.That(snapshots.Select(s => s.Id), Does.Contain(snapshot.Id));
    }
}
