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
    private static readonly List<string> KruskalHintKeywords = NormalizeKeywords(KeywordRules.KruskalHintKeywords);
    private static readonly List<string> PrimKeywords = NormalizeKeywords(KeywordRules.PrimKeywords);
    private static readonly List<string> EulerKeywords = NormalizeKeywords(KeywordRules.EulerKeywords);
    private static readonly List<string> HamiltonKeywords = NormalizeKeywords(KeywordRules.HamiltonKeywords);
    private static readonly List<string> ShortestPathKeywords = NormalizeKeywords(KeywordRules.ShortestPathKeywords);
    private static readonly List<string> MstKeywords = NormalizeKeywords(KeywordRules.MstKeywords);
    private static readonly List<string> BellmanHintKeywords = NormalizeKeywords(KeywordRules.BellmanHintKeywords);

    private static readonly HashSet<string> VertexBlacklist = new HashSet<string>
    (
        new[]
        {
            "va","voi","noi","ket","den","tu","co","canh","dinh","do","thi","trong","so",
            "chi","phi","duong","di","thoi","gian","tre","lai","lo","cua","phan","buoc",
            "giua","la","gom","cac","tat","ca","mot","hai","ba","bon","nam","sau","bay","tam","chin","muoi",
            "hay","truoc","floyd","warshall","bfs","dfs","dijkstra","bellman","ford","prim","kruskal","euler","hamilton",
            "khoang","cach","ngan","nhat","mst","g","v","e"
        },
        StringComparer.OrdinalIgnoreCase
    );

    public static GraphExtractionResult Extract(string raw)
    {
        var processed = Preprocess(raw);
        var rawLower = raw.Normalize(NormalizationForm.FormC).ToLowerInvariant();

        var result = new GraphExtractionResult();

        DetectAlgorithm(processed, result);
        DetectDirection(processed, rawLower, result);
        DetectWeighted(processed, result);

        ExtractVertices(processed, result);
        ExtractEdges(processed, result);

        if (result.edges.Any(e => e.weight.HasValue))
        {
            result.weighted = true;
        }

        InferVerticesFromEdges(result);
        DetectStartEnd(processed, rawLower, result);

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
        var normalizedNewlines = normalizedSymbols.Replace("\r\n", "\n").Replace("\r", "\n");
        var normalizedSpaces = Regex.Replace(normalizedNewlines, @"[ \t]+", " ");
        normalizedSpaces = Regex.Replace(normalizedSpaces, @"\n+", "\n").Trim();
        return normalizedSpaces;
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

    private static int ScoreKeywords(string text, IEnumerable<string> keywords)
    {
        var score = 0;
        foreach (var k in keywords)
        {
            if (text.Contains(k)) score++;
        }
        return score;
    }

    private static string BuildAlternation(IEnumerable<string> keywords)
    {
        return string.Join("|", keywords.Select(Regex.Escape));
    }

    private static bool IsVertexToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return false;
        if (VertexBlacklist.Contains(token)) return false;
        return Regex.IsMatch(token, @"^[a-zA-Z][a-zA-Z0-9]*$|^\d+$");
    }

    private static bool ContainsIgnoreDiacritics(string text, string value)
    {
        return CultureInfo.GetCultureInfo("vi-VN").CompareInfo.IndexOf(text, value, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase) >= 0;
    }

    private static List<string> ExtractVertexTokens(string raw)
    {
        var matches = Regex.Matches(raw, @"\b[a-zA-Z][a-zA-Z0-9]*\b|\b\d+\b");
        var tokens = matches.Select(m => m.Value)
            .Where(IsVertexToken)
            .Distinct()
            .ToList();
        return tokens;
    }

    private static string? FindFirstVertexAfter(string text, string token, IEnumerable<string> vertices)
    {
        var bestIndex = int.MaxValue;
        string? best = null;
        foreach (var v in vertices)
        {
            if (string.IsNullOrWhiteSpace(v)) continue;
            var m = Regex.Match(text, $@"\b{Regex.Escape(token)}\s+{Regex.Escape(v)}\b");
            if (m.Success && m.Index < bestIndex)
            {
                bestIndex = m.Index;
                best = v;
            }
        }
        return best;
    }

    private static double? ParseWeight(string numberText, string context)
    {
        if (string.IsNullOrWhiteSpace(numberText)) return null;
        var numMatch = Regex.Match(numberText, @"-?\d+(?:[.,]\d+)?");
        if (!numMatch.Success) return null;
        var normalized = numMatch.Value.Replace(",", ".");
        if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
        {
            return null;
        }
        if (!string.IsNullOrWhiteSpace(context))
        {
            if (context.Contains("lai"))
            {
                val = -Math.Abs(val);
            }
            else if (context.Contains("lo"))
            {
                val = Math.Abs(val);
            }
        }
        return val;
    }

    private static void DetectAlgorithm(string text, GraphExtractionResult result)
    {
        result.problem_group = "traversal";
        result.algorithm = "bfs";

        var scores = new Dictionary<string, int>
        {
            ["bellman-ford"] = ScoreKeywords(text, BellmanKeywords) + ScoreKeywords(text, BellmanHintKeywords),
            ["dijkstra"] = ScoreKeywords(text, DijkstraKeywords) + ScoreKeywords(text, ShortestPathKeywords),
            ["bfs"] = ScoreKeywords(text, BfsKeywords),
            ["dfs"] = ScoreKeywords(text, DfsKeywords),
            ["kruskal"] = ScoreKeywords(text, KruskalKeywords) + ScoreKeywords(text, KruskalHintKeywords),
            ["prim"] = ScoreKeywords(text, PrimKeywords),
            ["euler"] = ScoreKeywords(text, EulerKeywords),
            ["hamilton"] = ScoreKeywords(text, HamiltonKeywords)
        };

        var mstScore = ScoreKeywords(text, MstKeywords);
        if (mstScore > 0)
        {
            scores["prim"] += mstScore;
            scores["kruskal"] += mstScore;
        }

        var kruskalHintScore = ScoreKeywords(text, KruskalHintKeywords);
        if (scores["kruskal"] == scores["prim"] && scores["kruskal"] > 0 && kruskalHintScore > 0)
        {
            scores["kruskal"] += kruskalHintScore;
        }

        var best = scores.OrderByDescending(x => x.Value).FirstOrDefault();
        if (best.Value <= 0) return;

        result.algorithm = best.Key;
        if (best.Key == "bellman-ford" || best.Key == "dijkstra")
        {
            result.problem_group = "shortest_path";
        }
        else if (best.Key == "kruskal" || best.Key == "prim")
        {
            result.problem_group = "mst";
        }
        else if (best.Key == "euler" || best.Key == "hamilton")
        {
            result.problem_group = "euler_hamilton";
        }
    }

    private static void DetectDirection(string processed, string rawLower, GraphExtractionResult result)
    {
        var rawNoDiacritics = RemoveVietnameseDiacritics(rawLower);
        var rawSimplified = Regex.Replace(rawNoDiacritics, @"[^a-z0-9]+", " ").Trim();
        var rawStrip = Regex.Replace(rawLower.Normalize(NormalizationForm.FormD), @"\p{M}", "");
        rawStrip = rawStrip.Replace('đ', 'd').Replace('Đ', 'D');
        var rawAscii = new string(rawLower.Normalize(NormalizationForm.FormKD).Where(c => c < 128).ToArray());

        if (ContainsAny(processed, UndirectedKeywords) || ContainsIgnoreDiacritics(rawLower, "vo huong") || ContainsIgnoreDiacritics(rawLower, "khong huong") || rawNoDiacritics.Contains("vo huong") || rawNoDiacritics.Contains("khong huong") || rawLower.Contains("vô hướng") || rawLower.Contains("vo huong") || rawSimplified.Contains("vo huong") || rawSimplified.Contains("khong huong") || rawStrip.Contains("vo huong") || rawStrip.Contains("khong huong") || rawAscii.Contains("vo huong") || rawAscii.Contains("khong huong") || Regex.IsMatch(rawLower, @"\bv\p{L}*\s+h\p{L}*ng\b") || Regex.IsMatch(rawLower, @"\bkh\p{L}*\s+h\p{L}*ng\b"))
        {
            result.direction = "undirected";
            return;
        }

        if (Regex.IsMatch(processed, @"\bco\s+huong\b") || processed.Contains("co huong") || ContainsAny(processed, DirectedKeywords) || processed.Contains("->") || ContainsIgnoreDiacritics(rawLower, "co huong") || rawNoDiacritics.Contains("co huong") || rawLower.Contains("hướng") || rawLower.Contains("huong") || rawSimplified.Contains("co huong") || rawStrip.Contains("co huong") || rawAscii.Contains("co huong") || Regex.IsMatch(rawLower, @"\bc\p{L}*\s+h\p{L}*ng\b"))
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

        var hasWeightedEdgeTuple = Regex.IsMatch(processed, @"\(\s*\w+\s*,\s*\w+\s*,\s*-?\d+(?:[.,]\d+)?\s*\)");
        result.weighted = hasWeightedEdgeTuple;
    }

    private static void ExtractVertices(string text, GraphExtractionResult result)
    {
        var vertexPattern = BuildAlternation(VertexKeywords);
        var patterns = new[]
        {
            new Regex($@"\b(?:{vertexPattern})\b\s*(?:=|:)\s*\{{([^}}]+)\}}", RegexOptions.Singleline),
            new Regex($@"\b(?:{vertexPattern})\b\s*(?:=|:)\s*\[([^\]]+)\]", RegexOptions.Singleline),
            new Regex($@"\b(?:{vertexPattern})\b\s*(?:=|:)\s*\(([^\)]+)\)", RegexOptions.Singleline)
        };

        foreach (var rx in patterns)
        {
            var m = rx.Match(text);
            if (m.Success)
            {
                var inside = m.Groups[1].Value;
                var tokens = Regex.Split(inside, @"\s*(?:,|;|\s+va\s+|\s+và\s+)\s*")
                                   .Select(t => t.Trim())
                                   .Where(t => t.Length > 0)
                                   .Distinct()
                                   .ToList();
                result.vertices.AddRange(tokens);
                break;
            }
        }

        if (result.vertices.Count == 0)
        {
            var rxInline = new Regex(@"(?:\b\d+\s+dinh\b|\bcac\s+dinh\b|\bdinh\b)\s*(?:la|gom|:)\s*([^\n\.]+)", RegexOptions.Singleline);
            var mInline = rxInline.Match(text);
            if (mInline.Success)
            {
                var inside = mInline.Groups[1].Value;
                var tokens = ExtractVertexTokens(inside);
                result.vertices.AddRange(tokens);
            }
        }
    }

    private static void ExtractEdges(string text, GraphExtractionResult result)
    {
        if (TryParseAdjacencyList(text, result) || TryParseAdjacencyMatrix(text, result))
        {
            return;
        }
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
        var rxCurly = new Regex($@"\b(?:{edgePattern})\b\s*(?:=|:)\s*\{{(.*)\}}", RegexOptions.Singleline);
        var mCurly = rxCurly.Match(text);
        if (mCurly.Success) return mCurly.Groups[1].Value;

        var rxSquare = new Regex($@"\b(?:{edgePattern})\b\s*(?:=|:)\s*\[(.*)\]", RegexOptions.Singleline);
        var mSquare = rxSquare.Match(text);
        if (mSquare.Success) return mSquare.Groups[1].Value;

        return null;
    }

    private static bool TryParseAdjacencyList(string text, GraphExtractionResult result)
    {
        var lines = text.Split('\n');
        var matches = new List<(string from, string list)>();
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;
            var match = Regex.Match(trimmed, @"^(\w+)\s*(?:->|:|\|)\s*([^;]+)$");
            if (!match.Success) continue;
            matches.Add((match.Groups[1].Value, match.Groups[2].Value));
        }
        if (matches.Count < 2) return false;

        var any = false;
        foreach (var (from, list) in matches)
        {
            var targets = ExtractVertexTokens(list);
            foreach (var t in targets)
            {
                if (t == from) continue;
                if (IsVertexToken(from) && IsVertexToken(t))
                {
                    result.edges.Add(new Edge { from = from, to = t, weight = null });
                    any = true;
                }
            }
        }
        return any;
    }

    private static bool TryParseAdjacencyMatrix(string text, GraphExtractionResult result)
    {
        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length - 1; i++)
        {
            var headerTokens = TokenizeLine(lines[i]);
            if (headerTokens.Count < 2) continue;

            var nextTokens = TokenizeLine(lines[i + 1]);
            var headerHasLabel = headerTokens.Count == nextTokens.Count;
            var header = headerHasLabel ? headerTokens.Skip(1).ToList() : headerTokens;
            if (nextTokens.Count != header.Count + 1) continue;
            if (!nextTokens.Skip(1).All(IsNumberToken)) continue;

            var any = false;
            for (int r = i + 1; r < lines.Length; r++)
            {
                var rowTokens = TokenizeLine(lines[r]);
                if (rowTokens.Count != header.Count + 1) break;
                if (!rowTokens.Skip(1).All(IsNumberToken)) break;
                var rowLabel = rowTokens[0];
                for (int c = 0; c < header.Count; c++)
                {
                    var weight = ParseWeight(rowTokens[c + 1], rowTokens[c + 1]);
                    if (weight.HasValue && Math.Abs(weight.Value) > 0)
                    {
                        var colLabel = header[c];
                        if (IsVertexToken(rowLabel) && IsVertexToken(colLabel))
                        {
                            result.edges.Add(new Edge { from = rowLabel, to = colLabel, weight = weight });
                            any = true;
                        }
                    }
                }
            }
            if (any) return true;
        }
        return false;
    }

    private static List<string> TokenizeLine(string line)
    {
        return Regex.Matches(line, @"[A-Za-z0-9]+(?:[.,]\d+)?")
            .Select(m => m.Value)
            .ToList();
    }

    private static bool IsNumberToken(string token)
    {
        return Regex.IsMatch(token, @"^-?\d+(?:[.,]\d+)?$");
    }

    private static void ParseEdgesFromText(string text, GraphExtractionResult result)
    {
        var lines = text.Split(new[] { '\n', ';', '.', '|' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var cleanedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(cleanedLine)) continue;

            var tupleMatches = Regex.Matches(cleanedLine, @"[\(\[]\s*(\w+)\s*,\s*(\w+)(?:\s*,\s*(-?\d+(?:[.,]\d+)?[a-z%]*))?\s*[\)\]]");
            foreach (Match m in tupleMatches)
            {
                var u = m.Groups[1].Value;
                var v = m.Groups[2].Value;
                var w = m.Groups[3].Success ? ParseWeight(m.Groups[3].Value, m.Value) : null;
                if (IsVertexToken(u) && IsVertexToken(v))
                {
                    result.edges.Add(new Edge { from = u, to = v, weight = w });
                }
            }

            var dashWeightMatches = Regex.Matches(cleanedLine, @"(\w+)\s*-\s*(\w+)\s*[:=]\s*(-?\d+(?:[.,]\d+)?[a-z%]*)");
            foreach (Match m in dashWeightMatches)
            {
                var u = m.Groups[1].Value;
                var v = m.Groups[2].Value;
                var w = ParseWeight(m.Groups[3].Value, m.Value);
                if (IsVertexToken(u) && IsVertexToken(v))
                {
                    result.edges.Add(new Edge { from = u, to = v, weight = w });
                }
            }

            var arrowMatches = Regex.Matches(cleanedLine, @"(\w+)\s*(?:->|=>|—|–|-)\s*(\w+)(?:\s*[:=]?\s*(-?\d+(?:[.,]\d+)?[a-z%]*))?");
            foreach (Match m in arrowMatches)
            {
                var u = m.Groups[1].Value;
                var v = m.Groups[2].Value;
                var w = m.Groups[3].Success ? ParseWeight(m.Groups[3].Value, m.Value) : null;
                if (IsVertexToken(u) && IsVertexToken(v))
                {
                    result.edges.Add(new Edge { from = u, to = v, weight = w });
                }
            }

            var pairMatches = Regex.Matches(cleanedLine, @"(\w+)\s*,\s*(\w+)(?:\s*,\s*(-?\d+(?:[.,]\d+)?[a-z%]*))?");
            var edgeContext = ContainsAny(cleanedLine, EdgeKeywords.Where(k => k.Length > 1)) || cleanedLine.Contains("{") || cleanedLine.Contains("[") || cleanedLine.Contains("(") || cleanedLine.Contains(")") || cleanedLine.Contains("noi") || cleanedLine.Contains("ket noi") || cleanedLine.Contains("giua");
            if (edgeContext)
            {
                foreach (Match m in pairMatches)
                {
                    var u = m.Groups[1].Value;
                    var v = m.Groups[2].Value;
                    var w = m.Groups[3].Success ? ParseWeight(m.Groups[3].Value, m.Value) : null;
                    if (IsVertexToken(u) && IsVertexToken(v))
                    {
                        result.edges.Add(new Edge { from = u, to = v, weight = w });
                    }
                }
            }

            var hasExplicitVertices = result.vertices.Count > 0;

            if (Regex.Matches(cleanedLine, @"\btu\b").Count >= 2)
            {
                var directedListMatches = Regex.Matches(cleanedLine, @"tu\s+(\w+)\s+(?:co\s+)?(?:duong\s+di\s+den|chuyen\s+sang|den)\s+([^;\.]+)");
                foreach (Match m in directedListMatches)
                {
                    var from = m.Groups[1].Value;
                    var list = m.Groups[2].Value;
                    var targets = ExtractVertexTokens(list);
                    foreach (var t in targets)
                    {
                        if (t == from) continue;
                        if (!IsVertexToken(from) || !IsVertexToken(t)) continue;
                        if (hasExplicitVertices && !result.vertices.Contains(t)) continue;
                        result.edges.Add(new Edge { from = from, to = t, weight = null });
                    }
                }
            }

            var undirectedListMatches = Regex.Matches(cleanedLine, @"(\w+)\s+(?:noi\s+voi|ket\s+noi\s+voi|ket\s+noi)\s+([^;\.]+)");
            foreach (Match m in undirectedListMatches)
            {
                var from = m.Groups[1].Value;
                var list = m.Groups[2].Value;
                var targets = ExtractVertexTokens(list);
                foreach (var t in targets)
                {
                    if (t == from) continue;
                    if (!IsVertexToken(from) || !IsVertexToken(t)) continue;
                    if (hasExplicitVertices && !result.vertices.Contains(t)) continue;
                    result.edges.Add(new Edge { from = from, to = t, weight = null });
                }
            }
            var betweenMatches = Regex.Matches(cleanedLine, @"giua\s+(\w+)\s+va\s+(\w+)(?:\s+(?:co\s+)?(?:trong\s+so|chi\s+phi|do\s+dai|khoang\s+cach|thoi\s+gian|do\s+tre|lai|lo|phi)\s*[:=]?\s*(-?\d+(?:[.,]\d+)?[a-z%]*))?");
            foreach (Match m in betweenMatches)
            {
                var u = m.Groups[1].Value;
                var v = m.Groups[2].Value;
                var w = m.Groups[3].Success ? ParseWeight(m.Groups[3].Value, m.Value) : null;
                if (IsVertexToken(u) && IsVertexToken(v))
                {
                    result.edges.Add(new Edge { from = u, to = v, weight = w });
                }
            }

            var connectMatches = Regex.Matches(cleanedLine, @"(\w+)\s+(?:noi\s+voi|noi|ket\s+noi\s+voi|ket\s+noi)\s+(\w+)(?:\s+(?:co\s+)?(?:trong\s+so|chi\s+phi|do\s+dai|khoang\s+cach|thoi\s+gian|do\s+tre|lai|lo|phi)\s*[:=]?\s*(-?\d+(?:[.,]\d+)?[a-z%]*))?");
            foreach (Match m in connectMatches)
            {
                var u = m.Groups[1].Value;
                var v = m.Groups[2].Value;
                var w = m.Groups[3].Success ? ParseWeight(m.Groups[3].Value, m.Value) : null;
                if (IsVertexToken(u) && IsVertexToken(v))
                {
                    result.edges.Add(new Edge { from = u, to = v, weight = w });
                }
            }

            if (Regex.Matches(cleanedLine, @"\btu\b").Count >= 2)
            {
                var toMatches = Regex.Matches(cleanedLine, @"(\w+)\s+den\s+(\w+)\s*(?:co\s+)?(?:trong\s+so|chi\s+phi|do\s+dai|khoang\s+cach|thoi\s+gian|do\s+tre|lai|lo|phi|dai)?\s*[:=]?\s*(-?\d+(?:[.,]\d+)?[a-z%]*)");
                foreach (Match m in toMatches)
                {
                    var u = m.Groups[1].Value;
                    var v = m.Groups[2].Value;
                    var w = ParseWeight(m.Groups[3].Value, m.Value);
                    if (IsVertexToken(u) && IsVertexToken(v))
                    {
                        result.edges.Add(new Edge { from = u, to = v, weight = w });
                    }
                }
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

    private static void DetectStartEnd(string text, string rawLower, GraphExtractionResult result)
    {
        var startPattern = BuildAlternation(StartKeywords);
        var endPattern = BuildAlternation(EndKeywords);
        var nodeLabelPattern = BuildAlternation(NodeLabelKeywords);

        var startFromLeading = Regex.Match(text, @"^\s*tu\s+(\w+)");
        if (startFromLeading.Success)
        {
            var candidate = startFromLeading.Groups[1].Value;
            if (IsVertexToken(candidate))
            {
                result.start = candidate;
            }
        }

        var firstLine = text.Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? text;
        var firstLineTu = Regex.Match(firstLine, @"\btu\s+(\w+)\b");
        if (firstLineTu.Success)
        {
            var candidate = firstLineTu.Groups[1].Value;
            if (IsVertexToken(candidate))
            {
                result.start = candidate;
            }
        }

        if (result.start == null)
        {
            var directFromProcessed = Regex.Match(text, @"\btu\s+(\w+)\b");
            if (directFromProcessed.Success)
            {
                var candidate = directFromProcessed.Groups[1].Value;
                if (IsVertexToken(candidate))
                {
                    result.start = candidate;
                }
            }
        }

        if (result.start == null)
        {
            var directFromRaw = Regex.Match(rawLower, @"\btừ\s+(\w+)\b");
            if (directFromRaw.Success)
            {
                var candidate = directFromRaw.Groups[1].Value;
                if (IsVertexToken(candidate))
                {
                    result.start = candidate;
                }
            }
        }

        if (result.start == null)
        {
            var tokenMatches = Regex.Matches(text, @"[a-zA-Z0-9]+")
                .Select(m => m.Value)
                .ToList();
            for (int i = 0; i < tokenMatches.Count - 1; i++)
            {
                if (!string.Equals(tokenMatches[i], "tu", StringComparison.OrdinalIgnoreCase)) continue;
                var candidate = tokenMatches[i + 1];
                if (IsVertexToken(candidate))
                {
                    result.start = candidate;
                    break;
                }
            }
        }

        if (result.start == null)
        {
            var tuTokenMatch = Regex.Match(text, @"\btu\b");
            if (tuTokenMatch.Success)
            {
                var after = text.Substring(tuTokenMatch.Index + tuTokenMatch.Length);
                var tokens = ExtractVertexTokens(after);
                if (tokens.Count > 0)
                {
                    result.start = tokens[0];
                }
            }
        }

        if (result.start == null && (result.algorithm == "bfs" || result.algorithm == "dfs" || result.problem_group == "shortest_path"))
        {
            var firstLineTokens = ExtractVertexTokens(firstLine);
            var firstLineCandidate = firstLineTokens.FirstOrDefault(t => result.vertices.Contains(t));
            if (!string.IsNullOrWhiteSpace(firstLineCandidate))
            {
                result.start = firstLineCandidate;
            }
        }

        var startRegex = new Regex($@"\b(?:{startPattern})\b\s*(?:tu|tai|at|:|=)?\s*(?:(?:{nodeLabelPattern})\b\s*)?(\w+)", RegexOptions.Singleline);
        var endRegex = new Regex($@"\b(?:{endPattern})\b\s*(?:tai|den|toi|at|:|=)?\s*(?:(?:{nodeLabelPattern})\b\s*)?(\w+)", RegexOptions.Singleline);

        var startMatch = startRegex.Match(text);
        if (result.start == null && startMatch.Success)
        {
            var candidate = startMatch.Groups[1].Value;
            if (IsVertexToken(candidate))
            {
                result.start = candidate;
            }
        }

        var endMatch = endRegex.Match(text);
        if (endMatch.Success)
        {
            var candidate = endMatch.Groups[1].Value;
            if (IsVertexToken(candidate))
            {
                result.end = candidate;
            }
        }

        if (result.start == null && (result.algorithm == "bfs" || result.algorithm == "dfs" || result.problem_group == "shortest_path"))
        {
            var fromToMatch = Regex.Match(text, @"\btu\s+(\w+)\s+(?:den|toi)\b");
            if (fromToMatch.Success)
            {
                var candidate = fromToMatch.Groups[1].Value;
                if (IsVertexToken(candidate))
                {
                    result.start = candidate;
                }
            }

            if (result.start == null)
            {
                var fromMatch = Regex.Match(text, @"\btu\s+(\w+)\b");
                if (fromMatch.Success)
                {
                    var candidate = fromMatch.Groups[1].Value;
                    if (IsVertexToken(candidate))
                    {
                        result.start = candidate;
                    }
                }
            }
        }

        if (result.end == null && result.problem_group == "shortest_path")
        {
            var endToMatch = Regex.Match(text, @"\b(?:den|toi)\s+(\w+)\b");
            if (endToMatch.Success)
            {
                var candidate = endToMatch.Groups[1].Value;
                if (IsVertexToken(candidate))
                {
                    result.end = candidate;
                }
            }
        }

        if (result.start == null && (result.algorithm == "bfs" || result.algorithm == "dfs" || result.problem_group == "shortest_path"))
        {
            var rawNoDiacritics = RemoveVietnameseDiacritics(rawLower);
            var rawSimplified = Regex.Replace(rawNoDiacritics, @"[^a-z0-9]+", " ").Trim();
            var rawStrip = Regex.Replace(rawLower.Normalize(NormalizationForm.FormD), @"\p{M}", "");
            rawStrip = rawStrip.Replace('đ', 'd').Replace('Đ', 'D');
            var rawAscii = new string(rawLower.Normalize(NormalizationForm.FormKD).Where(c => c < 128).ToArray());
            var fromToMatchRaw = Regex.Match(rawLower, @"\btừ\s+(\w+)\s+(?:đến|toi|den)\b");
            if (!fromToMatchRaw.Success)
            {
                fromToMatchRaw = Regex.Match(rawNoDiacritics, @"\btu\s+(\w+)\s+(?:den|toi)\b");
            }
            if (!fromToMatchRaw.Success)
            {
                fromToMatchRaw = Regex.Match(rawStrip, @"\btu\s+(\w+)\s+(?:den|toi)\b");
            }
            if (fromToMatchRaw.Success)
            {
                var candidate = fromToMatchRaw.Groups[1].Value;
                if (IsVertexToken(candidate))
                {
                    result.start = candidate;
                }
            }
            if (result.start == null && ContainsIgnoreDiacritics(rawLower, "tu"))
            {
                var fallback = FindFirstVertexAfter(rawNoDiacritics, "tu", result.vertices);
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    result.start = fallback;
                }
            }
            if (result.start == null && rawLower.Contains("từ"))
            {
                var fallback = FindFirstVertexAfter(rawLower, "từ", result.vertices);
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    result.start = fallback;
                }
            }
            if (result.start == null && rawSimplified.Contains("tu"))
            {
                var fallback = FindFirstVertexAfter(rawSimplified, "tu", result.vertices);
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    result.start = fallback;
                }
            }
            if (result.start == null && rawStrip.Contains("tu"))
            {
                var fallback = FindFirstVertexAfter(rawStrip, "tu", result.vertices);
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    result.start = fallback;
                }
            }
            if (result.start == null && rawAscii.Contains("tu"))
            {
                var fallback = FindFirstVertexAfter(rawAscii, "tu", result.vertices);
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    result.start = fallback;
                }
            }
        }

        if (result.end == null && result.problem_group == "shortest_path")
        {
            var rawNoDiacritics = RemoveVietnameseDiacritics(rawLower);
            var rawSimplified = Regex.Replace(rawNoDiacritics, @"[^a-z0-9]+", " ").Trim();
            var rawStrip = Regex.Replace(rawLower.Normalize(NormalizationForm.FormD), @"\p{M}", "");
            rawStrip = rawStrip.Replace('đ', 'd').Replace('Đ', 'D');
            var rawAscii = new string(rawLower.Normalize(NormalizationForm.FormKD).Where(c => c < 128).ToArray());
            var endToMatchRaw = Regex.Match(rawLower, @"\b(?:đến|den|toi)\s+(\w+)\b");
            if (!endToMatchRaw.Success)
            {
                endToMatchRaw = Regex.Match(rawNoDiacritics, @"\b(?:den|toi)\s+(\w+)\b");
            }
            if (!endToMatchRaw.Success)
            {
                endToMatchRaw = Regex.Match(rawStrip, @"\b(?:den|toi)\s+(\w+)\b");
            }
            if (endToMatchRaw.Success)
            {
                var candidate = endToMatchRaw.Groups[1].Value;
                if (IsVertexToken(candidate))
                {
                    result.end = candidate;
                }
            }
            if (result.end == null && ContainsIgnoreDiacritics(rawLower, "den"))
            {
                var fallback = FindFirstVertexAfter(rawNoDiacritics, "den", result.vertices);
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    result.end = fallback;
                }
            }
            if (result.end == null && rawLower.Contains("đến"))
            {
                var fallback = FindFirstVertexAfter(rawLower, "đến", result.vertices);
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    result.end = fallback;
                }
            }
            if (result.end == null && rawSimplified.Contains("den"))
            {
                var fallback = FindFirstVertexAfter(rawSimplified, "den", result.vertices);
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    result.end = fallback;
                }
            }
            if (result.end == null && rawStrip.Contains("den"))
            {
                var fallback = FindFirstVertexAfter(rawStrip, "den", result.vertices);
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    result.end = fallback;
                }
            }
            if (result.end == null && rawAscii.Contains("den"))
            {
                var fallback = FindFirstVertexAfter(rawAscii, "den", result.vertices);
                if (!string.IsNullOrWhiteSpace(fallback))
                {
                    result.end = fallback;
                }
            }
        }

        var firstRawLine = rawLower.Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? rawLower;
        var firstRawNoDiacritics = RemoveVietnameseDiacritics(firstRawLine);
        var firstTuMatch = Regex.Match(firstRawNoDiacritics, @"\btu\s+([a-zA-Z0-9]+)\b");
        if (result.start == null && firstTuMatch.Success)
        {
            var candidate = firstTuMatch.Groups[1].Value;
            var inVertices = result.vertices.Count == 0 || result.vertices.Any(v => string.Equals(v, candidate, StringComparison.OrdinalIgnoreCase));
            if (inVertices && IsVertexToken(candidate))
            {
                result.start = candidate;
            }
        }

        var rawNoDiacriticsFinal = RemoveVietnameseDiacritics(rawLower);
        var tuIndexFinal = rawNoDiacriticsFinal.IndexOf("tu", StringComparison.Ordinal);
        if (result.start == null && tuIndexFinal >= 0)
        {
            var idx = tuIndexFinal + 2;
            while (idx < rawNoDiacriticsFinal.Length && !char.IsLetterOrDigit(rawNoDiacriticsFinal[idx])) idx++;
            var startIdx = idx;
            while (idx < rawNoDiacriticsFinal.Length && char.IsLetterOrDigit(rawNoDiacriticsFinal[idx])) idx++;
            if (startIdx < idx)
            {
                var candidate = rawNoDiacriticsFinal.Substring(startIdx, idx - startIdx);
                var inVertices = result.vertices.Count == 0 || result.vertices.Any(v => string.Equals(v, candidate, StringComparison.OrdinalIgnoreCase));
                if (inVertices && IsVertexToken(candidate))
                {
                    result.start = candidate;
                }
            }
        }

        var rawLineIndex = firstRawLine.IndexOf("từ", StringComparison.Ordinal);
        if (rawLineIndex < 0)
        {
            rawLineIndex = firstRawLine.IndexOf("tu", StringComparison.Ordinal);
        }
        if (rawLineIndex >= 0)
        {
            var idx = rawLineIndex + 2;
            while (idx < firstRawLine.Length && !char.IsLetterOrDigit(firstRawLine[idx])) idx++;
            var startIdx = idx;
            while (idx < firstRawLine.Length && char.IsLetterOrDigit(firstRawLine[idx])) idx++;
            if (startIdx < idx)
            {
                var rawCandidate = firstRawLine.Substring(startIdx, idx - startIdx);
                var candidate = RemoveVietnameseDiacritics(rawCandidate).ToLowerInvariant();
                var inVertices = result.vertices.Count == 0 || result.vertices.Any(v => string.Equals(v, candidate, StringComparison.OrdinalIgnoreCase));
                if (inVertices && IsVertexToken(candidate))
                {
                    result.start = candidate;
                }
            }
        }

        if (result.start == null && firstLineTu.Success)
        {
            var candidate = firstLineTu.Groups[1].Value;
            if (IsVertexToken(candidate))
            {
                result.start = candidate;
            }
        }

        if (result.problem_group == "shortest_path" && !string.IsNullOrWhiteSpace(result.algorithm))
        {
            var firstLineTokens = Regex.Matches(firstLine, @"[a-zA-Z0-9]+")
                .Select(m => m.Value)
                .ToList();
            var algoIndex = firstLineTokens.FindIndex(t => string.Equals(t, result.algorithm, StringComparison.OrdinalIgnoreCase));
            if (algoIndex >= 0)
            {
                var candidate = firstLineTokens.Skip(algoIndex + 1)
                    .FirstOrDefault(t => result.vertices.Count == 0 || result.vertices.Any(v => string.Equals(v, t, StringComparison.OrdinalIgnoreCase)));
                if (!string.IsNullOrWhiteSpace(candidate) && IsVertexToken(candidate))
                {
                    result.start = candidate;
                }
            }
        }

        if (result.start == null && result.problem_group == "shortest_path" && !string.IsNullOrWhiteSpace(result.algorithm))
        {
            var tokenList = Regex.Matches(text, @"[a-zA-Z0-9]+")
                .Select(m => m.Value)
                .ToList();
            var algoIndex = tokenList.FindIndex(t => string.Equals(t, result.algorithm, StringComparison.OrdinalIgnoreCase));
            if (algoIndex >= 0)
            {
                var candidate = tokenList.Skip(algoIndex + 1)
                    .FirstOrDefault(t => result.vertices.Any(v => string.Equals(v, t, StringComparison.OrdinalIgnoreCase)));
                if (!string.IsNullOrWhiteSpace(candidate) && IsVertexToken(candidate))
                {
                    result.start = candidate;
                }
            }
        }

        if (result.start == null && result.vertices.Count > 0)
        {
            result.start = FindFirstVertexAfter(text, "tu", result.vertices);
        }

        if (result.start == null && (result.algorithm == "bfs" || result.algorithm == "dfs" || result.problem_group == "shortest_path") && result.vertices.Count > 0)
        {
            result.start = result.vertices[0];
        }

        if (result.end == null && result.problem_group == "shortest_path" && result.vertices.Count > 0)
        {
            result.end = FindFirstVertexAfter(text, "den", result.vertices);
        }
    }

    private static void Validate(GraphExtractionResult result)
    {
        result.edges = result.edges
            .Where(e => !string.Equals(e.from, e.to, StringComparison.OrdinalIgnoreCase))
            .GroupBy(e => $"{e.from}|{e.to}|{(e.weight.HasValue ? e.weight.Value.ToString() : "")}")
            .Select(g => g.First())
            .ToList();

        result.vertices = result.vertices.Distinct().OrderBy(v => v).ToList();
    }
}
