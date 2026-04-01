namespace RagBasics.EmbeddingGenerator;

/// <summary>
/// Abstraction for generating vector embeddings from text.
/// Embeddings are numerical representations (float arrays) that capture semantic meaning,
/// enabling similarity search via pgvector in PostgreSQL.
/// </summary>
public interface IEmbeddingGenerator
{
    /// <summary>
    /// Converts the given text into a vector embedding (float array).
    /// </summary>
    Task<float[]> GenerateEmbeddingAsync(string text);
}