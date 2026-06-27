namespace LlmTornado.Cli.Core.State;

public interface IAgentStateStore
{
    AgentMemoryRecord StoreMemory(string? key, string content, IReadOnlyList<string> tags, string? sourceConversationId);
    IReadOnlyList<AgentMemoryRecord> SearchMemories(string? query, string? tag, int limit);
    AgentMemoryRecord? GetMemory(long id);
    bool DeleteMemory(long id);

    AgentStateRecord SetState(string key, string value, string? contentType);
    AgentStateRecord? GetState(string key);
    IReadOnlyList<AgentStateRecord> ListState(string? prefix, int limit);
    bool DeleteState(string key);

    AgentStateSnapshotRecord CreateSnapshot(string? label);
    IReadOnlyList<AgentStateSnapshotRecord> ListSnapshots(int limit);
    AgentStateSnapshotRecord? GetSnapshot(long id);
    bool RestoreSnapshot(long id);
}

public sealed record AgentMemoryRecord(
    long Id,
    string? Key,
    string Content,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? SourceConversationId);

public sealed record AgentStateRecord(
    string Key,
    string Value,
    string? ContentType,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AgentStateSnapshotRecord(
    long Id,
    string? Label,
    DateTimeOffset CreatedAt,
    IReadOnlyList<AgentStateRecord> State);
