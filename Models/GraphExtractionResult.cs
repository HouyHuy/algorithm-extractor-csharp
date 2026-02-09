namespace DACN_Algorithms;

public sealed class GraphExtractionResult
{
    public string? algorithm { get; set; }
    public string? problem_group { get; set; }
    public string direction { get; set; } = "undirected";
    public bool weighted { get; set; } = false;
    public List<string> vertices { get; set; } = new();
    public List<Edge> edges { get; set; } = new();
    public string? start { get; set; }
    public string? end { get; set; }
}
