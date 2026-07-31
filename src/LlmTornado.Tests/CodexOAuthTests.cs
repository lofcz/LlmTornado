using System.Net;
using System.Net.Http.Headers;
using System.Text;
using LlmTornado.Codex;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

[TestFixture]
public class CodexOAuthTests
{
    [Test]
    public async Task BrowserLogin_UsesPkceCallbackAndPersistsCredentials()
    {
        string idToken = Jwt(new JObject
        {
            ["email"] = "user@example.com",
            ["https://api.openai.com/auth"] = new JObject
            {
                ["chatgpt_account_id"] = "account-1",
                ["chatgpt_plan_type"] = "pro"
            }
        });
        string accessToken = Jwt(new JObject
        {
            ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        });
        string? tokenRequestBody = null;
        RecordingHandler handler = new RecordingHandler(async request =>
        {
            tokenRequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync();
            return JsonResponse(new JObject
            {
                ["id_token"] = idToken,
                ["access_token"] = accessToken,
                ["refresh_token"] = "refresh-1"
            });
        });
        CodexOAuthMemoryCredentialStore store = new CodexOAuthMemoryCredentialStore();

        await using CodexOAuthSession session = await new TornadoApi().Codex.ConnectOAuthAsync(
            new CodexOAuthOptions
            {
                CallbackPort = 0,
                FallbackCallbackPort = 0,
                CredentialStore = store,
                HttpClient = new HttpClient(handler)
            });
        CodexOAuthBrowserLogin login = await session.StartBrowserLoginAsync();
        Dictionary<string, string> authorizeQuery = CodexOAuthProtocol.ParseQuery(login.AuthorizationUrl.Query);
        Uri callback = new Uri(
            $"http://localhost:{login.CallbackPort}/auth/callback" +
            $"?code=test-code&state={Uri.EscapeDataString(authorizeQuery["state"])}");

        using HttpClient callbackClient = new HttpClient();
        using HttpResponseMessage callbackResponse = await callbackClient.GetAsync(callback);
        CodexOAuthLoginResult result = await login.WaitAsync();

        Assert.Multiple(() =>
        {
            Assert.That(callbackResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(result.Success, Is.True);
            Assert.That(result.Account?.Email, Is.EqualTo("user@example.com"));
            Assert.That(result.Account?.PlanType, Is.EqualTo("pro"));
            Assert.That(authorizeQuery["client_id"], Is.EqualTo("app_EMoamEEZ73f0CkXaXp7hrann"));
            Assert.That(authorizeQuery["code_challenge_method"], Is.EqualTo("S256"));
        });

        Dictionary<string, string> tokenForm = CodexOAuthProtocol.ParseQuery(tokenRequestBody ?? string.Empty);
        Assert.Multiple(() =>
        {
            Assert.That(tokenForm["grant_type"], Is.EqualTo("authorization_code"));
            Assert.That(tokenForm["code"], Is.EqualTo("test-code"));
            Assert.That(
                CodexOAuthProtocol.CreateCodeChallenge(tokenForm["code_verifier"]),
                Is.EqualTo(authorizeQuery["code_challenge"]));
        });

        CodexOAuthCredentials? saved = await store.LoadAsync();
        Assert.Multiple(() =>
        {
            Assert.That(saved?.AccessToken, Is.EqualTo(accessToken));
            Assert.That(saved?.RefreshToken, Is.EqualTo("refresh-1"));
            Assert.That(saved?.AccountId, Is.EqualTo("account-1"));
        });
    }

    [Test]
    public async Task ListModels_RefreshesRotatingTokenAndPreservesCatalogOrder()
    {
        CodexOAuthMemoryCredentialStore store = new CodexOAuthMemoryCredentialStore(
            Credentials("expired-access", "refresh-old", DateTimeOffset.UtcNow.AddMinutes(-1)));
        int refreshRequests = 0;
        string? modelAuthorization = null;
        string? modelAccount = null;
        string? catalogClientVersion = null;
        RecordingHandler handler = new RecordingHandler(async request =>
        {
            if (request.RequestUri?.AbsolutePath.EndsWith("/oauth/token", StringComparison.Ordinal) == true)
            {
                refreshRequests++;
                JObject refresh = JObject.Parse(await request.Content!.ReadAsStringAsync());
                Assert.That(refresh.Value<string>("refresh_token"), Is.EqualTo("refresh-old"));
                return JsonResponse(new JObject
                {
                    ["access_token"] = Jwt(new JObject
                    {
                        ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
                    }),
                    ["refresh_token"] = "refresh-new"
                });
            }

            modelAuthorization = request.Headers.Authorization?.ToString();
            modelAccount = request.Headers.TryGetValues("ChatGPT-Account-ID", out IEnumerable<string>? values)
                ? values.Single()
                : null;
            Dictionary<string, string> catalogQuery =
                CodexOAuthProtocol.ParseQuery(request.RequestUri?.Query ?? string.Empty);
            catalogQuery.TryGetValue("client_version", out catalogClientVersion);
            return JsonResponse(new JObject
            {
                ["models"] = new JArray
                {
                    BackendModel("gpt-5.4", true, "low", "medium", "high"),
                    BackendModel("gpt-5.3-codex", false, new[] { "medium", "high" }, "list"),
                    BackendModel("hidden-model", false, "medium", visibility: "hide")
                }
            });
        });

        await using CodexOAuthSession session = await new TornadoApi().Codex.ConnectOAuthAsync(
            new CodexOAuthOptions
            {
                CredentialStore = store,
                HttpClient = new HttpClient(handler),
                ClientVersion = "1.2.3"
            });
        IReadOnlyList<CodexModel> models = await session.ListModelsAsync();

        Assert.Multiple(() =>
        {
            Assert.That(refreshRequests, Is.EqualTo(1));
            Assert.That(modelAuthorization, Does.StartWith("Bearer "));
            Assert.That(modelAccount, Is.EqualTo("account-1"));
            Assert.That(catalogClientVersion, Is.EqualTo(CodexOAuthOptions.DefaultCodexProtocolVersion));
            Assert.That(catalogClientVersion, Is.Not.EqualTo("1.2.3"));
            Assert.That(models.Select(model => model.Model), Is.EqualTo(new[] { "gpt-5.4", "gpt-5.3-codex" }));
            Assert.That(
                models[0].SupportedReasoningEfforts.Select(effort => effort.ReasoningEffort),
                Is.EqualTo(new[] { "low", "medium", "high" }));
            Assert.That(
                models[0].ServiceTiers.Select(serviceTier => serviceTier.Id),
                Is.EqualTo(new[] { "priority", "future-tier" }));
            Assert.That(models[0].DefaultServiceTier, Is.EqualTo("priority"));
        });

        CodexOAuthCredentials? saved = await store.LoadAsync();
        Assert.That(saved?.RefreshToken, Is.EqualTo("refresh-new"));
    }

    [Test]
    public async Task ListModels_UsesConfiguredCodexProtocolVersion()
    {
        CodexOAuthMemoryCredentialStore store = new CodexOAuthMemoryCredentialStore(
            Credentials(
                Jwt(new JObject { ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds() }),
                "refresh-1",
                DateTimeOffset.UtcNow.AddHours(1)));
        string? catalogClientVersion = null;
        RecordingHandler handler = new RecordingHandler(request =>
        {
            Dictionary<string, string> query =
                CodexOAuthProtocol.ParseQuery(request.RequestUri?.Query ?? string.Empty);
            query.TryGetValue("client_version", out catalogClientVersion);
            return Task.FromResult(JsonResponse(new JObject { ["models"] = new JArray() }));
        });

        await using CodexOAuthSession session = await new TornadoApi().Codex.ConnectOAuthAsync(
            new CodexOAuthOptions
            {
                CredentialStore = store,
                HttpClient = new HttpClient(handler),
                ClientVersion = "1.2.3",
                CodexProtocolVersion = "0.147.1-test"
            });
        await session.ListModelsAsync();

        Assert.That(catalogClientVersion, Is.EqualTo("0.147.1-test"));
    }

    [Test]
    public async Task TextThread_SendsOnlyTextAndReplaysClientManagedHistory()
    {
        CodexOAuthMemoryCredentialStore store = new CodexOAuthMemoryCredentialStore(
            Credentials(
                Jwt(new JObject { ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds() }),
                "refresh-1",
                DateTimeOffset.UtcNow.AddHours(1)));
        List<JObject> payloads = [];
        int responseNumber = 0;
        RecordingHandler handler = new RecordingHandler(async request =>
        {
            if (request.RequestUri?.AbsolutePath.EndsWith("/models", StringComparison.Ordinal) == true)
            {
                return JsonResponse(new JObject
                {
                    ["models"] = new JArray
                    {
                        BackendModel("gpt-5.4", true, new[] { "medium", "high" }, "list")
                    }
                });
            }

            payloads.Add(JObject.Parse(await request.Content!.ReadAsStringAsync()));
            responseNumber++;
            string responseId = $"response-{responseNumber}";
            string sse =
                $"event: response.output_text.delta\n" +
                $"data: {{\"type\":\"response.output_text.delta\",\"response_id\":\"{responseId}\",\"item_id\":\"item-1\",\"delta\":\"Codex \"}}\n\n" +
                $"event: response.output_text.delta\n" +
                $"data: {{\"type\":\"response.output_text.delta\",\"response_id\":\"{responseId}\",\"item_id\":\"item-1\",\"delta\":\"reply\"}}\n\n" +
                $"event: response.output_item.done\n" +
                $"data: {{\"type\":\"response.output_item.done\",\"item\":{{\"id\":\"reasoning-{responseNumber}\",\"type\":\"reasoning\",\"encrypted_content\":\"encrypted-{responseNumber}\",\"summary\":[]}}}}\n\n" +
                $"event: response.output_item.done\n" +
                $"data: {{\"type\":\"response.output_item.done\",\"item\":{{\"id\":\"message-{responseNumber}\",\"type\":\"message\",\"role\":\"assistant\",\"content\":[{{\"type\":\"output_text\",\"text\":\"Codex reply\"}}]}}}}\n\n" +
                $"event: response.completed\n" +
                $"data: {{\"type\":\"response.completed\",\"response\":{{\"id\":\"{responseId}\",\"status\":\"completed\"}}}}\n\n";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(sse, Encoding.UTF8, "text/event-stream")
            };
        });

        await using CodexOAuthSession session = await new TornadoApi().Codex.ConnectOAuthAsync(
            new CodexOAuthOptions
            {
                CredentialStore = store,
                HttpClient = new HttpClient(handler)
            });
        CodexOAuthThread thread = await session.StartThreadAsync(new CodexOAuthThreadOptions
        {
            Model = "gpt-5.4",
            Instructions = "Reply briefly."
        });
        List<string> deltas = [];
        CodexOAuthTurnResult first = await thread.RunAsync("First", new CodexOAuthTurnOptions
        {
            ReasoningEffort = "high",
            ServiceTier = "priority",
            OnTextDelta = delta =>
            {
                deltas.Add(delta.Delta);
                return Task.CompletedTask;
            }
        });
        CodexOAuthTurnResult second = await thread.RunAsync("Second");

        Assert.Multiple(() =>
        {
            Assert.That(first.FinalResponse, Is.EqualTo("Codex reply"));
            Assert.That(second.ResponseId, Is.EqualTo("response-2"));
            Assert.That(deltas, Is.EqualTo(new[] { "Codex ", "reply" }));
            Assert.That(payloads[0].Value<string>("instructions"), Is.EqualTo("gpt-5.4 base instructions"));
            Assert.That(payloads[0]["input"]?[0]?["role"]?.Value<string>(), Is.EqualTo("developer"));
            Assert.That(payloads[0]["input"]?[1]?["content"]?[0]?["type"]?.Value<string>(), Is.EqualTo("input_text"));
            Assert.That(payloads[0].ToString(Formatting.None), Does.Not.Contain("image"));
            Assert.That(payloads[0]["include"]?[0]?.Value<string>(), Is.EqualTo("reasoning.encrypted_content"));
            Assert.That(payloads[0].Value<string>("service_tier"), Is.EqualTo("priority"));
            Assert.That(payloads[1].Property("previous_response_id"), Is.Null);
            Assert.That(payloads[1]["input"]?.Count(), Is.EqualTo(5));
            Assert.That(payloads[1]["input"]?[2]?["encrypted_content"]?.Value<string>(), Is.EqualTo("encrypted-1"));
            Assert.That(payloads[1]["input"]?[3]?["role"]?.Value<string>(), Is.EqualTo("assistant"));
            Assert.That(payloads[1]["input"]?[4]?["content"]?[0]?["text"]?.Value<string>(), Is.EqualTo("Second"));
            Assert.That(
                typeof(CodexOAuthSession).GetMethods().Any(method => method.Name.Contains("Image", StringComparison.Ordinal)),
                Is.False);
        });
    }

