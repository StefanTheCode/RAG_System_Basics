using System.Globalization;
using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;
using Npgsql;
using RagBasics.EmbeddingGenerator;

namespace RagBasics.Repository;

/// <summary>
/// Data access layer for storing and retrieving text embeddings in PostgreSQL (Neon) using pgvector.
/// Handles embedding generation via the injected IEmbeddingGenerator, then stores/queries
/// the text_contexts table which has a pgvector "vector" column for similarity search.
/// </summary>
public class TextRepository(string connectionString, IEmbeddingGenerator embeddingGenerator)
{
    private readonly string _connectionString = connectionString;
    private readonly IEmbeddingGenerator _embeddingGenerator = embeddingGenerator;

    /// <summary>
    /// Generates an embedding for the given text content and inserts it into the text_contexts table.
    /// The embedding is stored in a pgvector column for later similarity search.
    /// </summary>
    public async Task StoreTextAsync(string content)
    {
        // Step 1: Generate a vector embedding from the text using Ollama
        var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(content);

        // Step 2: Open a connection to the Neon PostgreSQL database
        using var conn = new NpgsqlConnection(_connectionString);

        await conn.OpenAsync();

        // Step 3: Insert the text content and its embedding into the text_contexts table
        string query = "INSERT INTO text_contexts (content, embedding) VALUES (@content, @embedding)";
        using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("content", content);
        cmd.Parameters.AddWithValue("embedding", embedding);

        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Finds the top 5 most semantically similar texts to the given query.
    /// Uses pgvector's <![CDATA[<->]]> (L2 distance) operator to compute similarity
    /// and filters results with a distance threshold of 0.7.
    /// </summary>
    public async Task<List<string>> RetrieveRelevantText(string query)
    {
        // Step 1: Convert the user's query into a vector embedding
        var queryEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(query);

        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        // Step 2: Query pgvector using the <-> operator (L2/Euclidean distance)
        // - WHERE clause filters out results that are too dissimilar (distance > 0.7)
        // - ORDER BY sorts by closest distance (most similar first)
        // - LIMIT 5 returns at most 5 relevant text chunks
        string querySql = @"
    SELECT content
    FROM text_contexts
    WHERE embedding <-> CAST(@queryEmbedding AS vector) > 0.7
    ORDER BY embedding <-> CAST(@queryEmbedding AS vector)
    LIMIT 5";

        using var cmd = new NpgsqlCommand(querySql, conn);

        // Convert the float[] embedding to a pgvector-compatible string format: "[0.1,0.2,...]"
        string embeddingString = $"[{string.Join(",", queryEmbedding.Select(v => v.ToString("G", CultureInfo.InvariantCulture)))}]";
        cmd.Parameters.AddWithValue("queryEmbedding", embeddingString);

        using var reader = await cmd.ExecuteReaderAsync();

        // Step 3: Collect all matching text content from the results
        List<string> results = new();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0)); // Read "content" column
        }

        return results.Any() ? results : new List<string> { "No relevant context found." };
    }

}