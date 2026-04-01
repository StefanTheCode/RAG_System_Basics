using RagBasics.Models;
using System.Text.Json;
using System.Text;
using RagBasics.Repository;

namespace RagBasics.Services;

/// <summary>
/// Core RAG (Retrieval-Augmented Generation) service.
/// Orchestrates the two-phase RAG pipeline:
///   1. Retrieval — finds semantically similar text chunks from pgvector (via TextRepository)
///   2. Generation — sends retrieved context + user query to Ollama LLM for a grounded answer
/// </summary>
public class RagService(TextRepository retriever, Uri ollamaUrl, string modelId = "mistral")
{
    private readonly TextRepository _textRepository = retriever;
    private readonly HttpClient _httpClient = new();
    private readonly Uri _ollamaUrl = ollamaUrl;
    private readonly string _modelId = modelId;

    /// <summary>
    /// Executes the full RAG pipeline for the given user query:
    /// 1. Retrieves relevant text chunks from pgvector via semantic similarity
    /// 2. Combines the chunks into a single context string
    /// 3. Sends the context + query to Ollama's /api/generate for a grounded LLM response
    /// </summary>
    public async Task<object> GetAnswerAsync(string query)
    {
        // --- Phase 1: RETRIEVAL ---
        // Query pgvector for the top semantically similar text chunks
        List<string> contexts = await _textRepository.RetrieveRelevantText(query);

        // Combine multiple retrieved chunks into one context block, separated by dividers
        string combinedContext = string.Join("\n\n---\n\n", contexts);

        // Early return if no relevant text was found in the database
        if (contexts.Count == 1 && contexts[0] == "No relevant context found.")
        {
            return new
            {
                Context = "No relevant data found in the database.",
                Response = "I don't know."
            };
        }

        // --- Phase 2: GENERATION ---
        // Build the prompt with strict instructions to only use the provided context
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
            stream = false // Request a single complete response (not streamed tokens)
        };

        // Send the augmented prompt to Ollama's text generation endpoint
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

        // Deserialize the Ollama response and extract the generated text
        var responseJson = await response.Content.ReadAsStringAsync();
        var serializationOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var completionResponse = JsonSerializer.Deserialize<OllamaCompletionResponse>(responseJson, serializationOptions);

        // Return both the retrieved context and the LLM's grounded answer
        return new
        {
            Context = combinedContext,
            Response = completionResponse?.Response ?? "I don't know. No relevant data found."
        };
    }
}