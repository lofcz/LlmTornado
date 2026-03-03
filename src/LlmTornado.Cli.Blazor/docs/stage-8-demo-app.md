# Stage 8: Demo Application

## Goal

Create `LlmTornado.Cli.Blazor.Demo` — a standalone Blazor Server app that showcases the chat component and provides a full **Settings** page for managing API keys, MCP servers, skills, and agents.

---

## 8.1: Project Setup

**Path:** `src/LlmTornado.Cli.Blazor.Demo/LlmTornado.Cli.Blazor.Demo.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <LangVersion>preview</LangVersion>
        <Nullable>enable</Nullable>
        <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="..\LlmTornado.Cli.Blazor\LlmTornado.Cli.Blazor.csproj" />
    </ItemGroup>
</Project>
```

---

## 8.2: Program.cs — DI & Routing

```csharp
using LlmTornado.Cli.Blazor;
using LlmTornado.Cli.Blazor.Demo.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register the ChatRuntimeController with options:
builder.Services.AddChatRuntime(options =>
{
    // The controller auto-detects providers from env vars.
    // Optional: override directories for skills/agents/conversations
    // options.SkillsDirectory = "skills";
    // options.AgentsDirectory = "agents";
    // options.ConversationsDirectory = "conversations";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
```

---

## 8.3: App Layout

**Path:** `Components/App.razor`

```razor
<!DOCTYPE html>
<html data-theme="light">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>LlmTornado Chat Demo</title>
    <link rel="stylesheet" href="_content/LlmTornado.Cli.Blazor/tornado-chat.css" />
    <link rel="stylesheet" href="app.css" />
    <HeadOutlet @rendermode="InteractiveServer" />
</head>
<body>
    <Routes @rendermode="InteractiveServer" />
    <script src="_framework/blazor.web.js"></script>
</body>
</html>
```

**Path:** `Components/Layout/MainLayout.razor`

```razor
@inherits LayoutComponentBase

<div class="demo-layout">
    <nav class="demo-nav">
        <a href="/" class="demo-nav__brand">🌪 Tornado Chat</a>
        <div class="demo-nav__links">
            <NavLink href="/" Match="NavLinkMatch.All">Chat</NavLink>
            <NavLink href="/settings">Settings</NavLink>
        </div>
        <button class="demo-nav__theme-toggle" onclick="toggleTheme()">
            🌓
        </button>
    </nav>
    <main class="demo-content">
        @Body
    </main>
</div>
```

---

## 8.4: Chat Page (`/`)

The main page embeds `TornadoChatPanel` at full height.

**Path:** `Components/Pages/Chat.razor`

```razor
@page "/"
@using LlmTornado.Cli.Blazor
@using LlmTornado.Cli.Blazor.Components

<div class="demo-chat-page">
    <TornadoChatPanel Controller="Controller"
                      ShowSidebar="true"
                      StreamingRenderIntervalMs="50" />
</div>

@code {
    [Inject] public IChatUiController Controller { get; set; } = default!;
}
```

Styling:

```css
/* app.css */
.demo-chat-page {
    height: calc(100vh - 52px); /* subtract nav height */
}
```

---

## 8.5: Settings Page (`/settings`)

The settings page is organized into **tabbed sections** using native HTML. Each tab corresponds to one management area.

**Path:** `Components/Pages/Settings.razor`

```razor
@page "/settings"
@using LlmTornado.Cli.Blazor
@using LlmTornado.Cli.Core.Providers
@using LlmTornado.Cli.Core.Skills
@using LlmTornado.Cli.Core.Agents

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

@code {
    private int _tab;
}
```

---

## 8.6: ProvidersPanel — API Key Management

Shows each of the 12 supported providers, their environment variable name, whether a key is detected, and allows the user to set/clear keys (stored in environment variables or a local config file).

**Path:** `Components/Settings/ProvidersPanel.razor`

