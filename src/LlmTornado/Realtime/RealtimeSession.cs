#if MODERN
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Realtime;

/// <summary>
/// Active GA OpenAI Realtime WebSocket session (net8+ / net10+).
/// </summary>
public sealed class RealtimeSession : IAsyncDisposable
{
    private readonly ClientWebSocket webSocket;
    private readonly RealtimeConnectOptions options;
    private readonly CancellationTokenSource linkedCts;
    private Task? receiveTask;

    private RealtimeSession(ClientWebSocket webSocket, RealtimeConnectOptions options, CancellationTokenSource linkedCts)
    {
        this.webSocket = webSocket;
        this.options = options;
        this.linkedCts = linkedCts;
    }

    /// <summary>
    /// Whether the socket is open.
    /// </summary>
    public bool IsOpen => webSocket.State == WebSocketState.Open;

    /// <summary>
    /// Connects to the GA Realtime API. Does not send the beta <c>OpenAI-Beta: realtime=v1</c> header.
    /// </summary>
    public static async Task<RealtimeSession> ConnectAsync(TornadoApi api, RealtimeConnectOptions options)
    {
        string? apiKey = options.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            ProviderAuthentication? auth = api.GetProviderAuthentication(LLmProviders.OpenAi);
            apiKey = auth?.ApiKey;
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key or ephemeral client secret is required for Realtime WebSocket connections.");
        }

        ClientWebSocket ws = new ClientWebSocket();
        ws.Options.SetRequestHeader("Authorization", $"Bearer {apiKey.Trim()}");

        if (!string.IsNullOrWhiteSpace(options.SafetyIdentifier))
        {
            ws.Options.SetRequestHeader("OpenAI-Safety-Identifier", options.SafetyIdentifier);
        }

        CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(options.CancellationToken);
        await ws.ConnectAsync(options.BuildWebSocketUri(), linked.Token).ConfigureAwait(false);

        RealtimeSession session = new RealtimeSession(ws, options, linked);
        options.OnOpen?.Invoke();
        session.receiveTask = session.ReceiveLoopAsync();
        return session;
    }

    /// <summary>
    /// Sends a client event JSON object (must include <c>type</c>).
    /// </summary>
    public Task SendAsync(object clientEvent, CancellationToken cancellationToken = default)
    {
        string json = JsonConvert.SerializeObject(clientEvent, EndpointBase.NullSettings);
        return SendRawAsync(json, cancellationToken);
    }

    /// <summary>
    /// Sends a <c>session.update</c> with GA session configuration.
    /// </summary>
    public Task UpdateSessionAsync(object sessionConfig, CancellationToken cancellationToken = default)
    {
        return SendAsync(new { type = "session.update", session = sessionConfig }, cancellationToken);
    }

    /// <summary>
    /// Appends base64 PCM16 audio to the input buffer.
    /// </summary>
    public Task AppendInputAudioAsync(string base64Pcm16, CancellationToken cancellationToken = default)
    {
        return SendAsync(new { type = "input_audio_buffer.append", audio = base64Pcm16 }, cancellationToken);
    }

    /// <summary>
    /// Commits the input audio buffer (voice-agent sessions).
    /// </summary>
    public Task CommitInputAudioAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync(new { type = "input_audio_buffer.commit" }, cancellationToken);
    }

    /// <summary>
    /// Creates a model response (voice-agent sessions only; not used for translation).
    /// </summary>
    public Task CreateResponseAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync(new { type = "response.create" }, cancellationToken);
    }

    /// <summary>
    /// Closes the realtime session gracefully.
    /// </summary>
    public Task CloseSessionAsync(CancellationToken cancellationToken = default)
    {
        return SendAsync(new { type = "session.close" }, cancellationToken);
    }

    /// <summary>
    /// Sends raw JSON text.
    /// </summary>
    public async Task SendRawAsync(string json, CancellationToken cancellationToken = default)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        await webSocket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
    }

    private async Task ReceiveLoopAsync()
    {
        byte[] buffer = new byte[1024 * 64];
        StringBuilder sb = new StringBuilder();

        try
        {
            while (webSocket.State == WebSocketState.Open && !linkedCts.IsCancellationRequested)
            {
                sb.Clear();
                WebSocketReceiveResult result;

                do
                {
                    result = await webSocket.ReceiveAsync(buffer, linkedCts.Token).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None).ConfigureAwait(false);
                        options.OnClose?.Invoke(result.CloseStatusDescription);
                        return;
                    }

                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                string raw = sb.ToString();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                RealtimeServerEvent? evt;
                try
                {
                    evt = JsonConvert.DeserializeObject<RealtimeServerEvent>(raw);
                    if (evt is not null)
                    {
                        evt.RawJson = raw;
                    }
                }
                catch
                {
                    evt = new RealtimeServerEvent { RawJson = raw, Type = JObject.Parse(raw)["type"]?.ToString() };
                }

                if (evt is not null)
                {
                    options.OnEvent?.Invoke(evt);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected on dispose
        }
        catch (Exception ex)
        {
            options.OnError?.Invoke(ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "dispose", CancellationToken.None).ConfigureAwait(false);
            }
        }
        catch
        {
            // ignore close errors
        }

        linkedCts.Cancel();
        if (receiveTask is not null)
        {
            try
            {
                await receiveTask.ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        }

        webSocket.Dispose();
        linkedCts.Dispose();
    }
}
#else
using System;
using System.Threading.Tasks;
using LlmTornado.Code;

namespace LlmTornado.Realtime;

/// <summary>
/// WebSocket Realtime sessions require a <c>MODERN</c> target (net8.0+). Use REST methods on <see cref="RealtimeEndpoint"/> on older frameworks.
/// </summary>
public sealed class RealtimeSession
{
    internal static Task<RealtimeSession> ConnectAsync(TornadoApi api, RealtimeConnectOptions options)
    {
        throw new NotSupportedException("Realtime WebSocket sessions require net8.0 or later. Use RealtimeEndpoint.CreateClientSecret and connect from a MODERN-target application.");
    }
}
#endif
