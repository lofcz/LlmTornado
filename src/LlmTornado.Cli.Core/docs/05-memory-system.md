# 05 — Conversation Memory System

The memory system manages conversation history with automatic token-aware compression, LLM-based summarization, and crash-resilient JSONL persistence.

## Architecture

```mermaid
classDiagram
    class ConversationMemoryManager {
        -CompressionStrategy _compressionStrategy
        -MessageSummarizer _summarizer
        -MessageMetadataTracker _metadataTracker
        -string _conversationPath
        -List~ChatMessage~ _messages
        +Messages: IReadOnlyList~ChatMessage~
        +AddMessage(message)
        +MaybeSummarize() Task~bool~
        +GetMessagesForAgent() List~ChatMessage~
        +NewConversation()
        +LoadConversation(messages)
        +UpdateModel(model, contextWindowTokens)
    }

    class CompressionStrategy {
        -int _contextWindowTokens
        +UncompressedThreshold: 0.60
        +ReCompressionThreshold: 0.80
        +TargetUtilization: 0.40
        +ReCompressionTarget: 0.20
        +LargeMessageThreshold: 10000
        +Analyze(messages, metadata) CompressionAnalysis
        +EstimateTokens(message) int
    }

    class MessageSummarizer {
        -TornadoApi _api
        -ChatModel _model
        +Summarize(messages, analysis, tracker) Task~List~ChatMessage~~
        +UpdateModel(model)
    }

    class MessageMetadataTracker {
        -Dictionary _states
        +Track(message)
        +MarkCompressed(id)
        +MarkReCompressed(id)
        +GetState(id) MessageCompressionState
        +Clear()
    }

    class ConversationStore {
        +Save(path, messages, metadata)$
        +Update(path, messages, metadata)$
        +Load(path) ConversationData$
        +List(directory, userId) List~ConversationMetadata~$
        +Delete(path)$
        +Exists(path) bool$
    }

    class MessageCompressionState {
        <<enumeration>>
        Uncompressed
        Compressed
        ReCompressed
    }

    ConversationMemoryManager --> CompressionStrategy
    ConversationMemoryManager --> MessageSummarizer
    ConversationMemoryManager --> MessageMetadataTracker
    MessageMetadataTracker --> MessageCompressionState
```

## Overall Flow

```mermaid
sequenceDiagram
    participant User
    participant CMM as ConversationMemoryManager
    participant CS as CompressionStrategy
    participant MS as MessageSummarizer
    participant Disk as JSONL Persistence

    User->>CMM: AddMessage(userMsg)
    CMM->>Disk: AppendMessage (JSONL)

    Note over CMM: Agent responds...

    User->>CMM: AddMessage(assistantMsg)
    CMM->>Disk: AppendMessage (JSONL)

    User->>CMM: MaybeSummarize()
    CMM->>CS: Analyze(messages, metadata)
    CS-->>CMM: CompressionAnalysis

    alt ShouldCompress = true
        CMM->>MS: Summarize(messages, analysis, tracker)
        MS-->>CMM: Compressed message list
        CMM->>Disk: RebuildPersistence (rewrite JSONL)
    end
```

## Compression Strategy

The `CompressionStrategy` analyzes the conversation to decide when summarization is needed. It works with configurable token utilization thresholds.

### Thresholds

| Threshold | Default | Meaning |
|-----------|---------|---------|
| `UncompressedThreshold` | 60% | Trigger first compression when uncompressed messages use >60% of context window |
| `ReCompressionThreshold` | 80% | Trigger re-compression when already-compressed + system messages use >80% |
| `TargetUtilization` | 40% | After first compression, aim for 40% utilization |
| `ReCompressionTarget` | 20% | After re-compression, aim for 20% utilization |
| `LargeMessageThreshold` | 10,000 tokens | Individual messages above this always trigger compression |

### Token Estimation

```mermaid
flowchart TD
    Msg["ChatMessage"] --> HasTokens{"message.Tokens<br/>set by API?"}
    HasTokens -->|"Yes"| UseTokens["Use message.Tokens"]
    HasTokens -->|"No"| Estimate["Estimate:<br/>charCount / 4"]
    Estimate --> Media{"Has media parts?"}
    Media -->|"Yes"| AddMedia["Add media costs:<br/>Image: 765 tokens<br/>Document: 1000 tokens<br/>Audio: 500 tokens"]
    Media -->|"No"| Total["Total estimated tokens"]
    AddMedia --> Total
    UseTokens --> Total
```

### Compression Analysis

```mermaid
flowchart TD
    Start["Analyze()"] --> Scan["Scan all messages"]
    Scan --> Categorize["Categorize by compression state:<br/>• System messages (token count)<br/>• Uncompressed (indices + tokens)<br/>• Compressed (indices + tokens)"]
    Categorize --> Compute["Compute utilization:<br/>total / contextWindow"]
    Compute --> Check{"Should compress?"}

    Check -->|"Large messages exist"| Yes["ShouldCompress = true"]
    Check -->|"Uncompressed util ≥ 60%"| Yes
    Check -->|"Compressed util ≥ 80%"| Yes2["ShouldCompress = true<br/>IsReCompression = true"]
    Check -->|"All below thresholds"| No["ShouldCompress = false"]
```

