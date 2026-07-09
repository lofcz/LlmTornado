using System.Net;
using System.Text;
using LlmTornado.Chat.Models;
using LlmTornado.Cli.Core.Memory;
using LlmTornado.Cli.Core.Providers;
using LlmTornado.Code;

namespace LlmTornado.Cli.Tests;

[TestFixture]
public class OpenAiCompatEndpointTests
{
    #region ParseEnv

    [Test]
    public void ParseEnv_Empty_ReturnsEmpty()
    {
        Assert.That(OpenAiCompatEndpoint.ParseEnv(null), Is.Empty);
        Assert.That(OpenAiCompatEndpoint.ParseEnv(""), Is.Empty);
        Assert.That(OpenAiCompatEndpoint.ParseEnv("   "), Is.Empty);
    }

    [Test]
    public void ParseEnv_NameUrlOnly()
    {
        List<OpenAiCompatEndpoint> result = OpenAiCompatEndpoint.ParseEnv("lmstudio=http://localhost:1234/v1");
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("lmstudio"));
        Assert.That(result[0].BaseUrl, Is.EqualTo("http://localhost:1234/v1"));
        Assert.That(result[0].ApiKey, Is.Null);
        Assert.That(result[0].ContextTokens, Is.Null);
        Assert.That(result[0].Enabled, Is.True);
    }

    [Test]
    public void ParseEnv_NameUrlKeyAndContext()
    {
        List<OpenAiCompatEndpoint> result = OpenAiCompatEndpoint.ParseEnv(
            "vllm=http://127.0.0.1:8000/v1|sk-test|32768");
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("vllm"));
        Assert.That(result[0].BaseUrl, Is.EqualTo("http://127.0.0.1:8000/v1"));
        Assert.That(result[0].ApiKey, Is.EqualTo("sk-test"));
        Assert.That(result[0].ContextTokens, Is.EqualTo(32768));
    }

    [Test]
    public void ParseEnv_MultipleEntries_AndSkipsInvalid()
    {
        List<OpenAiCompatEndpoint> result = OpenAiCompatEndpoint.ParseEnv(
            "a=http://a/v1,badentry,b=http://b/v1|key,=nourl,noname=");
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result.Select(e => e.Name), Is.EquivalentTo(new[] { "a", "b" }));
        Assert.That(result.First(e => e.Name == "b").ApiKey, Is.EqualTo("key"));
    }

    [Test]
    public void ParseEnv_AddsHttpScheme_WhenMissing()
    {
        List<OpenAiCompatEndpoint> result = OpenAiCompatEndpoint.ParseEnv("local=localhost:1234/v1");
        Assert.That(result[0].BaseUrl, Is.EqualTo("http://localhost:1234/v1"));
    }

    #endregion

    #region Merge / Normalize

    [Test]
    public void Merge_SettingsWinByName()
    {
        List<OpenAiCompatEndpoint> env =
        [
            new() { Name = "lmstudio", BaseUrl = "http://env:1234/v1", ApiKey = "env-key", ContextTokens = 4096 },
            new() { Name = "only-env", BaseUrl = "http://only-env/v1" },
        ];
        List<OpenAiCompatEndpoint> settings =
        [
            new() { Name = "lmstudio", BaseUrl = "http://settings:1234/v1", ApiKey = "settings-key", ContextTokens = 8192 },
        ];

        List<OpenAiCompatEndpoint> merged = OpenAiCompatEndpoint.Merge(settings, env);
        Assert.That(merged, Has.Count.EqualTo(2));

        OpenAiCompatEndpoint lm = merged.First(e => e.Name.Equals("lmstudio", StringComparison.OrdinalIgnoreCase));
        Assert.That(lm.BaseUrl, Is.EqualTo("http://settings:1234/v1"));
        Assert.That(lm.ApiKey, Is.EqualTo("settings-key"));
        Assert.That(lm.ContextTokens, Is.EqualTo(8192));
        Assert.That(merged.Any(e => e.Name == "only-env"), Is.True);
    }

    [Test]
    public void Merge_DisabledSettingsSuppressesEnv()
    {
        List<OpenAiCompatEndpoint> env =
        [
            new() { Name = "lmstudio", BaseUrl = "http://env:1234/v1" },
        ];
        List<OpenAiCompatEndpoint> settings =
        [
            new() { Name = "lmstudio", BaseUrl = "http://settings:1234/v1", Enabled = false },
        ];

        List<OpenAiCompatEndpoint> merged = OpenAiCompatEndpoint.Merge(settings, env);
        Assert.That(merged, Is.Empty);
    }

    [Test]
    public void NormalizeBaseUrl_TrimsSlash_AndAddsScheme()
    {
        Assert.That(OpenAiCompatEndpoint.NormalizeBaseUrl("http://x/v1/"), Is.EqualTo("http://x/v1"));
        Assert.That(OpenAiCompatEndpoint.NormalizeBaseUrl("https://x/v1"), Is.EqualTo("https://x/v1"));
        Assert.That(OpenAiCompatEndpoint.NormalizeBaseUrl("x:1234/v1"), Is.EqualTo("http://x:1234/v1"));
    }

    #endregion
}

