# Stage 1: Project Setup — LlmTornado.Cli.Blazor

## Goal

Create a new **Razor Class Library** project (`LlmTornado.Cli.Blazor`) that packages reusable Blazor chat UI components. The library uses **no MudBlazor** — plain Blazor with custom CSS custom properties for theming. It references `LlmTornado.Cli.Core` (which transitively brings `LlmTornado`, `LlmTornado.Agents`, and `LlmTornado.Mcp`).

## Why a Razor Class Library?

A Razor Class Library (RCL) uses the `Microsoft.NET.Sdk.Razor` SDK and produces a NuGet-distributable package containing `.razor` components, CSS, JS, and other static assets. Any Blazor app (Server, WASM, or Hybrid) can add a `<PackageReference>` and immediately use the components.

This mirrors how MudBlazor itself is distributed — consumers add one NuGet reference and get all the components.

## Project File

**Path:** `src/LlmTornado.Cli.Blazor/LlmTornado.Cli.Blazor.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

    <PropertyGroup>
        <TargetFrameworks>net10.0;net8.0</TargetFrameworks>
        <LangVersion>preview</LangVersion>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <RootNamespace>LlmTornado.Cli.Blazor</RootNamespace>

        <!-- NuGet metadata (mirrors LlmTornado.Acp pattern) -->
        <PackageId>LlmTornado.Cli.Blazor</PackageId>
        <Version>1.0.0-local</Version>
        <IsPackable>true</IsPackable>
        <Description>Event-driven Blazor chat UI components for LlmTornado AI agents</Description>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="..\LlmTornado.Cli.Core\LlmTornado.Cli.Core.csproj" />
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Markdig" Version="0.40.0" />
    </ItemGroup>

</Project>
```

### Key decisions explained

| Decision | Rationale |
|---|---|
| `Microsoft.NET.Sdk.Razor` | Required for `.razor` component compilation and static asset bundling via `_content/` path |
| `net10.0;net8.0` | Matches `LlmTornado.Acp` dual-target pattern. Covers current (`net8.0`) and latest (`net10.0`) |
| Reference `LlmTornado.Cli.Core` only | Cli.Core transitively brings `LlmTornado`, `LlmTornado.Agents`, `LlmTornado.Mcp`. No need to reference individually |
| `Markdig` package | For rendering assistant markdown responses as HTML. Lightweight, no other dependencies |
| No MudBlazor dependency | Per user choice — plain Blazor + CSS custom properties for maximum portability |

## Transitive Dependency Graph

```
LlmTornado.Cli.Blazor
├── LlmTornado.Cli.Core (net8.0)
│   ├── LlmTornado (core library)
│   ├── LlmTornado.Agents (ChatRuntime, TornadoAgent, events)
│   ├── LlmTornado.Mcp (MCPServer, tool integration)
│   └── YamlDotNet (SKILL.md frontmatter parsing)
└── Markdig (markdown → HTML rendering)
```

## Directory Structure

```
src/LlmTornado.Cli.Blazor/
├── LlmTornado.Cli.Blazor.csproj
├── _Imports.razor                    # Global Razor using directives
├── IChatUi.cs                        # UI manipulation interface
├── IChatUiController.cs              # User-action handler interface
├── Models/                           # UI-layer data models
│   ├── ChatUiMessage.cs
│   ├── ChatUiEventChip.cs
│   ├── ChatUiFile.cs
│   ├── ChatUiModel.cs
│   ├── ChatUiAgent.cs
│   ├── ChatUiConversation.cs
│   └── ToolApprovalRequest.cs
├── Controllers/                      # Default IChatUiController impl
│   ├── ChatRuntimeController.cs
│   └── ChatRuntimeControllerOptions.cs
├── Components/                       # Blazor Razor components
│   ├── TornadoChatPanel.razor
│   ├── TornadoChatPanel.razor.css
│   ├── ChatMessageBubble.razor
│   ├── ChatMessageBubble.razor.css
│   ├── ChatEventChip.razor
│   ├── ChatEventChip.razor.css
│   ├── ToolApprovalBanner.razor
│   ├── ToolApprovalBanner.razor.css
│   ├── FileAttachmentBar.razor
│   ├── FileAttachmentBar.razor.css
│   └── ConversationSidebar.razor
│       ConversationSidebar.razor.css
├── wwwroot/
│   └── tornado-chat.css              # Base theme + CSS custom properties
└── docs/
    ├── stage-1-project-setup.md      # (this file)
    ├── stage-2-ui-data-models.md
    └── ...
```

## `_Imports.razor`

This file provides global `@using` directives for all `.razor` files in the library, reducing boilerplate:

```razor
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Rendering
@using LlmTornado.Cli.Blazor
@using LlmTornado.Cli.Blazor.Models
@using LlmTornado.Cli.Blazor.Components
```

## Solution Integration

Add both the library and demo projects to `src/LlmTornado.slnx`:

```xml
<!-- Add alongside existing root-level projects (LlmTornado.Cli, LlmTornado.Cli.Core) -->
<Project Path="LlmTornado.Cli.Blazor/LlmTornado.Cli.Blazor.csproj" />
<Project Path="LlmTornado.Cli.Blazor.Demo/LlmTornado.Cli.Blazor.Demo.csproj" />
```

## InternalsVisibleTo Update

Update `LlmTornado.Cli.Core.csproj` to grant access to Cli.Core internals:

```xml
<!-- Existing -->
<InternalsVisibleTo Include="LlmTornado.Cli" />
<InternalsVisibleTo Include="LlmTornado.Cli.Tests" />
<InternalsVisibleTo Include="LlmTornado.Acp.Server" />
<!-- New -->
<InternalsVisibleTo Include="LlmTornado.Cli.Blazor" />
```

This allows `ChatRuntimeController` to access internal members of Cli.Core if needed (e.g., internal constructors or helpers).

## Verification

```bash
cd src
dotnet build LlmTornado.Cli.Blazor/LlmTornado.Cli.Blazor.csproj
```

Expected: Clean build on both `net8.0` and `net10.0` targets with 0 errors.

## How This Fits in the Architecture

```
┌─────────────────────────────────────────────────┐
│           Consumer Blazor App                    │
│  (VisualErp.Web, Demo app, any Blazor app)      │
│                                                  │
│  ┌────────────────────────────────────────────┐  │
│  │  <TornadoChatPanel Controller="@ctrl" />   │  │
│  │  (from LlmTornado.Cli.Blazor RCL)         │  │
│  └──────────────┬─────────────────────────────┘  │
│                 │ implements IChatUi              │
│                 │                                 │
│  ┌──────────────▼─────────────────────────────┐  │
│  │  ChatRuntimeController (or custom impl)    │  │
│  │  implements IChatUiController              │  │
│  │  (from LlmTornado.Cli.Blazor)              │  │
│  └──────────────┬─────────────────────────────┘  │
│                 │ uses                            │
│  ┌──────────────▼─────────────────────────────┐  │
│  │  LlmTornado.Cli.Core                       │  │
│  │  (AgentBuilder, SkillManager, MCP, etc.)   │  │
│  └────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

The RCL contains *both* the UI components *and* the default `ChatRuntimeController`. Consumers can use the default controller or implement `IChatUiController` themselves (e.g., to proxy requests to a remote API instead of using `ChatRuntime` directly).
