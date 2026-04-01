namespace RagBasics.Repository;

/// <summary>
/// Represents a row in the text_contexts table in PostgreSQL.
/// Each record holds the original text content and its corresponding vector embedding
/// (stored as a pgvector column for semantic similarity search).
/// </summary>
public class TextContext
{
    /// <summary>Primary key (auto-incremented).</summary>
    public int Id { get; set; }

    /// <summary>The original text content that was stored.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>The vector embedding generated from the content (used for pgvector similarity queries).</summary>
    public float[] Embedding { get; set; } = [];
}