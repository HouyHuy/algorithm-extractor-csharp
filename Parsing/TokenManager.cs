using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DACN_Algorithms;

public static class TokenManager
{
    public static readonly HashSet<string> BlacklistTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "tìm", "cho", "hãy", "xét", "đồ", "thị", "bằng", "dùng", "cây", "khung",
        "trọng", "số", "nhất", "liên", "thông", "thành", "phần", "tập", "hợp",
        "đỉnh", "cạnh", "vertices", "edges", "chu", "trình", "đường", "đi",
        "giá", "trị", "từ", "đến", "và", "hoặc", "của", "là", "các", "những",
        "nào", "trong", "sau", "với", "tại", "này", "kia", "đó", "đây",
        "bài", "toán", "giải", "thuật", "nhỏ", "lớn", "ngắn", "dài"
    };

    public static string CleanToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return "";
        var cleaned = Regex.Replace(token, @"[^a-zA-Z0-9]", "");
        if (string.IsNullOrWhiteSpace(cleaned)) return "";
        if (BlacklistTokens.Contains(cleaned)) return "";
        return cleaned;
    }

    public static List<string> TokenizeAndClean(string text)
    {
        var tokens = text.Split(new[] { ' ', '\t', '\n', '\r', ',', ';', '(', ')', '{', '}' }, StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();
        foreach (var token in tokens)
        {
            var cleaned = CleanToken(token);
            if (!string.IsNullOrEmpty(cleaned))
            {
                result.Add(cleaned);
            }
        }
        return result;
    }
}

public static class KeywordRules
{
    public static readonly string[] VertexKeywords =
    {
        "đỉnh", "dinh", "tập đỉnh", "tap dinh", "tập các đỉnh", "tap cac dinh",
        "đỉnh của đồ thị", "dinh cua do thi", "vertex", "vertices", "v"
    };

    public static readonly string[] EdgeKeywords =
    {
        "cạnh", "canh", "tập cạnh", "tap canh", "các cạnh", "cac canh",
        "cạnh của đồ thị", "canh cua do thi", "edge", "edges", "e", "cung", "arc", "arcs"
    };

    public static readonly string[] GraphKeywords =
    {
        "đồ thị", "do thi", "graph", "g"
    };

    public static readonly string[] DirectedKeywords =
    {
        "có hướng", "co huong", "đồ thị có hướng", "do thi co huong", "directed"
    };

    public static readonly string[] UndirectedKeywords =
    {
        "vô hướng", "vo huong", "không hướng", "khong huong", "đồ thị vô hướng", "do thi vo huong", "undirected"
    };

    public static readonly string[] WeightedKeywords =
    {
        "có trọng số", "co trong so", "trọng số", "trong so", "đồ thị có trọng số", "do thi co trong so", "weighted"
    };

    public static readonly string[] UnweightedKeywords =
    {
        "không trọng số", "khong trong so", "không có trọng số", "khong co trong so", "unweighted"
    };

    public static readonly string[] StartKeywords =
    {
        "xuất phát", "xuat phat", "bắt đầu", "bat dau", "start", "source", "nguồn", "nguon"
    };

    public static readonly string[] EndKeywords =
    {
        "kết thúc", "ket thuc", "đến", "den", "tới", "toi", "đích", "dich", "end", "target"
    };

    public static readonly string[] NodeLabelKeywords =
    {
        "đỉnh", "dinh", "vertex", "node", "nut"
    };

    public static readonly string[] BellmanKeywords =
    {
        "bellman-ford", "bellman ford", "bellman"
    };

    public static readonly string[] DijkstraKeywords =
    {
        "dijkstra"
    };

    public static readonly string[] BfsKeywords =
    {
        "bfs", "breadth first", "duyệt rộng", "duyet rong", "duyệt theo chiều rộng", "duyet theo chieu rong"
    };

    public static readonly string[] DfsKeywords =
    {
        "dfs", "depth first", "duyệt sâu", "duyet sau", "duyệt theo chiều sâu", "duyet theo chieu sau"
    };

    public static readonly string[] KruskalKeywords =
    {
        "kruskal"
    };

    public static readonly string[] PrimKeywords =
    {
        "prim"
    };

    public static readonly string[] EulerKeywords =
    {
        "euler"
    };

    public static readonly string[] HamiltonKeywords =
    {
        "hamilton"
    };
}
