using System.Text.Json;

namespace LlmTornado.VectorDatabases.Faiss;

/// <summary>
/// Client for managing FAISS collections and indexes.
/// </summary>
public class FaissClient
{
    private readonly string _indexDirectory;
    private readonly Dictionary<string, FaissCollection> _collections;
    private readonly string _metadataFile;

    public FaissClient(FaissConfigurationOptions options)
    {
        _indexDirectory = options.IndexDirectory;
        _metadataFile = Path.Combine(_indexDirectory, "collections.json");
        _collections = new Dictionary<string, FaissCollection>();
        
        // Ensure index directory exists
        if (!Directory.Exists(_indexDirectory))
        {
            Directory.CreateDirectory(_indexDirectory);
        }
        
        // Load existing collections metadata
        LoadCollectionsMetadata();
    }

    /// <summary>
    /// Initializes the FAISS client, ensuring the index directory exists.
    /// </summary>
    public Task InitializeAsync()
    {
        // Already done in constructor
        return Task.CompletedTask;
    }

    /// <summary>
    /// Lists all collections in the FAISS store.
    /// </summary>
    public Task<List<FaissCollection>> ListCollectionsAsync()
    {
        return Task.FromResult(_collections.Values.ToList());
    }

    /// <summary>
    /// Gets a specific collection by name.
    /// </summary>
    public Task<FaissCollection?> GetCollectionAsync(string name)
    {
        _collections.TryGetValue(name, out var collection);
        return Task.FromResult(collection);
    }

    /// <summary>
    /// Creates a new collection with the specified vector dimension.
    /// </summary>
    public Task<FaissCollection> CreateCollectionAsync(string name, int vectorDimension, Dictionary<string, object>? metadata = null)
    {
        if (_collections.ContainsKey(name))
        {
            throw new InvalidOperationException($"Collection '{name}' already exists.");
        }

        var collection = new FaissCollection(name, vectorDimension, metadata);
        _collections[name] = collection;
        SaveCollectionsMetadata();
        
        return Task.FromResult(collection);
    }

    /// <summary>
    /// Gets an existing collection or creates it if it doesn't exist.
    /// </summary>
    public async Task<FaissCollection> GetOrCreateCollectionAsync(string name, int vectorDimension, Dictionary<string, object>? metadata = null)
    {
        var collection = await GetCollectionAsync(name);
        if (collection != null)
        {
            return collection;
        }

        return await CreateCollectionAsync(name, vectorDimension, metadata);
    }

    /// <summary>
    /// Deletes a collection and its associated index files.
    /// </summary>
    public Task DeleteCollectionAsync(string name)
    {
        if (_collections.Remove(name))
        {
            SaveCollectionsMetadata();
            
            // Delete index file if it exists
            string indexFile = GetIndexFilePath(name);
            if (File.Exists(indexFile))
            {
                File.Delete(indexFile);
            }
            
            // Delete metadata file if it exists
            string metadataFile = GetMetadataFilePath(name);
            if (File.Exists(metadataFile))
            {
                File.Delete(metadataFile);
            }
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the file path for a collection's index.
    /// </summary>
    public string GetIndexFilePath(string collectionName)
    {
        return Path.Combine(_indexDirectory, $"{collectionName}.index");
    }

    /// <summary>
    /// Gets the file path for a collection's metadata store.
    /// </summary>
    public string GetMetadataFilePath(string collectionName)
    {
        return Path.Combine(_indexDirectory, $"{collectionName}.metadata.json");
    }

    private void LoadCollectionsMetadata()
    {
        if (File.Exists(_metadataFile))
        {
            try
            {
                string json = File.ReadAllText(_metadataFile);
                var collections = JsonSerializer.Deserialize<List<FaissCollection>>(json);
                if (collections != null)
                {
                    foreach (var collection in collections)
                    {
                        _collections[collection.Name] = collection;
                    }
                }
            }
            catch
            {
                // If metadata file is corrupted, start fresh
            }
        }
    }

    private void SaveCollectionsMetadata()
    {
        var collections = _collections.Values.ToList();
        string json = JsonSerializer.Serialize(collections, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_metadataFile, json);
    }
}
