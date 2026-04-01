using RagBasics.EmbeddingGenerator;
using RagBasics.Models;
using RagBasics.Repository;
using RagBasics.Services;

namespace RagBasics
{
    /// <summary>
    /// Entry point for the RAG (Retrieval-Augmented Generation) API.
    /// This application exposes endpoints to store text with vector embeddings
    /// and query them using semantic similarity via pgvector + Ollama LLM.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;

            // Retrieve the Neon PostgreSQL (pgvector-enabled) connection string from appsettings.json
            var connectionString = configuration.GetConnectionString("PostgreSQL");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Required configuration settings are missing.");
            }

            // --- Dependency Injection Setup ---

            // Register the Ollama-based embedding generator.
            // This service converts text into float[] vector embeddings using the Ollama /api/embeddings endpoint.
            builder.Services.AddSingleton<IEmbeddingGenerator>(sp =>
                new OllamaEmbeddingGenerator(new Uri("http://127.0.0.1:11434"), "mistral"));

            // Register the repository that stores and retrieves text + embeddings from PostgreSQL/Neon (pgvector).
            builder.Services.AddSingleton(sp =>
                new TextRepository(connectionString, sp.GetRequiredService<IEmbeddingGenerator>()));

            // Register the RAG service that orchestrates retrieval + LLM generation.
            // It fetches relevant context from the DB, then sends it along with the user query to Ollama for completion.
            builder.Services.AddSingleton(sp =>
                new RagService(sp.GetRequiredService<TextRepository>(), new Uri("http://127.0.0.1:11434"), "mistral"));

            var app = builder.Build();

            // --- Minimal API Endpoints ---

            // POST /add-text — Accepts a JSON body with a "content" field.
            // Generates an embedding for the text and stores both in the PostgreSQL text_contexts table.
            app.MapPost("/add-text", async (TextRepository textRepository, HttpContext context) =>
            {
                var request = await context.Request.ReadFromJsonAsync<AddTextRequest>();
                if (string.IsNullOrWhiteSpace(request?.Content))
                {
                    return Results.BadRequest("Content is required.");
                }

                await textRepository.StoreTextAsync(request.Content);

                return Results.Ok("Text added successfully.");
            });

            // GET /ask?query=... — Performs a RAG query:
            // 1. Converts the query to an embedding
            // 2. Finds semantically similar texts in pgvector using cosine distance (<->)
            // 3. Sends the retrieved context + query to Ollama LLM for a grounded answer
            app.MapGet("/ask", async (RagService ragService, string query) =>
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return Results.BadRequest("Query parameter is required.");
                }

                var response = await ragService.GetAnswerAsync(query);

                return Results.Ok(new { query, response });
            });

            app.Run();
        }
    }
}