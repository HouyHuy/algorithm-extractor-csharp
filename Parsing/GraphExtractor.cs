using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DACN_Algorithms;

public static class GraphExtractor
{
    private static readonly List<string> VertexKeywords = NormalizeKeywords(KeywordRules.VertexKeywords);
    private static readonly List<string> EdgeKeywords = NormalizeKeywords(KeywordRules.EdgeKeywords);
    private static readonly List<string> DirectedKeywords = NormalizeKeywords(KeywordRules.DirectedKeywords);
    private static readonly List<string> UndirectedKeywords = NormalizeKeywords(KeywordRules.UndirectedKeywords);
    private static readonly List<string> WeightedKeywords = NormalizeKeywords(KeywordRules.WeightedKeywords);
    private static readonly List<string> UnweightedKeywords = NormalizeKeywords(KeywordRules.UnweightedKeywords);
    private static readonly List<string> StartKeywords = NormalizeKeywords(KeywordRules.StartKeywords);
    private static readonly List<string> EndKeywords = NormalizeKeywords(KeywordRules.EndKeywords);
    private static readonly List<string> NodeLabelKeywords = NormalizeKeywords(KeywordRules.NodeLabelKeywords);
    private static readonly List<string> BellmanKeywords = NormalizeKeywords(KeywordRules.BellmanKeywords);
    private static readonly List<string> DijkstraKeywords = NormalizeKeywords(KeywordRules.DijkstraKeywords);
    private static readonly List<string> BfsKeywords = NormalizeKeywords(KeywordRules.BfsKeywords);
    private static readonly List<string> DfsKeywords = NormalizeKeywords(KeywordRules.DfsKeywords);
    private static readonly List<string> KruskalKeywords = NormalizeKeywords(KeywordRules.KruskalKeywords);
    private static readonly List<string> PrimKeywords = NormalizeKeywords(KeywordRules.PrimKeywords);
    private static readonly List<string> EulerKeywords = NormalizeKeywords(KeywordRules.EulerKeywords);
    private static readonly List<string> HamiltonKeywords = NormalizeKeywords(KeywordRules.HamiltonKeywords);

    public static GraphExtractionResult Extract(string raw)
    {
        var processed = Preprocess(raw);

        var result = new GraphExtractionResult();

        DetectAlgorithm(processed, result);
        DetectDirection(processed, result);
        DetectWeighted(processed, result);

        ExtractVertices(processed, result);
        ExtractEdges(processed, result);

        InferVerticesFromEdges(result);
        DetectStartEnd(processed, result);

        Validate(result);

        return result;
    }