### Media Token Cost Reference

| Media Type | Token Cost | Notes |
|------------|-----------|-------|
| Image (low detail) | 85 | Rarely used in estimates |
| Image (high/auto) | 765 | Conservative default |
| Document (PDF) | 1,000 | Per page estimate |
| Audio | 500 | Per attachment |

## Message Summarization

`MessageSummarizer` uses an LLM to compress conversation history into bullet-point summaries.

### Summarization Process

```mermaid
flowchart TD
    Input["Messages + CompressionAnalysis"] --> Split["Split messages:<br/>~80% older → to summarize<br/>~20% recent → keep as-is"]
    Split --> Build["Build conversation text<br/>(with media placeholders)"]
    Build --> Call["Call LLM as<br/>'conversation summarizer'"]
    Call --> Response["Bullet-point summary"]
    Response --> Create["Create system message:<br/>[Conversation Summary]<br/>+ summary text"]
    Create --> Combine["Combine:<br/>summary message + kept recent messages"]
    Combine --> Track["Mark summarized messages<br/>as Compressed in tracker"]
    Track --> Output["Return new message list"]
```

### Media Placeholders

When building the conversation text for the LLM summarizer, media attachments are replaced with descriptive placeholders:

| Media Type | Placeholder |
|------------|-------------|
| Image | `[image attached]` |
| Document | `[document attached]` |
| Audio | `[audio attached]` |

### Summary Output Format

The summarizer produces a system message that replaces the compressed conversation turns:

```
[Conversation Summary]
- User asked about project architecture and was given an overview of the microservices design
- File analysis revealed 3 services: auth, api-gateway, and user-service
- User requested security review of auth service, found 2 potential issues with JWT handling
- Discussed migration plan from monolith to microservices with phased approach
```

## Persistence

### JSONL Format (Crash-Resilient)

Each message is appended as a single JSON line to a `.jsonl` file:

```jsonl
{"role":"user","content":"Analyze my project","id":"abc-123","timestamp":"2026-03-01T10:00:00Z"}
{"role":"assistant","content":"I'll analyze your project structure...","id":"def-456","timestamp":"2026-03-01T10:00:05Z"}
```

**Append-only**: New messages are appended immediately via `PersistentConversation.AppendMessage()`. This means if the process crashes, all messages up to the last complete line are recoverable.

**Rebuild**: After summarization, the entire file is rewritten with the compressed message list.

### Conversation Store (Named Conversations)

`ConversationStore` provides CRUD operations for named/labeled conversations:

```mermaid
flowchart LR
    subgraph "Storage Format"
        JSONL["{id}.jsonl<br/>Message data"]
        META["{id}.meta.json<br/>Metadata"]
    end

    subgraph "ConversationMetadata"
        ID["Id"]
        Label["Label (slug-generated)"]
        User["UserId"]
        Created["CreatedAt"]
        Updated["UpdatedAt"]
        Model["Model"]
        Count["MessageCount"]
        Preview["FirstMessagePreview"]
        Skills["ActiveSkills"]
    end

    META --> ID
    META --> Label
    META --> User
    META --> Created
    META --> Updated
    META --> Model
    META --> Count
    META --> Preview
    META --> Skills
```

| Operation | Description |
|-----------|-------------|
| `Save` | Auto-generates ID or uses caller-provided; creates `.jsonl` + `.meta.json` |
| `Update` | Atomic write via temp file + rename |
| `List` | Scans directory for `.meta.json` files, optional userId filter, sorted newest-first |
| `Load` | Reads both files, returns messages + metadata |
| `Delete` | Removes both files |

### Atomic Writes

Updates use a temp-file-then-rename pattern for crash safety:

```mermaid
sequenceDiagram
    participant App
    participant FS as Filesystem

    App->>FS: Write to {id}.meta.tmp.json
    App->>FS: Rename {id}.meta.tmp.json → {id}.meta.json
    Note over FS: Atomic on most filesystems
```

## Conversation Lifecycle

```mermaid
stateDiagram-v2
    [*] --> New: NewConversation()
    [*] --> Resumed: Constructor detects<br/>existing .jsonl file

    New --> Active: AddMessage()
    Resumed --> Active: Messages loaded<br/>from JSONL

    Active --> Active: AddMessage() +<br/>auto-persist

    Active --> Checking: MaybeSummarize()
    Checking --> Active: No compression needed
    Checking --> Summarizing: Compression needed
    Summarizing --> Active: Messages replaced,<br/>JSONL rebuilt

    Active --> Saved: ConversationStore.Save()
    Saved --> Active: ConversationStore.Load()

    Active --> [*]: NewConversation()
```

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `MaxTurnsBeforeSummary` | *(in AgentSettings)* | Hint for when to check summarization |
| Context window | 128,000 tokens | Default if not specified by model |

The compression strategy dynamically adapts to the model's context window size. When `UpdateModel()` is called after a model switch, the context window is updated accordingly.
