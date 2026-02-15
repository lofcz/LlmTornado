using System.Text;
using System.Text.Json;
using LlmTornado.Acp;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using ChatMessageRoles = LlmTornado.Code.ChatMessageRoles;

namespace LlmTornado.Tests;

/// <summary>
/// Tests for the Agent Client Protocol (ACP) implementation.
/// Covers JSON-RPC models, protocol constants, Tornado extensions, and server message processing.
/// </summary>
[TestFixture]
public class AcpTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    #region JSON-RPC Model Serialization

    [Test]
    public void JsonRpcRequest_Serialization_RoundTrips()
    {
        AcpJsonRpcRequest request = new()
        {
            Id = 1,
            Method = AcpMethods.Initialize,
            Params = new { protocolVersion = 1 }
        };

        string json = JsonSerializer.Serialize(request, JsonOptions);
        AcpJsonRpcRequest? deserialized = JsonSerializer.Deserialize<AcpJsonRpcRequest>(json, JsonOptions);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.JsonRpc, Is.EqualTo("2.0"));
        Assert.That(deserialized.Method, Is.EqualTo(AcpMethods.Initialize));
    }

    [Test]
    public void JsonRpcRequest_DefaultValues_AreCorrect()
    {
        AcpJsonRpcRequest request = new();

        Assert.That(request.JsonRpc, Is.EqualTo("2.0"));
        Assert.That(request.Method, Is.EqualTo(string.Empty));
        Assert.That(request.Id, Is.Null);
        Assert.That(request.Params, Is.Null);
    }

    [Test]
    public void JsonRpcResponse_Serialization_RoundTrips()
    {
        AcpJsonRpcResponse response = new()
        {
            Id = 42,
            Result = new { status = "ok" }
        };

        string json = JsonSerializer.Serialize(response, JsonOptions);
        AcpJsonRpcResponse? deserialized = JsonSerializer.Deserialize<AcpJsonRpcResponse>(json, JsonOptions);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.JsonRpc, Is.EqualTo("2.0"));
    }

    [Test]
    public void JsonRpcErrorResponse_Serialization_RoundTrips()
    {
        AcpJsonRpcErrorResponse errorResponse = new()
        {
            Id = 1,
            Error = new AcpError
            {
                Code = AcpErrorCodes.MethodNotFound,
                Message = "Method not found"
            }
        };

        string json = JsonSerializer.Serialize(errorResponse, JsonOptions);
        AcpJsonRpcErrorResponse? deserialized = JsonSerializer.Deserialize<AcpJsonRpcErrorResponse>(json, JsonOptions);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Error.Code, Is.EqualTo(AcpErrorCodes.MethodNotFound));
        Assert.That(deserialized.Error.Message, Is.EqualTo("Method not found"));
    }

    [Test]
    public void JsonRpcNotification_HasNoId()
    {
        AcpJsonRpcNotification notification = new()
        {
            Method = AcpMethods.Cancel,
            Params = new { sessionId = "s1" }
        };

        string json = JsonSerializer.Serialize(notification, JsonOptions);

        Assert.That(json, Does.Not.Contain("\"id\""));
        Assert.That(json, Does.Contain("\"method\""));
    }

    [Test]
    public void JsonRpcNotification_Deserialization_Works()
    {
        string json = """{"jsonrpc":"2.0","method":"session/cancel","params":{"sessionId":"abc"}}""";
        AcpJsonRpcNotification? notification = JsonSerializer.Deserialize<AcpJsonRpcNotification>(json, JsonOptions);

        Assert.That(notification, Is.Not.Null);
        Assert.That(notification!.Method, Is.EqualTo(AcpMethods.Cancel));
        Assert.That(notification.JsonRpc, Is.EqualTo("2.0"));
    }

    [Test]
    public void AcpError_DefaultValues()
    {
        AcpError error = new();

        Assert.That(error.Code, Is.EqualTo(0));
        Assert.That(error.Message, Is.EqualTo(string.Empty));
        Assert.That(error.Data, Is.Null);
    }

    [Test]
    public void AcpError_WithData_Serializes()
    {
        AcpError error = new()
        {
            Code = AcpErrorCodes.InternalError,
            Message = "Something went wrong",
            Data = new { detail = "stack trace" }
        };

        string json = JsonSerializer.Serialize(error, JsonOptions);
        Assert.That(json, Does.Contain("\"detail\""));
        Assert.That(json, Does.Contain("\"code\":-32603"));
    }

    #endregion

    #region Protocol Constants / Error Codes

    [Test]
    public void AcpErrorCodes_HaveCorrectValues()
    {
        Assert.That(AcpErrorCodes.ParseError, Is.EqualTo(-32700));
        Assert.That(AcpErrorCodes.InvalidRequest, Is.EqualTo(-32600));
        Assert.That(AcpErrorCodes.MethodNotFound, Is.EqualTo(-32601));
        Assert.That(AcpErrorCodes.InvalidParams, Is.EqualTo(-32602));
        Assert.That(AcpErrorCodes.InternalError, Is.EqualTo(-32603));
        Assert.That(AcpErrorCodes.AuthenticationRequired, Is.EqualTo(-32000));
        Assert.That(AcpErrorCodes.ResourceNotFound, Is.EqualTo(-32002));
    }

    [Test]
    public void AcpMethods_HaveCorrectValues()
    {
        Assert.That(AcpMethods.Initialize, Is.EqualTo("initialize"));
        Assert.That(AcpMethods.Authenticate, Is.EqualTo("authenticate"));
        Assert.That(AcpMethods.NewSession, Is.EqualTo("session/new"));
        Assert.That(AcpMethods.LoadSession, Is.EqualTo("session/load"));
        Assert.That(AcpMethods.Prompt, Is.EqualTo("session/prompt"));
        Assert.That(AcpMethods.Cancel, Is.EqualTo("session/cancel"));
        Assert.That(AcpMethods.Update, Is.EqualTo("session/update"));
        Assert.That(AcpMethods.SetMode, Is.EqualTo("session/set_mode"));
        Assert.That(AcpMethods.SetConfigOption, Is.EqualTo("session/set_config_option"));
        Assert.That(AcpMethods.RequestPermission, Is.EqualTo("session/request_permission"));
        Assert.That(AcpMethods.ReadTextFile, Is.EqualTo("fs/read_text_file"));
        Assert.That(AcpMethods.WriteTextFile, Is.EqualTo("fs/write_text_file"));
        Assert.That(AcpMethods.CreateTerminal, Is.EqualTo("terminal/create"));
        Assert.That(AcpMethods.TerminalOutput, Is.EqualTo("terminal/output"));
        Assert.That(AcpMethods.ReleaseTerminal, Is.EqualTo("terminal/release"));
        Assert.That(AcpMethods.WaitForTerminalExit, Is.EqualTo("terminal/wait_for_exit"));
        Assert.That(AcpMethods.KillTerminal, Is.EqualTo("terminal/kill"));
    }

    [Test]
    public void AcpStopReasons_HaveCorrectValues()
    {
        Assert.That(AcpStopReasons.EndTurn, Is.EqualTo("end_turn"));
        Assert.That(AcpStopReasons.MaxTokens, Is.EqualTo("max_tokens"));
        Assert.That(AcpStopReasons.MaxTurnRequests, Is.EqualTo("max_turn_requests"));
        Assert.That(AcpStopReasons.Refusal, Is.EqualTo("refusal"));
        Assert.That(AcpStopReasons.Cancelled, Is.EqualTo("cancelled"));
    }

    [Test]
    public void AcpContentBlockTypes_HaveCorrectValues()
    {
        Assert.That(AcpContentBlockTypes.Text, Is.EqualTo("text"));
        Assert.That(AcpContentBlockTypes.Image, Is.EqualTo("image"));
        Assert.That(AcpContentBlockTypes.Audio, Is.EqualTo("audio"));
        Assert.That(AcpContentBlockTypes.ResourceLink, Is.EqualTo("resource_link"));
        Assert.That(AcpContentBlockTypes.Resource, Is.EqualTo("resource"));
    }

    [Test]
    public void AcpSessionUpdateTypes_HaveCorrectValues()
    {
        Assert.That(AcpSessionUpdateTypes.UserMessageChunk, Is.EqualTo("user_message_chunk"));
        Assert.That(AcpSessionUpdateTypes.AgentMessageChunk, Is.EqualTo("agent_message_chunk"));
        Assert.That(AcpSessionUpdateTypes.AgentThoughtChunk, Is.EqualTo("agent_thought_chunk"));
        Assert.That(AcpSessionUpdateTypes.ToolCall, Is.EqualTo("tool_call"));
        Assert.That(AcpSessionUpdateTypes.ToolCallUpdate, Is.EqualTo("tool_call_update"));
        Assert.That(AcpSessionUpdateTypes.Plan, Is.EqualTo("plan"));
        Assert.That(AcpSessionUpdateTypes.AvailableCommandsUpdate, Is.EqualTo("available_commands_update"));
        Assert.That(AcpSessionUpdateTypes.CurrentModeUpdate, Is.EqualTo("current_mode_update"));
        Assert.That(AcpSessionUpdateTypes.ConfigOptionUpdate, Is.EqualTo("config_option_update"));
    }

    [Test]
    public void AcpPlanEntryPriorities_HaveCorrectValues()
    {
        Assert.That(AcpPlanEntryPriorities.High, Is.EqualTo("high"));
        Assert.That(AcpPlanEntryPriorities.Medium, Is.EqualTo("medium"));
        Assert.That(AcpPlanEntryPriorities.Low, Is.EqualTo("low"));
    }

    [Test]
    public void AcpPlanEntryStatuses_HaveCorrectValues()
    {
        Assert.That(AcpPlanEntryStatuses.Pending, Is.EqualTo("pending"));
        Assert.That(AcpPlanEntryStatuses.InProgress, Is.EqualTo("in_progress"));
        Assert.That(AcpPlanEntryStatuses.Completed, Is.EqualTo("completed"));
    }

    [Test]
    public void AcpToolCallStatuses_HaveCorrectValues()
    {
        Assert.That(AcpToolCallStatuses.Pending, Is.EqualTo("pending"));
        Assert.That(AcpToolCallStatuses.InProgress, Is.EqualTo("in_progress"));
        Assert.That(AcpToolCallStatuses.Completed, Is.EqualTo("completed"));
        Assert.That(AcpToolCallStatuses.Failed, Is.EqualTo("failed"));
    }

    [Test]
    public void AcpToolKinds_HaveCorrectValues()
    {
        Assert.That(AcpToolKinds.Read, Is.EqualTo("read"));
        Assert.That(AcpToolKinds.Edit, Is.EqualTo("edit"));
        Assert.That(AcpToolKinds.Delete, Is.EqualTo("delete"));
        Assert.That(AcpToolKinds.Move, Is.EqualTo("move"));
        Assert.That(AcpToolKinds.Search, Is.EqualTo("search"));
        Assert.That(AcpToolKinds.Execute, Is.EqualTo("execute"));
        Assert.That(AcpToolKinds.Think, Is.EqualTo("think"));
        Assert.That(AcpToolKinds.Fetch, Is.EqualTo("fetch"));
        Assert.That(AcpToolKinds.SwitchMode, Is.EqualTo("switch_mode"));
        Assert.That(AcpToolKinds.Other, Is.EqualTo("other"));
    }

    #endregion

    #region Initialization Models

    [Test]
    public void AcpInitializeRequest_Serialization_RoundTrips()
    {
        AcpInitializeRequest request = new()
        {
            ProtocolVersion = 1,
            ClientInfo = new AcpImplementation { Name = "TestClient", Version = "1.0.0" },
            ClientCapabilities = new AcpClientCapabilities
            {
                Fs = new AcpFileSystemCapability { ReadTextFile = true, WriteTextFile = true },
                Terminal = true
            }
        };

        string json = JsonSerializer.Serialize(request, JsonOptions);
        AcpInitializeRequest? deserialized = JsonSerializer.Deserialize<AcpInitializeRequest>(json, JsonOptions);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.ProtocolVersion, Is.EqualTo(1));
        Assert.That(deserialized.ClientInfo!.Name, Is.EqualTo("TestClient"));
        Assert.That(deserialized.ClientCapabilities.Fs.ReadTextFile, Is.True);
        Assert.That(deserialized.ClientCapabilities.Terminal, Is.True);
    }

    [Test]
    public void AcpInitializeResponse_Serialization_RoundTrips()
    {
        AcpInitializeResponse response = new()
        {
            ProtocolVersion = 1,
            AgentInfo = new AcpImplementation { Name = "TestAgent", Version = "2.0.0", Title = "Test" },
            AgentCapabilities = new AcpAgentCapabilities
            {
                LoadSession = true,
                PromptCapabilities = new AcpPromptCapabilities { Image = true, Audio = false, EmbeddedContext = true }
            },
            AuthMethods = [new AcpAuthMethod { Id = "api_key", Name = "API Key", Description = "Token auth" }]
        };

        string json = JsonSerializer.Serialize(response, JsonOptions);
        AcpInitializeResponse? deserialized = JsonSerializer.Deserialize<AcpInitializeResponse>(json, JsonOptions);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.AgentInfo!.Name, Is.EqualTo("TestAgent"));
        Assert.That(deserialized.AgentInfo.Title, Is.EqualTo("Test"));
        Assert.That(deserialized.AgentCapabilities.LoadSession, Is.True);
        Assert.That(deserialized.AgentCapabilities.PromptCapabilities.Image, Is.True);
        Assert.That(deserialized.AuthMethods, Has.Count.EqualTo(1));
        Assert.That(deserialized.AuthMethods[0].Id, Is.EqualTo("api_key"));
    }

    [Test]
    public void AcpInitializeRequest_Defaults()
    {
        AcpInitializeRequest request = new();

        Assert.That(request.ProtocolVersion, Is.EqualTo(1));
        Assert.That(request.ClientCapabilities, Is.Not.Null);
        Assert.That(request.ClientInfo, Is.Null);
    }

    #endregion

    #region Session Models

    [Test]
    public void AcpNewSessionRequest_Serialization_RoundTrips()
    {
        AcpNewSessionRequest request = new()
        {
            Cwd = "/home/user/project",
            McpServers =
            [
                new AcpMcpServerConfig
                {
                    Name = "test-server",
                    Command = "npx",
                    Args = ["-y", "@test/server"],
                    Env = new Dictionary<string, string> { ["API_KEY"] = "secret" }
                }
            ]
        };

        string json = JsonSerializer.Serialize(request, JsonOptions);
        AcpNewSessionRequest? deserialized = JsonSerializer.Deserialize<AcpNewSessionRequest>(json, JsonOptions);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Cwd, Is.EqualTo("/home/user/project"));
        Assert.That(deserialized.McpServers, Has.Count.EqualTo(1));
        Assert.That(deserialized.McpServers[0].Name, Is.EqualTo("test-server"));
        Assert.That(deserialized.McpServers[0].Args, Has.Count.EqualTo(2));
        Assert.That(deserialized.McpServers[0].Env!["API_KEY"], Is.EqualTo("secret"));
    }

    [Test]
    public void AcpNewSessionResponse_Serialization_RoundTrips()
    {
        AcpNewSessionResponse response = new()
        {
            SessionId = "session-123",
            Modes = new AcpSessionModeState
            {
                CurrentModeId = "agent",
                AvailableModes =
                [
                    new AcpSessionMode { Id = "agent", Name = "Agent", Description = "Coding mode" },
                    new AcpSessionMode { Id = "chat", Name = "Chat" }
                ]
            },
            ConfigOptions =
            [
                new AcpSessionConfigOption
                {
                    Id = "model",
                    Name = "Model",
                    Type = "select",
                    CurrentValue = "gpt-4",
                    Options = [new AcpSessionConfigSelectGroup { Group = "models", Name = "Models", Options = [new AcpSessionConfigSelectOption { Value = "gpt-4", Name = "GPT-4" }] }]
                }
            ]
        };

        string json = JsonSerializer.Serialize(response, JsonOptions);
        AcpNewSessionResponse? deserialized = JsonSerializer.Deserialize<AcpNewSessionResponse>(json, JsonOptions);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.SessionId, Is.EqualTo("session-123"));
        Assert.That(deserialized.Modes!.CurrentModeId, Is.EqualTo("agent"));
        Assert.That(deserialized.Modes.AvailableModes, Has.Count.EqualTo(2));
        Assert.That(deserialized.ConfigOptions, Has.Count.EqualTo(1));
        Assert.That(deserialized.ConfigOptions![0].Options, Has.Count.EqualTo(1));
    }

    [Test]
    public void AcpMcpServerConfig_HttpType_Serializes()
    {
        AcpMcpServerConfig config = new()
        {
            Type = "http",
            Name = "remote-server",
            Url = "https://example.com/mcp",
            Headers = new Dictionary<string, string> { ["Authorization"] = "Bearer token" }
        };

        string json = JsonSerializer.Serialize(config, JsonOptions);
        AcpMcpServerConfig? deserialized = JsonSerializer.Deserialize<AcpMcpServerConfig>(json, JsonOptions);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Type, Is.EqualTo("http"));
        Assert.That(deserialized.Url, Is.EqualTo("https://example.com/mcp"));
        Assert.That(deserialized.Headers, Has.Count.EqualTo(1));
        Assert.That(deserialized.Headers!["Authorization"], Is.EqualTo("Bearer token"));
    }

    #endregion

    #region Prompt Models

    [Test]
    public void AcpPromptRequest_Serialization_RoundTrips()
    {
        AcpPromptRequest request = new()
        {
            SessionId = "s1",
            Prompt =
            [
                new AcpContentBlock { Type = AcpContentBlockTypes.Text, Text = "Hello world" }
            ]
        };

        string json = JsonSerializer.Serialize(request, JsonOptions);
        AcpPromptRequest? deserialized = JsonSerializer.Deserialize<AcpPromptRequest>(json, JsonOptions);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.SessionId, Is.EqualTo("s1"));
        Assert.That(deserialized.Prompt, Has.Count.EqualTo(1));
        Assert.That(deserialized.Prompt[0].Text, Is.EqualTo("Hello world"));
    }

    [Test]
    public void AcpPromptResponse_DefaultStopReason_IsEndTurn()
    {
        AcpPromptResponse response = new();

        Assert.That(response.StopReason, Is.EqualTo(AcpStopReasons.EndTurn));
    }

    [Test]
    public void AcpContentBlock_AllTypes_Serialize()
    {
        AcpContentBlock textBlock = new() { Type = AcpContentBlockTypes.Text, Text = "hello" };
        AcpContentBlock imageBlock = new() { Type = AcpContentBlockTypes.Image, Data = "base64data", MimeType = "image/png" };
        AcpContentBlock audioBlock = new() { Type = AcpContentBlockTypes.Audio, Data = "audiodata", MimeType = "audio/wav" };
        AcpContentBlock resourceLink = new()
        {
            Type = AcpContentBlockTypes.ResourceLink,
            Uri = "file:///test.txt",
            Name = "test.txt",
            Title = "Test File",
            Description = "A test file",
            Size = 1024
        };
        AcpContentBlock resourceBlock = new()
        {
            Type = AcpContentBlockTypes.Resource,
            Resource = new AcpResourceContents { Uri = "file:///test.txt", Text = "content", MimeType = "text/plain" }
        };

        string textJson = JsonSerializer.Serialize(textBlock, JsonOptions);
        string imageJson = JsonSerializer.Serialize(imageBlock, JsonOptions);
        string resourceJson = JsonSerializer.Serialize(resourceBlock, JsonOptions);

        Assert.That(textJson, Does.Contain("\"text\":\"hello\""));
        Assert.That(imageJson, Does.Contain("\"data\":\"base64data\""));

        AcpContentBlock? desResourceBlock = JsonSerializer.Deserialize<AcpContentBlock>(resourceJson, JsonOptions);
        Assert.That(desResourceBlock!.Resource!.Text, Is.EqualTo("content"));
    }

    [Test]
    public void AcpAnnotations_Serialization_RoundTrips()
    {
        AcpContentBlock block = new()
        {
            Type = AcpContentBlockTypes.Text,
            Text = "annotated",
            Annotations = new AcpAnnotations
            {
                Audience = ["user", "admin"],
                Priority = 0.75,
                LastModified = "2025-01-01T00:00:00Z"
            }
        };

        string json = JsonSerializer.Serialize(block, JsonOptions);
        AcpContentBlock? deserialized = JsonSerializer.Deserialize<AcpContentBlock>(json, JsonOptions);

        Assert.That(deserialized!.Annotations, Is.Not.Null);
        Assert.That(deserialized.Annotations!.Audience, Has.Count.EqualTo(2));
        Assert.That(deserialized.Annotations.Priority, Is.EqualTo(0.75));
    }

    #endregion

    #region Notification Models

    [Test]
    public void AcpSessionNotification_Serialization_RoundTrips()
    {
        AcpSessionNotification notification = new()
        {
            SessionId = "s1",
            Update = new AcpSessionUpdate
            {
                SessionUpdateType = AcpSessionUpdateTypes.AgentMessageChunk,
                Content = new AcpContentBlock { Type = AcpContentBlockTypes.Text, Text = "Hello" }
            }
        };

        string json = JsonSerializer.Serialize(notification, JsonOptions);
        AcpSessionNotification? deserialized = JsonSerializer.Deserialize<AcpSessionNotification>(json, JsonOptions);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.SessionId, Is.EqualTo("s1"));
        Assert.That(deserialized.Update.SessionUpdateType, Is.EqualTo(AcpSessionUpdateTypes.AgentMessageChunk));
        Assert.That(deserialized.Update.Content!.Text, Is.EqualTo("Hello"));
    }

    [Test]
    public void AcpSessionUpdate_ToolCall_Serializes()
    {
        AcpSessionUpdate update = new()
        {
            SessionUpdateType = AcpSessionUpdateTypes.ToolCall,
            ToolCallId = "tc-1",
            Title = "Read File",
            Kind = AcpToolKinds.Read,
            Status = AcpToolCallStatuses.InProgress,
            Locations = [new AcpToolCallLocation { Path = "/src/main.cs", Line = 42 }],
            ToolCallContent =
            [
                new AcpToolCallContent
                {
                    Type = "content",
                    Content = new AcpContentBlock { Type = AcpContentBlockTypes.Text, Text = "file content" }
                }
            ]
        };

        string json = JsonSerializer.Serialize(update, JsonOptions);
        AcpSessionUpdate? deserialized = JsonSerializer.Deserialize<AcpSessionUpdate>(json, JsonOptions);

        Assert.That(deserialized!.ToolCallId, Is.EqualTo("tc-1"));
        Assert.That(deserialized.Kind, Is.EqualTo(AcpToolKinds.Read));
        Assert.That(deserialized.Locations, Has.Count.EqualTo(1));
        Assert.That(deserialized.Locations![0].Line, Is.EqualTo(42));
        Assert.That(deserialized.ToolCallContent, Has.Count.EqualTo(1));
    }

    [Test]
    public void AcpSessionUpdate_Plan_Serializes()
    {
        AcpSessionUpdate update = new()
        {
            SessionUpdateType = AcpSessionUpdateTypes.Plan,
            Entries =
            [
                new AcpPlanEntry { Content = "Step 1", Priority = AcpPlanEntryPriorities.High, Status = AcpPlanEntryStatuses.Completed },
                new AcpPlanEntry { Content = "Step 2", Priority = AcpPlanEntryPriorities.Medium, Status = AcpPlanEntryStatuses.InProgress },
                new AcpPlanEntry { Content = "Step 3", Priority = AcpPlanEntryPriorities.Low, Status = AcpPlanEntryStatuses.Pending }
            ]
        };

        string json = JsonSerializer.Serialize(update, JsonOptions);
        AcpSessionUpdate? deserialized = JsonSerializer.Deserialize<AcpSessionUpdate>(json, JsonOptions);

        Assert.That(deserialized!.Entries, Has.Count.EqualTo(3));
        Assert.That(deserialized.Entries![0].Status, Is.EqualTo(AcpPlanEntryStatuses.Completed));
        Assert.That(deserialized.Entries[1].Priority, Is.EqualTo(AcpPlanEntryPriorities.Medium));
    }

    [Test]
    public void AcpCancelNotification_Serialization()
    {
        AcpCancelNotification cancel = new() { SessionId = "s1" };

        string json = JsonSerializer.Serialize(cancel, JsonOptions);
        AcpCancelNotification? deserialized = JsonSerializer.Deserialize<AcpCancelNotification>(json, JsonOptions);

        Assert.That(deserialized!.SessionId, Is.EqualTo("s1"));
    }

    #endregion

    #region AcpTornadoExtension - Content Block to ChatMessage Conversion

    [Test]
    public void ToTornadoMessage_TextBlock_CreatesTextPart()
    {
        List<AcpContentBlock> blocks =
        [
            new AcpContentBlock { Type = AcpContentBlockTypes.Text, Text = "Hello from ACP" }
        ];

        ChatMessage message = blocks.ToTornadoMessage();

        Assert.That(message.Role, Is.EqualTo(ChatMessageRoles.User));
        Assert.That(message.Parts, Has.Count.EqualTo(1));
        Assert.That(message.Parts![0].Type, Is.EqualTo(ChatMessageTypes.Text));
        Assert.That(message.Parts[0].Text, Is.EqualTo("Hello from ACP"));
    }

    [Test]
    public void ToTornadoMessage_MultipleBlocks_CreatesMultipleParts()
    {
        List<AcpContentBlock> blocks =
        [
            new AcpContentBlock { Type = AcpContentBlockTypes.Text, Text = "part 1" },
            new AcpContentBlock { Type = AcpContentBlockTypes.Text, Text = "part 2" }
        ];

        ChatMessage message = blocks.ToTornadoMessage();

        Assert.That(message.Parts, Has.Count.EqualTo(2));
    }

    [Test]
    public void ToTornadoMessage_EmptyBlocks_CreatesMessageWithNoParts()
    {
        List<AcpContentBlock> blocks = [];
        ChatMessage message = blocks.ToTornadoMessage();

        Assert.That(message.Parts, Is.Empty);
        Assert.That(message.Role, Is.EqualTo(ChatMessageRoles.User));
    }

    [Test]
    public void ToTornadoMessagePart_TextBlock_ReturnsTextPart()
    {
        AcpContentBlock block = new() { Type = AcpContentBlockTypes.Text, Text = "test text" };
        ChatMessagePart? part = block.ToTornadoMessagePart();

        Assert.That(part, Is.Not.Null);
        Assert.That(part!.Type, Is.EqualTo(ChatMessageTypes.Text));
        Assert.That(part.Text, Is.EqualTo("test text"));
    }

    [Test]
    public void ToTornadoMessagePart_TextBlockNullText_ReturnsEmptyString()
    {
        AcpContentBlock block = new() { Type = AcpContentBlockTypes.Text, Text = null };
        ChatMessagePart? part = block.ToTornadoMessagePart();

        Assert.That(part, Is.Not.Null);
        Assert.That(part!.Text, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ToTornadoMessagePart_ImageBlock_ReturnsImageType()
    {
        AcpContentBlock block = new() { Type = AcpContentBlockTypes.Image, Data = "base64image" };
        ChatMessagePart? part = block.ToTornadoMessagePart();

        Assert.That(part, Is.Not.Null);
        Assert.That(part!.Type, Is.EqualTo(ChatMessageTypes.Image));
    }

    [Test]
    public void ToTornadoMessagePart_ImageBlockNoData_ReturnsNull()
    {
        AcpContentBlock block = new() { Type = AcpContentBlockTypes.Image, Data = null };
        ChatMessagePart? part = block.ToTornadoMessagePart();

        Assert.That(part, Is.Null);
    }

    [Test]
    public void ToTornadoMessagePart_AudioBlock_ReturnsAudioType()
    {
        AcpContentBlock block = new() { Type = AcpContentBlockTypes.Audio, Data = "audiodata" };
        ChatMessagePart? part = block.ToTornadoMessagePart();

        Assert.That(part, Is.Not.Null);
        Assert.That(part!.Type, Is.EqualTo(ChatMessageTypes.Audio));
    }

    [Test]
    public void ToTornadoMessagePart_AudioBlockNoData_ReturnsNull()
    {
        AcpContentBlock block = new() { Type = AcpContentBlockTypes.Audio, Data = null };
        ChatMessagePart? part = block.ToTornadoMessagePart();

        Assert.That(part, Is.Null);
    }

    [Test]
    public void ToTornadoMessagePart_ResourceLinkBlock_ReturnsMarkdownLink()
    {
        AcpContentBlock block = new()
        {
            Type = AcpContentBlockTypes.ResourceLink,
            Name = "readme.md",
            Uri = "file:///readme.md"
        };

        ChatMessagePart? part = block.ToTornadoMessagePart();

        Assert.That(part, Is.Not.Null);
        Assert.That(part!.Type, Is.EqualTo(ChatMessageTypes.Text));
        Assert.That(part.Text, Is.EqualTo("[Resource: readme.md](file:///readme.md)"));
    }

    [Test]
    public void ToTornadoMessagePart_ResourceBlockWithText_ReturnsTextPart()
    {
        AcpContentBlock block = new()
        {
            Type = AcpContentBlockTypes.Resource,
            Resource = new AcpResourceContents { Uri = "file:///test.txt", Text = "resource content" }
        };

        ChatMessagePart? part = block.ToTornadoMessagePart();

        Assert.That(part, Is.Not.Null);
        Assert.That(part!.Text, Is.EqualTo("resource content"));
    }

    [Test]
    public void ToTornadoMessagePart_ResourceBlockNoText_ReturnsNull()
    {
        AcpContentBlock block = new()
        {
            Type = AcpContentBlockTypes.Resource,
            Resource = new AcpResourceContents { Uri = "file:///test.bin", Blob = "blobdata" }
        };

        ChatMessagePart? part = block.ToTornadoMessagePart();
        Assert.That(part, Is.Null);
    }

    [Test]
    public void ToTornadoMessagePart_UnknownType_ReturnsNull()
    {
        AcpContentBlock block = new() { Type = "unknown_type" };
        ChatMessagePart? part = block.ToTornadoMessagePart();

        Assert.That(part, Is.Null);
    }

    #endregion

    #region AcpTornadoExtension - ChatMessage to Content Block Conversion

    [Test]
    public void ToAcpContentBlocks_StringContent_CreatesTextBlock()
    {
        ChatMessage message = new() { Content = "Hello", Role = ChatMessageRoles.User };

        List<AcpContentBlock> blocks = message.ToAcpContentBlocks();

        Assert.That(blocks, Has.Count.EqualTo(1));
        Assert.That(blocks[0].Type, Is.EqualTo(AcpContentBlockTypes.Text));
        Assert.That(blocks[0].Text, Is.EqualTo("Hello"));
    }

    [Test]
    public void ToAcpContentBlocks_Parts_CreateMultipleBlocks()
    {
        ChatMessage message = new()
        {
            Role = ChatMessageRoles.Assistant,
            Parts =
            [
                new ChatMessagePart("first"),
                new ChatMessagePart("second")
            ]
        };

        List<AcpContentBlock> blocks = message.ToAcpContentBlocks();

        Assert.That(blocks, Has.Count.EqualTo(2));
        Assert.That(blocks[0].Text, Is.EqualTo("first"));
        Assert.That(blocks[1].Text, Is.EqualTo("second"));
    }

    [Test]
    public void ToAcpContentBlocks_NullContentAndParts_ReturnsEmpty()
    {
        ChatMessage message = new() { Role = ChatMessageRoles.User };

        List<AcpContentBlock> blocks = message.ToAcpContentBlocks();

        Assert.That(blocks, Is.Empty);
    }

    [Test]
    public void ToAcpContentBlock_TextPart_ReturnsTextBlock()
    {
        ChatMessagePart part = new("Hello ACP");
        AcpContentBlock? block = part.ToAcpContentBlock();

        Assert.That(block, Is.Not.Null);
        Assert.That(block!.Type, Is.EqualTo(AcpContentBlockTypes.Text));
        Assert.That(block.Text, Is.EqualTo("Hello ACP"));
    }

    [Test]
    public void ToAcpContentBlock_ImagePart_ReturnsImageBlock()
    {
        ChatMessagePart part = new(new Uri("https://example.com/image.png"));
        AcpContentBlock? block = part.ToAcpContentBlock();

        Assert.That(block, Is.Not.Null);
        Assert.That(block!.Type, Is.EqualTo(AcpContentBlockTypes.Image));
        Assert.That(block.Data, Is.EqualTo("https://example.com/image.png"));
    }

    [Test]
    public void ToAcpContentBlock_AudioPart_ReturnsAudioBlock()
    {
        ChatMessagePart part = new(ChatMessageTypes.Audio);
        AcpContentBlock? block = part.ToAcpContentBlock();

        Assert.That(block, Is.Not.Null);
        Assert.That(block!.Type, Is.EqualTo(AcpContentBlockTypes.Audio));
    }

    [Test]
    public void ToAcpContentBlock_ReasoningPart_ReturnsTextBlock()
    {
        ChatMessagePart part = new(new ChatMessageReasoningData { Content = "thinking..." });
        AcpContentBlock? block = part.ToAcpContentBlock();

        Assert.That(block, Is.Not.Null);
        Assert.That(block!.Type, Is.EqualTo(AcpContentBlockTypes.Text));
        Assert.That(block.Text, Is.EqualTo("thinking..."));
    }

    [Test]
    public void ToAcpContentBlock_UnsupportedType_ReturnsNull()
    {
        ChatMessagePart part = new(ChatMessageTypes.FileLink);
        AcpContentBlock? block = part.ToAcpContentBlock();

        Assert.That(block, Is.Null);
    }

    #endregion

    #region AcpTornadoExtension - Role Mapping

    [Test]
    public void ToTornadoMessageRole_User_ReturnsUser()
    {
        ChatMessageRoles role = AcpTornadoExtension.ToTornadoMessageRole("user");
        Assert.That(role, Is.EqualTo(ChatMessageRoles.User));
    }

    [Test]
    public void ToTornadoMessageRole_Assistant_ReturnsAssistant()
    {
        ChatMessageRoles role = AcpTornadoExtension.ToTornadoMessageRole("assistant");
        Assert.That(role, Is.EqualTo(ChatMessageRoles.Assistant));
    }

    [Test]
    public void ToTornadoMessageRole_Null_ReturnsUser()
    {
        ChatMessageRoles role = AcpTornadoExtension.ToTornadoMessageRole(null);
        Assert.That(role, Is.EqualTo(ChatMessageRoles.User));
    }

    [Test]
    public void ToTornadoMessageRole_Unknown_ReturnsUser()
    {
        ChatMessageRoles role = AcpTornadoExtension.ToTornadoMessageRole("unknown");
        Assert.That(role, Is.EqualTo(ChatMessageRoles.User));
    }

    [Test]
    public void ToAcpRole_User_ReturnsUserString()
    {
        string role = ((ChatMessageRoles?)ChatMessageRoles.User).ToAcpRole();
        Assert.That(role, Is.EqualTo("user"));
    }

    [Test]
    public void ToAcpRole_Assistant_ReturnsAssistantString()
    {
        string role = ((ChatMessageRoles?)ChatMessageRoles.Assistant).ToAcpRole();
        Assert.That(role, Is.EqualTo("assistant"));
    }

    [Test]
    public void ToAcpRole_System_ReturnsUserString()
    {
        string role = ((ChatMessageRoles?)ChatMessageRoles.System).ToAcpRole();
        Assert.That(role, Is.EqualTo("user"));
    }

    [Test]
    public void ToAcpRole_Null_ReturnsAssistantString()
    {
        ChatMessageRoles? nullRole = null;
        string role = nullRole.ToAcpRole();
        Assert.That(role, Is.EqualTo("assistant"));
    }

    #endregion

    #region AcpTornadoExtension - Events Conversion

    [Test]
    public void ToAcpSessionUpdate_CompletedEvent_ReturnsAgentMessageChunk()
    {
        ChatRuntimeCompletedEvent evt = new("runtime-1");
        AcpSessionUpdate update = evt.ToAcpSessionUpdate();

        Assert.That(update.SessionUpdateType, Is.EqualTo(AcpSessionUpdateTypes.AgentMessageChunk));
        Assert.That(update.Content, Is.Not.Null);
        Assert.That(update.Content!.Type, Is.EqualTo(AcpContentBlockTypes.Text));
        Assert.That(update.Content.Text, Does.Contain("completed"));
    }

    [Test]
    public void ToAcpSessionUpdate_ErrorEvent_ContainsErrorMessage()
    {
        ChatRuntimeErrorEvent evt = new(new InvalidOperationException("test failure"), "runtime-1");
        AcpSessionUpdate update = evt.ToAcpSessionUpdate();

        Assert.That(update.SessionUpdateType, Is.EqualTo(AcpSessionUpdateTypes.AgentMessageChunk));
        Assert.That(update.Content!.Text, Does.Contain("test failure"));
    }

    [Test]
    public void ToAcpSessionUpdate_ErrorEventNullException_ContainsUnknown()
    {
        // ErrorEvent requires a non-null exception in the constructor, but Text handles null Message
        ChatRuntimeErrorEvent evt = new(new Exception(), "runtime-1");
        AcpSessionUpdate update = evt.ToAcpSessionUpdate();

        Assert.That(update.Content!.Text, Does.Contain("Error:"));
    }

    [Test]
    public void ToAcpSessionUpdate_InvokedEventWithMessage_ContainsMessageContent()
    {
        ChatMessage msg = new() { Content = "hello from agent", Role = ChatMessageRoles.Assistant };
        ChatRuntimeInvokedEvent evt = new(msg, "runtime-1");
        AcpSessionUpdate update = evt.ToAcpSessionUpdate();

        Assert.That(update.SessionUpdateType, Is.EqualTo(AcpSessionUpdateTypes.AgentMessageChunk));
        Assert.That(update.Content!.Text, Is.EqualTo("hello from agent"));
    }

    [Test]
    public void ToAcpSessionUpdate_InvokedEventNullContent_ReturnsEmptyText()
    {
        ChatMessage msg = new() { Role = ChatMessageRoles.Assistant };
        ChatRuntimeInvokedEvent evt = new(msg, "runtime-1");
        AcpSessionUpdate update = evt.ToAcpSessionUpdate();

        Assert.That(update.Content!.Text, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ToAcpSessionUpdate_CancelledEvent_ContainsCancelledText()
    {
        ChatRuntimeCancelledEvent evt = new("runtime-1");
        AcpSessionUpdate update = evt.ToAcpSessionUpdate();

        Assert.That(update.SessionUpdateType, Is.EqualTo(AcpSessionUpdateTypes.AgentMessageChunk));
        Assert.That(update.Content!.Text, Does.Contain("cancelled").IgnoreCase);
    }

    [Test]
    public void ToAcpSessionUpdate_UnknownEvent_ContainsEventType()
    {
        ChatRuntimeStartedEvent evt = new("runtime-1");
        AcpSessionUpdate update = evt.ToAcpSessionUpdate();

        Assert.That(update.SessionUpdateType, Is.EqualTo(AcpSessionUpdateTypes.AgentMessageChunk));
        Assert.That(update.Content!.Text, Does.Contain("Runtime event"));
    }

    #endregion

    #region AcpJsonRpcServer - Stream-Based Message Processing

    [Test]
    public async Task Server_Initialize_ReturnsResponse()
    {
        MockAcpRuntime runtime = new();
        using MemoryStream input = new();
        using MemoryStream output = new();

        AcpJsonRpcRequest request = new()
        {
            Id = 1,
            Method = AcpMethods.Initialize,
            Params = new AcpInitializeRequest { ProtocolVersion = 1 }
        };

        WriteJsonLine(input, request);
        input.Position = 0;

        AcpJsonRpcServer server = new(runtime, input, output);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        await server.RunAsync(cts.Token);

        string responseJson = ReadOutput(output);
        Assert.That(responseJson, Does.Contain("\"result\""));
        Assert.That(responseJson, Does.Contain("\"id\":1"));
    }

    [Test]
    public async Task Server_NewSession_ReturnsSessionId()
    {
        MockAcpRuntime runtime = new();
        using MemoryStream input = new();
        using MemoryStream output = new();

        AcpJsonRpcRequest request = new()
        {
            Id = 2,
            Method = AcpMethods.NewSession,
            Params = new AcpNewSessionRequest { Cwd = "/test" }
        };

        WriteJsonLine(input, request);
        input.Position = 0;

        AcpJsonRpcServer server = new(runtime, input, output);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        await server.RunAsync(cts.Token);

        string responseJson = ReadOutput(output);
        Assert.That(responseJson, Does.Contain("\"result\""));
        Assert.That(responseJson, Does.Contain("\"sessionId\""));
    }

    [Test]
    public async Task Server_Prompt_ReturnsStopReason()
    {
        MockAcpRuntime runtime = new();
        using MemoryStream input = new();
        using MemoryStream output = new();

        AcpJsonRpcRequest request = new()
        {
            Id = 3,
            Method = AcpMethods.Prompt,
            Params = new AcpPromptRequest
            {
                SessionId = "s1",
                Prompt = [new AcpContentBlock { Type = AcpContentBlockTypes.Text, Text = "Hello" }]
            }
        };

        WriteJsonLine(input, request);
        input.Position = 0;

        AcpJsonRpcServer server = new(runtime, input, output);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        await server.RunAsync(cts.Token);

        string responseJson = ReadOutput(output);
        Assert.That(responseJson, Does.Contain("\"stopReason\""));
    }

    [Test]
    public async Task Server_UnknownMethod_ReturnsMethodNotFoundError()
    {
        MockAcpRuntime runtime = new();
        using MemoryStream input = new();
        using MemoryStream output = new();

        AcpJsonRpcRequest request = new()
        {
            Id = 4,
            Method = "unknown/method"
        };

        WriteJsonLine(input, request);
        input.Position = 0;

        AcpJsonRpcServer server = new(runtime, input, output);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        await server.RunAsync(cts.Token);

        string responseJson = ReadOutput(output);
        Assert.That(responseJson, Does.Contain($"{AcpErrorCodes.MethodNotFound}"));
    }

    [Test]
    public async Task Server_InvalidJson_ReturnsParseError()
    {
        MockAcpRuntime runtime = new();
        using MemoryStream input = new();
        using MemoryStream output = new();

        byte[] invalidJson = Encoding.UTF8.GetBytes("not valid json\n");
        input.Write(invalidJson);
        input.Position = 0;

        AcpJsonRpcServer server = new(runtime, input, output);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        await server.RunAsync(cts.Token);

        string responseJson = ReadOutput(output);
        Assert.That(responseJson, Does.Contain($"{AcpErrorCodes.ParseError}"));
    }

    [Test]
    public async Task Server_Notification_NoResponse()
    {
        MockAcpRuntime runtime = new();
        using MemoryStream input = new();
        using MemoryStream output = new();

        // Notification has no "id" field
        string notificationJson = JsonSerializer.Serialize(new AcpJsonRpcNotification
        {
            Method = AcpMethods.Cancel,
            Params = new AcpCancelNotification { SessionId = "s1" }
        }, JsonOptions);

        byte[] bytes = Encoding.UTF8.GetBytes(notificationJson + "\n");
        input.Write(bytes);
        input.Position = 0;

        AcpJsonRpcServer server = new(runtime, input, output);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        await server.RunAsync(cts.Token);

        string responseJson = ReadOutput(output);
        // Notifications should not produce a response
        Assert.That(responseJson, Is.Empty.Or.EqualTo(""));
    }

    [Test]
    public async Task Server_CancelNotification_InvokesRuntime()
    {
        MockAcpRuntime runtime = new();
        using MemoryStream input = new();
        using MemoryStream output = new();

        string notificationJson = JsonSerializer.Serialize(new AcpJsonRpcNotification
        {
            Method = AcpMethods.Cancel,
            Params = new AcpCancelNotification { SessionId = "cancel-me" }
        }, JsonOptions);

        byte[] bytes = Encoding.UTF8.GetBytes(notificationJson + "\n");
        input.Write(bytes);
        input.Position = 0;

        AcpJsonRpcServer server = new(runtime, input, output);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        await server.RunAsync(cts.Token);

        Assert.That(runtime.LastCancelledSessionId, Is.EqualTo("cancel-me"));
    }

    [Test]
    public async Task Server_EmptyLines_AreSkipped()
    {
        MockAcpRuntime runtime = new();
        using MemoryStream input = new();
        using MemoryStream output = new();

        // Write empty line then a valid request
        byte[] emptyLine = Encoding.UTF8.GetBytes("\n");
        input.Write(emptyLine);

        AcpJsonRpcRequest request = new()
        {
            Id = 10,
            Method = AcpMethods.Initialize,
            Params = new AcpInitializeRequest()
        };
        WriteJsonLine(input, request);
        input.Position = 0;

        AcpJsonRpcServer server = new(runtime, input, output);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        await server.RunAsync(cts.Token);

        string responseJson = ReadOutput(output);
        Assert.That(responseJson, Does.Contain("\"id\":10"));
    }

    [Test]
    public async Task Server_MultipleRequests_ProcessesAll()
    {
        MockAcpRuntime runtime = new();
        using MemoryStream input = new();
        using MemoryStream output = new();

        WriteJsonLine(input, new AcpJsonRpcRequest { Id = 1, Method = AcpMethods.Initialize, Params = new AcpInitializeRequest() });
        WriteJsonLine(input, new AcpJsonRpcRequest { Id = 2, Method = AcpMethods.NewSession, Params = new AcpNewSessionRequest { Cwd = "/test" } });
        input.Position = 0;

        AcpJsonRpcServer server = new(runtime, input, output);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        await server.RunAsync(cts.Token);

        string responseJson = ReadOutput(output);
        Assert.That(responseJson, Does.Contain("\"id\":1"));
        Assert.That(responseJson, Does.Contain("\"id\":2"));
    }

    [Test]
    public async Task Server_Authenticate_ReturnsEmptyResult()
    {
        MockAcpRuntime runtime = new();
        using MemoryStream input = new();
        using MemoryStream output = new();

        WriteJsonLine(input, new AcpJsonRpcRequest { Id = 5, Method = AcpMethods.Authenticate });
        input.Position = 0;

        AcpJsonRpcServer server = new(runtime, input, output);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        await server.RunAsync(cts.Token);

        string responseJson = ReadOutput(output);
        Assert.That(responseJson, Does.Contain("\"id\":5"));
        Assert.That(responseJson, Does.Contain("\"result\""));
    }

    [Test]
    public void Server_NullRuntime_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new AcpJsonRpcServer(null!, new MemoryStream(), new MemoryStream());
        });
    }

    [Test]
    public void Server_NullInputStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new AcpJsonRpcServer(new MockAcpRuntime(), null!, new MemoryStream());
        });
    }

    [Test]
    public void Server_NullOutputStream_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = new AcpJsonRpcServer(new MockAcpRuntime(), new MemoryStream(), null!);
        });
    }

    [Test]
    public async Task Server_StringId_IsPreserved()
    {
        MockAcpRuntime runtime = new();
        using MemoryStream input = new();
        using MemoryStream output = new();

        string requestJson = """{"jsonrpc":"2.0","id":"abc-123","method":"initialize","params":{"protocolVersion":1}}""" + "\n";
        input.Write(Encoding.UTF8.GetBytes(requestJson));
        input.Position = 0;

        AcpJsonRpcServer server = new(runtime, input, output);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        await server.RunAsync(cts.Token);

        string responseJson = ReadOutput(output);
        Assert.That(responseJson, Does.Contain("abc-123"));
    }

    [Test]
    public async Task Server_RuntimeThrows_ReturnsInternalError()
    {
        MockAcpRuntime runtime = new() { ThrowOnPrompt = true };
        using MemoryStream input = new();
        using MemoryStream output = new();

        AcpJsonRpcRequest request = new()
        {
            Id = 99,
            Method = AcpMethods.Prompt,
            Params = new AcpPromptRequest
            {
                SessionId = "s1",
                Prompt = [new AcpContentBlock { Type = AcpContentBlockTypes.Text, Text = "fail" }]
            }
        };

        WriteJsonLine(input, request);
        input.Position = 0;

        AcpJsonRpcServer server = new(runtime, input, output);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        await server.RunAsync(cts.Token);

        string responseJson = ReadOutput(output);
        Assert.That(responseJson, Does.Contain($"{AcpErrorCodes.InternalError}"));
    }

    #endregion

    #region IAcpRuntimeConfiguration Mock-Based Integration

    [Test]
    public async Task MockRuntime_Initialize_ReturnsCapabilities()
    {
        MockAcpRuntime runtime = new();

        AcpInitializeResponse response = await runtime.InitializeAsync(new AcpInitializeRequest
        {
            ProtocolVersion = 1,
            ClientInfo = new AcpImplementation { Name = "TestClient", Version = "1.0" }
        }, CancellationToken.None);

        Assert.That(response.ProtocolVersion, Is.EqualTo(1));
        Assert.That(response.AgentInfo!.Name, Is.EqualTo("MockAgent"));
    }

    [Test]
    public async Task MockRuntime_NewSession_ReturnsUniqueId()
    {
        MockAcpRuntime runtime = new();

        AcpNewSessionResponse response1 = await runtime.NewSessionAsync(new AcpNewSessionRequest { Cwd = "/a" }, CancellationToken.None);
        AcpNewSessionResponse response2 = await runtime.NewSessionAsync(new AcpNewSessionRequest { Cwd = "/b" }, CancellationToken.None);

        Assert.That(response1.SessionId, Is.Not.Empty);
        Assert.That(response2.SessionId, Is.Not.Empty);
        Assert.That(response1.SessionId, Is.Not.EqualTo(response2.SessionId));
    }

    [Test]
    public async Task MockRuntime_Prompt_ReturnsEndTurn()
    {
        MockAcpRuntime runtime = new();

        AcpPromptResponse response = await runtime.PromptAsync(new AcpPromptRequest
        {
            SessionId = "s1",
            Prompt = [new AcpContentBlock { Type = AcpContentBlockTypes.Text, Text = "test" }]
        }, CancellationToken.None);

        Assert.That(response.StopReason, Is.EqualTo(AcpStopReasons.EndTurn));
    }

    [Test]
    public async Task MockRuntime_Cancel_RecordsSessionId()
    {
        MockAcpRuntime runtime = new();

        await runtime.CancelAsync(new AcpCancelNotification { SessionId = "session-to-cancel" }, CancellationToken.None);

        Assert.That(runtime.LastCancelledSessionId, Is.EqualTo("session-to-cancel"));
    }

    [Test]
    public async Task MockRuntime_SessionUpdate_FiresEvent()
    {
        MockAcpRuntime runtime = new();

        AcpSessionNotification? received = null;
        runtime.OnSessionUpdate += (notification) =>
        {
            received = notification;
            return Task.CompletedTask;
        };

        await runtime.RaiseSessionUpdate(new AcpSessionNotification
        {
            SessionId = "s1",
            Update = new AcpSessionUpdate
            {
                SessionUpdateType = AcpSessionUpdateTypes.AgentMessageChunk,
                Content = new AcpContentBlock { Type = AcpContentBlockTypes.Text, Text = "update" }
            }
        });

        Assert.That(received, Is.Not.Null);
        Assert.That(received!.SessionId, Is.EqualTo("s1"));
    }

    #endregion

    #region MCP Integration — Rider-style JSON through AcpJsonRpcServer

    /// <summary>
    /// Writes a raw JSON string to a MemoryStream as a single line (compacted), matching the
    /// newline-delimited JSON format that AcpJsonRpcServer expects over stdio.
    /// </summary>
    private static void WriteRawJsonLine(MemoryStream stream, string json)
    {
        // Compact multi-line JSON to a single line, as ACP stdio transport requires
        string compacted = json.ReplaceLineEndings(" ").Trim();
        byte[] bytes = Encoding.UTF8.GetBytes(compacted + "\n");
        stream.Write(bytes);
    }

    [Test]
    public async Task Server_NewSession_WithRiderStdioMcpServer_DeserializesCorrectly()
    {
        // Raw JSON exactly as Rider 2025.x sends it for a stdio MCP server
        string riderJson = """{"jsonrpc":"2.0","id":10,"method":"session/new","params":{"cwd":"/home/user/myproject","mcpServers":[{"type":"stdio","name":"filesystem","command":"npx","args":["-y","@modelcontextprotocol/server-filesystem","/tmp"],"env":{"NODE_ENV":"production","DEBUG":"false"}}]}}""";

        MockAcpRuntime runtime = new();
        using MemoryStream input = new();
        using MemoryStream output = new();

        WriteRawJsonLine(input, riderJson);
        input.Position = 0;

        AcpJsonRpcServer server = new(runtime, input, output);
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        await server.RunAsync(cts.Token);

        // Verify the runtime received the correctly deserialized MCP config
        Assert.That(runtime.LastNewSessionRequest, Is.Not.Null);
        Assert.That(runtime.LastNewSessionRequest!.Cwd, Is.EqualTo("/home/user/myproject"));
        Assert.That(runtime.LastNewSessionRequest.McpServers, Has.Count.EqualTo(1));

        AcpMcpServerConfig mcpServer = runtime.LastNewSessionRequest.McpServers[0];
        Assert.That(mcpServer.Type, Is.EqualTo("stdio"));
        Assert.That(mcpServer.Name, Is.EqualTo("filesystem"));
        Assert.That(mcpServer.Command, Is.EqualTo("npx"));
        Assert.That(mcpServer.Args, Is.EqualTo(new[] { "-y", "@modelcontextprotocol/server-filesystem", "/tmp" }));
        Assert.That(mcpServer.Env, Has.Count.EqualTo(2));
        Assert.That(mcpServer.Env!["NODE_ENV"], Is.EqualTo("production"));
        Assert.That(mcpServer.Env["DEBUG"], Is.EqualTo("false"));

        // Verify JSON-RPC response is valid
        string responseJson = ReadOutput(output);
        Assert.That(responseJson, Does.Contain("\"id\":10"));
        Assert.That(responseJson, Does.Contain("\"sessionId\""));
    }

    [Test]
    public async Task Server_NewSession_WithRiderHttpMcpServer_DeserializesCorrectly()
    {
        // Raw JSON as Rider sends it for an HTTP/SSE MCP server
        string riderJson = """{"jsonrpc":"2.0","id":11,"method":"session/new","params":{"cwd":"/workspace","mcpServers":[{"type":"http","name":"remote-tools","url":"https://mcp.example.com/sse","headers":{"Authorization":"Bearer sk-test-key","X-Custom":"value"}}]}}""";

        MockAcpRuntime runtime = new();
        using MemoryStream input = new();
        using MemoryStream output = new();

        WriteRawJsonLine(input, riderJson);
        input.Position = 0;

        AcpJsonRpcServer server = new(runtime, input, output);
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        await server.RunAsync(cts.Token);

        Assert.That(runtime.LastNewSessionRequest, Is.Not.Null);

        AcpMcpServerConfig mcpServer = runtime.LastNewSessionRequest!.McpServers[0];
        Assert.That(mcpServer.Type, Is.EqualTo("http"));
        Assert.That(mcpServer.Name, Is.EqualTo("remote-tools"));
        Assert.That(mcpServer.Url, Is.EqualTo("https://mcp.example.com/sse"));
        Assert.That(mcpServer.Command, Is.Null);
        Assert.That(mcpServer.Args, Is.Null);
        Assert.That(mcpServer.Headers, Has.Count.EqualTo(2));
        Assert.That(mcpServer.Headers!["Authorization"], Is.EqualTo("Bearer sk-test-key"));
        Assert.That(mcpServer.Headers["X-Custom"], Is.EqualTo("value"));
    }

    [Test]
    public async Task Server_NewSession_WithMultipleMcpServers_DeserializesAll()
    {
        // Rider can pass multiple MCP servers in a single session/new
        string riderJson = """{"jsonrpc":"2.0","id":12,"method":"session/new","params":{"cwd":"/project","mcpServers":[{"type":"stdio","name":"local-fs","command":"node","args":["server.js"]},{"type":"http","name":"cloud-tools","url":"https://tools.example.com"}]}}""";

        MockAcpRuntime runtime = new();
        using MemoryStream input = new();
        using MemoryStream output = new();

        WriteRawJsonLine(input, riderJson);
        input.Position = 0;

        AcpJsonRpcServer server = new(runtime, input, output);
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        await server.RunAsync(cts.Token);

        Assert.That(runtime.LastNewSessionRequest, Is.Not.Null);
        Assert.That(runtime.LastNewSessionRequest!.McpServers, Has.Count.EqualTo(2));
        Assert.That(runtime.LastNewSessionRequest.McpServers[0].Name, Is.EqualTo("local-fs"));
        Assert.That(runtime.LastNewSessionRequest.McpServers[0].Type, Is.EqualTo("stdio"));
        Assert.That(runtime.LastNewSessionRequest.McpServers[1].Name, Is.EqualTo("cloud-tools"));
        Assert.That(runtime.LastNewSessionRequest.McpServers[1].Type, Is.EqualTo("http"));
    }

    [Test]
    public async Task Server_NewSession_WithNoMcpServers_DeserializesEmptyList()
    {
        string riderJson = """{"jsonrpc":"2.0","id":13,"method":"session/new","params":{"cwd":"/project","mcpServers":[]}}""";

        MockAcpRuntime runtime = new();
        using MemoryStream input = new();
        using MemoryStream output = new();

        WriteRawJsonLine(input, riderJson);
        input.Position = 0;

        AcpJsonRpcServer server = new(runtime, input, output);
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        await server.RunAsync(cts.Token);

        Assert.That(runtime.LastNewSessionRequest, Is.Not.Null);
        Assert.That(runtime.LastNewSessionRequest!.McpServers, Is.Empty);
    }

    [Test]
    public async Task Server_Initialize_McpCapabilities_ReturnedInResponse()
    {
        MockAcpRuntime runtime = new();
        using MemoryStream input = new();
        using MemoryStream output = new();

        string initJson = """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":1,"clientInfo":{"name":"JetBrains Rider","version":"2025.2"},"clientCapabilities":{"fs":{"readTextFile":true,"writeTextFile":true},"terminal":true}}}""";

        WriteRawJsonLine(input, initJson);
        input.Position = 0;

        AcpJsonRpcServer server = new(runtime, input, output);
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        await server.RunAsync(cts.Token);

        string responseJson = ReadOutput(output);
        Assert.That(responseJson, Does.Contain("\"mcpCapabilities\""));
        Assert.That(responseJson, Does.Contain("\"http\":true"));

        // Verify client info was deserialized
        Assert.That(runtime.LastInitializeRequest, Is.Not.Null);
        Assert.That(runtime.LastInitializeRequest!.ClientInfo!.Name, Is.EqualTo("JetBrains Rider"));
        Assert.That(runtime.LastInitializeRequest.ClientCapabilities.Terminal, Is.True);
    }

    [Test]
    public async Task Server_FullRiderHandshake_WithMcpServers()
    {
        // Simulates Rider's full ACP handshake: initialize → session/new (with MCP servers) → prompt
        MockAcpRuntime runtime = new();
        using MemoryStream input = new();
        using MemoryStream output = new();

        // 1. initialize
        WriteRawJsonLine(input, """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":1,"clientInfo":{"name":"JetBrains Rider","version":"2025.2"}}}""");
        // 2. session/new with MCP servers
        WriteRawJsonLine(input, """{"jsonrpc":"2.0","id":2,"method":"session/new","params":{"cwd":"/project","mcpServers":[{"type":"stdio","name":"git-mcp","command":"uvx","args":["mcp-server-git","--repository","/project"],"env":{"GIT_AUTHOR_NAME":"test"}}]}}""");
        // 3. prompt
        WriteRawJsonLine(input, """{"jsonrpc":"2.0","id":3,"method":"session/prompt","params":{"sessionId":"mock-session-1","prompt":[{"type":"text","text":"Explain this repo"}]}}""");
        input.Position = 0;

        AcpJsonRpcServer server = new(runtime, input, output);
        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
        await server.RunAsync(cts.Token);

        string responseJson = ReadOutput(output);

        // All three requests should have gotten responses
        Assert.That(responseJson, Does.Contain("\"id\":1"));
        Assert.That(responseJson, Does.Contain("\"id\":2"));
        Assert.That(responseJson, Does.Contain("\"id\":3"));

        // MCP config was correctly deserialized
        Assert.That(runtime.LastNewSessionRequest, Is.Not.Null);
        Assert.That(runtime.LastNewSessionRequest!.McpServers, Has.Count.EqualTo(1));
        Assert.That(runtime.LastNewSessionRequest.McpServers[0].Name, Is.EqualTo("git-mcp"));
        Assert.That(runtime.LastNewSessionRequest.McpServers[0].Command, Is.EqualTo("uvx"));
        Assert.That(runtime.LastNewSessionRequest.McpServers[0].Env!["GIT_AUTHOR_NAME"], Is.EqualTo("test"));
    }

    #endregion

    #region Helpers

    private static void WriteJsonLine(MemoryStream stream, object obj)
    {
        string json = JsonSerializer.Serialize(obj, JsonOptions);
        byte[] bytes = Encoding.UTF8.GetBytes(json + "\n");
        stream.Write(bytes);
    }

    private static string ReadOutput(MemoryStream output)
    {
        output.Position = 0;
        using StreamReader reader = new(output, Encoding.UTF8, leaveOpen: true);
        return reader.ReadToEnd().Trim();
    }

    #endregion

    #region Mock Runtime

    /// <summary>
    /// Simple mock implementation of IAcpRuntimeConfiguration for testing the server without real LLM dependencies.
    /// </summary>
    private class MockAcpRuntime : IAcpRuntimeConfiguration
    {
        public string? LastCancelledSessionId { get; private set; }
        public AcpNewSessionRequest? LastNewSessionRequest { get; private set; }
        public AcpInitializeRequest? LastInitializeRequest { get; private set; }
        public bool ThrowOnPrompt { get; set; }
        private int _sessionCounter;

        public event Func<AcpSessionNotification, Task>? OnSessionUpdate;

        public Task<AcpInitializeResponse> InitializeAsync(AcpInitializeRequest request, CancellationToken cancellationToken)
        {
            LastInitializeRequest = request;
            return Task.FromResult(new AcpInitializeResponse
            {
                ProtocolVersion = request.ProtocolVersion,
                AgentInfo = new AcpImplementation { Name = "MockAgent", Version = "1.0.0" },
                AgentCapabilities = new AcpAgentCapabilities
                {
                    McpCapabilities = new AcpMcpCapabilities { Http = true, Sse = false },
                    PromptCapabilities = new AcpPromptCapabilities { Image = false, Audio = false }
                }
            });
        }

        public Task<AcpNewSessionResponse> NewSessionAsync(AcpNewSessionRequest request, CancellationToken cancellationToken)
        {
            LastNewSessionRequest = request;
            int id = Interlocked.Increment(ref _sessionCounter);
            return Task.FromResult(new AcpNewSessionResponse
            {
                SessionId = $"mock-session-{id}"
            });
        }

        public Task<AcpPromptResponse> PromptAsync(AcpPromptRequest request, CancellationToken cancellationToken)
        {
            if (ThrowOnPrompt)
            {
                throw new InvalidOperationException("Simulated prompt failure");
            }

            return Task.FromResult(new AcpPromptResponse
            {
                StopReason = AcpStopReasons.EndTurn
            });
        }

        public Task CancelAsync(AcpCancelNotification notification, CancellationToken cancellationToken)
        {
            LastCancelledSessionId = notification.SessionId;
            return Task.CompletedTask;
        }

        public Task<AcpSetSessionModeResponse> SetModeAsync(AcpSetSessionModeRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AcpSetSessionModeResponse());
        }

        public Task<AcpSetSessionConfigOptionResponse> SetConfigOptionAsync(AcpSetSessionConfigOptionRequest request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new AcpSetSessionConfigOptionResponse());
        }

        public async Task RaiseSessionUpdate(AcpSessionNotification notification)
        {
            if (OnSessionUpdate is not null)
            {
                await OnSessionUpdate.Invoke(notification);
            }
        }
    }

    #endregion
}
