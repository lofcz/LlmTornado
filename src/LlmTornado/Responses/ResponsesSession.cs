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
    public ResponseRequest? Request { get; set; }
    
    /// <summary>
    /// The endpoint.
    /// </summary>
    public ResponsesEndpoint Endpoint { get; set; }
    
    /// <summary>
    /// Events handler.
    /// </summary>
    public ResponseStreamEventHandler? EventsHandler { get; set; }

    /// <summary>
    /// Streams next response.
    /// </summary>
    public async Task StreamResponseRich(ResponseRequest? request = null, ResponseStreamEventHandler? eventHandler = null, CancellationToken token = default)
    {
        ResponseRequest requestToUse = request ?? Request;

        if (request is not null)
        {
            // todo: go through all fields and if the field is null, set the value from the stored Request
        }
        
        if (CurrentResponse is not null && requestToUse is not null)
        {
            requestToUse.PreviousResponseId ??= CurrentResponse.Id;
        }
        
        await Endpoint.StreamResponseRichInternal(requestToUse, this, eventHandler ?? EventsHandler, token);
    }
}