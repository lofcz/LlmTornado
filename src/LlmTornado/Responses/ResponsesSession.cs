using System.Threading;
using System.Threading.Tasks;

namespace LlmTornado.Responses;

/// <summary>
/// Session for responses. Similar to Conversation for Chat.
/// </summary>
public class ResponsesSession
{
    /// <summary>
    /// The current request.
    /// </summary>
    public ResponseRequest Request { get; set; }
    
    /// <summary>
    /// The endpoint.
    /// </summary>
    public ResponsesEndpoint Endpoint { get; set; }
    
    /// <summary>
    /// Events handler.
    /// </summary>
    public ResponseStreamEventHandler EventsHandler { get; set; }

    /// <summary>
    /// Streams next response.
    /// </summary>
    public async Task StreamNext(CancellationToken token = default)
    {
        await Endpoint.StreamResponseRich(Request, EventsHandler, token);
    }
}