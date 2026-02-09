using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DACN_Algorithms;

public class GraphSolver
{
    private readonly GraphExtractionResult _data;
    private readonly List<string> _vertices;
    private readonly List<Edge> _edges;
    private readonly bool _isDirected;

    public GraphSolver(GraphExtractionResult data)
    {
        _data = data;
        _vertices = data.vertices.Distinct().OrderBy(x => x).ToList();
        _edges = data.edges;
        _isDirected = data.direction == "directed";
    }

    public string Solve()
    {
        var algo = _data.algorithm?.ToLowerInvariant() ?? "bfs";

        if (algo.Contains("bellman")) return RunBellmanFord();
        if (algo.Contains("dijkstra")) return RunDijkstra();
        if (algo.Contains("bfs")) return RunBFS();
        if (algo.Contains("dfs")) return RunDFS();
        if (algo.Contains("kruskal")) return RunKruskal();
        if (algo.Contains("prim")) return RunPrim();

        return $"Thuật toán '{algo}' chưa được hỗ trợ.";
    }

    private string GetStartNode()
    {
        if (!string.IsNullOrEmpty(_data.start) && _vertices.Contains(_data.start))
            return _data.start;
        return _vertices.FirstOrDefault() ?? "";
    }

    private string RunBellmanFord()
    {
        var start = GetStartNode();
        var dist = _vertices.ToDictionary(v => v, v => double.PositiveInfinity);
        var parent = _vertices.ToDictionary(v => v, v => (string?)null);
        dist[start] = 0;

        int V = _vertices.Count;
        for (int i = 0; i < V - 1; i++)
        {
            foreach (var edge in _edges)
            {
                double w = edge.weight ?? 1;
                if (dist[edge.from] != double.PositiveInfinity && dist[edge.from] + w < dist[edge.to])
                {
                    dist[edge.to] = dist[edge.from] + w;
                    parent[edge.to] = edge.from;
                }

                if (!_isDirected)
                {
                    if (dist[edge.to] != double.PositiveInfinity && dist[edge.to] + w < dist[edge.from])
                    {
                        dist[edge.from] = dist[edge.to] + w;
                        parent[edge.from] = edge.to;
                    }
                }
            }
        }

        foreach (var edge in _edges)
        {
            double w = edge.weight ?? 1;
            if (dist[edge.from] != double.PositiveInfinity && dist[edge.from] + w < dist[edge.to])
                return "Phát hiện chu trình âm! (Negative Cycle Detected)";
        }

        return FormatPathResult(dist, parent, start);
    }

    private string RunDijkstra()
    {
        var start = GetStartNode();
        var dist = _vertices.ToDictionary(v => v, v => double.PositiveInfinity);
        var parent = _vertices.ToDictionary(v => v, v => (string?)null);
        var pq = new SortedSet<(double distance, string u)>();

        dist[start] = 0;
        pq.Add((0, start));

        while (pq.Count > 0)
        {
            var (d, u) = pq.Min;
            pq.Remove(pq.Min);

            if (d > dist[u]) continue;

            var neighbors = GetNeighbors(u);
            foreach (var (v, w) in neighbors)
            {
                if (dist[u] + w < dist[v])
                {
                    pq.Remove((dist[v], v));
                    dist[v] = dist[u] + w;
                    parent[v] = u;
                    pq.Add((dist[v], v));
                }
            }
        }

        return FormatPathResult(dist, parent, start);
    }