    public static string ExtractToJson(string raw)
    {
        var result = Extract(raw);
        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static string Preprocess(string input)
    {
        var nfc = input.Normalize(NormalizationForm.FormC);
        var lower = nfc.ToLowerInvariant();
        var noDiacritics = RemoveVietnameseDiacritics(lower);
        var normalizedSymbols = NormalizeEdgeSymbols(noDiacritics);
        return normalizedSymbols;
    }

    private static string NormalizeEdgeSymbols(string s)
    {
        s = s.Replace("→", "->")
             .Replace("⇒", "->")
             .Replace("—", "-")
             .Replace("–", "-");
        return s;
    }

    private static string RemoveVietnameseDiacritics(string s)
    {
        var formD = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in formD)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark &&
                uc != UnicodeCategory.SpacingCombiningMark &&
                uc != UnicodeCategory.EnclosingMark)
            {
                sb.Append(ch);
            }
        }
        var stripped = sb.ToString().Normalize(NormalizationForm.FormC);
        stripped = stripped.Replace('đ', 'd').Replace('Đ', 'D');
        return stripped;
    }

    private static List<string> NormalizeKeywords(IEnumerable<string> keywords)
    {
        return keywords
            .Select(k => RemoveVietnameseDiacritics(k.ToLowerInvariant()))
            .Select(k => Regex.Replace(k, @"\s+", " ").Trim())
            .Where(k => k.Length > 0)
            .Distinct()
            .ToList();
    }

    private static bool ContainsAny(string text, IEnumerable<string> keywords)
    {
        foreach (var k in keywords)
        {
            if (text.Contains(k)) return true;
        }
        return false;
    }

    private static string BuildAlternation(IEnumerable<string> keywords)
    {
        return string.Join("|", keywords.Select(Regex.Escape));
    }

    private static void DetectAlgorithm(string text, GraphExtractionResult result)
    {
        result.problem_group = "traversal";
        result.algorithm = "bfs";

        if (ContainsAny(text, BellmanKeywords))
        {
            result.algorithm = "bellman-ford";
            result.problem_group = "shortest_path";
            return;
        }

        if (ContainsAny(text, DijkstraKeywords))
        {
            result.algorithm = "dijkstra";
            result.problem_group = "shortest_path";
            return;
        }

        if (ContainsAny(text, KruskalKeywords))
        {
            result.algorithm = "kruskal";
            result.problem_group = "mst";
            return;
        }

        if (ContainsAny(text, PrimKeywords))
        {
            result.algorithm = "prim";
            result.problem_group = "mst";
            return;
        }

        if (ContainsAny(text, DfsKeywords))
        {
            result.algorithm = "dfs";
            result.problem_group = "traversal";
            return;
        }

        if (ContainsAny(text, BfsKeywords))
        {
            result.algorithm = "bfs";
            result.problem_group = "traversal";
            return;
        }

        if (ContainsAny(text, EulerKeywords))
        {
            result.algorithm = "euler";
            return;
        }

        if (ContainsAny(text, HamiltonKeywords))
        {
            result.algorithm = "hamilton";
            return;
        }
    }

    private static void DetectDirection(string processed, GraphExtractionResult result)
    {
        if (ContainsAny(processed, UndirectedKeywords))
        {
            result.direction = "undirected";
            return;
        }

        if (ContainsAny(processed, DirectedKeywords) || processed.Contains("->"))
        {
            result.direction = "directed";
            return;
        }

        result.direction = "undirected";
    }

    private static void DetectWeighted(string processed, GraphExtractionResult result)
    {
        if (ContainsAny(processed, UnweightedKeywords))
        {
            result.weighted = false;
            return;
        }

        if (ContainsAny(processed, WeightedKeywords))
        {
            result.weighted = true;
            return;
        }

        var hasWeightedEdgeTuple = Regex.IsMatch(processed, @"\(\s*\w+\s*,\s*\w+\s*,\s*-?\d+\s*\)");
        result.weighted = hasWeightedEdgeTuple;
    }

    private static void ExtractVertices(string text, GraphExtractionResult result)
    {
        var vertexPattern = BuildAlternation(VertexKeywords);
        var patterns = new[]
        {
            new Regex($@"\b(?:{vertexPattern})\b\s*(?:=|:)\s*\{{([^}}]+)\}}", RegexOptions.Singleline)
        };

        foreach (var rx in patterns)
        {
            var m = rx.Match(text);
            if (m.Success)
            {
                var inside = m.Groups[1].Value;
                var tokens = inside.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                   .Select(t => t.Trim())
                                   .Where(t => t.Length > 0)
                                   .Distinct()
                                   .ToList();
                result.vertices.AddRange(tokens);
                break;
            }
        }
    }

    private static void ExtractEdges(string text, GraphExtractionResult result)
    {
        var edgeBlock = FindEdgesBlock(text);
        if (edgeBlock == null)
        {
            ParseEdgesFromText(text, result);
            return;
        }
        ParseEdgesFromText(edgeBlock, result);
    }

    private static string? FindEdgesBlock(string text)
    {
        var edgePattern = BuildAlternation(EdgeKeywords);
        var rx = new Regex($@"\b(?:{edgePattern})\b\s*(?:=|:)\s*\{{(.*)\}}", RegexOptions.Singleline);
        var m = rx.Match(text);
        if (m.Success) return m.Groups[1].Value;
        return null;
    }

    private static void ParseEdgesFromText(string text, GraphExtractionResult result)
    {
        var lines = text.Split(new[] { '\n', ';', '.' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var cleanedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(cleanedLine)) continue;

            var tupleMatches = Regex.Matches(cleanedLine, @"\(\s*(\w+)\s*,\s*(\w+)(?:\s*,\s*(-?\d+))?\s*\)");
            if (tupleMatches.Count > 0)
            {
                foreach (Match m in tupleMatches)
                {
                    var u = m.Groups[1].Value;
                    var v = m.Groups[2].Value;
                    int? w = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : null;
                    result.edges.Add(new Edge { from = u, to = v, weight = w });
                }
                continue;
            }

            var arrowMatch = Regex.Match(cleanedLine, @"(\w+)\s*(?:->|=>|-)\s*(\w+)(?:\s*(\d+))?");
            if (arrowMatch.Success)
            {
                var u = arrowMatch.Groups[1].Value;
                var v = arrowMatch.Groups[2].Value;
                int? w = arrowMatch.Groups[3].Success ? int.Parse(arrowMatch.Groups[3].Value) : null;
                result.edges.Add(new Edge { from = u, to = v, weight = w });
            }
        }
    }

    private static void InferVerticesFromEdges(GraphExtractionResult result)
    {
        var set = new HashSet<string>(result.vertices);
        foreach (var e in result.edges)
        {
            if (!set.Contains(e.from))
            {
                result.vertices.Add(e.from);
                set.Add(e.from);
            }
            if (!set.Contains(e.to))
            {
                result.vertices.Add(e.to);
                set.Add(e.to);
            }
        }
    }

    private static void DetectStartEnd(string text, GraphExtractionResult result)
    {
        var startPattern = BuildAlternation(StartKeywords);
        var endPattern = BuildAlternation(EndKeywords);
        var nodeLabelPattern = BuildAlternation(NodeLabelKeywords);

        var startRegex = new Regex($@"\b(?:{startPattern})\b\s*(?:tu|tai|at|:|=)?\s*(?:(?:{nodeLabelPattern})\b\s*)?(\w+)", RegexOptions.Singleline);
        var endRegex = new Regex($@"\b(?:{endPattern})\b\s*(?:tai|den|toi|at|:|=)?\s*(?:(?:{nodeLabelPattern})\b\s*)?(\w+)", RegexOptions.Singleline);

        var startMatch = startRegex.Match(text);
        if (startMatch.Success)
        {
            result.start = startMatch.Groups[1].Value;
        }

        var endMatch = endRegex.Match(text);
        if (endMatch.Success)
        {
            result.end = endMatch.Groups[1].Value;
        }
    }

    private static void Validate(GraphExtractionResult result)
    {
        result.edges = result.edges
            .GroupBy(e => $"{e.from}|{e.to}|{(e.weight.HasValue ? e.weight.Value.ToString() : "")}")
            .Select(g => g.First())
            .ToList();

        result.vertices = result.vertices.Distinct().OrderBy(v => v).ToList();
    }
}