```razor
@using LlmTornado.Cli.Core.Providers
@using LlmTornado.Code

<div class="settings-section">
    <p class="settings-section__desc">
        API keys are detected from environment variables. You can override them here
        for this session (changes are not persisted to the OS environment).
    </p>

    <table class="settings-table">
        <thead>
            <tr>
                <th>Provider</th>
                <th>Environment Variable</th>
                <th>Status</th>
                <th>API Key</th>
                <th>Models</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var provider in _providers)
            {
                <tr>
                    <td><strong>@provider.Name</strong></td>
                    <td><code>@provider.EnvVar</code></td>
                    <td>
                        @if (provider.IsDetected)
                        {
                            <span class="status-badge status-badge--ok">✓ Active</span>
                        }
                        else
                        {
                            <span class="status-badge status-badge--missing">✗ Not set</span>
                        }
                    </td>
                    <td>
                        <div class="settings-key-input">
                            <input type="@(provider.ShowKey ? "text" : "password")"
                                   value="@provider.MaskedKey"
                                   placeholder="sk-..."
                                   @onchange="e => HandleKeyChange(provider, e.Value?.ToString())" />
                            <button class="tornado-chat__btn tornado-chat__btn--icon"
                                    @onclick="() => provider.ShowKey = !provider.ShowKey"
                                    title="Toggle visibility">
                                @(provider.ShowKey ? "🙈" : "👁")
                            </button>
                        </div>
                    </td>
                    <td>@provider.ModelCount models</td>
                </tr>
            }
        </tbody>
    </table>
</div>

@code {
    [Inject] public IChatUiController Controller { get; set; } = default!;

    private List<ProviderRow> _providers = [];

    protected override void OnInitialized()
    {
        // Build rows from the 12 known providers
        _providers =
        [
            Row("OpenAI",       "OPENAI_API_KEY",      LLmProviders.OpenAi),
            Row("Anthropic",    "ANTHROPIC_API_KEY",    LLmProviders.Anthropic),
            Row("Google",       "GOOGLE_API_KEY",       LLmProviders.Google),
            Row("Groq",         "GROQ_API_KEY",         LLmProviders.Groq),
            Row("Cohere",       "COHERE_API_KEY",       LLmProviders.Cohere),
            Row("Mistral",      "MISTRAL_API_KEY",      LLmProviders.Mistral),
            Row("DeepSeek",     "DEEPSEEK_API_KEY",     LLmProviders.DeepSeek),
            Row("xAI",          "XAI_API_KEY",          LLmProviders.XAi),
            Row("Perplexity",   "PERPLEXITY_API_KEY",   LLmProviders.Perplexity),
            Row("OpenRouter",   "OPENROUTER_API_KEY",   LLmProviders.OpenRouter),
            Row("DeepInfra",    "DEEPINFRA_API_KEY",    LLmProviders.DeepInfra),
            Row("Voyage",       "VOYAGE_API_KEY",       LLmProviders.Voyage),
        ];
    }

    private ProviderRow Row(string name, string envVar, LLmProviders provider)
    {
        string? key = Environment.GetEnvironmentVariable(envVar);
        bool detected = !string.IsNullOrWhiteSpace(key);
        int modelCount = detected
            ? ProviderDetector.GetModelsForProvider(provider).Count
            : 0;

        return new ProviderRow
        {
            Name = name,
            EnvVar = envVar,
            Provider = provider,
            IsDetected = detected,
            RawKey = key,
            ModelCount = modelCount,
        };
    }

    private void HandleKeyChange(ProviderRow row, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            Environment.SetEnvironmentVariable(row.EnvVar, null);
            row.IsDetected = false;
            row.RawKey = null;
        }
        else
        {
            Environment.SetEnvironmentVariable(row.EnvVar, value);
            row.IsDetected = true;
            row.RawKey = value;
            row.ModelCount = ProviderDetector.GetModelsForProvider(row.Provider).Count;
        }
        StateHasChanged();
    }

    private sealed class ProviderRow
    {
        public string Name { get; init; } = "";
        public string EnvVar { get; init; } = "";
        public LLmProviders Provider { get; init; }
        public bool IsDetected { get; set; }
        public string? RawKey { get; set; }
        public bool ShowKey { get; set; }
        public int ModelCount { get; set; }
        public string MaskedKey => ShowKey
            ? (RawKey ?? "")
            : (RawKey is { Length: > 8 }
                ? RawKey[..4] + "•••" + RawKey[^4..]
                : (RawKey is not null ? "••••••" : ""));
    }
}
```

---

## 8.7: McpServersPanel — MCP Server Status

Shows the MCP servers loaded from `mcp.json`, their type (stdio/http), connection status, and tool count.

**Path:** `Components/Settings/McpServersPanel.razor`

