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
        "đỉnh của đồ thị", "dinh cua do thi", "vertex", "vertices", "v",
        "ds dinh", "danh sach dinh", "danh sách đỉnh", "tap v", "set v", "nodes", "node set", "list of vertices"
    };

    public static readonly string[] EdgeKeywords =
    {
        "cạnh", "canh", "tập cạnh", "tap canh", "các cạnh", "cac canh",
        "cạnh của đồ thị", "canh cua do thi", "edge", "edges", "e", "cung", "arc", "arcs",
        "ds canh", "danh sach canh", "danh sách cạnh", "tap e", "set e", "edge set", "list of edges"
    };

    public static readonly string[] GraphKeywords =
    {
        "đồ thị", "do thi", "graph", "g"
    };

    public static readonly string[] DirectedKeywords =
    {
        "có hướng", "co huong", "đồ thị có hướng", "do thi co huong", "directed",
        "digraph", "oriented", "co chieu", "co huong doi voi canh",
        "mot chieu", "1 chieu", "one-way", "one way", "mot huong"
    };

    public static readonly string[] UndirectedKeywords =
    {
        "vô hướng", "vo huong", "không hướng", "khong huong", "đồ thị vô hướng", "do thi vo huong", "undirected",
        "khong co huong", "not directed", "non-directed", "hai chieu", "2 chieu", "two-way", "two way"
    };

    public static readonly string[] WeightedKeywords =
    {
        "có trọng số", "co trong so", "trọng số", "trong so", "đồ thị có trọng số", "do thi co trong so", "weighted",
        "co trong so tren canh", "weighted graph", "cost", "weight", "w",
        "chi phi", "do dai", "khoang cach", "thoi gian", "do tre"
    };

    public static readonly string[] UnweightedKeywords =
    {
        "không trọng số", "khong trong so", "không có trọng số", "khong co trong so", "unweighted",
        "khong co trong so tren canh", "unweighted graph"
    };

    public static readonly string[] StartKeywords =
    {
        "xuất phát", "xuat phat", "bắt đầu", "bat dau", "start", "source", "nguồn", "nguon",
        "di tu", "bat dau tu", "start from", "source vertex", "from"
    };

    public static readonly string[] EndKeywords =
    {
        "kết thúc", "ket thuc", "đến", "den", "tới", "toi", "đích", "dich", "end", "target",
        "to", "destination", "den dinh", "end at"
    };

    public static readonly string[] NodeLabelKeywords =
    {
        "đỉnh", "dinh", "vertex", "node", "nut", "dinh so", "dinh thu", "vertex number"
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
        "bfs", "breadth first", "duyệt rộng", "duyet rong", "duyệt theo chiều rộng", "duyet theo chieu rong",
        "breadth-first", "uu tien cac diem gan nhat", "gan nhat truoc", "kham pha gan nhat"
    };

    public static readonly string[] DfsKeywords =
    {
        "dfs", "depth first", "duyệt sâu", "duyet sau", "duyệt theo chiều sâu", "duyet theo chieu sau",
        "depth-first", "di sau", "tham sau", "di sau vao", "di sau vao mot huong",
        "theo chieu sau", "duyet do thi theo chieu sau"
    };

    public static readonly string[] KruskalKeywords =
    {
        "kruskal", "tong trong so cac canh", "tong chi phi cac canh", "nhieu canh",
        "ong nuoc", "bo tri ong", "khu do thi"
    };

    public static readonly string[] KruskalHintKeywords =
    {
        "sap xep canh", "sắp xếp cạnh", "union-find", "union find", "hop nhat tap", "gop tap",
        "nhieu canh", "chon canh nho nhat"
    };

    public static readonly string[] PrimKeywords =
    {
        "prim"
    };

    public static readonly string[] EulerKeywords =
    {
        "euler", "chu trinh euler", "duong di euler", "di qua tat ca cac canh dung mot lan",
        "di qua tat ca cac cua dung mot lan", "di qua tat ca cac con pho dung mot lan",
        "di qua moi canh dung mot lan", "quay ve diem xuat phat",
        "di qua tat ca cac cua dung 1 lan", "di qua tat ca cac con pho dung 1 lan",
        "di qua tat ca cac canh dung 1 lan", "tat ca cac cua dung mot lan",
        "tat ca cac con pho dung mot lan", "tat ca cac bang chuyen dung mot lan",
        "kiem tra tat ca cac bang chuyen dung mot lan", "kiem tra tat ca cac cua dung mot lan"
    };

    public static readonly string[] HamiltonKeywords =
    {
        "hamilton", "duong di hamilton", "di qua tat ca cac dinh dung mot lan",
        "di qua tat ca cac kho dung mot lan", "di qua moi dia diem dung mot lan",
        "di qua moi diem dung mot lan", "di qua moi phong dung mot lan",
        "duong di qua tat ca cac dinh dung mot lan", "di qua tat ca cac dinh dung 1 lan",
        "di qua moi dinh dung mot lan"
    };

    public static readonly string[] ShortestPathKeywords =
    {
        "duong di ngan nhat", "duong di nhanh nhat", "duong di tot nhat", "do tre thap nhat",
        "tiet kiem nang luong", "duong di tiet kiem", "chi phi thap nhat den"
    };

    public static readonly string[] MstKeywords =
    {
        "cay khung", "cay khung toi tieu", "tong trong so nho nhat", "tong chi phi thap nhat",
        "tong chi phi nho nhat", "tong do dai nho nhat", "tong do dai thap nhat", "minimum spanning tree",
        "ket noi tat ca", "tong chieu dai nho nhat", "tong chieu dai thap nhat",
        "tong chieu dai ngan nhat", "tong do dai ngan nhat", "tong do dai la nho nhat"
    };

    public static readonly string[] BellmanHintKeywords =
    {
        "trong so am", "so am", "chu trinh am", "am tiem an", "co trong so am"
    };
}
