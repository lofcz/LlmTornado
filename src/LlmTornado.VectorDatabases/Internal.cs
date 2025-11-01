namespace LlmTornado.VectorDatabases;

internal class VectorDbInternal
{
    public static float[] GenerateRandomEmbedding(int dimension)
    {
        Random random = new Random();
        float[] embedding = new float[dimension];
        for (int i = 0; i < dimension; i++)
        {
            embedding[i] = (float)random.NextDouble();
        }
        return embedding;
    }
}