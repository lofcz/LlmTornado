using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Codex;

/// <summary>
/// A live connection to the official Codex app-server.
/// </summary>
public sealed class CodexSession : IDisposable, IAsyncDisposable
{
    private readonly CodexAppServerOptions options;
    private readonly ICodexAppServerTransport transport;
    private readonly CancellationTokenSource shutdown = new CancellationTokenSource();
    private readonly SemaphoreSlim writeLock = new SemaphoreSlim(1, 1);
    private readonly ConcurrentDictionary<long, TaskCompletionSource<JToken>> pendingRequests = new ConcurrentDictionary<long, TaskCompletionSource<JToken>>();
    private readonly ConcurrentDictionary<string, CodexNotificationQueue> turnNotifications = new ConcurrentDictionary<string, CodexNotificationQueue>(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<CodexLoginResult>> loginCompletions = new ConcurrentDictionary<string, TaskCompletionSource<CodexLoginResult>>(StringComparer.Ordinal);
    private long nextRequestId;
    private Task? readerTask;
    private bool disposed;

    private CodexSession(CodexAppServerOptions options, ICodexAppServerTransport transport)
    {
        this.options = options;
        this.transport = transport;
    }

    /// <summary>
    /// Initialization metadata reported by the connected app-server.
    /// </summary>
    public CodexInitialization Initialization { get; private set; } = new CodexInitialization();

    /// <summary>
    /// Recent app-server diagnostic lines written to stderr.
    /// </summary>
    public IReadOnlyCollection<string> RecentStandardError => transport.RecentStandardError;

    /// <summary>
    /// Raised for every app-server notification.
    /// </summary>
    public event Action<CodexNotification>? NotificationReceived;

    internal static Task<CodexSession> ConnectAsync(CodexAppServerOptions options, CancellationToken cancellationToken)
        => ConnectAsync(options, new CodexProcessTransport(options), cancellationToken);

    internal static async Task<CodexSession> ConnectAsync(
        CodexAppServerOptions options,
        ICodexAppServerTransport transport,
        CancellationToken cancellationToken = default)
    {
        CodexSession session = new CodexSession(options, transport);

        try
        {
            await transport.StartAsync(cancellationToken).ConfigureAwait(false);
            session.readerTask = session.ReadMessagesAsync();

            JObject initializeParams = new JObject
            {
                ["clientInfo"] = new JObject
                {
                    ["name"] = options.ClientName,
                    ["title"] = options.ClientTitle,
                    ["version"] = options.ClientVersion ?? typeof(TornadoApi).Assembly.GetName().Version?.ToString() ?? "unknown"
                }
            };

            JToken initialization = await session.RequestAsync("initialize", initializeParams, cancellationToken).ConfigureAwait(false);
            session.Initialization = initialization.ToObject<CodexInitialization>() ?? new CodexInitialization();
            await session.SendNotificationAsync("initialized", cancellationToken).ConfigureAwait(false);
            return session;
        }
        catch
        {
            await session.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Reads the currently authenticated Codex account. Token refresh remains owned by the app-server.
    /// </summary>
    public async Task<CodexAccountResult> GetAccountAsync(
        bool refreshToken = false,
        CancellationToken cancellationToken = default)
    {
        JObject parameters = new JObject { ["refreshToken"] = refreshToken };
        JToken result = await RequestAsync("account/read", parameters, cancellationToken).ConfigureAwait(false);
        return result.ToObject<CodexAccountResult>() ?? new CodexAccountResult();
    }

    /// <summary>
    /// Starts the official ChatGPT browser login flow. The caller opens the returned URL.
    /// </summary>
    public async Task<CodexBrowserLogin> StartBrowserLoginAsync(CancellationToken cancellationToken = default)
    {
        JToken result = await RequestAsync(
            "account/login/start",
            new JObject { ["type"] = "chatgpt" },
            cancellationToken).ConfigureAwait(false);

        string loginId = result.Value<string>("loginId")
                         ?? throw new InvalidOperationException("Codex app-server did not return a loginId.");
        string authUrl = result.Value<string>("authUrl")
                         ?? throw new InvalidOperationException("Codex app-server did not return an authUrl.");

        loginCompletions.GetOrAdd(loginId, CreateLoginCompletion);
        return new CodexBrowserLogin(this, loginId, new Uri(authUrl, UriKind.Absolute));
    }

    /// <summary>
    /// Logs out of the Codex account. Stored credentials are removed by the app-server.
    /// </summary>
    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        await RequestAsync("account/logout", null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists models advertised for the current Codex account, preserving server order.
    /// </summary>
    public async Task<IReadOnlyList<CodexModel>> ListModelsAsync(
        bool includeHidden = false,
        CancellationToken cancellationToken = default)
    {
        List<CodexModel> models = [];
        string? cursor = null;

        do
        {
            JObject parameters = new JObject { ["includeHidden"] = includeHidden };

            if (cursor is not null)
            {
                parameters["cursor"] = cursor;
            }

            JToken result = await RequestAsync("model/list", parameters, cancellationToken).ConfigureAwait(false);
            CodexModelPage page = result.ToObject<CodexModelPage>() ?? new CodexModelPage();
            models.AddRange(page.Data);
            cursor = page.NextCursor;
        }
        while (!string.IsNullOrEmpty(cursor));

        return models;
    }

    /// <summary>
    /// Starts a new Codex thread with a model selected from <see cref="ListModelsAsync"/>.
    /// </summary>
    public async Task<CodexThread> StartThreadAsync(
        CodexThreadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        CodexThreadOptions effectiveOptions = options ?? new CodexThreadOptions();
        JObject parameters = new JObject();
        AddIfNotNull(parameters, "model", effectiveOptions.Model);
        AddIfNotNull(parameters, "cwd", effectiveOptions.WorkingDirectory);
        AddIfNotNull(parameters, "approvalPolicy", effectiveOptions.ApprovalPolicy);
        AddIfNotNull(parameters, "sandbox", effectiveOptions.Sandbox);

        if (effectiveOptions.Ephemeral.HasValue)
        {
            parameters["ephemeral"] = effectiveOptions.Ephemeral.Value;
        }

        JToken result = await RequestAsync("thread/start", parameters, cancellationToken).ConfigureAwait(false);
        JObject thread = result["thread"] as JObject
                         ?? throw new InvalidOperationException("Codex app-server did not return a thread.");
        string threadId = thread.Value<string>("id")
                          ?? throw new InvalidOperationException("Codex app-server returned a thread without an id.");
        return new CodexThread(this, threadId, result.Value<string>("model") ?? effectiveOptions.Model);
    }

    internal Task<CodexLoginResult> WaitForLoginAsync(string loginId, CancellationToken cancellationToken)
    {
        TaskCompletionSource<CodexLoginResult> completion = loginCompletions.GetOrAdd(loginId, CreateLoginCompletion);
        return CodexTask.WithCancellation(completion.Task, cancellationToken);
    }

    internal async Task CancelLoginAsync(string loginId, CancellationToken cancellationToken)
    {
        await RequestAsync(
            "account/login/cancel",
            new JObject { ["loginId"] = loginId },
            cancellationToken).ConfigureAwait(false);
    }

    internal async Task<CodexTurnResult> RunTextTurnAsync(
        string threadId,
        string input,
        CodexTurnOptions options,
        CancellationToken cancellationToken)
    {
        JObject parameters = new JObject
        {
            ["threadId"] = threadId,
            ["input"] = new JArray
            {
                new JObject
                {
                    ["type"] = "text",
                    ["text"] = input
                }
            }
        };
        AddIfNotNull(parameters, "model", options.Model);
        AddIfNotNull(parameters, "effort", options.ReasoningEffort);

        JToken response = await RequestAsync("turn/start", parameters, cancellationToken).ConfigureAwait(false);
        JObject initialTurn = response["turn"] as JObject
                              ?? throw new InvalidOperationException("Codex app-server did not return a turn.");
        string turnId = initialTurn.Value<string>("id")
                        ?? throw new InvalidOperationException("Codex app-server returned a turn without an id.");
        CodexNotificationQueue notifications = turnNotifications.GetOrAdd(turnId, _ => new CodexNotificationQueue());
        StringBuilder finalResponse = new StringBuilder();

        try
        {
            while (true)
            {
                CodexNotification notification = await notifications.ReadAsync(cancellationToken).ConfigureAwait(false);

                if (notification.Method == "item/agentMessage/delta")
                {
                    string delta = notification.Parameters.Value<string>("delta") ?? string.Empty;
                    finalResponse.Append(delta);

                    if (options.OnTextDelta is not null)
                    {
                        CodexTextDelta textDelta = new CodexTextDelta(
                            notification.Parameters.Value<string>("threadId") ?? threadId,
                            notification.Parameters.Value<string>("turnId") ?? turnId,
                            notification.Parameters.Value<string>("itemId") ?? string.Empty,
                            delta);
                        await options.OnTextDelta(textDelta).ConfigureAwait(false);
                    }

                    continue;
                }

                if (notification.Method != "turn/completed")
                {
                    continue;
                }

                JObject completedTurn = notification.Parameters["turn"] as JObject ?? initialTurn;
                string? status = completedTurn.Value<string>("status");
                return new CodexTurnResult(threadId, turnId, finalResponse.ToString(), status, completedTurn);
            }
        }
        catch (OperationCanceledException)
        {
            await TryInterruptTurnAsync(threadId, turnId).ConfigureAwait(false);
            throw;
        }
        finally
        {
            turnNotifications.TryRemove(turnId, out _);
        }
    }

    private async Task TryInterruptTurnAsync(string threadId, string turnId)
    {
        try
        {
            await RequestAsync(
                "turn/interrupt",
                new JObject
                {
                    ["threadId"] = threadId,
                    ["turnId"] = turnId
                },
                CancellationToken.None).ConfigureAwait(false);
        }
        catch
        {
        }
    }

    private async Task<JToken> RequestAsync(string method, JObject? parameters, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        long id = Interlocked.Increment(ref nextRequestId);
        TaskCompletionSource<JToken> completion = new TaskCompletionSource<JToken>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!pendingRequests.TryAdd(id, completion))
        {
            throw new InvalidOperationException($"Duplicate Codex request id {id}.");
        }

        JObject request = new JObject
        {
            ["method"] = method,
            ["id"] = id
        };

        if (parameters is not null)
        {
            request["params"] = parameters;
        }

        try
        {
            await WriteMessageAsync(request, cancellationToken).ConfigureAwait(false);
            return await CodexTask.WithCancellation(completion.Task, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            pendingRequests.TryRemove(id, out _);
        }
    }

    private Task SendNotificationAsync(string method, CancellationToken cancellationToken)
        => WriteMessageAsync(new JObject { ["method"] = method }, cancellationToken);

    private async Task WriteMessageAsync(JObject message, CancellationToken cancellationToken)
    {
        string json = message.ToString(Formatting.None);
        await writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await transport.WriteLineAsync(json, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writeLock.Release();
        }
    }

    private async Task ReadMessagesAsync()
    {
        try
        {
            while (!shutdown.IsCancellationRequested)
            {
                string? line = await transport.ReadLineAsync(shutdown.Token).ConfigureAwait(false);

                if (line is null)
                {
                    throw new InvalidOperationException(BuildClosedMessage());
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                JObject message = JObject.Parse(line);
                JToken? id = message["id"];
                string? method = message.Value<string>("method");

                if (id is not null && method is not null)
                {
                    _ = HandleServerRequestAsync(id, method, message["params"] as JObject ?? new JObject());
                    continue;
                }

                if (id is not null)
                {
                    CompleteRequest(id, message);
                    continue;
                }

                if (method is not null)
                {
                    DispatchNotification(new CodexNotification(method, message["params"] as JObject ?? new JObject()));
                }
            }
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            FailPendingOperations(exception);
        }
    }

    private void CompleteRequest(JToken id, JObject message)
    {
        if (id.Type != JTokenType.Integer || !pendingRequests.TryRemove(id.Value<long>(), out TaskCompletionSource<JToken>? completion))
        {
            return;
        }

        if (message["error"] is JObject error)
        {
            completion.TrySetException(new CodexRpcException(
                error.Value<int?>("code") ?? -32000,
                error.Value<string>("message") ?? "Codex app-server request failed.",
                error["data"]));
            return;
        }

        completion.TrySetResult(message["result"] ?? new JObject());
    }

    private void DispatchNotification(CodexNotification notification)
    {
        if (notification.Method == "account/login/completed")
        {
            string? loginId = notification.Parameters.Value<string>("loginId");

            if (loginId is not null)
            {
                CodexLoginResult result = notification.Parameters.ToObject<CodexLoginResult>() ?? new CodexLoginResult();
                loginCompletions.GetOrAdd(loginId, CreateLoginCompletion).TrySetResult(result);
            }
        }

        string? turnId = notification.Parameters.Value<string>("turnId")
                         ?? notification.Parameters["turn"]?.Value<string>("id");

        if (turnId is not null)
        {
            turnNotifications.GetOrAdd(turnId, _ => new CodexNotificationQueue()).Enqueue(notification);
        }

        try
        {
            NotificationReceived?.Invoke(notification);
        }
        catch
        {
        }
    }

    private async Task HandleServerRequestAsync(JToken id, string method, JObject parameters)
    {
        JObject response = new JObject { ["id"] = id.DeepClone() };

        try
        {
            if (options.ServerRequestHandler is null)
            {
                response["error"] = new JObject
                {
                    ["code"] = -32601,
                    ["message"] = $"No handler is registered for Codex server request '{method}'."
                };
            }
            else
            {
                JToken? result = await options.ServerRequestHandler(
                    new CodexServerRequest(id.DeepClone(), method, parameters),
                    shutdown.Token).ConfigureAwait(false);
                response["result"] = result ?? new JObject();
            }

            await WriteMessageAsync(response, shutdown.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (!shutdown.IsCancellationRequested)
        {
            response.Remove("result");
            response["error"] = new JObject
            {
                ["code"] = -32603,
                ["message"] = exception.Message
            };

            try
            {
                await WriteMessageAsync(response, shutdown.Token).ConfigureAwait(false);
            }
            catch
            {
            }
        }
    }

    private void FailPendingOperations(Exception exception)
    {
        foreach (KeyValuePair<long, TaskCompletionSource<JToken>> request in pendingRequests.ToArray())
        {
            if (pendingRequests.TryRemove(request.Key, out TaskCompletionSource<JToken>? completion))
            {
                completion.TrySetException(exception);
            }
        }

        foreach (CodexNotificationQueue queue in turnNotifications.Values)
        {
            queue.Fail(exception);
        }

        foreach (TaskCompletionSource<CodexLoginResult> completion in loginCompletions.Values)
        {
            completion.TrySetException(exception);
        }
    }

    private string BuildClosedMessage()
    {
        string diagnostics = string.Join(Environment.NewLine, RecentStandardError.Take(20));
        return diagnostics.Length == 0
            ? "Codex app-server closed its output stream."
            : $"Codex app-server closed its output stream.{Environment.NewLine}{diagnostics}";
    }

    private static TaskCompletionSource<CodexLoginResult> CreateLoginCompletion(string _)
        => new TaskCompletionSource<CodexLoginResult>(TaskCreationOptions.RunContinuationsAsynchronously);

    private static void AddIfNotNull(JObject target, string name, string? value)
    {
        if (value is not null)
        {
            target[name] = value;
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(CodexSession));
        }
    }

    /// <summary>
    /// Stops the app-server process and releases pending operations.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        shutdown.Cancel();
        await transport.StopAsync().ConfigureAwait(false);

        if (readerTask is not null)
        {
            try
            {
                await readerTask.ConfigureAwait(false);
            }
            catch
            {
            }
        }

        FailPendingOperations(new ObjectDisposedException(nameof(CodexSession)));
        transport.Dispose();
        writeLock.Dispose();
        shutdown.Dispose();
    }

    /// <summary>
    /// Stops the app-server process and releases pending operations.
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private sealed class CodexNotificationQueue
    {
        private readonly ConcurrentQueue<CodexNotification> notifications = new ConcurrentQueue<CodexNotification>();
        private readonly SemaphoreSlim signal = new SemaphoreSlim(0);
        private Exception? failure;

        internal void Enqueue(CodexNotification notification)
        {
            notifications.Enqueue(notification);
            signal.Release();
        }

        internal void Fail(Exception exception)
        {
            failure = exception;
            signal.Release();
        }

        internal async Task<CodexNotification> ReadAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (notifications.TryDequeue(out CodexNotification? notification))
                {
                    return notification;
                }

                if (failure is not null)
                {
                    throw failure;
                }

                await signal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
