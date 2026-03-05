# Phase 3: Demo UI — General Settings Panel

## Objective

Create a new `GeneralPanel.razor` component in the demo and wire it as the first tab ("General") in the Settings page. This gives users a UI to view and change the agent's working directory at runtime, triggering a full reload of all CWD-dependent resources.

## Files to Modify

1. **`src/LlmTornado.Cli.Blazor.Demo/Components/Pages/Settings.razor`** — insert "General" tab as tab 0
2. **NEW: `src/LlmTornado.Cli.Blazor.Demo/Components/Settings/GeneralPanel.razor`** — working directory UI

---

## File 1: Settings.razor

### Current State (lines 1–45)

```razor
@page "/settings"
@using LlmTornado.Cli.Blazor
@using LlmTornado.Cli.Core.Providers
@using LlmTornado.Cli.Core.Skills
@using LlmTornado.Cli.Core.Agents

<div class="settings-page">
<h1>Settings</h1>

<div class="settings-tabs">
    <button class="settings-tab @(_tab == 0 ? "settings-tab--active" : "")"
            @onclick="() => _tab = 0">Providers</button>
    <button class="settings-tab @(_tab == 1 ? "settings-tab--active" : "")"
            @onclick="() => _tab = 1">MCP Servers</button>
    <button class="settings-tab @(_tab == 2 ? "settings-tab--active" : "")"
            @onclick="() => _tab = 2">Skills</button>
    <button class="settings-tab @(_tab == 3 ? "settings-tab--active" : "")"
            @onclick="() => _tab = 3">Agents</button>
</div>

<div class="settings-content">
    @switch (_tab)
    {
        case 0:
            <ProvidersPanel />
            break;
        case 1:
            <McpServersPanel />
            break;
        case 2:
            <SkillsPanel />
            break;
        case 3:
            <AgentsPanel />
            break;
    }
</div>
</div>

@code {
    private int _tab;
}
```

### Changes

Insert "General" as tab 0, shift all existing tabs by +1. The `@code` block initializes `_tab` to 0, which now means "General" is the default view.

### After (full file)

```razor
@page "/settings"
@using LlmTornado.Cli.Blazor
@using LlmTornado.Cli.Core.Providers
@using LlmTornado.Cli.Core.Skills
@using LlmTornado.Cli.Core.Agents

<div class="settings-page">
<h1>Settings</h1>

<div class="settings-tabs">
    <button class="settings-tab @(_tab == 0 ? "settings-tab--active" : "")"
            @onclick="() => _tab = 0">General</button>
    <button class="settings-tab @(_tab == 1 ? "settings-tab--active" : "")"
            @onclick="() => _tab = 1">Providers</button>
    <button class="settings-tab @(_tab == 2 ? "settings-tab--active" : "")"
            @onclick="() => _tab = 2">MCP Servers</button>
    <button class="settings-tab @(_tab == 3 ? "settings-tab--active" : "")"
            @onclick="() => _tab = 3">Skills</button>
    <button class="settings-tab @(_tab == 4 ? "settings-tab--active" : "")"
            @onclick="() => _tab = 4">Agents</button>
</div>

<div class="settings-content">
    @switch (_tab)
    {
        case 0:
            <GeneralPanel />
            break;
        case 1:
            <ProvidersPanel />
            break;
        case 2:
            <McpServersPanel />
            break;
        case 3:
            <SkillsPanel />
            break;
        case 4:
            <AgentsPanel />
            break;
    }
</div>
</div>

@code {
    private int _tab;
}
```

### Diff Preview

```diff
 <div class="settings-tabs">
+    <button class="settings-tab @(_tab == 0 ? "settings-tab--active" : "")"
+            @onclick="() => _tab = 0">General</button>
     <button class="settings-tab @(_tab == 1 ? "settings-tab--active" : "")"
-            @onclick="() => _tab = 0">Providers</button>
+            @onclick="() => _tab = 1">Providers</button>
     <button class="settings-tab @(_tab == 2 ? "settings-tab--active" : "")"
-            @onclick="() => _tab = 1">MCP Servers</button>
+            @onclick="() => _tab = 2">MCP Servers</button>
     <button class="settings-tab @(_tab == 3 ? "settings-tab--active" : "")"
-            @onclick="() => _tab = 2">Skills</button>
+            @onclick="() => _tab = 3">Skills</button>
     <button class="settings-tab @(_tab == 4 ? "settings-tab--active" : "")"
-            @onclick="() => _tab = 3">Agents</button>
+            @onclick="() => _tab = 4">Agents</button>
 </div>
 
 <div class="settings-content">
     @switch (_tab)
     {
         case 0:
+            <GeneralPanel />
+            break;
+        case 1:
             <ProvidersPanel />
             break;
-        case 1:
+        case 2:
             <McpServersPanel />
             break;
-        case 2:
+        case 3:
             <SkillsPanel />
             break;
-        case 3:
+        case 4:
             <AgentsPanel />
             break;
     }
```

