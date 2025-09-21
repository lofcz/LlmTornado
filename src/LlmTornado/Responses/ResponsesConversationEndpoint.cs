using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;

namespace LlmTornado.Responses
{
    /// <summary>
    /// Create and manage conversations to store and retrieve conversation state across Response API calls.
    /// </summary>
    public class ResponsesConversationEndpoint : EndpointBase
    {
        internal ResponsesConversationEndpoint(TornadoApi api) : base(api)
        {
            
        }
        
        /// <summary>
        ///     The name of the endpoint, which is the final path segment in the API URL.  For example, "conversations".
        /// </summary>
        protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.ResponsesConversation;
        
        /// <summary>
        /// Create a conversation.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<ConversationResult> CreateConversation(ResponsesConversationRequest request, CancellationToken cancellationToken = default)
        {
            IEndpointProvider provider = Api.GetProvider(LLmProviders.OpenAi);
            HttpCallResult<ConversationResult> result = await HttpPost<ConversationResult>(provider, Endpoint, postData: request, ct: cancellationToken);
            
            if (!result.Ok)
            {
                throw result.Exception;
            }
        
            return result.Data;
        }
    }
}
