using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;
using Microsoft.Extensions.AI;

namespace LlmTornado.Microsoft.Extensions.AI;

/// <summary>
/// Provides an <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> implementation for LlmTornado.
/// </summary>
public sealed class TornadoEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private static readonly ActivitySource ActivitySource = new ActivitySource("LlmTornado.Microsoft.Extensions.AI.Embeddings");

    private readonly TornadoApi _api;
    private readonly EmbeddingModel _defaultModel;
    private readonly int? _defaultDimensions;

    /// <summary>
    /// Initializes a new instance of <see cref="TornadoEmbeddingGenerator"/>.
    /// </summary>
    /// <param name="api">The LlmTornado API instance.</param>
    /// <param name="defaultModel">The default model to use for embedding operations.</param>
    /// <param name="defaultDimensions">Optional default dimensions for embeddings.</param>
    public TornadoEmbeddingGenerator(TornadoApi api, EmbeddingModel defaultModel, int? defaultDimensions = null)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _defaultModel = defaultModel ?? throw new ArgumentNullException(nameof(defaultModel));
        _defaultDimensions = defaultDimensions;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="TornadoEmbeddingGenerator"/>.
    /// </summary>
    /// <param name="api">The LlmTornado API instance.</param>
    /// <param name="defaultModel">The default model string to use for embedding operations.</param>
    /// <param name="defaultDimensions">Optional default dimensions for embeddings.</param>
    public TornadoEmbeddingGenerator(TornadoApi api, string defaultModel, int? defaultDimensions = null)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _defaultModel = defaultModel ?? throw new ArgumentNullException(nameof(defaultModel));
        _defaultDimensions = defaultDimensions;
    }

    /// <inheritdoc />
    public EmbeddingGeneratorMetadata Metadata => new EmbeddingGeneratorMetadata(providerName: "LlmTornado", providerUri: new Uri("https://github.com/lofcz/LlmTornado"), defaultModelId: _defaultModel.ToString(), defaultModelDimensions: _defaultDimensions);

    /// <inheritdoc />
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        List<string> valuesList = values.ToList();
        
        using Activity? activity = ActivitySource.StartActivity("GenerateAsync");

        activity?.SetTag("llm.model", _defaultModel.ToString());
        activity?.SetTag("llm.embedding.input.count", valuesList.Count);

        try
        {
            // Determine dimensions
            int? dimensions = _defaultDimensions;
            if (options?.Dimensions.HasValue == true)
            {
                dimensions = options.Dimensions.Value;
            }

            activity?.SetTag("llm.embedding.dimensions", dimensions);

            // Create request
            EmbeddingResult? result;

            if (dimensions.HasValue)
            {
                if (valuesList.Count == 1)
                {
                    result = await _api.Embeddings.CreateEmbedding(_defaultModel, valuesList[0], dimensions.Value);
                }
                else
                {
                    EmbeddingRequest request = new EmbeddingRequest(_defaultModel, valuesList)
                    {
                        Dimensions = dimensions.Value
                    };
                    result = await _api.Embeddings.CreateEmbedding(request);
                }
            }
            else
            {
                if (valuesList.Count == 1)
                {
                    result = await _api.Embeddings.CreateEmbedding(_defaultModel, valuesList[0]);
                }
                else
                {
                    result = await _api.Embeddings.CreateEmbedding(_defaultModel, valuesList);
                }
            }

            if (result?.Data == null)
            {
                throw new InvalidOperationException("Embedding generation returned null result.");
            }

            // Convert to Microsoft.Extensions.AI format
            List<Embedding<float>> embeddings = result.Data.Select((entry, index) => 
                new Embedding<float>(entry.Embedding ?? [])
                {
                    ModelId = result.Model,
                    CreatedAt = result.Created
                }).ToList();

            GeneratedEmbeddings<Embedding<float>> generated = new GeneratedEmbeddings<Embedding<float>>(embeddings);

            // Add usage information if available
            if (result.Usage != null)
            {
                generated.Usage = new UsageDetails
                {
                    InputTokenCount = result.Usage.PromptTokens,
                    TotalTokenCount = result.Usage.TotalTokens
                };
            }

            // Set additional properties
            generated.AdditionalProperties ??= new AdditionalPropertiesDictionary();
            generated.AdditionalProperties["RawRepresentation"] = result;

            activity?.SetTag("llm.embedding.output.count", embeddings.Count);
            activity?.SetTag("llm.usage.input_tokens", generated.Usage?.InputTokenCount);
            activity?.SetTag("llm.usage.total_tokens", generated.Usage?.TotalTokenCount);

            return generated;
        }
        catch (Exception ex)
        {
            activity?.SetTag("error", true);
            activity?.SetTag("error.type", ex.GetType().FullName);
            activity?.SetTag("error.message", ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType == typeof(TornadoApi) ? _api : null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // TornadoApi doesn't implement IDisposable, so nothing to dispose
    }
}