```razor
@using LlmTornado.Cli.Core.Mcp

<div class="settings-section">
    <p class="settings-section__desc">
        MCP servers are loaded from <code>mcp.json</code>. Edit the file to add or remove servers.
    </p>

    @if (_servers.Count == 0)
    {
        <p class="settings-section__empty">No MCP servers configured.</p>
    }
    else
    {
        <table class="settings-table">
            <thead>
                <tr>
                    <th>Server</th>
                    <th>Type</th>
                    <th>Command / URL</th>
                    <th>Status</th>
                    <th>Tools</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var server in _servers)
                {
                    <tr>
                        <td><strong>@server.Name</strong></td>
                        <td>
                            <span class="status-badge">
                                @(server.IsStdio ? "stdio" : "http")
                            </span>
                        </td>
                        <td class="settings-table__mono">
                            @(server.IsStdio ? server.Command : server.Url)
                        </td>
                        <td>
                            @if (server.IsConnected)
                            {
                                <span class="status-badge status-badge--ok">Connected</span>
                            }
                            else
                            {
                                <span class="status-badge status-badge--missing">Disconnected</span>
                            }
                        </td>
                        <td>@server.ToolCount</td>
                    </tr>
                }
            </tbody>
        </table>
    }

    <div class="settings-section__actions">
        <button class="tornado-chat__btn" @onclick="ReloadConfig">
            ↻ Reload mcp.json
        </button>
    </div>
</div>

@code {
    [Inject] public IChatUiController Controller { get; set; } = default!;

    private List<McpServerRow> _servers = [];

    protected override void OnInitialized()
    {
        LoadServers();
    }

    private void LoadServers()
    {
        // The controller exposes the underlying runtime state; for now
        // we can get MCP info from the controller's GetMcpServers() method
        // (to be added to IChatUiController or a separate ISettingsProvider).
        // For the demo, we illustrate the expected data shape:
        _servers = [];
    }

    private void ReloadConfig()
    {
        LoadServers();
        StateHasChanged();
    }

    private sealed class McpServerRow
    {
        public string Name { get; init; } = "";
        public bool IsStdio { get; init; }
        public string? Command { get; init; }
        public string? Url { get; init; }
        public bool IsConnected { get; set; }
        public int ToolCount { get; set; }
    }
}
```

---

## 8.8: SkillsPanel — Skill Management

**Path:** `Components/Settings/SkillsPanel.razor`

```razor
@using LlmTornado.Cli.Core.Skills

<div class="settings-section">
    <p class="settings-section__desc">
        Skills are loaded from the global (<code>%APPDATA%/llmtornado/skills/</code>) and
        project-local (<code>./skills/</code>) directories.
    </p>

    @if (_skills.Count == 0)
    {
        <p class="settings-section__empty">No skills found.</p>
    }
    else
    {
        <table class="settings-table">
            <thead>
                <tr>
                    <th>Name</th>
                    <th>Source</th>
                    <th>Description</th>
                    <th>Tools</th>
                    <th>Enabled</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var skill in _skills)
                {
                    <tr class="@(!skill.Enabled ? "settings-table__row--disabled" : "")">
                        <td><strong>@skill.Name</strong></td>
                        <td>
                            <span class="status-badge">@skill.Source</span>
                        </td>
                        <td>@skill.Description</td>
                        <td>@skill.AllowedTools.Count</td>
                        <td>
                            <label class="toggle">
                                <input type="checkbox"
                                       checked="@skill.Enabled"
                                       @onchange="e => HandleToggle(skill, (bool)(e.Value ?? false))" />
                                <span class="toggle__slider"></span>
                            </label>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    }

    <div class="settings-section__actions">
        <button class="tornado-chat__btn" @onclick="RefreshSkills">
            ↻ Refresh Skills
        </button>
        <label class="tornado-chat__btn">
            📂 Import Skill
            <InputFile OnChange="HandleImport" style="display:none" accept=".md,.zip" />
        </label>
    </div>
</div>

@code {
    [Inject] public IChatUiController Controller { get; set; } = default!;

    private List<Skill> _skills = [];

    protected override void OnInitialized()
    {
        LoadSkills();
    }

    private void LoadSkills()
    {
        // Controller wraps SkillManager; get all skills
        // _skills = Controller.GetSkills();
        _skills = [];
    }

    private void HandleToggle(Skill skill, bool enabled)
    {
        skill.Enabled = enabled;
        // Controller.SetSkillEnabled(skill.Name, enabled);
        StateHasChanged();
    }

    private async Task HandleImport(InputFileChangeEventArgs e)
    {
        // Copy uploaded .md or .zip to skills directory, then refresh
        // await Controller.ImportSkill(e.File);
        LoadSkills();
    }

    private void RefreshSkills()
    {
        LoadSkills();
        StateHasChanged();
    }
}
```

