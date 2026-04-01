using System.Text;
using System.Text.Json;

namespace RagBasics.EmbeddingGenerator;

/// <summary>
/// Generates vector embeddings by calling the Ollama REST API (/api/embeddings).
/// Ollama runs locally and serves models like Mistral for embedding generation.
/// The resulting float[] is stored alongside text in pgvector for semantic search.
/// </summary>
public class OllamaEmbeddingGenerator(Uri ollamaUrl, string modelId = "mistral") : IEmbeddingGenerator
{
    private readonly HttpClient _httpClient = new();
    private readonly Uri _ollamaUrl = ollamaUrl;
    private readonly string _modelId = modelId;

    /// <summary>
    /// Sends text to Ollama's embedding endpoint and returns the resulting vector.
    /// </summary>
    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        // Build the request payload with the model name and the text to embed
        var requestBody = new { model = _modelId, prompt = text };

        // POST to Ollama's /api/embeddings endpoint
        var response = await _httpClient.PostAsync(
            new Uri(_ollamaUrl, "/api/embeddings"),
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Ollama API error: {await response.Content.ReadAsStringAsync()}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        Console.WriteLine("Ollama Response: " + responseJson);

        // Deserialize the JSON response into the embedding float array
        var serializationOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var embeddingResponse = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(responseJson, serializationOptions);

        if (embeddingResponse?.Embedding == null || embeddingResponse.Embedding.Length == 0)
        {
            throw new Exception("Failed to generate embedding.");
        }

        return embeddingResponse.Embedding;
    }
}