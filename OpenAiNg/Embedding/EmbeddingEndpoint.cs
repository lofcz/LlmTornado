using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OpenAiNg.Code;
using OpenAiNg.Models;

namespace OpenAiNg.Embedding;

/// <summary>
///     OpenAI’s text embeddings measure the relatedness of text strings by generating an embedding, which is a vector
///     (list) of floating point numbers. The distance between two vectors measures their relatedness. Small distances
///     suggest high relatedness and large distances suggest low relatedness.
/// </summary>
public class EmbeddingEndpoint : EndpointBase, IEmbeddingEndpoint
{
	/// <summary>
	///     Constructor of the api endpoint.  Rather than instantiating this yourself, access it through an instance of
	///     <see cref="OpenAiApi" /> as <see cref="OpenAiApi.Embeddings" />.
	/// </summary>
	/// <param name="api"></param>
	internal EmbeddingEndpoint(OpenAiApi api) : base(api)
    {
    }

	/// <summary>
	///     The name of the endpoint, which is the final path segment in the API URL.  For example, "embeddings".
	/// </summary>
	protected override string Endpoint => "embeddings";
    
	/// <summary>
	/// 
	/// </summary>
	protected override CapabilityEndpoints CapabilityEndpoint => CapabilityEndpoints.Embeddings;
	

	/// <summary>
	///     This allows you to send request to the recommended model without needing to specify. Every request uses the
	///     <see cref="Model.AdaTextEmbedding" /> model
	/// </summary>
	public EmbeddingRequest DefaultEmbeddingRequestArgs { get; set; } = new() { Model = Model.AdaTextEmbedding };

	/// <summary>
	///     Ask the API to embedd text using the default embedding model <see cref="Model.AdaTextEmbedding" />
	/// </summary>
	/// <param name="input">Text to be embedded</param>
	/// <returns>
	///     Asynchronously returns the embedding result. Look in its <see cref="Data.Embedding" /> property of
	///     <see cref="EmbeddingResult.Data" /> to find the vector of floating point numbers
	/// </returns>
	public async Task<EmbeddingResult> CreateEmbeddingAsync(string input)
    {
        EmbeddingRequest req = new(DefaultEmbeddingRequestArgs.Model, input);
        return await CreateEmbeddingAsync(req);
    }

	/// <summary>
	///     Ask the API to embedd text using the default embedding model <see cref="Model.AdaTextEmbedding" />
	/// </summary>
	/// <param name="input">Text to be embedded</param>
	/// <returns>
	///     Asynchronously returns the embedding result. Look in its <see cref="Data.Embedding" /> property of
	///     <see cref="EmbeddingResult.Data" /> to find the vector of floating point numbers
	/// </returns>
	public async Task<EmbeddingResult> CreateEmbeddingAsync(IEnumerable<string> input)
    {
        EmbeddingRequestArray req = new(DefaultEmbeddingRequestArgs.Model, input);
        return await CreateEmbeddingAsync(req);
    }

	/// <summary>
	///     Ask the API to embedd text using a custom request
	/// </summary>
	/// <param name="request">Request to be send</param>
	/// <returns>
	///     Asynchronously returns the embedding result. Look in its <see cref="Data.Embedding" /> property of
	///     <see cref="EmbeddingResult.Data" /> to find the vector of floating point numbers
	/// </returns>
	public async Task<EmbeddingResult> CreateEmbeddingAsync(EmbeddingRequest request)
    {
        return await HttpPost1<EmbeddingResult>(Api.EndpointProvider, CapabilityEndpoint, postData: request);
    }

	/// <summary>
	///     Ask the API to embedd text using the default embedding model <see cref="Model.AdaTextEmbedding" />
	/// </summary>
	/// <param name="input">Text to be embedded</param>
	/// <returns>Asynchronously returns the first embedding result as an array of floats.</returns>
	public async Task<float[]> GetEmbeddingsAsync(string input)
    {
        EmbeddingRequest req = new(DefaultEmbeddingRequestArgs.Model, input);
        EmbeddingResult? embeddingResult = await CreateEmbeddingAsync(req);
        return embeddingResult?.Data?[0]?.Embedding;
    }

	/// <summary>
	///     Ask the API to embedd text using the default embedding model <see cref="Model.AdaTextEmbedding" />
	/// </summary>
	/// <param name="input">Text to be embedded</param>
	/// <returns>Asynchronously returns the first embedding result as an array of floats.</returns>
	public async Task<List<float[]>> GetEmbeddingsAsync(IEnumerable<string> input)
    {
        EmbeddingRequestArray req = new(DefaultEmbeddingRequestArgs.Model, input);
        EmbeddingResult embeddingResult = await CreateEmbeddingAsync(req);
        return embeddingResult?.Data.Select(x => x.Embedding).ToList() ?? new List<float[]>();
    }

	/// <summary>
	///     Ask the API to embedd text using a custom request
	/// </summary>
	/// <param name="request">Request to be send</param>
	/// <returns>
	///     Asynchronously returns the embedding result. Look in its <see cref="Data.Embedding" /> property of
	///     <see cref="EmbeddingResult.Data" /> to find the vector of floating point numbers
	/// </returns>
	public async Task<EmbeddingResult> CreateEmbeddingAsync(EmbeddingRequestArray request)
    {
        return await HttpPost1<EmbeddingResult>(Api.EndpointProvider, CapabilityEndpoint, postData: request);
    }
}