# Stage 9: Solution Integration & Verification

## Goal

Wire the new `LlmTornado.Cli.Blazor` library and its demo app into the existing solution, configure project references, and verify the build compiles cleanly.

---

## 9.1: Solution File Changes

Add both new projects to `LlmTornado.slnx`. They logically belong alongside the existing CLI projects (Cli, Cli.Core, Cli.Tests).

**File:** `src/LlmTornado.slnx`

Add these two lines in the root project section (e.g. after the `LlmTornado.Cli.Tests` entry):

```xml
  <Project Path="LlmTornado.Cli.Blazor/LlmTornado.Cli.Blazor.csproj" />
  <Project Path="LlmTornado.Cli.Blazor.Demo/LlmTornado.Cli.Blazor.Demo.csproj" />
```

The full `LlmTornado.slnx` after modification (relevant section):

```xml
  <Project Path="LlmTornado.Cli/LlmTornado.Cli.csproj" />
  <Project Path="LlmTornado.Cli.Core/LlmTornado.Cli.Core.csproj" />
  <Project Path="LlmTornado.Cli.Tests/LlmTornado.Cli.Tests.csproj" />
  <Project Path="LlmTornado.Cli.Blazor/LlmTornado.Cli.Blazor.csproj" />
  <Project Path="LlmTornado.Cli.Blazor.Demo/LlmTornado.Cli.Blazor.Demo.csproj" />
```

---

## 9.2: Project Reference Chain

```
LlmTornado.Cli.Blazor.Demo
  └─► LlmTornado.Cli.Blazor
       └─► LlmTornado.Cli.Core
            ├─► LlmTornado.Agents
            │    └─► LlmTornado
            └─► LlmTornado.Mcp
                 └─► LlmTornado
```

The Blazor library (`Cli.Blazor`) references only `Cli.Core`. The demo app references only `Cli.Blazor` (which transitively brings everything else).

### `LlmTornado.Cli.Blazor.csproj` reference:

```xml
<ItemGroup>
    <ProjectReference Include="..\LlmTornado.Cli.Core\LlmTornado.Cli.Core.csproj" />
</ItemGroup>
```

### `LlmTornado.Cli.Blazor.Demo.csproj` reference:

```xml
<ItemGroup>
    <ProjectReference Include="..\LlmTornado.Cli.Blazor\LlmTornado.Cli.Blazor.csproj" />
</ItemGroup>
```

---

## 9.3: Complete File Manifest

Here is every file that will be created across all stages:

```
src/
├── LlmTornado.Cli.Blazor/
│   ├── LlmTornado.Cli.Blazor.csproj
│   ├── _Imports.razor
│   ├── Models/
│   │   ├── ChatUiMessage.cs
│   │   ├── ChatUiEventChip.cs
│   │   ├── ChatUiFile.cs
│   │   ├── ChatUiModel.cs
│   │   ├── ChatUiAgent.cs
│   │   ├── ChatUiConversation.cs
│   │   └── ToolApprovalRequest.cs
│   ├── IChatUi.cs
│   ├── IChatUiController.cs
│   ├── ChatRuntimeController.cs
│   ├── ServiceCollectionExtensions.cs
│   ├── Components/
│   │   ├── TornadoChatPanel.razor
│   │   ├── ChatMessageBubble.razor
│   │   ├── ChatEventChip.razor
│   │   ├── ToolApprovalBanner.razor
│   │   ├── FileAttachmentBar.razor
│   │   └── ConversationSidebar.razor
│   ├── wwwroot/
│   │   └── tornado-chat.css
│   └── docs/        ← (planning docs, not shipped)
│       ├── stage-1-project-setup.md
│       ├── stage-2-ui-data-models.md
│       ├── stage-3-ichatui-interface.md
│       ├── stage-4-ichatuicontroller-interface.md
│       ├── stage-5-chatruntimecontroller.md
│       ├── stage-6-blazor-ui-components.md
│       ├── stage-7-css-theming.md
│       ├── stage-8-demo-app.md
│       └── stage-9-solution-integration.md
│
├── LlmTornado.Cli.Blazor.Demo/
│   ├── LlmTornado.Cli.Blazor.Demo.csproj
│   ├── Program.cs
│   ├── wwwroot/
│   │   └── app.css
│   └── Components/
│       ├── App.razor
│       ├── Routes.razor
│       ├── _Imports.razor
│       ├── Layout/
│       │   └── MainLayout.razor
│       ├── Pages/
│       │   ├── Chat.razor
│       │   └── Settings.razor
│       └── Settings/
│           ├── ProvidersPanel.razor
│           ├── McpServersPanel.razor
│           ├── SkillsPanel.razor
│           └── AgentsPanel.razor
```

