using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Embedding.Models;
using LlmTornado.Models;

namespace LlmTornado.Embedding;

/// <summary>
///     Text embeddings measure the relatedness of text strings by generating an embedding, which is a vector
///     (list) of floating point numbers. The distance between two vectors measures their relatedness. Small distances
///     suggest high relatedness and large distances suggest low relatedness.
/// </summary>
public class EmbeddingEndpoint : EndpointBase
{
	/// <summary>
	///     Constructor of the api endpoint.  Rather than instantiating this yourself, access it through an instance of
	///     <see cref="TornadoApi" /> as <see cref="TornadoApi.Embeddings" />.
	/// </summary>
	/// <param name="api"></param>
	internal EmbeddingEndpoint(TornadoApi api) : base(api)
    {
    }

	/// <summary>
	///     The name of the endpoint, which is the final path segment in the API URL.
	/// </summary>
	protected override CapabilityEndpoints Endpoint => CapabilityEndpoints.Embeddings;

	/// <summary>
	///     Ask the API to embed text using the default embedding model <see cref="Model.AdaTextEmbedding" />
	/// </summary>
	/// <param name="model">Model to be used</param>
	/// <param name="input">Text to be embedded</param>
	/// <returns>
	///     Asynchronously returns the embedding result. Look in its <see cref="EmbeddingEntry.Embedding" /> property of
	///     <see cref="EmbeddingResult.Data" /> to find the vector of floating point numbers
	/// </returns>
	public async Task<EmbeddingResult?> CreateEmbedding(EmbeddingModel model, string input)
    {
        EmbeddingRequest req = new EmbeddingRequest(model, input);
        return await CreateEmbedding(req);
    }

	/// <summary>
	///     Ask the API to embed text using the default embedding model <see cref="Model.AdaTextEmbedding" />
	/// </summary>
	/// <param name="input">Text to be embedded</param>
	/// <returns>
	///     Asynchronously returns the embedding result. Look in its <see cref="EmbeddingEntry.Embedding" /> property of
	///     <see cref="EmbeddingResult.Data" /> to find the vector of floating point numbers
	/// </returns>
	public async Task<EmbeddingResult?> CreateEmbedding(EmbeddingModel model, IEnumerable<string> input)
    {
	    EmbeddingRequest req = new EmbeddingRequest(model, input);
        return await CreateEmbedding(req);
    }

	/// <summary>
	///     Ask the API to embed text using the default embedding model <see cref="Model.AdaTextEmbedding" />
	/// </summary>
	/// <param name="input">Text to be embedded</param>
	/// <returns>Asynchronously returns the first embedding result as an array of floats.</returns>
	public async Task<float[]> GetEmbeddings(EmbeddingModel model, string input)
    {
        EmbeddingRequest req = new EmbeddingRequest(model, input);
        EmbeddingResult? embeddingResult = await CreateEmbedding(req);
        return embeddingResult?.Data?[0]?.Embedding ?? [];
    }

	/// <summary>
	///     Ask the API to embed text using the default embedding model <see cref="Model.AdaTextEmbedding" />
	/// </summary>
	/// <param name="input">Text to be embedded</param>
	/// <returns>Asynchronously returns the first embedding result as an array of floats.</returns>
	public async Task<List<float[]>> GetEmbeddings(EmbeddingModel model, IEnumerable<string> input)
    {
	    EmbeddingRequest req = new EmbeddingRequest(model, input);
        EmbeddingResult? embeddingResult = await CreateEmbedding(req);
        return embeddingResult?.Data.Select(x => x.Embedding).ToList() ?? [];
    }

	/// <summary>
	///     Ask the API to embed text using a custom request
	/// </summary>
	/// <param name="request">Request to be sent</param>
	/// <returns>
	///     Asynchronously returns the embedding result. Look in its <see cref="EmbeddingEntry.Embedding" /> property of
	///     <see cref="EmbeddingResult.Data" /> to find the vector of floating point numbers
	/// </returns>
	public async Task<EmbeddingResult?> CreateEmbedding(EmbeddingRequest request)
    {
	    IEndpointProvider provider = Api.GetProvider(request.Model);
	    TornadoRequestContent requestBody = request.Serialize(provider);
        return await HttpPost1<EmbeddingResult>(provider, Endpoint, requestBody.Url, requestBody.Body);
    }
}