using LlmTornado.Code;

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
    protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Moderation;
}