---

## 8.9: AgentsPanel — Agent Persona Management

**Path:** `Components/Settings/AgentsPanel.razor`

```razor
@using LlmTornado.Cli.Core.Agents

<div class="settings-section">
    <p class="settings-section__desc">
        Agent personas define the AI's behavior, system prompt, and available tools.
        Built-in agents ship with the library; custom agents can be created in the
        <code>agents/</code> directory.
    </p>

    @if (_agents.Count == 0)
    {
        <p class="settings-section__empty">No agent personas found.</p>
    }
    else
    {
        <table class="settings-table">
            <thead>
                <tr>
                    <th>Name</th>
                    <th>Source</th>
                    <th>Description</th>
                    <th>Capabilities</th>
                    <th></th>
                </tr>
            </thead>
            <tbody>
                @foreach (var agent in _agents)
                {
                    <tr class="@(agent.Name == _activeAgentName ? "settings-table__row--active" : "")">
                        <td>
                            <strong>@agent.Name</strong>
                            @if (agent.Name == _activeAgentName)
                            {
                                <span class="status-badge status-badge--ok">Active</span>
                            }
                        </td>
                        <td>
                            <span class="status-badge">@agent.Source</span>
                        </td>
                        <td>@agent.Description</td>
                        <td>
                            @if (agent.HasCapabilityCuration)
                            {
                                <span title="This agent curates available tools">⚙ Curated</span>
                            }
                            else
                            {
                                <span>All tools</span>
                            }
                        </td>
                        <td>
                            @if (agent.Name != _activeAgentName)
                            {
                                <button class="tornado-chat__btn"
                                        @onclick="() => ActivateAgent(agent.Name)">
                                    Activate
                                </button>
                            }
                            else
                            {
                                <button class="tornado-chat__btn"
                                        @onclick="DeactivateAgent">
                                    Deactivate
                                </button>
                            }
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    }

    <div class="settings-section__actions">
        <button class="tornado-chat__btn" @onclick="RefreshAgents">
            ↻ Refresh Agents
        </button>
        <button class="tornado-chat__btn" @onclick="CreateNewAgent">
            + New Agent
        </button>
    </div>

    @if (_showCreateForm)
    {
        <div class="settings-form">
            <h3>Create New Agent</h3>
            <div class="settings-form__field">
                <label>Name</label>
                <input type="text" @bind="_newAgentName" placeholder="my-agent" />
            </div>
            <div class="settings-form__field">
                <label>Description</label>
                <input type="text" @bind="_newAgentDescription" placeholder="A helpful assistant..." />
            </div>
            <div class="settings-form__field">
                <label>System Prompt</label>
                <textarea @bind="_newAgentPrompt" rows="6"
                          placeholder="You are a helpful AI assistant..."></textarea>
            </div>
            <div class="settings-form__actions">
                <button class="tornado-chat__btn tornado-chat__btn--primary"
                        @onclick="SaveNewAgent">
                    Save
                </button>
                <button class="tornado-chat__btn"
                        @onclick="() => _showCreateForm = false">
                    Cancel
                </button>
            </div>
        </div>
    }
</div>

@code {
    [Inject] public IChatUiController Controller { get; set; } = default!;

    private List<AgentRow> _agents = [];
    private string? _activeAgentName;

    // Create form state
    private bool _showCreateForm;
    private string _newAgentName = "";
    private string _newAgentDescription = "";
    private string _newAgentPrompt = "";

    protected override void OnInitialized()
    {
        LoadAgents();
    }

    private void LoadAgents()
    {
        // Controller wraps AgentDefinitionManager; get all personas
        // var manager = Controller.GetAgentManager();
        // _agents = manager.GetAllPersonas().Select(AgentRow.From).ToList();
        // _activeAgentName = manager.ActivePersonaName;
        _agents = [];
        _activeAgentName = null;
    }

    private async Task ActivateAgent(string name)
    {
        await Controller.SelectAgentAsync(name);
        _activeAgentName = name;
        StateHasChanged();
    }

    private async Task DeactivateAgent()
    {
        await Controller.SelectAgentAsync(null);
        _activeAgentName = null;
        StateHasChanged();
    }

    private void CreateNewAgent()
    {
        _showCreateForm = true;
        _newAgentName = "";
        _newAgentDescription = "";
        _newAgentPrompt = "";
    }

    private void SaveNewAgent()
    {
        if (string.IsNullOrWhiteSpace(_newAgentName)) return;

        // Write AGENT.md to agents directory
        // Controller.CreateAgent(_newAgentName, _newAgentDescription, _newAgentPrompt);
        _showCreateForm = false;
        LoadAgents();
    }

    private void RefreshAgents()
    {
        LoadAgents();
        StateHasChanged();
    }

    private sealed class AgentRow
    {
        public string Name { get; init; } = "";
        public string Source { get; init; } = "";
        public string Description { get; init; } = "";
        public bool HasCapabilityCuration { get; init; }
    }
}
```

