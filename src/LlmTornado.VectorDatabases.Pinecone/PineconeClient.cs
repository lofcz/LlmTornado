using Pinecone;
using System.Linq;
using Grpc.Net.Client;
using Index = Pinecone.Index;

namespace LlmTornado.VectorDatabases.Pinecone;

/// <summary>
/// Client for managing Pinecone indexes and connections.
/// </summary>
public class PineconeVectorClient
{
    public PineconeClient Client { get; }

    private readonly PineconeConfigurationOptions options;
    private readonly Lazy<Task<ModelIndexEmbed?>> _embedConfigLazy;

    /// <summary>
    /// Gets the currently active index name.
    /// </summary>
    public string? ActiveIndexName { get; private set; }

    /// <summary>
    /// Creates a new PineconeVectorClient instance.
    /// </summary>
    /// <param name="options">Configuration options</param>
    public PineconeVectorClient(PineconeConfigurationOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        Client = new PineconeClient(options.ApiKey, new ClientOptions
        {
            
        });
        
        // Initialize lazy loader for embed configuration from index
        _embedConfigLazy = new Lazy<Task<ModelIndexEmbed?>>(LoadEmbedConfigFromIndexAsync);
    }

    /// <summary>
    /// Gets the Pinecone SDK client.
    /// </summary>
    internal PineconeClient GetClient() => Client;

    /// <summary>
    /// Lists all available indexes.
    /// </summary>
    /// <returns>Collection of index names</returns>
    public async Task<IEnumerable<string>> ListIndexesAsync()
    {
        try
        {
            IndexList indexes = await Client.ListIndexesAsync();
            return indexes.Indexes?.Select(idx => idx.Name) ?? [];
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to list Pinecone indexes. Check your API key and connection.", ex);
        }
    }

