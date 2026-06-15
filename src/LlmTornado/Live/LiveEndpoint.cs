using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Live.Vendors.Google;

namespace LlmTornado.Live;

/// <summary>
/// Gemini Live API endpoint for real-time voice and multimodal dialogue over WebSockets.
/// </summary>
public class LiveEndpoint
{
    /// <summary>
    /// Creates the Live endpoint.
    /// </summary>
    public LiveEndpoint(TornadoApi api)
    {
        Api = api;
    }

    /// <summary>
    /// Parent API instance.
    /// </summary>
    public TornadoApi Api { get; }

    /// <summary>
    /// Opens a Live API WebSocket session using the configured Google API key or an ephemeral access token.
    /// </summary>
    public async Task<LiveSession> ConnectAsync(LiveConnectOptions? options = null, CancellationToken cancellationToken = default)
    {
        LiveConnectOptions connectOptions = options ?? new LiveConnectOptions();
        CancellationToken token = CancellationTokenSource.CreateLinkedTokenSource(connectOptions.CancellationToken, cancellationToken).Token;

        ProviderAuthentication? auth = Api.GetProviderAuthentication(LLmProviders.Google);
        string? apiKey = connectOptions.AccessToken is null ? auth?.ApiKey : null;
        string url = VendorGoogleLiveMapper.BuildWebSocketUrl(connectOptions.ApiVersion, apiKey, connectOptions.AccessToken);

        ClientWebSocket webSocket = new ClientWebSocket();
        await webSocket.ConnectAsync(new Uri(url), token).ConfigureAwait(false);
        connectOptions.OnOpen?.Invoke();

        LiveSession session = new LiveSession(webSocket, connectOptions);
        session.StartReceiveLoop(token);
        await session.SendSetupAsync(connectOptions.Config, token).ConfigureAwait(false);

        return session;
    }
}
