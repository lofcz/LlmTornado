namespace LlmTornado.VectorDatabases;

public enum SimilarityMetric
{
    /// <summary>
    /// Cosine similarity - measures the cosine of the angle between vectors.
    /// Best for most text embeddings. Range: -1 to 1 (higher is more similar).
    /// </summary>
    Cosine,
    
    /// <summary>
    /// L2 / Euclidean distance - measures straight-line distance between vectors.
    /// Range: 0 to ∞ (lower is more similar).
    /// </summary>
    Euclidean,
    
    /// <summary>
    /// Dot product (sometimes called "inner product") - measures the product of vector magnitudes and cosine.
    /// Useful when vector magnitude is meaningful. Range: -∞ to ∞ (higher is more similar).
    /// </summary>
    DotProduct,
    
    /// <summary>
    /// L1 / Manhattan distance - measures the sum of absolute differences between vector components.
    /// Useful for sparse data or when differences along each dimension are equally important.
    /// Range: 0 to ∞ (lower is more similar).
    /// </summary>
    Manhattan
}