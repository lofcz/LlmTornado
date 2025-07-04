using System.Threading;
using System.Threading.Tasks;

namespace LlmTornado.Responses;

/// <summary>
/// Session for responses. Similar to Conversation for Chat.
/// </summary>
public class ResponsesSession
{
    public ResponseResult? CurrentResponse { get; set; }
    
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
    public async Task StreamResponseRich(CancellationToken token = default)
    {
        if (CurrentResponse is not null)
        {
            Request.PreviousResponseId = CurrentResponse.Id;
        }
        
        await Endpoint.StreamResponseRichInternal(Request, this, EventsHandler, token);
    }
}