---

## 8.10: Settings Styles

**Path:** `wwwroot/app.css` (appended)

```css
/* ── Demo layout ── */
.demo-layout {
    display: flex;
    flex-direction: column;
    height: 100vh;
}

.demo-nav {
    display: flex;
    align-items: center;
    height: 52px;
    padding: 0 16px;
    background: var(--tc-bg-secondary, #f5f5f5);
    border-bottom: 1px solid var(--tc-border, #e0e0e0);
    gap: 16px;
    flex-shrink: 0;
}

.demo-nav__brand {
    font-weight: 600;
    font-size: 16px;
    text-decoration: none;
    color: var(--tc-text, #1a1a1a);
}

.demo-nav__links {
    display: flex;
    gap: 12px;
}

.demo-nav__links a {
    text-decoration: none;
    color: var(--tc-text-secondary, #666);
    padding: 4px 8px;
    border-radius: 4px;
}

.demo-nav__links a.active {
    background: var(--tc-accent-subtle, #eff6ff);
    color: var(--tc-accent, #2563eb);
}

.demo-nav__theme-toggle {
    margin-left: auto;
    background: none;
    border: none;
    font-size: 18px;
    cursor: pointer;
}

.demo-content {
    flex: 1;
    overflow: auto;
}

/* ── Settings ── */
.settings-tabs {
    display: flex;
    gap: 0;
    border-bottom: 2px solid var(--tc-border, #e0e0e0);
    padding: 0 16px;
}

.settings-tab {
    padding: 10px 20px;
    border: none;
    background: none;
    cursor: pointer;
    font-size: 14px;
    color: var(--tc-text-secondary, #666);
    border-bottom: 2px solid transparent;
    margin-bottom: -2px;
    transition: color 0.15s, border-color 0.15s;
}

.settings-tab:hover {
    color: var(--tc-text, #1a1a1a);
}

.settings-tab--active {
    color: var(--tc-accent, #2563eb);
    border-bottom-color: var(--tc-accent, #2563eb);
    font-weight: 500;
}

.settings-content {
    padding: 20px;
}

.settings-section__desc {
    margin: 0 0 16px;
    color: var(--tc-text-secondary, #666);
    font-size: 13px;
}

.settings-section__empty {
    color: var(--tc-text-muted, #999);
    font-style: italic;
    padding: 20px 0;
}

.settings-section__actions {
    display: flex;
    gap: 8px;
    margin-top: 16px;
}

/* ── Settings Table ── */
.settings-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 13px;
}

.settings-table th {
    text-align: left;
    padding: 8px 12px;
    border-bottom: 1px solid var(--tc-border, #e0e0e0);
    color: var(--tc-text-secondary, #666);
    font-weight: 600;
    font-size: 12px;
    text-transform: uppercase;
    letter-spacing: 0.5px;
}

.settings-table td {
    padding: 10px 12px;
    border-bottom: 1px solid color-mix(in srgb, var(--tc-border, #e0e0e0) 50%, transparent);
    vertical-align: middle;
}

.settings-table__mono {
    font-family: 'Consolas', 'Monaco', monospace;
    font-size: 12px;
}

.settings-table__row--disabled {
    opacity: 0.5;
}

.settings-table__row--active {
    background: var(--tc-accent-subtle, #eff6ff);
}

/* ── Status badges ── */
.status-badge {
    display: inline-flex;
    padding: 2px 8px;
    border-radius: 10px;
    font-size: 11px;
    font-weight: 500;
    background: var(--tc-chip-bg, #f0f0f0);
    border: 1px solid var(--tc-chip-border, #d0d0d0);
}

.status-badge--ok {
    background: var(--tc-chip-success, #dcfce7);
    border-color: var(--tc-chip-success-border, #86efac);
    color: #166534;
}

.status-badge--missing {
    background: var(--tc-chip-fail, #fee2e2);
    border-color: var(--tc-chip-fail-border, #fca5a5);
    color: #991b1b;
}

/* ── Key input ── */
.settings-key-input {
    display: flex;
    align-items: center;
    gap: 4px;
}

.settings-key-input input {
    width: 200px;
    padding: 4px 8px;
    border: 1px solid var(--tc-input-border, #d0d0d0);
    border-radius: 4px;
    font-size: 12px;
    font-family: monospace;
    background: var(--tc-input-bg, #fff);
    color: var(--tc-text, #1a1a1a);
}

.settings-key-input input:focus {
    outline: none;
    border-color: var(--tc-input-focus, #2563eb);
}

/* ── Toggle switch ── */
.toggle {
    position: relative;
    display: inline-block;
    width: 36px;
    height: 20px;
    cursor: pointer;
}

.toggle input {
    opacity: 0;
    width: 0;
    height: 0;
}

.toggle__slider {
    position: absolute;
    inset: 0;
    background: var(--tc-chip-bg, #ccc);
    border-radius: 10px;
    transition: background 0.2s;
}

.toggle__slider::before {
    content: '';
    position: absolute;
    left: 2px;
    top: 2px;
    width: 16px;
    height: 16px;
    background: #fff;
    border-radius: 50%;
    transition: transform 0.2s;
}

.toggle input:checked + .toggle__slider {
    background: var(--tc-accent, #2563eb);
}

.toggle input:checked + .toggle__slider::before {
    transform: translateX(16px);
}

/* ── Settings form ── */
.settings-form {
    margin-top: 20px;
    padding: 16px;
    background: var(--tc-bg-secondary, #f5f5f5);
    border: 1px solid var(--tc-border, #e0e0e0);
    border-radius: 8px;
}

.settings-form h3 {
    margin: 0 0 12px;
}

.settings-form__field {
    margin-bottom: 12px;
}

.settings-form__field label {
    display: block;
    font-size: 12px;
    font-weight: 600;
    margin-bottom: 4px;
    color: var(--tc-text-secondary, #666);
}

.settings-form__field input,
.settings-form__field textarea {
    width: 100%;
    padding: 8px 10px;
    border: 1px solid var(--tc-input-border, #d0d0d0);
    border-radius: 4px;
    font-size: 13px;
    background: var(--tc-input-bg, #fff);
    color: var(--tc-text, #1a1a1a);
    font-family: inherit;
}

.settings-form__field textarea {
    resize: vertical;
}

.settings-form__actions {
    display: flex;
    gap: 8px;
}
```

