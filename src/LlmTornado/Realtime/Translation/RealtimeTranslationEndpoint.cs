using System;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;

namespace LlmTornado.Realtime.Translation;

/// <summary>
/// OpenAI Realtime translation API (<c>/v1/realtime/translations</c>).
/// </summary>
public class RealtimeTranslationEndpoint
{
    private readonly TornadoApi api;

    internal RealtimeTranslationEndpoint(TornadoApi api)
    {
        this.api = api;
    }

    /// <summary>
    /// Opens a WebSocket session to <c>wss://api.openai.com/v1/realtime/translations</c>.
    /// </summary>
    public Task<RealtimeTranslationSession> ConnectAsync(
        RealtimeTranslationConnectOptions options,
        CancellationToken cancellationToken = default)
    {
#if MODERN
        return RealtimeTranslationSession.ConnectAsync(api, options, cancellationToken);
#else
        throw new NotSupportedException("Realtime translation WebSocket sessions require net8.0 or later.");
#endif
    }
}
