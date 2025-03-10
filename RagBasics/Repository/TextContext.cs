namespace RagBasics.Repository;

public class TextContext
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public float[] Embedding { get; set; } = [];
}