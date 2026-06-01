using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Live.Vendors.Google;
using Newtonsoft.Json;

namespace LlmTornado.Live;

/// <summary>
/// An active Gemini Live API WebSocket session.
/// </summary>
public sealed class LiveSession : IAsyncDisposable
{
    private readonly ClientWebSocket webSocket;
    private readonly LiveConnectOptions options;
    private readonly SemaphoreSlim sendLock = new SemaphoreSlim(1, 1);
    private readonly TaskCompletionSource setupComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool disposed;

    internal LiveSession(ClientWebSocket webSocket, LiveConnectOptions options)
    {
        this.webSocket = webSocket;
        this.options = options;
    }

    /// <summary>
    /// Whether the initial <c>setupComplete</c> message was received from the server.
    /// </summary>
    public bool IsSetupComplete => setupComplete.Task.Status == TaskStatus.RanToCompletion;

    /// <summary>
    /// Waits until the server acknowledges session setup.
    /// </summary>
    public async Task WaitForSetupCompleteAsync(CancellationToken cancellationToken = default)
    {
        if (setupComplete.Task.IsCompleted)
        {
            await setupComplete.Task.ConfigureAwait(false);
            return;
        }

        if (!cancellationToken.CanBeCanceled)
        {
            await setupComplete.Task.ConfigureAwait(false);
            return;
        }

        TaskCompletionSource<object?> cancelSignal = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using CancellationTokenRegistration registration = cancellationToken.Register(static state =>
        {
            ((TaskCompletionSource<object?>)state!).TrySetResult(null);
        }, cancelSignal);

        Task completed = await Task.WhenAny(setupComplete.Task, cancelSignal.Task).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        await setupComplete.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// Sends incremental conversation content. On Gemini 3.1 Flash Live, use only to seed initial history
    /// when <see cref="LiveHistoryConfig.InitialHistoryInClientContent"/> is enabled.
    /// </summary>
    public Task SendClientContentAsync(LiveClientContent content, CancellationToken cancellationToken = default)
    {
        VendorGoogleLiveClientEnvelope envelope = new VendorGoogleLiveClientEnvelope
        {
            ClientContent = VendorGoogleLiveMapper.ToClientContent(content)
        };

        return SendEnvelopeAsync(envelope, cancellationToken);
    }

    /// <summary>
    /// Sends realtime audio, video, or text input.
    /// </summary>
    public Task SendRealtimeInputAsync(LiveRealtimeInput input, CancellationToken cancellationToken = default)
    {
        VendorGoogleLiveClientEnvelope envelope = new VendorGoogleLiveClientEnvelope
        {
            RealtimeInput = VendorGoogleLiveMapper.ToRealtimeInput(input)
        };

        return SendEnvelopeAsync(envelope, cancellationToken);
    }

    /// <summary>
    /// Sends function call results for a server <see cref="LiveToolCall"/>.
    /// </summary>
    public Task SendToolResponseAsync(LiveToolResponse response, CancellationToken cancellationToken = default)
    {
        VendorGoogleLiveClientEnvelope envelope = new VendorGoogleLiveClientEnvelope
        {
            ToolResponse = VendorGoogleLiveMapper.ToToolResponse(response)
        };

        return SendEnvelopeAsync(envelope, cancellationToken);
    }

    /// <summary>
    /// Closes the WebSocket session gracefully.
    /// </summary>
    public async Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, string? closeMessage = null, CancellationToken cancellationToken = default)
    {
        if (webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await webSocket.CloseAsync(closeStatus, closeMessage, cancellationToken).ConfigureAwait(false);
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
            // ignored during dispose
        }

        webSocket.Dispose();
        sendLock.Dispose();
    }

    internal void StartReceiveLoop(CancellationToken cancellationToken)
    {
        _ = RunReceiveLoopAsync(cancellationToken);
    }

    internal async Task SendSetupAsync(LiveSessionConfig config, CancellationToken cancellationToken)
    {
        VendorGoogleLiveClientEnvelope setupEnvelope = new VendorGoogleLiveClientEnvelope
        {
            Setup = VendorGoogleLiveMapper.ToSetup(config)
        };

        await SendEnvelopeAsync(setupEnvelope, cancellationToken).ConfigureAwait(false);
    }

    private async Task RunReceiveLoopAsync(CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[8192];
        StringBuilder messageBuilder = new StringBuilder();

        try
        {
            while (!cancellationToken.IsCancellationRequested && webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                messageBuilder.Clear();
                WebSocketReceiveResult result;

                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);

                    if (result.MessageType is WebSocketMessageType.Close)
                    {
                        options.OnClose?.Invoke(result.CloseStatusDescription);
                        return;
                    }

                    messageBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                }
                while (!result.EndOfMessage);

                string rawJson = messageBuilder.ToString();
                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    continue;
                }

                VendorGoogleLiveServerEnvelope? envelope = JsonConvert.DeserializeObject<VendorGoogleLiveServerEnvelope>(rawJson, VendorGoogleLiveJson.Settings);
                if (envelope is null)
                {
                    continue;
                }

                LiveServerMessage message = VendorGoogleLiveMapper.ToServerMessage(rawJson, envelope);
                options.OnMessage?.Invoke(message);

                if (message.SetupComplete)
                {
                    setupComplete.TrySetResult();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected on cancellation
        }
        catch (Exception ex)
        {
            options.OnError?.Invoke(ex);
            setupComplete.TrySetException(ex);
        }
    }

    private async Task SendEnvelopeAsync(VendorGoogleLiveClientEnvelope envelope, CancellationToken cancellationToken)
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(LiveSession));
        }

        if (webSocket.State is not WebSocketState.Open)
        {
            throw new InvalidOperationException("Live session WebSocket is not open.");
        }

        string json = JsonConvert.SerializeObject(envelope, VendorGoogleLiveJson.Settings);
        byte[] payload = Encoding.UTF8.GetBytes(json);

        await sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await webSocket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            sendLock.Release();
        }
    }
}
