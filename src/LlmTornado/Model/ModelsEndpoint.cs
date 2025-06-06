using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Models.Vendors;
using Newtonsoft.Json;

namespace LlmTornado.Models;

/// <summary>
///     The API endpoint for querying available models
/// </summary>
public class ModelsEndpoint : EndpointBase
{
	/// <summary>
	///     Constructor of the api endpoint.  Rather than instantiating this yourself, access it through an instance of
	///     <see cref="TornadoApi" /> as <see cref="TornadoApi.Models" />.
	/// </summary>
	/// <param name="api"></param>
	internal ModelsEndpoint(TornadoApi api) : base(api)
    {
    }

	/// <summary>
	///     The name of the endpoint, which is the final path segment in the API URL.  For example, "models".
	/// </summary>
	protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Models;

	/// <summary>
	///     Get details about a particular Model from the API, specifically properties such as <see cref="Model.OwnedBy" /> and
	///     permissions.
	/// </summary>
	/// <param name="id">The id/name of the model to get more details about</param>
	/// <returns>Asynchronously returns the <see cref="Model" /> with all available properties</returns>
	public async Task<Model> GetModelDetails(string? id)
    {
        string resultAsString = await HttpGetContent(Api.GetProvider(LLmProviders.OpenAi), Endpoint, $"/{id}");
        Model? model = JsonConvert.DeserializeObject<Model>(resultAsString);
        return model;
    }

	/// <summary>
	///     List all models of a given Provider.
	/// </summary>
	/// <returns>Asynchronously returns the list of all <see cref="Model" />s</returns>
	public async Task<List<RetrievedModel>?> GetModels(LLmProviders provider = LLmProviders.OpenAi)
	{
		Dictionary<string, object>? queryPars = provider switch
		{
			LLmProviders.Google => new Dictionary<string, object> { { "pageSize", 1000 } },
			LLmProviders.Cohere => new Dictionary<string, object> { { "page_size", 1000 } },
			_ => null
		};

		return (await HttpGet<RetrievedModelsResult>(Api.GetProvider(provider), Endpoint, queryParams: queryPars))?.Data;
	}
}