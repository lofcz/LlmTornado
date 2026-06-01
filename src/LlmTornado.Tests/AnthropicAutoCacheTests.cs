using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Demo;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

/// <summary>
/// Unit and integration tests for Anthropic automatic prompt caching (request-level cache_control).
/// </summary>
[TestFixture]
[Category("Integration")]
public class AnthropicAutoCacheTests
{
    private TornadoApi? _api;
    private IEndpointProvider? _provider;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        string? envKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            _api = new TornadoApi(LLmProviders.Anthropic, envKey);
            _provider = _api.GetProvider(LLmProviders.Anthropic);
            return;
        }

        if (await Program.SetupApi())
        {
            _api = Program.Connect();
            _provider = _api.GetProvider(LLmProviders.Anthropic);
        }
    }

    private static IEndpointProvider GetSerializationProvider()
    {
        return new TornadoApi("test-key").GetProvider(LLmProviders.Anthropic);
    }

    [Test]
    public void AutoCache_Null_OmitsTopLevelCacheControl()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude45.Sonnet250929,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")]
        };

        TornadoRequestContent serialized = request.Serialize(GetSerializationProvider());
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body.ContainsKey("cache_control"), Is.False);
    }

    [Test]
    public void AutoCache_Ephemeral_SerializesTopLevelCacheControl()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude45.Sonnet250929,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            AutoCache = ChatRequestCacheSettings.Ephemeral
        };

        TornadoRequestContent serialized = request.Serialize(GetSerializationProvider());
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["cache_control"]?["type"]?.ToString(), Is.EqualTo("ephemeral"));
        Assert.That(body["cache_control"]?["ttl"], Is.Null);
    }

    [Test]
    public void AutoCache_EphemeralWithOneHourTtl_SerializesTtl()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude45.Sonnet250929,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            AutoCache = ChatRequestCacheSettings.EphemeralWithTtl(ChatRequestCacheTtl.OneHour)
        };

        TornadoRequestContent serialized = request.Serialize(GetSerializationProvider());
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["cache_control"]?["type"]?.ToString(), Is.EqualTo("ephemeral"));
        Assert.That(body["cache_control"]?["ttl"]?.ToString(), Is.EqualTo("1h"));
    }

    [Test]
    public void AutoCache_CopiedFromBaseRequest()
    {
        ChatRequest baseRequest = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude45.Sonnet250929,
            AutoCache = ChatRequestCacheSettings.EphemeralWithTtl(ChatRequestCacheTtl.OneHour)
        };

        ChatRequest derived = new ChatRequest(baseRequest)
        {
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")]
        };

        TornadoRequestContent serialized = derived.Serialize(GetSerializationProvider());
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["cache_control"]?["type"]?.ToString(), Is.EqualTo("ephemeral"));
        Assert.That(body["cache_control"]?["ttl"]?.ToString(), Is.EqualTo("1h"));
    }

    [Test]
    public void AutoCache_DoesNotAddBlockLevelCacheControl()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude45.Sonnet250929,
            Messages =
            [
                new ChatMessage(ChatMessageRoles.System, "You are a helpful assistant."),
                new ChatMessage(ChatMessageRoles.User, "Hello")
            ],
            AutoCache = ChatRequestCacheSettings.Ephemeral
        };

        TornadoRequestContent serialized = request.Serialize(GetSerializationProvider());
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body["cache_control"]?["type"]?.ToString(), Is.EqualTo("ephemeral"));
        Assert.That(body["system"]?.Type, Is.EqualTo(JTokenType.String).Or.EqualTo(JTokenType.Array));

        if (body["system"]?.Type == JTokenType.Array)
        {
            foreach (JToken block in body["system"]!)
            {
                Assert.That(block["cache_control"], Is.Null);
            }
        }
    }

    [Test]
    public void BlockLevelCacheControl_StillSupportedWithoutAutoCache()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude45.Sonnet250929,
            Messages =
            [
                new ChatMessage(ChatMessageRoles.System,
                [
                    new ChatMessagePart("Cached system prompt", new ChatMessagePartAnthropicExtensions
                    {
                        Cache = AnthropicCacheSettings.Ephemeral
                    })
                ]),
                new ChatMessage(ChatMessageRoles.User, "Hello")
            ]
        };

        TornadoRequestContent serialized = request.Serialize(GetSerializationProvider());
        JObject body = JObject.Parse(serialized.Body.ToString()!);

        Assert.That(body.ContainsKey("cache_control"), Is.False);
        Assert.That(body["system"]?[0]?["cache_control"]?["type"]?.ToString(), Is.EqualTo("ephemeral"));
    }

    [Test]
    [Explicit("Requires Anthropic API key and makes real API calls")]
    public async Task AutoCache_FirstRequest_CreatesCacheTokens()
    {
        if (_api is null)
        {
            Assert.Ignore("Anthropic API key not configured. Set ANTHROPIC_API_KEY or provide apiKey.json.");
        }

        string longContext = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "Static", "Files", "pride_and_prejudice.txt"));

        ChatResult result = await _api!.Chat.CreateChatCompletion(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude45.Sonnet250929,
            MaxTokens = 64,
            AutoCache = ChatRequestCacheSettings.Ephemeral,
            Messages =
            [
                new ChatMessage(ChatMessageRoles.System,
                [
                    new ChatMessagePart("You are a literary assistant. Answer questions about the following text."),
                    new ChatMessagePart(longContext)
                ]),
                new ChatMessage(ChatMessageRoles.User, "Who says \"I am sick of Mr. Bingley\"? Reply with just the character name.")
            ]
        });

        Assert.That(result.Ok, Is.True, () => result.Exception?.Message ?? "Request failed");
        Assert.That(result.Usage, Is.Not.Null);
        Assert.That(result.Usage!.CacheCreationTokens, Is.GreaterThan(0),
            "Expected cache_creation_input_tokens on the first cached request.");
        Assert.That(result.Usage.CacheReadTokens.GetValueOrDefault(), Is.EqualTo(0));
    }

    [Test]
    [Explicit("Requires Anthropic API key and makes real API calls")]
    public async Task AutoCache_SecondRequest_ReadsCacheTokens()
    {
        if (_api is null)
        {
            Assert.Ignore("Anthropic API key not configured. Set ANTHROPIC_API_KEY or provide apiKey.json.");
        }

        string longContext = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "Static", "Files", "pride_and_prejudice.txt"));

        List<ChatMessage> messages =
        [
            new ChatMessage(ChatMessageRoles.System,
            [
                new ChatMessagePart("You are a literary assistant. Answer questions about the following text."),
                new ChatMessagePart(longContext)
            ]),
            new ChatMessage(ChatMessageRoles.User, "Who says \"I am sick of Mr. Bingley\"? Reply with just the character name.")
        ];

        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude45.Sonnet250929,
            MaxTokens = 64,
            AutoCache = ChatRequestCacheSettings.Ephemeral,
            Messages = messages
        };

        ChatResult first = await _api!.Chat.CreateChatCompletion(request);
        Assert.That(first.Ok, Is.True, () => first.Exception?.Message ?? "First request failed");
        Assert.That(first.Usage?.CacheCreationTokens, Is.GreaterThan(0), "First request should create cache.");

        messages.Add(new ChatMessage(ChatMessageRoles.Assistant, first.Choices![0].Message?.Content ?? string.Empty));
        messages.Add(new ChatMessage(ChatMessageRoles.User, "What is the first name of that character? Reply with one word."));

        ChatResult second = await _api.Chat.CreateChatCompletion(new ChatRequest(request)
        {
            Messages = messages
        });

        Assert.That(second.Ok, Is.True, () => second.Exception?.Message ?? "Second request failed");
        Assert.That(second.Usage, Is.Not.Null);
        Assert.That(second.Usage!.CacheReadTokens, Is.GreaterThan(0),
            "Expected cache_read_input_tokens when reusing the cached prefix.");
    }

    [Test]
    [Explicit("Requires Anthropic API key and makes real API calls")]
    public async Task AutoCache_VendorUsageObject_ExposesCacheTokenDetails()
    {
        if (_api is null)
        {
            Assert.Ignore("Anthropic API key not configured. Set ANTHROPIC_API_KEY or provide apiKey.json.");
        }

        string longContext = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "Static", "Files", "pride_and_prejudice.txt"));

        ChatResult result = await _api!.Chat.CreateChatCompletion(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude45.Sonnet250929,
            MaxTokens = 32,
            AutoCache = ChatRequestCacheSettings.Ephemeral,
            Messages =
            [
                new ChatMessage(ChatMessageRoles.System,
                [
                    new ChatMessagePart("You are a literary assistant."),
                    new ChatMessagePart(longContext)
                ]),
                new ChatMessage(ChatMessageRoles.User, "Summarize the opening chapter in one sentence.")
            ]
        });

        Assert.That(result.Ok, Is.True, () => result.Exception?.Message ?? "Request failed");
        Assert.That(result.Usage?.VendorUsageObject, Is.InstanceOf<VendorAnthropicUsage>());

        VendorAnthropicUsage vendorUsage = (VendorAnthropicUsage)result.Usage!.VendorUsageObject!;
        Assert.That(vendorUsage.CacheCreationInputTokens.GetValueOrDefault() + vendorUsage.CacheReadInputTokens.GetValueOrDefault(),
            Is.GreaterThan(0), "Expected Anthropic usage to report cache_creation or cache_read tokens.");
    }
}