    private string RunBFS()
    {
        var start = GetStartNode();
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        var traversal = new List<string>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var u = queue.Dequeue();
            traversal.Add(u);

            foreach (var (v, _) in GetNeighbors(u).OrderBy(x => x.v))
            {
                if (!visited.Contains(v))
                {
                    visited.Add(v);
                    queue.Enqueue(v);
                }
            }
        }
        return $"Duyệt BFS từ {start}: {string.Join(" -> ", traversal)}";
    }

    private string RunDFS()
    {
        var start = GetStartNode();
        var visited = new HashSet<string>();
        var traversal = new List<string>();
        var stack = new Stack<string>();

        stack.Push(start);

        while (stack.Count > 0)
        {
            var u = stack.Pop();
            if (!visited.Contains(u))
            {
                visited.Add(u);
                traversal.Add(u);

                var neighbors = GetNeighbors(u).Select(x => x.v).OrderByDescending(x => x).ToList();
                foreach (var v in neighbors)
                {
                    if (!visited.Contains(v))
                        stack.Push(v);
                }
            }
        }
        return $"Duyệt DFS từ {start}: {string.Join(" -> ", traversal)}";
    }

    private string RunKruskal()
    {
        var sortedEdges = _edges.OrderBy(e => e.weight ?? 0).ToList();
        var parent = _vertices.ToDictionary(v => v, v => v);
        var mstEdges = new List<Edge>();
        double totalWeight = 0;

        string Find(string i)
        {
            if (parent[i] != i) parent[i] = Find(parent[i]);
            return parent[i];
        }

        void Union(string i, string j)
        {
            var rootI = Find(i);
            var rootJ = Find(j);
            if (rootI != rootJ) parent[rootI] = rootJ;
        }

        foreach (var edge in sortedEdges)
        {
            var rootU = Find(edge.from);
            var rootV = Find(edge.to);
            if (rootU != rootV)
            {
                mstEdges.Add(edge);
                totalWeight += edge.weight ?? 0;
                Union(rootU, rootV);
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine($"MST (Kruskal) - Tổng trọng số: {totalWeight}");
        foreach (var e in mstEdges) sb.AppendLine($"  {e.from} - {e.to}: {e.weight}");
        return sb.ToString();
    }

    private string RunPrim()
    {
        if (_vertices.Count == 0) return "Không có đỉnh.";
        if (_isDirected) return "Đồ thị có hướng không áp dụng được Prim.";

        var start = GetStartNode();
        var inMst = new HashSet<string>();
        var key = _vertices.ToDictionary(v => v, v => double.PositiveInfinity);
        var parent = _vertices.ToDictionary(v => v, v => (string?)null);
        key[start] = 0;

        for (int i = 0; i < _vertices.Count; i++)
        {
            string? u = null;
            double minKey = double.PositiveInfinity;
            foreach (var v in _vertices)
            {
                if (!inMst.Contains(v) && key[v] < minKey)
                {
                    minKey = key[v];
                    u = v;
                }
            }

            if (u == null) break;
            inMst.Add(u);

            foreach (var (v, w) in GetNeighbors(u))
            {
                if (!inMst.Contains(v) && w < key[v])
                {
                    key[v] = w;
                    parent[v] = u;
                }
            }
        }

        var sb = new StringBuilder();
        double total = 0;
        sb.AppendLine("MST (Prim)");
        foreach (var v in _vertices)
        {
            var p = parent[v];
            if (p != null)
            {
                total += key[v];
                sb.AppendLine($"  {p} - {v}: {key[v]}");
            }
        }
        sb.AppendLine($"Tổng trọng số: {total}");
        return sb.ToString();
    }

    private List<(string v, double w)> GetNeighbors(string u)
    {
        var neighbors = new List<(string, double)>();
        foreach (var edge in _edges)
        {
            double w = edge.weight ?? 1;
            if (edge.from == u) neighbors.Add((edge.to, w));
            else if (!_isDirected && edge.to == u) neighbors.Add((edge.from, w));
        }
        return neighbors;
    }

    private string FormatPathResult(Dictionary<string, double> dist, Dictionary<string, string?> parent, string start)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Khoảng cách từ {start}:");
        foreach (var v in _vertices)
        {
            var d = dist[v] == double.PositiveInfinity ? "Inf" : dist[v].ToString();
            sb.Append($"  -> {v}: {d}");

            if (dist[v] != double.PositiveInfinity)
            {
                var path = new List<string>();
                var curr = v;
                while (curr != null)
                {
                    path.Add(curr);
                    curr = parent[curr];
                }
                path.Reverse();
                sb.Append($" [Path: {string.Join("->", path)}]");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