    [Test]
    public async Task UnauthorizedModelRequest_RefreshesAndRetriesOnce()
    {
        CodexOAuthMemoryCredentialStore store = new CodexOAuthMemoryCredentialStore(
            Credentials(
                Jwt(new JObject { ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds() }),
                "refresh-old",
                DateTimeOffset.UtcNow.AddHours(1)));
        int modelRequests = 0;
        int refreshRequests = 0;
        RecordingHandler handler = new RecordingHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath.EndsWith("/oauth/token", StringComparison.Ordinal) == true)
            {
                refreshRequests++;
                return Task.FromResult(JsonResponse(new JObject
                {
                    ["access_token"] = Jwt(new JObject
                    {
                        ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
                    }),
                    ["refresh_token"] = "refresh-new"
                }));
            }

            modelRequests++;
            return Task.FromResult(modelRequests == 1
                ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
                : JsonResponse(new JObject
                {
                    ["models"] = new JArray { BackendModel("gpt-5.4", true, "medium") }
                }));
        });

        await using CodexOAuthSession session = await new TornadoApi().Codex.ConnectOAuthAsync(
            new CodexOAuthOptions
            {
                CredentialStore = store,
                HttpClient = new HttpClient(handler)
            });
        IReadOnlyList<CodexModel> models = await session.ListModelsAsync();

        Assert.Multiple(() =>
        {
            Assert.That(models, Has.Count.EqualTo(1));
            Assert.That(modelRequests, Is.EqualTo(2));
            Assert.That(refreshRequests, Is.EqualTo(1));
        });
    }

    private static CodexOAuthCredentials Credentials(
        string accessToken,
        string refreshToken,
        DateTimeOffset expiresAt)
        => new CodexOAuthCredentials
        {
            IdToken = Jwt(new JObject
            {
                ["email"] = "user@example.com",
                ["https://api.openai.com/auth"] = new JObject
                {
                    ["chatgpt_account_id"] = "account-1",
                    ["chatgpt_plan_type"] = "pro"
                }
            }),
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccountId = "account-1",
            Email = "user@example.com",
            PlanType = "pro",
            ExpiresAtUtc = expiresAt,
            LastRefreshUtc = DateTimeOffset.UtcNow
        };

    private static JObject BackendModel(
        string slug,
        bool isDefault,
        params string[] efforts)
        => BackendModel(slug, isDefault, efforts, "list");

    private static JObject BackendModel(
        string slug,
        bool isDefault,
        string effort,
        string visibility)
        => BackendModel(slug, isDefault, new[] { effort }, visibility);

    private static JObject BackendModel(
        string slug,
        bool isDefault,
        IReadOnlyList<string> efforts,
        string visibility)
        => new JObject
        {
            ["slug"] = slug,
            ["display_name"] = slug,
            ["description"] = $"{slug} description",
            ["base_instructions"] = $"{slug} base instructions",
            ["visibility"] = visibility,
            ["show_in_picker"] = true,
            ["supported_in_api"] = true,
            ["is_default"] = isDefault,
            ["default_reasoning_level"] = efforts.First(),
            ["default_service_tier"] = "priority",
            ["supported_reasoning_levels"] = new JArray(efforts.Select(effort => new JObject
            {
                ["effort"] = effort,
                ["description"] = effort
            })),
            ["service_tiers"] = new JArray(new JObject
            {
                ["id"] = "priority",
                ["name"] = "Fast",
                ["description"] = "1.5x speed"
            }, new JObject
            {
                ["id"] = "future-tier",
                ["name"] = "Future",
                ["description"] = "Future catalog value"
            }),
            ["input_modalities"] = new JArray("text")
        };

    private static string Jwt(JObject claims)
    {
        string header = Base64Url(Encoding.UTF8.GetBytes("{}"));
        string payload = Base64Url(Encoding.UTF8.GetBytes(claims.ToString(Formatting.None)));
        return $"{header}.{payload}.signature";
    }

    private static string Base64Url(byte[] value)
        => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static HttpResponseMessage JsonResponse(JObject body)
        => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body.ToString(Formatting.None), Encoding.UTF8, "application/json")
        };

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> handler;

        internal RecordingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
        {
            this.handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return handler(request);
        }
    }
}
