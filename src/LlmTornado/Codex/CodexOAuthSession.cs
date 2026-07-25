using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Codex;

/// <summary>
/// Direct ChatGPT subscription session with browser OAuth, token refresh, model discovery, and text turns.
/// </summary>
public sealed class CodexOAuthSession : IDisposable, IAsyncDisposable
{
    private static readonly TimeSpan FallbackRefreshInterval = TimeSpan.FromDays(8);
    private readonly CodexOAuthOptions options;
    private readonly ICodexOAuthCredentialStore credentialStore;
    private readonly HttpClient httpClient;
    private readonly bool ownsHttpClient;
    private readonly SemaphoreSlim credentialLock = new SemaphoreSlim(1, 1);
    private CodexOAuthCredentials? credentials;
    private bool disposed;

    private CodexOAuthSession(
        CodexOAuthOptions options,
        HttpClient httpClient,
        bool ownsHttpClient,
        CodexOAuthCredentials? credentials)
    {
        this.options = options;
        this.httpClient = httpClient;
        this.ownsHttpClient = ownsHttpClient;
        this.credentials = credentials;
        credentialStore = options.CredentialStore;
        ClientVersion = options.ClientVersion
                        ?? typeof(CodexOAuthSession).Assembly.GetName().Version?.ToString()
                        ?? "0.0.0";
    }

    /// <summary>
    /// Client version sent to the Codex backend.
    /// </summary>
    public string ClientVersion { get; }

    internal static async Task<CodexOAuthSession> ConnectAsync(
        CodexOAuthOptions options,
        CancellationToken cancellationToken)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (options.CredentialStore is null)
        {
            throw new ArgumentNullException(nameof(options.CredentialStore));
        }

