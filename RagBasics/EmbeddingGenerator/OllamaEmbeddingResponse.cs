using System.Text.Json.Serialization;

namespace RagBasics.EmbeddingGenerator;

/// <summary>
/// Deserialization target for the Ollama /api/embeddings JSON response.
/// Contains the vector embedding as a float array.
/// </summary>
public class OllamaEmbeddingResponse
{
    /// <summary>The vector embedding returned by Ollama (e.g., 4096 floats for Mistral).</summary>
    [JsonPropertyName("embedding")]
    public float[] Embedding { get; set; } = [];
}