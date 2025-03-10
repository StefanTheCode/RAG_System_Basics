using RagBasics.EmbeddingGenerator;
using RagBasics.Models;
using RagBasics.Repository;
using RagBasics.Services;

namespace RagBasics
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var configuration = builder.Configuration;

            // Load settings from appsettings.json
            var connectionString = configuration.GetConnectionString("PostgreSQL");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Required configuration settings are missing.");
            }

            // Register services
            builder.Services.AddSingleton<IEmbeddingGenerator>(sp =>
                new OllamaEmbeddingGenerator(new Uri("http://127.0.0.1:11434"), "mistral"));

            builder.Services.AddSingleton(sp =>
                new TextRepository(connectionString, sp.GetRequiredService<IEmbeddingGenerator>()));

            builder.Services.AddSingleton(sp =>
                new RagService(sp.GetRequiredService<TextRepository>(), new Uri("http://127.0.0.1:11434"), "mistral"));

            var app = builder.Build();

            // Minimal API endpoints
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