using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace LlmTornado.Responses;

/// <summary>
/// Session for responses. Similar to Conversation for Chat.
/// </summary>
public class ResponsesSession
{
    /// <summary>
    /// Last response in the session.
    /// </summary>
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
    /// Whether to use HTTP-level safe APIs.
    /// </summary>
    public bool HttpSafe { get; set; }

    /// <summary>
    /// Streams next response.
    /// </summary>
    public async Task StreamResponseRich(ResponseRequest? request = null, ResponseStreamEventHandler? eventHandler = null, CancellationToken token = default)
    {
        ResponseRequest? requestToUse = request ?? Request;

        if (requestToUse is null)
        {
            return;
        }
        
        // todo: go through all fields and if the field is null, set the value from the stored Request
        
        if (CurrentResponse is not null)
        {
            requestToUse.PreviousResponseId ??= CurrentResponse.Id;
        }
        
        await Endpoint.StreamResponseRichInternal(requestToUse, this, eventHandler ?? EventsHandler, HttpSafe, token);
    }

    /// <summary>
    /// Serializes either the given request or the last response in the session (if any).
    /// </summary>
    public TornadoRequestContent? Serialize(ResponseRequest? request = null, ResponseRequestSerializeOptions? options = null)
    {
        ResponseRequest? toSerialize = request ?? Request;

        if (toSerialize is null)
        {
            return null;
        }

        bool? streamOption = null;
        
        if (options?.Stream is not null)
        {
            streamOption = toSerialize.Stream;
            toSerialize.Stream = true;   
        }
        
        IEndpointProvider provider = Endpoint.Api.GetProvider(toSerialize.Model ?? ChatModel.OpenAi.Gpt35.Turbo);
        TornadoRequestContent requestBody = toSerialize.Serialize(provider, options);
        
        if (streamOption is not null)
        {
            toSerialize.Stream = streamOption;
        }

        return requestBody;
    }
}