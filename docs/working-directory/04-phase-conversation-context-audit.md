# Phase 04: Conversation Persistence and Context Window Audit

## Objective

Document the exact implementation path for how conversation state is saved and served to model requests, with emphasis on whether system/skill prompts are inflating request context.

This file is intended as a low-friction handoff for future CLI sessions so the next agent can continue without re-discovery.

## Scope

- Projects reviewed:
  - `src/LlmTornado.Cli`
  - `src/LlmTornado.Cli.Core`
  - `src/LlmTornado.Cli.Blazor`
  - `src/LlmTornado.Agents`
- Branch context:
  - Current branch: `Tornado-CLI`
  - Default branch: `master`

---

## Executive Findings

1. System prompt is injected once per request, not duplicated as historical system messages.
2. Skill metadata is included in system prompt each turn; full skill instructions are typically injected via `load_skill` tool output (tool-context growth risk).
3. CLI compression/summarization exists but does not currently govern runtime request context.
4. CLI incremental SQLite persistence is not active unless `ConversationMemoryManager` has a non-null conversation id.
5. Blazor conversation loading restores UI, but does not rehydrate runtime conversation state.

---

## Exact Save Path (CLI)

### Storage locations

- Root/AppData paths:
  - `src/LlmTornado.Cli/CliStorage.cs:10`
- SQLite DB:
  - `src/LlmTornado.Cli/CliStorage.cs:18`
- Attachment directory:
  - `src/LlmTornado.Cli/CliStorage.cs:19`

### Runtime setup

- Store + memory manager constructed in:
  - `src/LlmTornado.Cli/Program.cs:120`
  - `src/LlmTornado.Cli/Program.cs:121`

### Per-turn message persistence logic

- User msg appended to memory manager:
  - `src/LlmTornado.Cli/Program.cs:233`
- Assistant msg appended to memory manager:
  - `src/LlmTornado.Cli/Program.cs:256`
- Summarization/compression check:
  - `src/LlmTornado.Cli/Program.cs:265`

### ConversationMemoryManager behavior

- SQLite ctor and conversation id field setup:
  - `src/LlmTornado.Cli.Core/Memory/ConversationMemoryManager.cs:65`
  - `src/LlmTornado.Cli.Core/Memory/ConversationMemoryManager.cs:73`
- Incremental append only if conversation id is present:
  - `src/LlmTornado.Cli.Core/Memory/ConversationMemoryManager.cs:79`
  - `src/LlmTornado.Cli.Core/Memory/ConversationMemoryManager.cs:98`
- Summarization writes summary/snapshot and rewrites message set:
  - `src/LlmTornado.Cli.Core/Memory/ConversationMemoryManager.cs:111`
  - `src/LlmTornado.Cli.Core/Memory/ConversationMemoryManager.cs:139`

### SQLite store APIs

- Save/upsert full conversation:
  - `src/LlmTornado.Cli.Core/Storage/SqliteConversationStore.cs:44`
- Append one message:
  - `src/LlmTornado.Cli.Core/Storage/SqliteConversationStore.cs:153`
- Visible load:
  - `src/LlmTornado.Cli.Core/Storage/SqliteConversationStore.cs:187`
- Full load:
  - `src/LlmTornado.Cli.Core/Storage/SqliteConversationStore.cs:196`
- Attachment-resolved load:
  - `src/LlmTornado.Cli.Core/Storage/SqliteConversationStore.cs:205`
- Compression visibility filtering in SQL:
  - `src/LlmTornado.Cli.Core/Storage/SqliteConversationStore.cs:513`

### Manual save path

- `/conversation save` persists current memory:
  - `src/LlmTornado.Cli/Commands/ConversationCommand.cs:37`
- `/exit` autosaves current memory:
  - `src/LlmTornado.Cli/Commands/ExitCommand.cs:27`

---

## Exact Serve Path (what gets sent to model)

### Runtime conversation source

- Runtime appends incoming user message to its own in-memory conversation:
  - `src/LlmTornado.Agents/ChatRuntime/RuntimeConfigurations/SingletonRuntimeConfiguration.cs:53`
