using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.VectorDatabases;

/// <summary>
/// Class to retrieve a Vector Collection
/// </summary>
public class VectorCollection
{
    /// <summary>
    /// Name of the vector collection
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Embedding vector dimension
    /// </summary>
    public int? VectorDimension { get; set; }

    /// <summary>
    /// Embedding Metadata (Good to included embedding model info)
    /// </summary>
    public Dictionary<string, string>? MetadataSchema { get; set; }

    /// <summary>
    /// Vector Collection Class for retrieval of vector documents
    /// </summary>
    /// <param name="name">Name of the Collection</param>
    /// <param name="vectorDimension">Dimension of the vectors</param>
    /// <param name="metadataSchema">Metadata schema for the collection</param>
    public VectorCollection(string name, int? vectorDimension = null, Dictionary<string, string>? metadataSchema = null)
    {
        Name = name;
        VectorDimension = vectorDimension;
        MetadataSchema = metadataSchema ?? new Dictionary<string, string>();
    }
}
