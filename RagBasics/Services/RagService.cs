using RagBasics.Models;
using System.Text.Json;
using System.Text;
using RagBasics.Repository;

namespace RagBasics.Services;

public class RagService(TextRepository retriever, Uri ollamaUrl, string modelId = "mistral")
{
    private readonly TextRepository _textRepository = retriever;
    private readonly HttpClient _httpClient = new();
    private readonly Uri _ollamaUrl = ollamaUrl;
    private readonly string _modelId = modelId;

    public async Task<object> GetAnswerAsync(string query)
    {
        // Retrieve multiple relevant texts
        List<string> contexts = await _textRepository.RetrieveRelevantText(query);

        // Combine multiple contexts into one string
        string combinedContext = string.Join("\n\n---\n\n", contexts);

        // If no relevant context is found, return a strict message
        if (contexts.Count == 1 && contexts[0] == "No relevant context found.")
        {
            return new
            {
                Context = "No relevant data found in the database.",
                Response = "I don't know."
            };
        }

        var requestBody = new
        {
            model = _modelId,
            prompt = $"""
        You are a strict AI assistant. You MUST answer ONLY using the provided context. 
        If the answer is not in the context, respond with "I don't know. No relevant data found."

        Context:
        {combinedContext}

        Question: {query}
        """,
            stream = false
        };

        var response = await _httpClient.PostAsync(
            new Uri(_ollamaUrl, "/api/generate"),
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            return new
            {
                Context = combinedContext,
                Response = "Error: Unable to generate response."
            };
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var serializationOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var completionResponse = JsonSerializer.Deserialize<OllamaCompletionResponse>(responseJson, serializationOptions);

        return new
        {
            Context = combinedContext,
            Response = completionResponse?.Response ?? "I don't know. No relevant data found."
        };
    }
}