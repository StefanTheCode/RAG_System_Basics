using System.Text;
using System.Text.Json;

namespace RagBasics.EmbeddingGenerator;

public class OllamaEmbeddingGenerator(Uri ollamaUrl, string modelId = "mistral") : IEmbeddingGenerator
{
    private readonly HttpClient _httpClient = new();
    private readonly Uri _ollamaUrl = ollamaUrl;
    private readonly string _modelId = modelId;

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var requestBody = new { model = _modelId, prompt = text };

        var response = await _httpClient.PostAsync(
            new Uri(_ollamaUrl, "/api/embeddings"),
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Ollama API error: {await response.Content.ReadAsStringAsync()}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        Console.WriteLine("Ollama Response: " + responseJson);

        var serializationOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        var embeddingResponse = JsonSerializer.Deserialize<OllamaEmbeddingResponse>(responseJson, serializationOptions);

        if (embeddingResponse?.Embedding == null || embeddingResponse.Embedding.Length == 0)
        {
            throw new Exception("Failed to generate embedding.");
        }

        return embeddingResponse.Embedding;
    }
}