**Total: ~30 files** (14 library + 13 demo + docs)

---

## 9.4: Build Verification

After creating all files, run the build to verify:

```powershell
cd src

# Build the library alone
dotnet build LlmTornado.Cli.Blazor/LlmTornado.Cli.Blazor.csproj

# Build the demo app (transitively builds library)
dotnet build LlmTornado.Cli.Blazor.Demo/LlmTornado.Cli.Blazor.Demo.csproj

# Build the entire solution to check for no regressions
dotnet build LlmTornado.slnx
```

Expected outcome:
- 0 errors in Cli.Blazor and Cli.Blazor.Demo
- Pre-existing warnings in other projects (e.g. LlmTornado.Agents) are normal
- Both net10.0 and net8.0 TFMs should compile for the library

---

## 9.5: Runtime Verification

```powershell
cd src/LlmTornado.Cli.Blazor.Demo
dotnet run
```

Then open `https://localhost:5001` and verify:
1. Chat page loads with model/agent dropdowns
2. Sidebar shows (empty conversation list initially)
3. Typing a message and clicking Send calls the controller
4. Settings page has 4 working tabs
5. Providers tab shows detected keys from env vars
6. Dark/light theme toggle works

---

## 9.6: Excluding Docs from Package

The `docs/` folder in `Cli.Blazor` is for planning only. Ensure it's not shipped in the NuGet package:

```xml
<!-- In LlmTornado.Cli.Blazor.csproj -->
<ItemGroup>
    <None Remove="docs\**" />
</ItemGroup>
```

---

## 9.7: InternalsVisibleTo (Optional)

If unit tests are added later for the Blazor library:

```xml
<!-- In LlmTornado.Cli.Blazor.csproj -->
<ItemGroup>
    <InternalsVisibleTo Include="LlmTornado.Cli.Blazor.Tests" />
</ItemGroup>
```

---

## 9.8: Wiring into an External App (e.g. VisualErp.Web)

Any Blazor app can consume the library:

1. **Add project/package reference:**
   ```xml
   <PackageReference Include="LlmTornado.Cli.Blazor" Version="1.0.0" />
   ```

2. **Register services in `Program.cs`:**
   ```csharp
   builder.Services.AddChatRuntime();
   ```

3. **Link the stylesheet:**
   ```html
   <link rel="stylesheet" href="_content/LlmTornado.Cli.Blazor/tornado-chat.css" />
   ```

4. **Embed the component in any page:**
   ```razor
   @inject IChatUiController ChatController

   <div style="height: 600px;">
       <TornadoChatPanel Controller="ChatController" />
   </div>
   ```

That's it — the component handles everything else internally.

---

## 9.9: Implementation Order

When actually writing code, implement in this order for smooth incremental builds:

| Step | What | Why |
|------|------|-----|
| 1 | Create `.csproj` files + solution entries | Enables `dotnet restore` |
| 2 | Models (`Models/*.cs`) | No dependencies, compile first |
| 3 | `IChatUi.cs` + `IChatUiController.cs` | Interfaces used by everything |
| 4 | `ChatRuntimeController.cs` | Core logic, depends on interfaces + Cli.Core |
| 5 | `ServiceCollectionExtensions.cs` | DI registration |
| 6 | Build & fix library | Verify library compiles cleanly |
| 7 | Razor components (`Components/*.razor`) | Depends on models + interfaces |
| 8 | `tornado-chat.css` | Static asset, no deps |
| 9 | Build library again | Verify with Razor components |
| 10 | Demo app scaffolding | `Program.cs`, `App.razor`, layout |
| 11 | Demo pages | Chat page, Settings page + panels |
| 12 | `app.css` | Demo styling |
| 13 | Full build + run | End-to-end verification |