---

## 8.11: Theme Toggle Script

```html
<!-- in App.razor, before closing </body> -->
<script>
    function toggleTheme() {
        const html = document.documentElement;
        const current = html.getAttribute('data-theme');
        const next = current === 'dark' ? 'light' : 'dark';
        html.setAttribute('data-theme', next);
        localStorage.setItem('theme', next);
    }

    // Restore saved theme
    const saved = localStorage.getItem('theme');
    if (saved) document.documentElement.setAttribute('data-theme', saved);
</script>
```

---

## 8.12: Directory Structure

```
LlmTornado.Cli.Blazor.Demo/
├── LlmTornado.Cli.Blazor.Demo.csproj
├── Program.cs
├── wwwroot/
│   └── app.css
└── Components/
    ├── App.razor
    ├── Routes.razor
    ├── _Imports.razor
    ├── Layout/
    │   └── MainLayout.razor
    ├── Pages/
    │   ├── Chat.razor
    │   └── Settings.razor
    └── Settings/
        ├── ProvidersPanel.razor
        ├── McpServersPanel.razor
        ├── SkillsPanel.razor
        └── AgentsPanel.razor
```

---

## Key Design Decisions

### Why separate panels per settings tab?

Each panel is a self-contained component that can manage its own state and data fetching. This keeps `Settings.razor` thin (just a tab switcher) and makes each section independently testable.

### Why session-only key overrides?

Setting `Environment.SetEnvironmentVariable` in-process does not persist to the OS registry. This is intentional for the demo — users can test with temporary keys without modifying their machine. A production app would persist to encrypted storage.

### Why commented-out controller calls?

The `IChatUiController` interface defined in Stage 4 covers chat operations. Settings management (skills, agents, MCP) may warrant a separate `ISettingsProvider` interface or extension of the controller. The demo shows the UI patterns; the actual wiring depends on how exposed the Cli.Core managers are. During implementation (Stage 9), these stubs will be connected to real `SkillManager`, `AgentDefinitionManager`, and `McpConfigLoader` instances.