        CodexOAuthCredentials? credentials =
            await options.CredentialStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        bool ownsHttpClient = options.HttpClient is null;
        HttpClient client = options.HttpClient ?? new HttpClient();
        return new CodexOAuthSession(options, client, ownsHttpClient, credentials);
    }

    /// <summary>
    /// Reads the current account. Set <paramref name="refreshToken"/> to force a token refresh first.
    /// </summary>
    public async Task<CodexAccountResult> GetAccountAsync(
        bool refreshToken = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        CodexOAuthCredentials? current = credentials;
        if (current is null)
        {
            return new CodexAccountResult
            {
                Account = null,
                RequiresOpenAiAuthentication = true
            };
        }

        current = await GetValidCredentialsAsync(refreshToken, cancellationToken).ConfigureAwait(false);
        return new CodexAccountResult
        {
            Account = ToAccount(current),
            RequiresOpenAiAuthentication = true
        };
    }

    /// <summary>
    /// Starts a direct browser OAuth login and local callback listener.
    /// </summary>
    public Task<CodexOAuthBrowserLogin> StartBrowserLoginAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new CodexOAuthBrowserLogin(CodexOAuthLoginOperation.Start(this, options)));
    }

    /// <summary>
    /// Revokes the current refresh token on a best-effort basis and clears the credential store.
    /// </summary>
    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await credentialLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            CodexOAuthCredentials? current = credentials;
            credentials = null;

            if (current is not null)
            {
                await TryRevokeAsync(current, cancellationToken).ConfigureAwait(false);
            }

            await credentialStore.ClearAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            credentialLock.Release();
        }
    }

    /// <summary>
    /// Lists text-capable Codex models available to the authenticated ChatGPT subscription.
    /// </summary>
    public async Task<IReadOnlyList<CodexModel>> ListModelsAsync(
        bool includeHidden = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        Uri modelsUri = CodexOAuthProtocol.GetApiEndpoint(
            options,
            $"models?client_version={Uri.EscapeDataString(ClientVersion)}");

        using HttpResponseMessage response = await SendAuthenticatedAsync(
            () => new HttpRequestMessage(HttpMethod.Get, modelsUri),
            HttpCompletionOption.ResponseContentRead,
            cancellationToken).ConfigureAwait(false);
        string body = await ReadContentAsync(response.Content, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(response, body, "Codex model discovery failed.");

        CodexBackendModelsResponse catalog =
            JsonConvert.DeserializeObject<CodexBackendModelsResponse>(body)
            ?? new CodexBackendModelsResponse();
        List<CodexModel> models = [];

        foreach (JObject source in catalog.Models)
        {
            bool supportedInApi = source.Value<bool?>("supported_in_api") ?? true;
            string visibility = source.Value<string>("visibility") ?? "list";
            bool showInPicker = source.Value<bool?>("show_in_picker") ?? true;
            bool hidden = !string.Equals(visibility, "list", StringComparison.OrdinalIgnoreCase) || !showInPicker;

            if (!supportedInApi || (!includeHidden && hidden))
            {
                continue;
            }

            string modelId = source.Value<string>("slug")
                             ?? source.Value<string>("id")
                             ?? string.Empty;
            if (string.IsNullOrWhiteSpace(modelId))
            {
                continue;
            }

            CodexModel model = new CodexModel
            {
                Id = modelId,
                Model = modelId,
                DisplayName = source.Value<string>("display_name") ?? modelId,
                Description = source.Value<string>("description") ?? string.Empty,
                BaseInstructions = source.Value<string>("base_instructions") ?? string.Empty,
                Hidden = hidden,
                IsDefault = source.Value<bool?>("is_default") ?? false,
                DefaultReasoningEffort = source.Value<string>("default_reasoning_level") ?? string.Empty,
                SupportsPersonality = source.Value<bool?>("supports_personality") ?? false,
                InputModalities = source["input_modalities"]?.Values<string>().ToList() ?? []
            };

            if (source["supported_reasoning_levels"] is JArray efforts)
            {
                foreach (JObject effort in efforts.OfType<JObject>())
                {
                    model.SupportedReasoningEfforts.Add(new CodexReasoningEffort
                    {
                        ReasoningEffort = effort.Value<string>("effort") ?? string.Empty,
                        Description = effort.Value<string>("description") ?? string.Empty
                    });
                }
            }

            models.Add(model);
        }

        return models;
    }

    /// <summary>
    /// Creates a client-side text thread using a subscription model.
    /// </summary>
    public async Task<CodexOAuthThread> StartThreadAsync(
        CodexOAuthThreadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        options ??= new CodexOAuthThreadOptions();
        bool hasExplicitModel = !string.IsNullOrWhiteSpace(options.Model);
        IReadOnlyList<CodexModel> models = await ListModelsAsync(
            includeHidden: hasExplicitModel,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        CodexModel? selectedModel = hasExplicitModel
            ? models.FirstOrDefault(candidate =>
                string.Equals(candidate.Model, options.Model, StringComparison.Ordinal))
            : models.FirstOrDefault(candidate => candidate.IsDefault) ?? models.FirstOrDefault();

        if (selectedModel is null)
        {
            throw new CodexOAuthException(
                hasExplicitModel
                    ? $"The ChatGPT subscription does not provide Codex model '{options.Model}'."
                    : "The ChatGPT subscription did not return a usable Codex model.");
        }

        if (string.IsNullOrWhiteSpace(selectedModel.BaseInstructions))
        {
            throw new CodexOAuthException(
                $"The Codex model catalog did not provide base instructions for '{selectedModel.Model}'.");
        }

        return new CodexOAuthThread(
            this,
            Guid.NewGuid().ToString(),
            selectedModel.Model,
            selectedModel.BaseInstructions,
            options.Instructions);
    }

    internal async Task<CodexAccount> CompleteBrowserLoginAsync(
        string code,
        Uri redirectUri,
        string codeVerifier,
        CancellationToken cancellationToken)
    {
        using FormUrlEncodedContent content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri.AbsoluteUri,
            ["client_id"] = options.ClientId,
            ["code_verifier"] = codeVerifier
        });
        using HttpRequestMessage request = new HttpRequestMessage(
            HttpMethod.Post,
            CodexOAuthProtocol.GetIssuerEndpoint(options, "oauth/token"))
        {
            Content = content
        };
        ApplyClientHeaders(request);

        using HttpResponseMessage response =
            await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        string body = await ReadContentAsync(response.Content, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(response, body, "OAuth authorization-code exchange failed.");
        CodexOAuthTokenResponse tokenResponse =
            JsonConvert.DeserializeObject<CodexOAuthTokenResponse>(body)
            ?? throw new CodexOAuthException("OpenAI returned an empty OAuth token response.");

        await credentialLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            CodexOAuthCredentials updated = CodexOAuthProtocol.MergeCredentials(tokenResponse, credentials);
            await credentialStore.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
            credentials = updated;
            return ToAccount(updated);
        }
        finally
        {
            credentialLock.Release();
        }
    }

    internal async Task<CodexOAuthTurnResult> RunTextTurnAsync(
        string threadId,
        string model,
        string baseInstructions,
        IReadOnlyList<JObject> input,
        CodexOAuthTurnOptions turnOptions,
        CancellationToken cancellationToken)
    {
        JObject payload = new JObject
        {
            ["model"] = model,
            ["instructions"] = baseInstructions,
            ["input"] = new JArray(input.Select(item => item.DeepClone())),
            ["tools"] = new JArray(),
            ["tool_choice"] = "auto",
            ["parallel_tool_calls"] = false,
            ["store"] = false,
            ["stream"] = true,
            ["include"] = new JArray("reasoning.encrypted_content")
        };

        if (!string.IsNullOrWhiteSpace(turnOptions.ReasoningEffort))
        {
            payload["reasoning"] = new JObject
            {
                ["effort"] = turnOptions.ReasoningEffort,
                ["summary"] = "auto"
            };
        }

        Uri responsesUri = CodexOAuthProtocol.GetApiEndpoint(options, "responses");
        using HttpResponseMessage response = await SendAuthenticatedAsync(
            () =>
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, responsesUri)
                {
                    Content = new StringContent(
                        payload.ToString(Formatting.None),
                        Encoding.UTF8,
                        "application/json")
                };
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                request.Headers.TryAddWithoutValidation("session-id", threadId);
                request.Headers.TryAddWithoutValidation("thread-id", threadId);
                request.Headers.TryAddWithoutValidation("x-client-request-id", threadId);
                return request;
            },
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            string errorBody = await ReadContentAsync(response.Content, cancellationToken).ConfigureAwait(false);
            EnsureSuccess(response, errorBody, "Codex text turn failed.");
        }

        return await ParseResponseStreamAsync(
            response,
            threadId,
            turnOptions.OnTextDelta,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<CodexOAuthCredentials> GetValidCredentialsAsync(
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        await credentialLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            CodexOAuthCredentials current = credentials
                ?? throw new CodexOAuthException("ChatGPT authentication is required. Start browser OAuth login first.");

            if (forceRefresh || NeedsRefresh(current))
            {
                current = await RefreshCredentialsLockedAsync(current, cancellationToken).ConfigureAwait(false);
            }

            return current.Clone();
        }
        finally
        {
            credentialLock.Release();
        }
    }

    private async Task RefreshAfterUnauthorizedAsync(
        string rejectedAccessToken,
        CancellationToken cancellationToken)
    {
        await credentialLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            CodexOAuthCredentials current = credentials
                ?? throw new CodexOAuthException("ChatGPT authentication is required. Start browser OAuth login first.");

            if (string.Equals(current.AccessToken, rejectedAccessToken, StringComparison.Ordinal))
            {
                await RefreshCredentialsLockedAsync(current, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            credentialLock.Release();
        }
    }

    private bool NeedsRefresh(CodexOAuthCredentials current)
    {
        if (current.ExpiresAtUtc.HasValue)
        {
            return current.ExpiresAtUtc.Value <= DateTimeOffset.UtcNow + options.RefreshBeforeExpiration;
        }

        return current.LastRefreshUtc + FallbackRefreshInterval <= DateTimeOffset.UtcNow;
    }

    private async Task<CodexOAuthCredentials> RefreshCredentialsLockedAsync(
        CodexOAuthCredentials current,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(current.RefreshToken))
        {
            throw new CodexOAuthException("The stored ChatGPT credentials do not contain a refresh token.");
        }

        JObject payload = new JObject
        {
            ["client_id"] = options.ClientId,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = current.RefreshToken
        };
        using HttpRequestMessage request = new HttpRequestMessage(
            HttpMethod.Post,
            CodexOAuthProtocol.GetIssuerEndpoint(options, "oauth/token"))
        {
            Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json")
        };
        ApplyClientHeaders(request);

        using HttpResponseMessage response =
            await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        string body = await ReadContentAsync(response.Content, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(response, body, "ChatGPT OAuth token refresh failed.");
        CodexOAuthTokenResponse tokenResponse =
            JsonConvert.DeserializeObject<CodexOAuthTokenResponse>(body)
            ?? throw new CodexOAuthException("OpenAI returned an empty token refresh response.");
        CodexOAuthCredentials updated = CodexOAuthProtocol.MergeCredentials(tokenResponse, current);
        await credentialStore.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
        credentials = updated;
        return updated;
    }

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(
        Func<HttpRequestMessage> requestFactory,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 2; attempt++)
        {
            CodexOAuthCredentials current =
                await GetValidCredentialsAsync(false, cancellationToken).ConfigureAwait(false);
            using HttpRequestMessage request = requestFactory();
            ApplyClientHeaders(request);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", current.AccessToken);

            if (!string.IsNullOrWhiteSpace(current.AccountId))
            {
                request.Headers.TryAddWithoutValidation("ChatGPT-Account-ID", current.AccountId);
            }

            if (current.IsFedRamp)
            {
                request.Headers.TryAddWithoutValidation("X-OpenAI-Fedramp", "true");
            }

            HttpResponseMessage response =
                await httpClient.SendAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.Unauthorized || attempt > 0)
            {
                return response;
            }

            response.Dispose();
            await RefreshAfterUnauthorizedAsync(current.AccessToken, cancellationToken).ConfigureAwait(false);
        }

        throw new CodexOAuthException("ChatGPT authentication failed after token refresh.");
    }

    private async Task TryRevokeAsync(
        CodexOAuthCredentials current,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(current.RefreshToken))
        {
            return;
        }

        JObject payload = new JObject
        {
            ["token"] = current.RefreshToken,
            ["token_type_hint"] = "refresh_token",
            ["client_id"] = options.ClientId
        };

        try
        {
            using HttpRequestMessage request = new HttpRequestMessage(
                HttpMethod.Post,
                CodexOAuthProtocol.GetIssuerEndpoint(options, "oauth/revoke"))
            {
                Content = new StringContent(payload.ToString(Formatting.None), Encoding.UTF8, "application/json")
            };
            ApplyClientHeaders(request);
            using HttpResponseMessage response =
                await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
        }
    }

    private void ApplyClientHeaders(HttpRequestMessage request)
    {
        request.Headers.TryAddWithoutValidation("originator", options.Originator);
        request.Headers.TryAddWithoutValidation("version", ClientVersion);
        request.Headers.TryAddWithoutValidation("User-Agent", $"{options.Originator}/{ClientVersion}");
    }

    private static CodexAccount ToAccount(CodexOAuthCredentials current)
        => new CodexAccount
        {
            Type = "chatgpt",
            Email = current.Email,
            PlanType = current.PlanType
        };

    private static async Task<CodexOAuthTurnResult> ParseResponseStreamAsync(
        HttpResponseMessage response,
        string threadId,
        Func<CodexOAuthTextDelta, Task>? onTextDelta,
        CancellationToken cancellationToken)
    {
        Stream stream = await CodexTask.WithCancellation(
            response.Content.ReadAsStreamAsync(),
            cancellationToken).ConfigureAwait(false);
        using StreamReader reader = new StreamReader(stream);
        StringBuilder finalText = new StringBuilder();
        StringBuilder eventData = new StringBuilder();
        string? eventName = null;
        JObject? completedResponse = null;
        List<JObject> outputItems = [];
        string responseId = string.Empty;
        string? status = null;

        while (true)
        {
            string? line = await CodexTask.WithCancellation(reader.ReadLineAsync(), cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                break;
            }

            if (line.Length == 0)
            {
                if (eventData.Length > 0)
                {
                    await DispatchAsync(eventName, eventData.ToString()).ConfigureAwait(false);
                }

                eventName = null;
                eventData.Clear();
                continue;
            }

            if (line.StartsWith("event:", StringComparison.Ordinal))
            {
                eventName = line.Substring("event:".Length).Trim();
            }
            else if (line.StartsWith("data:", StringComparison.Ordinal))
            {
                if (eventData.Length > 0)
                {
                    eventData.Append('\n');
                }

                eventData.Append(line.Substring("data:".Length).TrimStart());
            }
        }

        if (eventData.Length > 0)
        {
            await DispatchAsync(eventName, eventData.ToString()).ConfigureAwait(false);
        }

        if (completedResponse is null)
        {
            throw new CodexOAuthException("Codex response stream ended before response.completed.");
        }

        if (finalText.Length == 0)
        {
            foreach (JObject content in outputItems
                         .Where(item => string.Equals(item.Value<string>("type"), "message", StringComparison.Ordinal))
                         .SelectMany(item => item["content"]?.OfType<JObject>() ?? []))
            {
                if (string.Equals(content.Value<string>("type"), "output_text", StringComparison.Ordinal))
                {
                    finalText.Append(content.Value<string>("text"));
                }
            }
        }

        return new CodexOAuthTurnResult(
            threadId,
            responseId,
            finalText.ToString(),
            status,
            completedResponse,
            outputItems);

        async Task DispatchAsync(string? sseEvent, string data)
        {
            if (string.Equals(data, "[DONE]", StringComparison.Ordinal))
            {
                return;
            }

            JObject evt = JObject.Parse(data);
            string type = evt.Value<string>("type") ?? sseEvent ?? string.Empty;

            if (string.Equals(type, "response.output_text.delta", StringComparison.Ordinal))
            {
                string delta = evt.Value<string>("delta") ?? string.Empty;
                responseId = evt.Value<string>("response_id") ?? responseId;
                finalText.Append(delta);
                if (onTextDelta is not null)
                {
                    await onTextDelta(new CodexOAuthTextDelta(
                        threadId,
                        responseId,
                        evt.Value<string>("item_id") ?? string.Empty,
                        delta)).ConfigureAwait(false);
                }
            }
            else if (string.Equals(type, "response.completed", StringComparison.Ordinal))
            {
                completedResponse = evt["response"] as JObject ?? evt;
                responseId = completedResponse.Value<string>("id") ?? responseId;
                status = completedResponse.Value<string>("status") ?? "completed";
                if (completedResponse["output"] is JArray completedOutput)
                {
                    foreach (JObject item in completedOutput.OfType<JObject>())
                    {
                        AddOutputItem(item);
                    }
                }
            }
            else if (string.Equals(type, "response.output_item.done", StringComparison.Ordinal)
                     && evt["item"] is JObject outputItem)
            {
                AddOutputItem(outputItem);
            }
            else if (string.Equals(type, "response.failed", StringComparison.Ordinal)
                     || string.Equals(type, "response.incomplete", StringComparison.Ordinal)
                     || string.Equals(type, "error", StringComparison.Ordinal))
            {
                JObject error = evt["response"] as JObject ?? evt;
                throw new CodexOAuthException(
                    error["error"]?.ToString(Formatting.None)
                    ?? $"Codex returned {type}.");
            }
        }

        void AddOutputItem(JObject item)
        {
            string? itemId = item.Value<string>("id");
            if (!string.IsNullOrWhiteSpace(itemId)
                && outputItems.Any(existing =>
                    string.Equals(existing.Value<string>("id"), itemId, StringComparison.Ordinal)))
            {
                return;
            }

            outputItems.Add((JObject)item.DeepClone());
        }
    }

    private static async Task<string> ReadContentAsync(
        HttpContent content,
        CancellationToken cancellationToken)
        => await CodexTask.WithCancellation(content.ReadAsStringAsync(), cancellationToken).ConfigureAwait(false);

    private static void EnsureSuccess(HttpResponseMessage response, string body, string message)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new CodexOAuthException(message, (int)response.StatusCode, body);
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(CodexOAuthSession));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        credentialLock.Dispose();
        if (ownsHttpClient)
        {
            httpClient.Dispose();
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
}