- Runtime calls agent with full runtime conversation as `appendMessages`:
  - `src/LlmTornado.Agents/ChatRuntime/RuntimeConfigurations/SingletonRuntimeConfiguration.cs:56`

### System prompt injection behavior

- Agent instructions created from CLI `BuildSystemPrompt`:
  - `src/LlmTornado.Cli.Core/AgentBuilder.cs:282`
- Skills catalog injected into system prompt:
  - `src/LlmTornado.Cli.Core/AgentBuilder.cs:303`
- During request build, system message added from `agent.Instructions`:
  - `src/LlmTornado.Agents/TornadoRunner.cs:299`
- Existing system-role messages in historical context are skipped:
  - `src/LlmTornado.Agents/TornadoRunner.cs:311`

Implication: there is generally one system message per outbound request, not an accumulating list of system messages.

---

## Skill Prompt Behavior and Context Risk

### Metadata vs full instructions

- Skill metadata in system prompt:
  - `src/LlmTornado.Cli.Core/Skills/SkillManager.cs:95`
- Full skill body loaded from `SKILL.md` on activation:
  - `src/LlmTornado.Cli.Core/Skills/SkillManager.cs:79`
  - `src/LlmTornado.Cli.Core/Skills/SkillLoader.cs:191`
  - `src/LlmTornado.Cli.Core/Skills/SkillLoader.cs:196`
- `load_skill` tool returns instruction text:
  - `src/LlmTornado.Cli.Core/AgentBuilder.cs:353`

Implication: activated skill payload can be large and retained in conversation/tool context, which can inflate subsequent turns.

---

## Compression/Summarization vs Actual Request Context

### Implemented compression logic

- Compression thresholds and token estimates:
  - `src/LlmTornado.Cli.Core/Memory/CompressionStrategy.cs:43`
  - `src/LlmTornado.Cli.Core/Memory/CompressionStrategy.cs:96`
- Summary inserted as system message:
  - `src/LlmTornado.Cli.Core/Memory/MessageSummarizer.cs:58`

### Critical mismatch

- CLI runs `memory.MaybeSummarize()` after each response:
  - `src/LlmTornado.Cli/Program.cs:265`
- But request context is sourced from runtime conversation (`SingletonRuntimeConfiguration.Conversation`), not directly from `ConversationMemoryManager._messages`.
- Therefore, summary compaction in `ConversationMemoryManager` may not reduce request payload unless runtime state is synchronized with compressed memory.

---

## Additional Verified Gaps

### Gap A: CLI incremental persistence not guaranteed

- `ConversationMemoryManager.AddMessage()` only appends to SQLite when `_conversationId` is non-null:
  - `src/LlmTornado.Cli.Core/Memory/ConversationMemoryManager.cs:79`
  - `src/LlmTornado.Cli.Core/Memory/ConversationMemoryManager.cs:98`
- In CLI startup, manager is created without explicit conversation id:
  - `src/LlmTornado.Cli/Program.cs:121`
- Result: persistence may rely mostly on explicit save/exit pathways.

### Gap B: CLI load command does not bind memory manager to loaded id

- `/conversation load` fetches list and calls `LoadConversation(messages)` overload:
  - `src/LlmTornado.Cli/Commands/ConversationCommand.cs:44`
  - `src/LlmTornado.Cli/Commands/ConversationCommand.cs:50`
- This bypasses `LoadConversation(string conversationId)` path:
  - `src/LlmTornado.Cli.Core/Memory/ConversationMemoryManager.cs:170`
- Result: loaded conversation id is not reliably tracked for incremental appends.

### Gap C: Blazor load restores UI, not runtime conversation state

- Blazor load fetches full conversation and clears runtime:
  - `src/LlmTornado.Cli.Blazor/Controllers/ChatRuntimeController.Conversations.cs:20`
  - `src/LlmTornado.Cli.Blazor/Controllers/ChatRuntimeController.Conversations.cs:30`
- It then maps to UI messages only; no re-append into runtime conversation.
- On send, it saves runtime messages back to same conversation id:
  - `src/LlmTornado.Cli.Blazor/Controllers/ChatRuntimeController.Chat.cs:62`
  - `src/LlmTornado.Cli.Blazor/Controllers/ChatRuntimeController.Chat.cs:67`