---

## File 2: GeneralPanel.razor (NEW)

### Design Principles

1. **Match existing patterns**: Use the same CSS classes (`settings-section`, `settings-form`, `settings-form__field`, `tornado-chat__btn`, `status-badge`) as other settings panels.
2. **Minimal**: One input field, one button, read-only resolved paths display, status message.
3. **Error handling**: Catch `DirectoryNotFoundException` from the controller and display an error message.
4. **Immediate feedback**: Show a success message with the new path after a successful change. Show resolved paths that update after the change.

### Layout

```
┌──────────────────────────────────────────────────────────────────┐
│ Working directory affects where the agent looks for skills,     │
│ agents, and MCP configuration relative to the project root.     │
│                                                                  │
│ ┌──────────────────────────────────────────────────────────────┐ │
│ │ Working Directory                                            │ │
│ │ ┌────────────────────────────────────────────┐ ┌───────────┐ │ │
│ │ │ C:\Users\...\LLMTornado\src\Demo           │ │  Apply    │ │ │
│ │ └────────────────────────────────────────────┘ └───────────┘ │ │
│ └──────────────────────────────────────────────────────────────┘ │
│                                                                  │
│ ✓ Working directory changed successfully                        │
│                                                                  │
│ Resolved Paths                                                   │
│ ┌──────────────────────────────────────────────────────────────┐ │
│ │ Skills Directory    C:\...\Demo\skills                       │ │
│ │ Agents Directory    C:\...\Demo\agents                       │ │
│ │ MCP Config Path     C:\...\Demo\mcp.json                    │ │
│ └──────────────────────────────────────────────────────────────┘ │
└──────────────────────────────────────────────────────────────────┘
```

### Full Component Source

```razor
@using LlmTornado.Cli.Blazor

<div class="settings-section">
    <p class="settings-section__desc">
        Working directory affects where the agent looks for skills, agents, and MCP
        configuration relative to the project root. Changing this reloads all
        directory-dependent resources.
    </p>

    <div class="settings-form">
        <div class="settings-form__field">
            <label>Working Directory</label>
            <div style="display: flex; gap: 8px; align-items: flex-start;">
                <input type="text"
                       @bind="_inputPath"
                       @bind:event="oninput"
                       placeholder="Enter absolute path..."
                       style="flex: 1; font-family: 'Consolas', monospace; font-size: 12px;" />
                <button class="tornado-chat__btn tornado-chat__btn--primary"
                        @onclick="ApplyAsync"
                        disabled="@_isApplying">
                    @(_isApplying ? "Applying…" : "Apply")
                </button>
            </div>
        </div>
    </div>

    @if (!string.IsNullOrEmpty(_statusMessage))
    {
        <p class="@(_isError ? "settings-section__error" : "settings-section__success")"
           style="margin-top: 12px; font-size: 13px;">
            @_statusMessage
        </p>
    }

    <div style="margin-top: 20px;">
        <h3 style="font-size: 14px; margin: 0 0 12px;">Resolved Paths</h3>
        <table class="settings-table">
            <thead>
                <tr>
                    <th>Resource</th>
                    <th>Path</th>
                    <th>Status</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td>Skills Directory</td>
                    <td class="settings-table__mono">@_skillsDir</td>
                    <td>
                        @if (Directory.Exists(_skillsDir))
                        {
                            <span class="status-badge status-badge--ok">exists</span>
                        }
                        else
                        {
                            <span class="status-badge status-badge--missing">not found</span>
                        }
                    </td>
                </tr>
                <tr>
                    <td>Agents Directory</td>
                    <td class="settings-table__mono">@_agentsDir</td>
                    <td>
                        @if (Directory.Exists(_agentsDir))
                        {
                            <span class="status-badge status-badge--ok">exists</span>
                        }
                        else
                        {
                            <span class="status-badge status-badge--missing">not found</span>
                        }
                    </td>
                </tr>
                <tr>
                    <td>MCP Config</td>
                    <td class="settings-table__mono">@_mcpPath</td>
                    <td>
                        @if (File.Exists(_mcpPath))
                        {
                            <span class="status-badge status-badge--ok">exists</span>
                        }
                        else
                        {
                            <span class="status-badge status-badge--missing">not found</span>
                        }
                    </td>
                </tr>
            </tbody>
        </table>
    </div>
</div>

@code {
    [Inject] public ISettingsController Settings { get; set; } = default!;

    private string _inputPath = "";
    private string _skillsDir = "";
    private string _agentsDir = "";
    private string _mcpPath = "";
    private string? _statusMessage;
    private bool _isError;
    private bool _isApplying;

    protected override void OnInitialized()
    {
        _inputPath = Settings.GetWorkingDirectory();
        RefreshResolvedPaths();
    }

    private async Task ApplyAsync()
    {
        if (string.IsNullOrWhiteSpace(_inputPath))
        {
            _statusMessage = "Path cannot be empty.";
            _isError = true;
            return;
        }

        _isApplying = true;
        _statusMessage = null;
        StateHasChanged();

        try
        {
            await Settings.ChangeWorkingDirectoryAsync(_inputPath.Trim());
            _inputPath = Settings.GetWorkingDirectory();
            RefreshResolvedPaths();
            _statusMessage = $"Working directory changed to {_inputPath}";
            _isError = false;
        }
        catch (DirectoryNotFoundException ex)
        {
            _statusMessage = ex.Message;
            _isError = true;
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error: {ex.Message}";
            _isError = true;
        }
        finally
        {
            _isApplying = false;
        }
    }

    private void RefreshResolvedPaths()
    {
        _skillsDir = Settings.GetSkillsDirectory();
        _agentsDir = Settings.GetAgentsDirectory();
        _mcpPath = Settings.GetMcpConfigPath();
    }
}
```