[TestFixture]
public class OpenAiCompatProberTests
{
    [Test]
    public void ResolveContextTokens_Order_Model_Endpoint_Cap_Default()
    {
        Assert.That(OpenAiCompatProber.ResolveContextTokens(16384, 8192, 4096), Is.EqualTo(16384));
        Assert.That(OpenAiCompatProber.ResolveContextTokens(null, 8192, 4096), Is.EqualTo(8192));
        Assert.That(OpenAiCompatProber.ResolveContextTokens(null, null, 4096), Is.EqualTo(4096));
        Assert.That(OpenAiCompatProber.ResolveContextTokens(null, null, null), Is.EqualTo(8192));
        Assert.That(OpenAiCompatProber.ResolveContextTokens(0, 0, 0), Is.EqualTo(8192));
    }

    [Test]
    public void ProbeModels_ParsesOpenAiStylePayload_AndContextFields()
    {
        using LocalModelsServer server = new(
            """
            {
              "data": [
                { "id": "model-a", "context_length": 32768 },
                { "id": "model-b", "max_model_len": 16384 },
                { "id": "model-c" },
                { "id": "model-a" }
              ]
            }
            """);

        OpenAiCompatEndpoint endpoint = new()
        {
            Name = "lmstudio",
            BaseUrl = server.BaseUrl,
            ContextTokens = 8192,
        };

        List<ChatModel> models = OpenAiCompatProber.ProbeModels(endpoint, out string? warning);
        Assert.That(warning, Is.Null);
        Assert.That(models, Has.Count.EqualTo(3));
        Assert.That(models.Select(m => m.Name), Is.EquivalentTo(new[] { "model-a", "model-b", "model-c" }));
        Assert.That(models.All(m => m.Provider == LLmProviders.Custom), Is.True);
        Assert.That(models.First(m => m.Name == "model-a").ContextTokens, Is.EqualTo(32768));
        Assert.That(models.First(m => m.Name == "model-b").ContextTokens, Is.EqualTo(16384));
        Assert.That(models.First(m => m.Name == "model-c").ContextTokens, Is.EqualTo(8192));
    }

    [Test]
    public void ProbeModels_Unreachable_ReturnsEmptyWithWarning()
    {
        OpenAiCompatEndpoint endpoint = new()
        {
            Name = "dead",
            BaseUrl = "http://127.0.0.1:1/v1",
        };

        List<ChatModel> models = OpenAiCompatProber.ProbeModels(endpoint, out string? warning);
        Assert.That(models, Is.Empty);
        Assert.That(warning, Does.Contain("dead"));
    }

    [Test]
    public void CreateApi_UsesEndpointBaseUrl()
    {
        OpenAiCompatEndpoint endpoint = new()
        {
            Name = "lmstudio",
            BaseUrl = "http://localhost:1234/v1",
            ApiKey = "sk-test",
        };

        TornadoApi api = OpenAiCompatProber.CreateApi(endpoint);
        Assert.That(api, Is.Not.Null);
    }

    /// <summary>Minimal HttpListener that serves a fixed /models JSON body.</summary>
    private sealed class LocalModelsServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _loop;

        public string BaseUrl { get; }

        public LocalModelsServer(string modelsJson)
        {
            int port = GetFreePort();
            BaseUrl = $"http://127.0.0.1:{port}/v1";
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            _listener.Start();

            _loop = Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    HttpListenerContext ctx;
                    try
                    {
                        ctx = await _listener.GetContextAsync().WaitAsync(_cts.Token);
                    }
                    catch
                    {
                        break;
                    }

                    byte[] body = Encoding.UTF8.GetBytes(modelsJson);
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentType = "application/json";
                    ctx.Response.ContentLength64 = body.Length;
                    await ctx.Response.OutputStream.WriteAsync(body);
                    ctx.Response.Close();
                }
            });
        }

        private static int GetFreePort()
        {
            System.Net.Sockets.TcpListener l = new(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return port;
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _listener.Stop(); } catch { /* ignore */ }
            try { _listener.Close(); } catch { /* ignore */ }
            try { _loop.Wait(TimeSpan.FromSeconds(1)); } catch { /* ignore */ }
            _cts.Dispose();
        }
    }
}

[TestFixture]
public class ProviderDetectionResultRoutingTests
{
    private static ProviderDetectionResult BuildResult(
        TornadoApi sharedApi,
        params DetectedProvider[] providers) => new()
    {
        Api = sharedApi,
        Providers = [.. providers],
        ActiveModel = providers[0].DefaultModel,
    };

