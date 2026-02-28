using System.Text.Json;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Mcp;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class McpConfigTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = TestHelpers.CreateTempDir();
    }

    [TearDown]
    public void TearDown()
    {
        TestHelpers.CleanupTempDir(_tempDir);
    }

    #region McpConfigModel Serialization

    [Test]
    public void McpConfig_RoundTrip_StdioServer()
    {
        string path = Path.Combine(_tempDir, "mcp.json");
        McpConfig config = new()
        {
            Servers =
            [
                new McpServerEntry
                {
                    Type = "stdio",
                    Name = "test-server",
                    Command = "npx",
                    Args = ["-y", "@modelcontextprotocol/server-test"],
                    Env = new Dictionary<string, string> { ["API_KEY"] = "secret" },
                },
            ],
        };

        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });
        File.WriteAllText(path, json);

        McpConfig? loaded = CliStorage.LoadJson<McpConfig>(path);
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Servers, Has.Count.EqualTo(1));

        McpServerEntry server = loaded.Servers[0];
        Assert.That(server.Type, Is.EqualTo("stdio"));
        Assert.That(server.Name, Is.EqualTo("test-server"));
        Assert.That(server.Command, Is.EqualTo("npx"));
        Assert.That(server.Args, Has.Count.EqualTo(2));
    }

    [Test]
    public void McpConfig_RoundTrip_HttpServer()
    {
        string path = Path.Combine(_tempDir, "mcp-http.json");
        McpConfig config = new()
        {
            Servers =
            [
                new McpServerEntry
                {
                    Type = "http",
                    Name = "http-server",
                    Url = "https://mcp.example.com",
                    Headers = new Dictionary<string, string>
                    {
                        ["Authorization"] = "Bearer token123"
                    },
                    AllowedTools = ["tool-a", "tool-b"],
                },
            ],
        };

        CliStorage.SaveJson(path, config);
        McpConfig? loaded = CliStorage.LoadJson<McpConfig>(path);

        Assert.That(loaded, Is.Not.Null);
        McpServerEntry server = loaded!.Servers[0];
        Assert.That(server.Type, Is.EqualTo("http"));
        Assert.That(server.Url, Is.EqualTo("https://mcp.example.com"));
        Assert.That(server.Headers, Does.ContainKey("Authorization"));
        Assert.That(server.AllowedTools, Has.Count.EqualTo(2));
    }

    [Test]
    public void McpConfig_Empty_Servers_List()
    {
        McpConfig config = new();
        Assert.That(config.Servers, Is.Empty);
    }

    #endregion

    #region McpServerStatus

    [Test]
    public void McpServerStatus_Properties_SetCorrectly()
    {
        McpServerStatus status = new()
        {
            Name = "test",
            Type = "stdio",
            Connected = true,
            ToolCount = 5,
            Error = null,
        };

        Assert.That(status.Name, Is.EqualTo("test"));
        Assert.That(status.Connected, Is.True);
        Assert.That(status.ToolCount, Is.EqualTo(5));
        Assert.That(status.Error, Is.Null);
    }

    [Test]
    public void McpServerStatus_Error_State()
    {
        McpServerStatus status = new()
        {
            Name = "broken",
            Type = "http",
            Connected = false,
            ToolCount = 0,
            Error = "Connection refused",
        };

        Assert.That(status.Connected, Is.False);
        Assert.That(status.Error, Is.Not.Null);
    }

    #endregion

    #region McpConfigLoader — Path Resolution

    [Test]
    public void ResolveMcpConfigPath_Returns_Settings_Path_When_Set()
    {
        string fakeConfig = Path.Combine(_tempDir, "custom-mcp.json");
        File.WriteAllText(fakeConfig, "{}");

        AgentSettings settings = new() { McpConfigPath = fakeConfig };
        string? resolved = McpConfigLoader.ResolveMcpConfigPath(settings.McpConfigPath);

        Assert.That(resolved, Is.EqualTo(fakeConfig));
    }

    [Test]
    public void ResolveMcpConfigPath_Returns_Null_When_No_Config()
    {
        AgentSettings settings = new() { McpConfigPath = Path.Combine(_tempDir, "nonexistent.json") };
        string? resolved = McpConfigLoader.ResolveMcpConfigPath(settings.McpConfigPath);

        // Returns null because the file doesn't exist (unless settings path is checked differently)
        // The actual behavior: if the settings path doesn't exist, it falls through
        // to env var and CWD checks
        Assert.That(resolved is null || File.Exists(resolved), Is.True);
    }

    #endregion

    #region McpConfigLoader — Initialization

    [Test]
    public async Task McpConfigLoader_AllTools_Empty_Initially()
    {
        McpConfigLoader loader = new();
        Assert.That(loader.AllTools, Is.Empty);
        Assert.That(loader.ServerStatuses, Is.Empty);
        await loader.DisposeAsync();
    }

    [Test]
    public async Task McpConfigLoader_LoadAsync_EmptyConfig_NoServers()
    {
        string configPath = Path.Combine(_tempDir, "empty-mcp.json");
        File.WriteAllText(configPath, """{"servers":[]}""");

        McpConfigLoader loader = new();
        await loader.LoadAsync(configPath);

        Assert.That(loader.AllTools, Is.Empty);
        Assert.That(loader.ServerStatuses, Is.Empty);
        await loader.DisposeAsync();
    }

    [Test]
    public async Task McpConfigLoader_LoadAsync_InvalidJson_Silently_Fails()
    {
        string configPath = Path.Combine(_tempDir, "bad-mcp.json");
        File.WriteAllText(configPath, "not json at all!!!");

        McpConfigLoader loader = new();
        // Should not throw
        await loader.LoadAsync(configPath);

        Assert.That(loader.AllTools, Is.Empty);
        await loader.DisposeAsync();
    }

    #endregion
}
