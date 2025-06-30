using System;
using System.Threading.Tasks;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Responses;

/// <summary>
///     This endpoint classifies text against the OpenAI Content Policy
/// </summary>
public class ResponsesEndpoint : EndpointBase
{
    internal ResponsesEndpoint(TornadoApi api) : base(api)
    {
    }

    /// <summary>
    ///     The name of the endpoint, which is the final path segment in the API URL.  For example, "completions".
    /// </summary>
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Responses;

    /// <summary>
    /// Creates a responses API request.
    /// </summary>
    /// <param name="request">The request</param>
    public async Task<ResponseResult?> CreateResponse(ResponseRequest request)
    {
        IEndpointProvider provider = Api.GetProvider(request.Model ?? ChatModel.OpenAi.Gpt35.Turbo);
        TornadoRequestContent requestBody = request.Serialize(provider);
        
        HttpCallResult<ResponseResult> result = await HttpPost<ResponseResult>(provider, Endpoint, url: requestBody.Url, postData: requestBody.Body, model: request.Model, ct: request.CancellationToken).ConfigureAwait(false);
        
        if (result.Exception is not null)
        {
            throw result.Exception;
        }

        return result.Data;
    }
}