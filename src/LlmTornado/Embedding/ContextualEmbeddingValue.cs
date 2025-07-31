namespace LlmTornado.Embedding;

/// <summary>
/// Base class for an embedding value.
/// </summary>
public abstract class ContextualEmbeddingValue;

/// <summary>
/// Represents an embedding vector of floats.
/// </summary>
public class ContextualEmbeddingValueFloat : ContextualEmbeddingValue
{
    /// <summary>
    /// The embedding values.
    /// </summary>
    public float[] Values { get; }

    internal ContextualEmbeddingValueFloat(float[] values)
    {
        Values = values;
    }
}

/// <summary>
/// Represents an embedding vector of integers.
/// </summary>
public class ContextualEmbeddingValueInt : ContextualEmbeddingValue
{
    /// <summary>
    /// The embedding values.
    /// </summary>
    public int[] Values { get; }
    
    internal ContextualEmbeddingValueInt(int[] values)
    {
        Values = values;
    }
}

/// <summary>
/// Represents a base64 encoded embedding vector.
/// </summary>
public class ContextualEmbeddingValueString : ContextualEmbeddingValue
{
    /// <summary>
    /// The base64 encoded embedding.
    /// </summary>
    public string Base64 { get; }
    
    internal ContextualEmbeddingValueString(string base64)
    {
        Base64 = base64;
    }
}