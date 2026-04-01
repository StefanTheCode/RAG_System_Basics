namespace RagBasics.Models;

/// <summary>
/// Request body for the POST /add-text endpoint.
/// Contains the raw text content that will be embedded and stored in the vector database.
/// </summary>
public class AddTextRequest
{
    /// <summary>The text content to store. An embedding will be generated for this text.</summary>
    public string Content { get; set; } = string.Empty;
}