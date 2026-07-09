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

## DONE — Phase 2 (complete, tests green: 435 passed)
- `ConversationMemoryManager`: `ReportActualUsage(prompt, completion)` (pending), sealed at `SyncFrom` (`_actualTokensAtSync`/`_messageCountAtSync`), `EstimateCurrentTokens()` = real + estimated tail, `HasActualTokenCount`, invalidated on compression/trim/New/Load; `MaybeSummarize` passes actual to `Analyze`; `EnforceHardBudget` uses `EstimateCurrentTokens()`; `ConfigureCompressionThresholds(trigger, target)`.
- `CompressionStrategy`: `Analyze(..., int? actualTotalTokens)`; retuned defaults `UncompressedThreshold 0.80`, `ReCompressionThreshold 0.85`, new `LargeMessageUtilizationFloor 0.50` (large msg only triggers when util ≥ floor); overall-utilization trigger added.
- `AgentSettings` new keys: `compression_trigger_utilization`, `compression_target_utilization`, `tool_result_truncation` (true), `tool_result_max_tokens` (4000).
- `Cli.Core/Tools/ToolResultTruncator.cs` (new): head 70%/tail 30%, marker, surrogate-safe; wired in `AgentBuilder.Build` via `_agent.ToolResultProcessor` (cap = min(setting, window/8), re-read per call; exempt select_tools/list_all_tools).
- Stable prompt prefix: `AgentBuilder.BuildSystemPrompt` no longer appends cwd (says env is in `<env>` message); new `AgentBuilder.EnvironmentTag` + `BuildEnvironmentMessage()` (cwd/platform/date); `ManagedConversationRuntimeConfiguration` ctor takes `Func<ChatMessage>? environmentMessageFactory`, `RehydrateConversation` pins fresh env first + drops stale env messages from history.
- Program.cs: forwards usage events to `_memoryManager.ReportActualUsage`; applies compression thresholds after memory ctor; optimizer model set to null for `LLmProviders.Custom` at startup (session-only, prints notice).
- New tests: `ToolResultTruncatorTests.cs`, `MemoryRealUsageTests.cs` (incl. `CompressionStrategyRealTokenTests`), `EnvironmentMessageTests.cs`, plus `Build_SystemPromptIsStable_EnvMessageCarriesCwd` added to `CliAgentBuilderTests.cs`.
- Fixed pre-existing `Analyze_DetectsLargeMessages` for utilization floor gating.

## DONE — Phase 3 (complete, tests green: 451 passed / 1 skipped; UNCOMMITTED)
- `Providers/OpenAiCompatEndpoint.cs` (new): settings record + `Merge(settings, env)` + `ParseEnv` for `TORNADO_OPENAI_COMPAT`.
- `Providers/OpenAiCompatProber.cs` (new): GET `{base_url}/models` → ChatModels; `ResolveContextTokens` fallback chain (endpoint ctx → compression cap → 8192).
- `ProviderDetector.Detect(endpoints, warn)` overload; `ProviderDetectionResult.GetApiForModel(model)` routes Custom models to their endpoint's dedicated `TornadoApi`.
- `AgentBuilder.SetModel(model, api, handler)` overload; `ConversationMemoryManager.UpdateApi` → `MessageSummarizer.UpdateApi`.
- `Commands/EndpointCommand.cs` (new): `/endpoint list|add|remove`, persists settings, re-probes.
- `ModelCommand`: `/model set <model|endpoint/model>`, `/model refresh` (re-runs detection), api routing on set.
- `Program.cs`: merges endpoints at startup (line ~75), resolves context tokens via prober, `GetApiForModel` for the active model, registers EndpointCommand.
- Tests: `OpenAiCompatEndpointTests.cs` (17 tests).

## DONE — Phase 4 (complete, tests green)
- `SqliteConversationStore.GetMostRecentConversationId()`; `--continue`/`-c`/`--resume <id>` args in `Program.RunAsync` + `auto_resume` setting feed `conversationId` into the memory-manager ctor (missing id → warning + fresh start).
- `Commands/ResumeCommand.cs` (new): `/resume` numbered picker over 10 most recent (stdin gated via `ConsoleInputGate.Suspend`), `/resume <id>` direct; loads via `ConversationConfig.LoadConversation`.
- `Commands/ConfigCommand.cs` (new, delegate-injected for testability): `/config` effective table; `temperature <0..2|off>`, `max-output-tokens <n|off>`, `system-prompt <path|off>` (rebuilds agent). Settings keys `temperature`, `max_output_tokens`, `system_prompt_file`, `auto_resume`.
- `AgentBuilder`: `ApplySamplingOptions()` (Options.Temperature/MaxTokens, applied in Build + live), `ReadSystemPromptFile()` replaces persona layer only.
- `/context stats` shows measured vs estimated tokens, last-turn request/output/reasoning counts, compression events (SessionTelemetry param on ContextCommand).
- Tests: `Phase4SessionTests.cs` (recency queries + 12 ConfigCommand cases).

## DONE — Phase 5 (complete, tests green: 494 passed / 1 skipped)
- `Cli.Core/Tools/Native/NativeToolkit.cs` (new): `read_file` (numbered lines, offset/limit, 2000-line cap), `write_file`, `edit_file` (unique-match or replace_all), `glob` (hand-rolled `GlobToRegex`, skips .git/node_modules/bin/obj, 20k-file walk cap), `grep` (regex w/ 2s timeout, binary sniff, glob filter, max_results), `list_dir`, `shell` (cmd.exe /c | /bin/sh -c, async pipe readers, kill-tree on timeout, 30k output cap). All paths through `McpSessionPolicy` (`NativeToolContext` resolves cwd/policy per call).
- Registration: in `CollectTools` before MCP tools + generic name-dedup (first wins) at the end; read-only tools pre-approved when `auto_approve_native_read_tools` (default true); ≤3-line system-prompt blurb when enabled.
- Desktop Commander now **opt-in**: `builtin_desktop_commander` (default false) gates the built-in server in `McpConfigLoader.LoadMergedAsync` (name stays reserved). Settings key `native_tools` (default true).
- Tests: `NativeToolkitTests.cs` (24: round-trips, edit errors, glob/grep behavior, policy denials, shell exec/exit-code/timeout, GlobToRegex cases), `NativeToolRegistrationTests.cs` (4: default/disabled registration, dedup shadowing, prompt blurb); 2 pre-existing McpConfig tests updated for the opt-in default + new skipped-by-default test.

## STATUS: ALL 5 PHASES COMPLETE
Full suite: 494 passed / 1 skipped. Live smoke test verified: provider detection with `[ollama]` endpoint grouping, no npx launch by default, `/config`, `/endpoint list`, `/reasoning` working.

Remaining manual verification (needs interactive terminal + local model):
- Esc mid-stream on a real streaming response (watcher + re-armed cts)
- ctx % status line vs server-reported prompt tokens; llama.cpp `prompt eval count` staying ≈ new-tokens-only across turns (prefix cache)
- `--continue` restores last conversation; native read_file/edit_file/shell through approval prompts
