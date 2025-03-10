namespace RagBasics.EmbeddingGenerator;

public interface IEmbeddingGenerator
{
    Task<float[]> GenerateEmbeddingAsync(string text);
}