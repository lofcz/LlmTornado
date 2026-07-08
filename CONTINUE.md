# CONTINUE — CLI Improvement Plan (Phases 1-5)

**Context**: Implementing the approved plan in `C:\Users\johnl\.claude\plans\my-llmtornado-cli-text-flickering-goblet.md` (local-model backbone + critical features). Branch `Tornodo-CLI-Alpha`. Test baseline was 413 passed / 1 skipped before Phase 2 tests.

## DONE — Phase 1 (complete, tests green)
- **Cancellation fixed** (2 layers):
  - `SingletonRuntimeConfiguration.cs` (Agents lib): re-arms spent `cts`, links with caller token, passes `RunnerOptions` + linked token to `Agent.Run`.
  - `TornadoRunner.cs` (Agents lib): threaded `cancellationToken` into `GetNewResponse` → `GetResponseRichSafe(handler, ct)` and `HandleStreaming(..., ct)` → `StreamResponseRich(handler, ct)`. (Root cause: those calls stomped `RequestParameters.CancellationToken` with `default`.)
- `Program.cs`: handles `AgentRunnerCancelledEvent`/`MaxTurnsReached`/`MaxTokensReached`/`RequestPrepared`/`UsageReceived`; suppresses OCE error events; Esc-to-interrupt via new `Cli/Input/TurnInterruptWatcher.cs` (+ `ConsoleInputGate`; suspended in `ToolApprovalManager` prompts); ReplLoop chat body consolidated into `RunChatTurnAsync`; after-turn status line via `WriteTurnStatusLine` → `ConsoleRenderer.WriteDimStatus` (new).
- `Cli.Core/Telemetry/SessionTelemetry.cs` (new): folds usage events; `EstimatedNextPromptTokens` = last prompt+completion.
- Input history: `Cli/Input/PersistentInputHistory.cs` (new), `CommandHistoryNavigator` seed ctor + `EntryAdded` event, `LineEditor` seed/sink params, `CliStorage.InputHistoryPath`, wired in ReplLoop.
- `/reasoning` command (new `Commands/ReasoningCommand.cs`, takes `Action<string?> applyEffort`); `AgentBuilder.SetReasoningEffort` + `CliAgentBuilder` forwarder; registered in Program.
- Deleted dead `AgentSettings.MaxTurnsBeforeSummary` (+ fixed CliStorageTests).
- `AgentBuilder.Build`: sets `runtimeConfig.RunnerOptions` (TokenLimit = model ContextTokens or 2M, all ThrowOn* false).
- New tests: `RuntimeCancellationTests.cs` (incl. HttpListener mid-request abort test), `SessionTelemetryTests.cs`, `PersistentInputHistoryTests.cs`, `ReasoningCommandTests.cs`.

## DONE — Phase 2 (code complete, FINAL TEST RUN NOT DONE)
- `ConversationMemoryManager`: `ReportActualUsage(prompt, completion)` (pending), sealed at `SyncFrom` (`_actualTokensAtSync`/`_messageCountAtSync`), `EstimateCurrentTokens()` = real + estimated tail, `HasActualTokenCount`, invalidated on compression/trim/New/Load; `MaybeSummarize` passes actual to `Analyze`; `EnforceHardBudget` uses `EstimateCurrentTokens()`; `ConfigureCompressionThresholds(trigger, target)`.
- `CompressionStrategy`: `Analyze(..., int? actualTotalTokens)`; retuned defaults `UncompressedThreshold 0.80`, `ReCompressionThreshold 0.85`, new `LargeMessageUtilizationFloor 0.50` (large msg only triggers when util ≥ floor); overall-utilization trigger added.
- `AgentSettings` new keys: `compression_trigger_utilization`, `compression_target_utilization`, `tool_result_truncation` (true), `tool_result_max_tokens` (4000).
- `Cli.Core/Tools/ToolResultTruncator.cs` (new): head 70%/tail 30%, marker, surrogate-safe; wired in `AgentBuilder.Build` via `_agent.ToolResultProcessor` (cap = min(setting, window/8), re-read per call; exempt select_tools/list_all_tools).
- Stable prompt prefix: `AgentBuilder.BuildSystemPrompt` no longer appends cwd (says env is in `<env>` message); new `AgentBuilder.EnvironmentTag` + `BuildEnvironmentMessage()` (cwd/platform/date); `ManagedConversationRuntimeConfiguration` ctor takes `Func<ChatMessage>? environmentMessageFactory`, `RehydrateConversation` pins fresh env first + drops stale env messages from history.
- Program.cs: forwards usage events to `_memoryManager.ReportActualUsage`; applies compression thresholds after memory ctor; optimizer model set to null for `LLmProviders.Custom` at startup (session-only, prints notice).
- New tests written: `ToolResultTruncatorTests.cs`, `MemoryRealUsageTests.cs` (incl. `CompressionStrategyRealTokenTests`), `EnvironmentMessageTests.cs`, plus `Build_SystemPromptIsStable_EnvMessageCarriesCwd` added to `CliAgentBuilderTests.cs`.

## NEXT STEP (interrupted here)
1. Run: `dotnet test src/LlmTornado.Cli.Tests --filter "FullyQualifiedName!~LiveIntegrationTests"`
2. Fix failures. Expected risk spots: pre-existing `CompressionStrategy`/`ConversationMemoryTests` tests that assumed the old 0.60 threshold or the unconditional large-message trigger — update those to the new thresholds; my new tests may have constructor-signature mismatches.
3. Mark task #8 completed.

## REMAINING PHASES (per plan file — read it for details)
- **Phase 3** (task #9): OpenAI-compat endpoints — `openai_compat_endpoints` settings + `TORNADO_OPENAI_COMPAT` env; new `Providers/OpenAiCompatEndpoint.cs` + `OpenAiCompatProber.cs` (GET /models); `DetectedProvider.EndpointName`/`DedicatedApi`; `ProviderDetectionResult.GetApiForModel`; `AgentBuilder.SetModel(model, api)` + `ConversationMemoryManager.UpdateApi`; `/endpoint` command; `/model` grouping + refresh; unknown ctx → cap → 8192 fallback.
- **Phase 4** (task #10): auto-resume (`SqliteConversationStore.GetMostRecentConversationId`/`ListRecent`, `--continue`/`--resume` args, `/resume` picker via WizardSupport, `auto_resume` setting); `/config` command (temperature/max_output_tokens/system_prompt_file, apply in AgentBuilder.Build next to ApplyReasoningEffort); `/context stats` shows SessionTelemetry real numbers.
- **Phase 5** (task #11): native tools `Cli.Core/Tools/Native/` (read_file/write_file/edit_file/glob/grep/list_dir/shell) under existing `McpSessionPolicy` checks; registered before MCP in `CollectTools` + name-dedup (first wins); settings `native_tools` (true), `builtin_desktop_commander` (false → `BuiltInMcpServerCatalog` consults it), `auto_approve_native_read_tools` (true, via `PreApproveSkillTools`); ≤4-line system-prompt blurb.

## Verification per phase (from plan)
Esc mid-stream works; ctx % matches server-reported prompt tokens; truncation marker on big tool results; `/endpoint add lmstudio http://localhost:1234/v1` → `/model` lists; `--continue` restores; native tools work with Desktop Commander disabled (no npx). Full suite must stay green.