    [Test]
    public void GetApiForModel_CloudUsesShared_CustomUsesDedicated()
    {
        TornadoApi shared = new("cloud-key");
        TornadoApi dedicated = new(new Uri("http://localhost:1234/v1/"), "local", LLmProviders.Custom);

        ChatModel cloudModel = ChatModel.OpenAi.Gpt41.V41Nano;
        ChatModel localModel = new("qwen", LLmProviders.Custom, 8192);

        DetectedProvider cloud = new()
        {
            Provider = LLmProviders.OpenAi,
            ApiKey = "cloud-key",
            Models = [cloudModel],
            DefaultModel = cloudModel,
        };
        DetectedProvider local = new()
        {
            Provider = LLmProviders.Custom,
            ApiKey = "local",
            Models = [localModel],
            DefaultModel = localModel,
            EndpointName = "lmstudio",
            DedicatedApi = dedicated,
            DefaultContextTokens = 8192,
        };

        ProviderDetectionResult result = BuildResult(shared, cloud, local);

        Assert.That(result.GetApiForModel(cloudModel), Is.SameAs(shared));
        Assert.That(result.GetApiForModel(localModel), Is.SameAs(dedicated));
        Assert.That(result.FindOwner(localModel)?.EndpointName, Is.EqualTo("lmstudio"));
    }

    [Test]
    public void ResolveModel_BareName_AndQualified_AndAmbiguous()
    {
        TornadoApi shared = new("cloud-key");
        TornadoApi aApi = new(new Uri("http://a/v1/"), "", LLmProviders.Custom);
        TornadoApi bApi = new(new Uri("http://b/v1/"), "", LLmProviders.Custom);

        ChatModel sharedNameA = new("shared-model", LLmProviders.Custom);
        ChatModel sharedNameB = new("shared-model", LLmProviders.Custom);
        ChatModel unique = new("unique-model", LLmProviders.Custom);

        DetectedProvider epA = new()
        {
            Provider = LLmProviders.Custom,
            ApiKey = "",
            Models = [sharedNameA, unique],
            DefaultModel = unique,
            EndpointName = "lmstudio",
            DedicatedApi = aApi,
        };
        DetectedProvider epB = new()
        {
            Provider = LLmProviders.Custom,
            ApiKey = "",
            Models = [sharedNameB],
            DefaultModel = sharedNameB,
            EndpointName = "vllm",
            DedicatedApi = bApi,
        };

        ProviderDetectionResult result = BuildResult(shared, epA, epB);

        Assert.That(result.ResolveModel("unique-model", out string? amb1), Is.SameAs(unique));
        Assert.That(amb1, Is.Null);

        Assert.That(result.ResolveModel("shared-model", out string? amb2), Is.Null);
        Assert.That(amb2, Does.Contain("multiple endpoints"));

        Assert.That(result.ResolveModel("lmstudio/shared-model", out string? amb3), Is.SameAs(sharedNameA));
        Assert.That(amb3, Is.Null);

        Assert.That(result.ResolveModel("vllm/shared-model", out _), Is.SameAs(sharedNameB));
        Assert.That(result.ResolveModel("missing/model", out _), Is.Null);
    }
}

[TestFixture]
public class ConversationMemoryUnknownContextTests
{
    [Test]
    public void NullContext_FallsBackTo8192_Not128k()
    {
#pragma warning disable CS0618
        ConversationMemoryManager manager = new(
            new TornadoApi("fake-key"),
            new ChatModel("local", LLmProviders.Custom),
            contextWindowTokens: null,
            conversationPath: null);
#pragma warning restore CS0618

        Assert.That(manager.ModelContextWindowTokens, Is.EqualTo(8192));
        Assert.That(manager.EffectiveCompressionContextTokens, Is.EqualTo(8192));
    }

    [Test]
    public void NullContext_PrefersCompressionCap_Over8192()
    {
#pragma warning disable CS0618
        ConversationMemoryManager manager = new(
            new TornadoApi("fake-key"),
            new ChatModel("local", LLmProviders.Custom),
            contextWindowTokens: null,
            conversationPath: null,
            compressionContextTokenCap: 16384);
#pragma warning restore CS0618

        Assert.That(manager.ModelContextWindowTokens, Is.EqualTo(16384));
        Assert.That(manager.EffectiveCompressionContextTokens, Is.EqualTo(16384));
    }

    [Test]
    public void UpdateModel_NullContext_FallsBackTo8192()
    {
#pragma warning disable CS0618
        ConversationMemoryManager manager = new(
            new TornadoApi("fake-key"),
            ChatModel.OpenAi.Gpt41.V41Nano,
            128_000,
            conversationPath: null);
#pragma warning restore CS0618

        manager.UpdateModel(new ChatModel("local", LLmProviders.Custom), null);
        Assert.That(manager.ModelContextWindowTokens, Is.EqualTo(8192));
    }
}
