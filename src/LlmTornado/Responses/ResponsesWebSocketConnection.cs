using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Responses.Events;
using LlmTornado.Threads;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses;

/// <summary>
/// Persistent WebSocket connection to the OpenAI Responses API (<c>wss://api.openai.com/v1/responses</c>).
/// Supports sequential <c>response.create</c> events with connection-local <c>previous_response_id</c> continuation.
/// </summary>
public sealed class ResponsesWebSocketConnection : IAsyncDisposable
{
    private readonly ResponsesEndpoint endpoint;
    private readonly IEndpointProvider provider;
    private readonly ClientWebSocket webSocket;
    private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
    private readonly byte[] receiveBuffer = new byte[8192];
    private bool disposed;

    internal ResponsesWebSocketConnection(ResponsesEndpoint endpoint, IEndpointProvider provider, ClientWebSocket webSocket)
    {
        this.endpoint = endpoint;
        this.provider = provider;
        this.webSocket = webSocket;
    }

    /// <summary>
    /// The underlying WebSocket state.
    /// </summary>
    public WebSocketState State => webSocket.State;

    /// <summary>
    /// The ID of the most recently completed response on this connection, if any.
    /// </summary>
    public string? CurrentResponseId { get; private set; }

    /// <summary>
    /// The most recently completed response on this connection, if any.
    /// </summary>
    public ResponseResult? CurrentResponse { get; private set; }

    /// <summary>
    /// Sends a <c>response.create</c> event and streams server events until the response completes or fails.
    /// Only one response may be in flight per connection at a time.
    /// </summary>
    public Task<ResponseResult?> CreateResponseAsync(
        ResponseRequest request,
        ResponseStreamEventHandler? eventsHandler = null,
        ResponseWebSocketCreateOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return CreateResponseInternalAsync(request, eventsHandler, options, false, cancellationToken);
    }

    /// <summary>
    /// Sends a <c>response.create</c> event and streams server events. HTTP-level exceptions are routed to
    /// <see cref="ResponseStreamEventHandler.OnException"/> when set instead of being thrown.
    /// </summary>
    public Task<ResponseResult?> CreateResponseSafeAsync(
        ResponseRequest request,
        ResponseStreamEventHandler? eventsHandler = null,
        ResponseWebSocketCreateOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return CreateResponseInternalAsync(request, eventsHandler, options, true, cancellationToken);
    }

    private async Task<ResponseResult?> CreateResponseInternalAsync(
        ResponseRequest request,
        ResponseStreamEventHandler? eventsHandler,
        ResponseWebSocketCreateOptions? options,
        bool isSafe,
        CancellationToken cancellationToken)
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ResponsesWebSocketConnection));
        }

        if (webSocket.State != WebSocketState.Open)
        {
            InvalidOperationException ex = new InvalidOperationException("WebSocket is not open.");
            if (isSafe && eventsHandler?.OnException is not null)
            {
                await eventsHandler.OnException(new TornadoStreamRequest { Exception = ex }).ConfigureAwait(false);
                return null;
            }

            throw ex;
        }

        if (string.IsNullOrEmpty(request.PreviousResponseId) && CurrentResponseId is not null)
        {
            request.PreviousResponseId ??= CurrentResponseId;
        }

        string payload = BuildCreatePayload(request, provider, options);

        try
        {
            await SendTextAsync(payload, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (isSafe && eventsHandler?.OnException is not null)
        {
            await eventsHandler.OnException(new TornadoStreamRequest { Exception = ex }).ConfigureAwait(false);
            return null;
        }

        ResponsesSession session = new ResponsesSession
        {
            Endpoint = endpoint,
            EventsHandler = eventsHandler,
            Request = request
        };

        ResponseResult? result = null;

        while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            string? message = await ReceiveTextMessageAsync(cancellationToken).ConfigureAwait(false);
            if (message is null)
            {
                break;
            }

            string eventType = ResponsesEndpoint.ResolveResponseEventType(null, message);
            bool terminal = await endpoint.DispatchResponseStreamEventAsync(
                eventType,
                message,
                session,
                eventsHandler,
                cancellationToken).ConfigureAwait(false);

            if (session.CurrentResponse is not null)
            {
                CurrentResponse = session.CurrentResponse;
                CurrentResponseId = session.CurrentResponse.Id;
            }

            if (terminal)
            {
                result = session.CurrentResponse;
                break;
            }
        }

        if (result is not null)
        {
            await ResponsesEndpoint.HandleResponse(request, result).ConfigureAwait(false);
        }

        return result;
    }

    /// <summary>
    /// Closes the WebSocket connection gracefully.
    /// </summary>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        try
        {
            await CloseAsync().ConfigureAwait(false);
        }
        catch
        {
            // ignore close errors during dispose
        }

        webSocket.Dispose();
        sendLock.Dispose();
    }

    internal static string BuildWebSocketUrl(IEndpointProvider provider, string? urlOverride)
    {
        if (!string.IsNullOrWhiteSpace(urlOverride))
        {
            return urlOverride;
        }

        string httpUrl = provider.ApiUrl(CapabilityEndpoints.Responses, null);
        string wsUrl = httpUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? "wss://" + httpUrl.Substring(8)
            : httpUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                ? "ws://" + httpUrl.Substring(7)
                : httpUrl;
        return wsUrl;
    }

    internal static void ApplyAuthHeaders(ClientWebSocket webSocket, IEndpointProvider provider)
    {
        ProviderAuthentication? auth = provider.Api?.GetProvider(provider.Provider).Auth;

        if (auth?.ApiKey is not null)
        {
            webSocket.Options.SetRequestHeader("Authorization", $"Bearer {auth.ApiKey.Trim()}");
        }

        if (auth?.Organization is not null)
        {
            webSocket.Options.SetRequestHeader("OpenAI-Organization", auth.Organization.Trim());
        }

        webSocket.Options.SetRequestHeader("User-Agent", EndpointBase.ResolveUserAgent(provider.Api));
    }

    internal static string BuildCreatePayload(ResponseRequest request, IEndpointProvider provider, ResponseWebSocketCreateOptions? options)
    {
        TornadoRequestContent serialized = request.Serialize(provider);
        string bodyJson = serialized.Body as string ?? JsonConvert.SerializeObject(serialized.Body, EndpointBase.NullSettings);
        JObject jo = JObject.Parse(bodyJson);

        jo.Remove("stream");
        jo.Remove("background");
        jo.Remove("stream_options");
        jo["type"] = "response.create";

        if (options?.Generate is not null)
        {
            jo["generate"] = options.Generate.Value;
        }

        return jo.ToString(Formatting.None);
    }


    private async Task SendTextAsync(string text, CancellationToken cancellationToken)
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ResponsesWebSocketConnection));
        }

        byte[] bytes = Encoding.UTF8.GetBytes(text);
        await sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            sendLock.Release();
        }
    }

    private async Task<string?> ReceiveTextMessageAsync(CancellationToken cancellationToken)
    {
        using MemoryStream ms = new MemoryStream();

        while (webSocket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await webSocket
                .ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken)
                .ConfigureAwait(false);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            ms.Write(receiveBuffer, 0, result.Count);

            if (result.EndOfMessage)
            {
                break;
            }
        }

        if (ms.Length == 0)
        {
            return null;
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }
}
