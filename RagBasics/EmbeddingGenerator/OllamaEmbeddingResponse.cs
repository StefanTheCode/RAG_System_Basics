using System.Text.Json.Serialization;

namespace RagBasics.EmbeddingGenerator;

public class OllamaEmbeddingResponse
{
    [JsonPropertyName("embedding")]
    public float[] Embedding { get; set; } = [];
}