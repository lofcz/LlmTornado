using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Models;

namespace LlmTornado.Moderation;

/// <summary>
///     This endpoint classifies text against content policies.
/// </summary>
public class ModerationEndpoint : EndpointBase
{
	internal ModerationEndpoint(TornadoApi api) : base(api)
    {
    }

	/// <summary>
	///     The name of the endpoint, which is the final path segment in the API URL.  For example, "completions".
	/// </summary>
	protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Moderation;

	/// <summary>
	///     This allows you to send request to the recommended model without needing to specify. OpenAI recommends using the
	///     <see cref="Model.TextModerationLatest" /> model
	/// </summary>
	public ModerationRequest DefaultModerationRequestArgs { get; set; } = new ModerationRequest { Model = Model.TextModerationLatest };

	/// <summary>
	///     Ask the API to classify the text using the default model.
	/// </summary>
	/// <param name="input">Text to classify</param>
	/// <returns>Asynchronously returns the classification result</returns>
	public async Task<ModerationResult?> CallModerationAsync(string input)
    {
        ModerationRequest req = new ModerationRequest(input, DefaultModerationRequestArgs.Model);
        return await CallModerationAsync(req);
    }

	/// <summary>
	///     Ask the API to classify the text using a custom request.
	/// </summary>
	/// <param name="request">Request to send to the API</param>
	/// <returns>Asynchronously returns the classification result</returns>
	public async Task<ModerationResult?> CallModerationAsync(ModerationRequest request)
    {
        return await HttpPost1<ModerationResult>(Api.GetProvider(LLmProviders.OpenAi), Endpoint, postData: request);
    }
}