    /// <summary>
    /// Gets or creates a Pinecone index based on the configuration.
    /// </summary>
    /// <param name="indexName">Optional index name override</param>
    /// <returns>The index name</returns>
    public async Task<string> GetOrCreateIndexAsync(string? indexName = null)
    {
        string? targetIndexName = indexName ?? options.IndexName;
        
        if (string.IsNullOrEmpty(targetIndexName))
        {
            if (!options.Dimension.HasValue)
            {
                throw new InvalidOperationException("Either IndexName or Dimension must be specified in configuration.");
            }
            targetIndexName = $"llmtornado-index-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        IEnumerable<string> existingIndexes = await ListIndexesAsync();
        
        if (!existingIndexes.Contains(targetIndexName))
        {
            if (!options.Dimension.HasValue)
            {
                throw new InvalidOperationException($"Index '{targetIndexName}' does not exist and Dimension is not specified for auto-creation.");
            }

            // Create serverless index
            CreateIndexRequest createRequest = new CreateIndexRequest
            {
                Name = targetIndexName,
                Dimension = options.Dimension.Value,
                Metric = options.Metric switch
                {
                    SimilarityMetric.Euclidean => MetricType.Euclidean,
                    SimilarityMetric.DotProduct => MetricType.Dotproduct,
                    SimilarityMetric.Cosine => MetricType.Cosine,
                    _ => throw new ArgumentOutOfRangeException()
                },
                Spec = new ServerlessIndexSpec
                {
                    Serverless = new ServerlessSpec
                    {
                        Cloud = options.Cloud switch
                        {
                            PineconeCloud.Gcp => ServerlessSpecCloud.Gcp,
                            PineconeCloud.Azure => ServerlessSpecCloud.Azure,
                            _ => ServerlessSpecCloud.Aws
                        },
                        Region = options.Region
                    }
                }
            };
            
            await Client.CreateIndexAsync(createRequest);

            // Wait for index to be ready
            await WaitForIndexReadyAsync(targetIndexName);
        }

        ActiveIndexName = targetIndexName;
        return targetIndexName;
    }

    /// <summary>
    /// Waits for an index to become ready.
    /// </summary>
    /// <param name="indexName">The index name</param>
    /// <param name="maxWaitSeconds">Maximum wait time in seconds</param>
    private async Task WaitForIndexReadyAsync(string indexName, int maxWaitSeconds = 60)
    {
        DateTime startTime = DateTime.UtcNow;
        
        while ((DateTime.UtcNow - startTime).TotalSeconds < maxWaitSeconds)
        {
            try
            {
                Index index = await Client.DescribeIndexAsync(indexName);
                if (index.Status?.Ready == true)
                {
                    return;
                }
            }
            catch
            {
                // Index not ready yet
            }

            await Task.Delay(2000); // Wait 2 seconds before checking again
        }

        throw new TimeoutException($"Index '{indexName}' did not become ready within {maxWaitSeconds} seconds.");
    }

    /// <summary>
    /// Deletes an index.
    /// </summary>
    /// <param name="indexName">The index name to delete</param>
    public async Task DeleteIndexAsync(string indexName)
    {
        await Client.DeleteIndexAsync(indexName);
        
        if (ActiveIndexName == indexName)
        {
            ActiveIndexName = null;
        }
    }

    /// <summary>
    /// Gets the Pinecone client for direct operations.
    /// </summary>
    internal PineconeClient GetPineconeClient() => Client;

    /// <summary>
    /// Gets the active index name.
    /// </summary>
    public string GetActiveIndexName() => ActiveIndexName ?? throw new InvalidOperationException("No active index. Call GetOrCreateIndexAsync first.");

    /// <summary>
    /// Gets the dimension of an index.
    /// </summary>
    /// <param name="indexName">The index name</param>
    /// <returns>The vector dimension</returns>
    public async Task<int> GetIndexDimensionAsync(string indexName)
    {
        Index indexInfo = await Client.DescribeIndexAsync(indexName);
        return indexInfo.Dimension ?? options.Dimension ?? 1536;
    }

    /// <summary>
    /// Gets the embed configuration from the index.
    /// Lazily loads and caches the configuration (thread-safe).
    /// </summary>
    /// <returns>The ModelIndexEmbed configuration, or null if not configured</returns>
    public async Task<ModelIndexEmbed?> GetEmbedConfigAsync()
    {
        return await _embedConfigLazy.Value;
    }

    /// <summary>
    /// Checks if the index has integrated embedding configured.
    /// </summary>
    public async Task<bool> HasIntegratedEmbeddingAsync()
    {
        ModelIndexEmbed? embedConfig = await GetEmbedConfigAsync();
        return embedConfig != null;
    }

    /// <summary>
    /// Gets the embedding model for auto-generating embeddings.
    /// Checks configuration first, then lazily loads from index Embed property.
    /// </summary>
    /// <returns>The embedding model name, or null if not configured</returns>
    public async Task<string?> GetEmbeddingModelAsync()
    {
        // First check config - takes precedence
        if (options.EmbeddingModel != null)
            return options.EmbeddingModel;
        
        // Lazy load from index Embed property (thread-safe, cached)
        ModelIndexEmbed? embedConfig = await GetEmbedConfigAsync();
        return embedConfig?.Model;
    }

    /// <summary>
    /// Gets the record field name from the index's FieldMap.
    /// This is the field where document text should be stored for auto-embedding.
    /// </summary>
    /// <returns>The record field name (typically "text"), or null if not configured</returns>
    public async Task<string?> GetRecordFieldNameAsync()
    {
        ModelIndexEmbed? embedConfig = await GetEmbedConfigAsync();
        if (embedConfig?.FieldMap == null || embedConfig.FieldMap.Count == 0)
            return null;
            
        // The FieldMap typically contains one entry like { "text": "..." }
        // We want the key, which is the field name
        return embedConfig.FieldMap.Keys.FirstOrDefault();
    }

    /// <summary>
    /// Loads the embed configuration from the index's Embed property.
    /// Called lazily and cached for thread-safe access.
    /// </summary>
    private async Task<ModelIndexEmbed?> LoadEmbedConfigFromIndexAsync()
    {
        if (ActiveIndexName == null)
            return null;
            
        try
        {
            Index indexInfo = await Client.DescribeIndexAsync(ActiveIndexName);
            return indexInfo.Embed;
        }
        catch
        {
            // If we can't get the index info, return null
            return null;
        }
    }
}