- Risk: loaded thread context may be absent in next model call and/or overwritten in storage.

---

## Why This Matters for Context Window Pressure

1. System prompt repetition is not the primary growth vector; tool and chat history growth is.
2. Large skill bodies (from `load_skill`) can remain in context over many turns.
3. Compression exists but is not currently authoritative for request assembly in CLI runtime flow.
4. Loaded conversations may not faithfully drive runtime context in Blazor path.

---

## Recommended Fix Plan (ordered)

### Fix 1: Make one source of truth for request context

- Before each `runtime.InvokeAsync`, rebuild/sync runtime conversation from `ConversationMemoryManager.GetMessagesForAgent()` (compressed set), or
- Change runtime entry so `appendMessages` is supplied from memory manager rather than runtime local history.

Acceptance criteria:
- After summarization, preflight token count drops on subsequent turns.
- Request payload reflects compressed memory, not stale full runtime history.

### Fix 2: Ensure stable conversation id lifecycle in CLI

- On startup, create/select a current conversation id and call `EnsureConversation`.
- Use `LoadConversation(string conversationId)` in `/conversation load` instead of loading list then calling list-based overload.

Acceptance criteria:
- Incremental append always writes to SQLite for active thread.
- Loading a conversation continues writing into same id unless user starts a new one.

### Fix 3: Rehydrate runtime state on Blazor load

- After loading conversation from store, append each non-system message into runtime conversation state (not only UI).

Acceptance criteria:
- First post-load user message has full expected history in model context.
- Saving after load preserves prior history.

### Fix 4: Constrain skill payload footprint

- Keep system prompt at metadata level only (already true), and
- Add policy to summarize/truncate `load_skill` output in-turn or store normalized compact skill context.

Acceptance criteria:
- Activating a large skill does not permanently balloon context for many subsequent turns.

### Fix 5: Add hard request token budget guard

- At request assembly stage, enforce max context budget with deterministic trimming policy.

Acceptance criteria:
- No request exceeds configured token budget.
- Trimming order is predictable and test-covered.

---

## Fast Start for Next Session (minimal tool calls)

Read these files in this order:

1. `src/LlmTornado.Cli/Program.cs`
2. `src/LlmTornado.Agents/ChatRuntime/RuntimeConfigurations/SingletonRuntimeConfiguration.cs`
3. `src/LlmTornado.Agents/TornadoRunner.cs`
4. `src/LlmTornado.Cli.Core/Memory/ConversationMemoryManager.cs`
5. `src/LlmTornado.Cli.Core/Storage/SqliteConversationStore.cs`
6. `src/LlmTornado.Cli.Core/AgentBuilder.cs`
7. `src/LlmTornado.Cli.Core/Skills/SkillManager.cs`
8. `src/LlmTornado.Cli.Core/Skills/SkillLoader.cs`
9. `src/LlmTornado.Cli.Blazor/Controllers/ChatRuntimeController.Conversations.cs`
10. `src/LlmTornado.Cli.Blazor/Controllers/ChatRuntimeController.Chat.cs`

Then run targeted tests:

```powershell
dotnet test src/LlmTornado.Cli.Tests/LlmTornado.Cli.Tests.csproj --filter Conversation
dotnet test src/LlmTornado.Cli.Tests/LlmTornado.Cli.Tests.csproj --filter Skill
```

If no matching tests exist yet, create focused tests for:
- conversation id continuity
- post-summarization request token reduction
- load->send preserving thread history
- load_skill large payload containment

---

## Suggested Work Items (copy into issue tracker)

- [ ] Wire runtime request context to compressed memory source.
- [ ] Initialize and persist active conversation id in CLI startup.
- [ ] Fix `/conversation load` to bind by id, not list-only load.
- [ ] Rehydrate runtime conversation on Blazor load.
- [ ] Add skill output compaction policy for `load_skill`.
- [ ] Add request token budget guard at send boundary.
- [ ] Add regression tests for all above.

---

## Notes for Agent/CLI Handoff

If this file is provided as initial context to a future coding session, the new session should not need broad repository search. The line-anchored map above identifies all primary call paths and failure points relevant to context window pressure from system/skill prompt handling.