### Component Behavior

| Action | Result |
|--------|--------|
| Page loads | Input pre-populated with current `GetWorkingDirectory()` value. Resolved paths table shows current skills/agents/MCP paths with exists/not-found status. |
| User types new path + clicks Apply | `ChangeWorkingDirectoryAsync(path)` called. On success: input updates to normalized path, resolved paths refresh, success message shown. Skills/Agents/MCP tabs now show resources from new directory. |
| User types non-existent path + clicks Apply | `DirectoryNotFoundException` caught. Error message displayed. Nothing changes. |
| User types empty path + clicks Apply | Client-side validation catches it — error message "Path cannot be empty." |
| While applying | Button disabled with "Applying…" text. Prevents double-submission. |

### CSS Additions

Two small utility classes are needed for the status/error message. These should be added to `app.css`:

```css
/* ── General panel status messages ── */
.settings-section__success {
    color: #166534;
    font-weight: 500;
}

.settings-section__error {
    color: #991b1b;
    font-weight: 500;
}
```

**Location in app.css**: Insert after the existing `.settings-section__actions` block (around line 127).

---

## Dependency Injection Notes

The panel injects `ISettingsController` directly (not `IChatUiController`). This works because `ChatRuntimeController` implements both interfaces, and both are registered as scoped services pointing to the same instance in `ServiceCollectionExtensions.cs`:

```csharp
services.AddScoped<IChatUiController>(sp => sp.GetRequiredService<ChatRuntimeController>());
services.AddScoped<ISettingsController>(sp => sp.GetRequiredService<ChatRuntimeController>());
```

The other settings panels (SkillsPanel, AgentsPanel, McpServersPanel) follow this same pattern.

---

## Interaction with Other Tabs

After changing the working directory:

| Tab | Expected Effect |
|-----|-----------------|
| **Providers** | No change — API keys come from environment variables, not from CWD |
| **MCP Servers** | Shows servers from new `mcp.json` (or empty if no config in new dir) |
| **Skills** | Shows skills from new `skills/` directory (global skills unchanged) |
| **Agents** | Shows custom agents from new `agents/` directory (built-in agents unchanged) |

The user does NOT need to manually click "Refresh" on other tabs. The `ChangeWorkingDirectoryAsync` implementation in Phase 2 already reloads all managers internally.

---

## Verification

After all three phases, build and test:

```bash
cd src && dotnet build LlmTornado.Cli.Blazor.Demo/LlmTornado.Cli.Blazor.Demo.csproj
```

Then launch and test:

1. Navigate to Settings — "General" tab should be the default (first tab)
2. Should see current working directory pre-populated
3. Resolved paths table should show skills/agents/MCP paths with status badges
4. Change to a directory that has `skills/`, `agents/`, `mcp.json` → verify success message + resolved paths update
5. Switch to Skills tab → verify skills from new directory
6. Switch to Agents tab → verify agents from new directory
7. Switch to MCP tab → verify servers from new directory
8. Change to non-existent path → verify error message appears
9. Change to a directory without subdirectories → verify graceful empty state in other tabs

## Dependencies

- **Depends on**: Phase 1 (interface) + Phase 2 (controller implementation)
- **Blocks**: Nothing (final phase)
