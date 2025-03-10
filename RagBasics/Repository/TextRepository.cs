using System.Globalization;
using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;
using Npgsql;
using RagBasics.EmbeddingGenerator;

namespace RagBasics.Repository;

public class TextRepository(string connectionString, IEmbeddingGenerator embeddingGenerator)
{
    private readonly string _connectionString = connectionString;
    private readonly IEmbeddingGenerator _embeddingGenerator = embeddingGenerator;

    public async Task StoreTextAsync(string content)
    {
        var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(content);

        using var conn = new NpgsqlConnection(_connectionString);

        await conn.OpenAsync();

        string query = "INSERT INTO text_contexts (content, embedding) VALUES (@content, @embedding)";
        using var cmd = new NpgsqlCommand(query, conn);
        cmd.Parameters.AddWithValue("content", content);
        cmd.Parameters.AddWithValue("embedding", embedding);

        await cmd.ExecuteNonQueryAsync();
    }
    public async Task<List<string>> RetrieveRelevantText(string query)
    {
        var queryEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(query);

        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        string querySql = @"
    SELECT content
    FROM text_contexts
    WHERE embedding <-> CAST(@queryEmbedding AS vector) > 0.7
    ORDER BY embedding <-> CAST(@queryEmbedding AS vector)
    LIMIT 5";

        using var cmd = new NpgsqlCommand(querySql, conn);

        string embeddingString = $"[{string.Join(",", queryEmbedding.Select(v => v.ToString("G", CultureInfo.InvariantCulture)))}]";
        cmd.Parameters.AddWithValue("queryEmbedding", embeddingString);

        using var reader = await cmd.ExecuteReaderAsync();

        List<string> results = new();
        while (await reader.ReadAsync())
        {
            results.Add(reader.GetString(0)); // Read "content" column
        }

        return results.Any() ? results : new List<string> { "No relevant context found." };
    }

}