# GraphExtractor - Trích Xuất Đồ Thị Từ Ngôn Ngữ Tự Nhiên

Một thư viện C# để trích xuất cấu trúc đồ thị (đỉnh, cạnh, trọng số) và xác định thuật toán từ văn bản tiếng Việt/tiếng Anh.

## 📋 Mục Lục

1. [Thuật Toán Cắt Chuỗi](#1-thuật-toán-cắt-chuỗi)
2. [Bài Toán Thiết Kế](#2-bài-toán-thiết-kế)
3. [Mã Giả Quy Trình](#3-mã-giả-quy-trình)
4. [Test Cases](#4-test-cases)
5. [Cấu Trúc Project](#5-cấu-trúc-project)

---

## 1. Thuật Toán Cắt Chuỗi

### 1.1 Input / Output

**Input:**
- Chuỗi văn bản tự do (tiếng Việt có dấu/không dấu, tiếng Anh)
- Mô tả bài toán đồ thị với các thành phần: đỉnh, cạnh, trọng số, thuật toán

**Output:**
- `GraphExtractionResult` object chứa:
  - `algorithm`: Thuật toán được phát hiện (bfs, dfs, dijkstra, bellman-ford, ...)
  - `problem_group`: Nhóm bài toán (traversal, shortest_path, mst)
  - `direction`: Hướng đồ thị (directed/undirected)
  - `weighted`: Có trọng số hay không
  - `vertices`: Danh sách các đỉnh
  - `edges`: Danh sách các cạnh (from, to, weight)
  - `start`: Đỉnh bắt đầu
  - `end`: Đỉnh kết thúc

### 1.2 Các Kỹ Thuật Cắt Chuỗi Chính

| Kỹ Thuật | Mô Tả | Ví Dụ Pattern |
|----------|-------|---------------|
| **Tokenization** | Tách chuỗi thành các token | `\b[a-zA-Z][a-zA-Z0-9]*\b\|\b\d+\b` |
| **Keyword Matching** | Tìm từ khóa đặc trưng | `đỉnh`, `cạnh`, `từ`, `đến` |
| **Regex Pattern** | Pattern cho cấu trúc dữ liệu | `\(\s*(\w+)\s*,\s*(\w+)\s*,\s*(-?\d+)\s*\)` |
| **Blacklist Filter** | Loại bỏ từ nhiễu | `va`, `voi`, `noi`, `ket` |
| **Multi-layer Parsing** | Thử nhiều cách phân tích | Adjacency List → Matrix → Tuple → Natural Language |

---

## 2. Bài Toán Thiết Kế

### 2.1 Phân Tích Yêu Cầu

**Bài toán:** Trích xuất cấu trúc đồ thị từ ngôn ngữ tự nhiên không có định dạng cố định.

**Thách thức:**
1. **Đa dạng định dạng input**: Cùng một thông tin có thể được viết theo nhiều cách
2. **Ngôn ngữ tự nhiên không chuẩn**: Tiếng Việt có dấu/không dấu, viết tắt
3. **Thiếu thông tin**: Một số thông tin phải suy luận từ ngữ cảnh
4. **Nhiễu dữ liệu**: Có từ không liên quan xen kẽ

### 2.2 Giải Pháp Thiết Kế

```
┌─────────────────────────────────────────────────────────────┐
│                    INPUT TEXT (Raw)                         │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│              PREPROCESSING PIPELINE                         │
│  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐          │
│  │ Unicode NFC  │→│ Lowercase    │→│ Remove       │          │
│  │ Normalize    │ │              │   │ Diacritics   │          │
│  └──────────────┘ └──────────────┘ └──────────────┘          │
│  ┌──────────────┐ ┌──────────────┐                            │
│  │ Normalize    │→│ Whitespace   │                            │
│  │ Edge Symbols │   │ Collapse     │                            │
│  └──────────────┘ └──────────────┘                            │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│              DETECTION MODULES                               │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐            │
│  │ Algorithm   │ │ Direction   │ │ Weighted    │            │
│  │ Detection   │   │ Detection   │   │ Detection   │            │
│  │ (Scoring)   │   │ (Keywords)  │   │ (Patterns)  │            │
│  └─────────────┘ └─────────────┘ └─────────────┘            │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│              EXTRACTION MODULES                              │
│  ┌─────────────┐ ┌─────────────┐                            │
│  │ Vertex      │ │ Edge        │                            │
│  │ Extraction  │   │ Extraction  │                            │
│  └─────────────┘ └─────────────┘                            │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│              START/END DETECTION                             │
│                 (Multi-layer Fallback)                       │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │ Layer 1: Pattern "tu X" / "den X" in processed text    │ │
│  │ Layer 2: Raw Vietnamese "từ X" / "đến X"               │ │
│  │ Layer 3: FindFirstVertexAfter(processed, "tu", V)      │ │
│  │ Layer 4: Try rawNoDiacritics, rawSimplified, rawStrip  │ │
│  │ Layer 5: Algorithm-specific fallback (vertices[0])   │ │
│  └─────────────────────────────────────────────────────────┘ │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│              POST-PROCESSING                               │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐            │
│  │ Infer       │ │ Validate    │ │ Deduplicate │            │
│  │ Vertices    │   │ Result      │   │ Edges       │            │
│  └─────────────┘ └─────────────┘ └─────────────┘            │
└───────────────────────┬─────────────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────────────┐
│              OUTPUT (GraphExtractionResult)                  │
└─────────────────────────────────────────────────────────────┘
```

### 2.3 Chiến Lược Pattern Matching

**1. Thuật toán phát hiện (Keyword Scoring):**
```
For each algorithm in [bellman-ford, dijkstra, bfs, dfs, kruskal, prim, ...]:
    score[algorithm] = CountMatchingKeywords(text, algorithm_keywords)
    
    if MST keywords found:
        score[prim]++, score[kruskal]++
    
    if negative weight hints found:
        score[bellman-ford]++
        
result.algorithm = algorithm with max(score)
```

**2. Trích xuất đỉnh (Multi-Pattern):**
```
Priority 1: V = {a, b, c} / V = [a, b, c]
Priority 2: "các đỉnh là: a, b, c"
Priority 3: Extract từ danh sách cạnh
```

**3. Trích xuất cạnh (Cascade):**
```
Step 1: Thử Adjacency List (A: B, C)
Step 2: Thử Adjacency Matrix (ma trận kề)
Step 3: Thử Edge Block (E = {(a,b,1), (b,c,2)})
Step 4: Thử Natural Language ("a nối với b có trọng số 1")
Step 5: Thử Tuple Pattern ((a,b,1), [a,b,1])
```

---

## 3. Mã Giả Quy Trình

```
FUNCTION Extract(raw_text):
    
    // ========== PREPROCESSING ==========
    processed = Preprocess(raw_text)
    // - Unicode NFC normalize
    // - Convert to lowercase
    // - Remove Vietnamese diacritics
    // - Normalize edge symbols (→, —, – → -)
    // - Collapse whitespace
    
    result = new GraphExtractionResult()
    
    // ========== DETECTION PHASE ==========
    
    // 1. Algorithm Detection (Keyword Scoring)
    scores = {
        "bellman-ford": CountKeywords(processed, BELLMAN_KEYWORDS),
        "dijkstra": CountKeywords(processed, DIJKSTRA_KEYWORDS),
        "floyd-warshall": CountKeywords(processed, FLOYD_KEYWORDS),
        "bfs": CountKeywords(processed, BFS_KEYWORDS),
        "dfs": CountKeywords(processed, DFS_KEYWORDS),
        "kruskal": CountKeywords(processed, KRUSKAL_KEYWORDS),
        "prim": CountKeywords(processed, PRIM_KEYWORDS),
        ...
    }
    
    // Boost scores based on hints
    IF MST keywords found:
        scores["kruskal"]++, scores["prim"]++
    IF negative weight hints found:
        scores["bellman-ford"]++
    
    result.algorithm = ARGMAX(scores)
    result.problem_group = MapAlgorithmToGroup(result.algorithm)
    
    // 2. Direction Detection
    IF Contains(processed, UNDIRECTED_KEYWORDS):
        result.direction = "undirected"
    ELSE IF Contains(processed, DIRECTED_KEYWORDS) OR Contains(processed, "->"):
        result.direction = "directed"
    ELSE:
        result.direction = "undirected"  // default
    
    // 3. Weighted Detection
    IF Contains(processed, UNWEIGHTED_KEYWORDS):
        result.weighted = false
    ELSE IF Contains(processed, WEIGHTED_KEYWORDS):
        result.weighted = true
    ELSE IF RegexMatch(processed, "\(\w+,\w+,\d+\)"):
        result.weighted = true
    ELSE:
        result.weighted = false
    
    // ========== EXTRACTION PHASE ==========
    
    // 4. Vertex Extraction
    patterns = [
        "V\s*[=:]\s*\{([^}]+)\}",           // V = {a, b, c}
        "V\s*[=:]\s*\[([^\]]+)\]",         // V = [a, b, c]
        "V\s*[=:]\s*\(([^\)]+)\)",         // V = (a, b, c)
        "các đỉnh\s*(?:là|gồm|:)\s*([^\.]+)" // các đỉnh là: a, b, c
    ]
    
    FOR each pattern IN patterns:
        match = RegexMatch(processed, pattern)
        IF match.Success:
            tokens = Split(match.Group(1), ",;\s+và\s+")
            result.vertices.AddRange(tokens)
            BREAK
    
    // 5. Edge Extraction
    IF TryParseAdjacencyList(processed, result):
        // Parsed as adjacency list format
        PASS
    ELSE IF TryParseAdjacencyMatrix(processed, result):
        // Parsed as adjacency matrix format
        PASS
    ELSE:
        edgeBlock = FindEdgesBlock(processed)
        IF edgeBlock != null:
            ParseEdgesFromText(edgeBlock, result)
        ELSE:
            ParseEdgesFromText(processed, result)
    
    // 6. Post-processing
    IF Any edge has weight:
        result.weighted = true
    
    InferVerticesFromEdges(result)  // Add vertices from edges if missing
    
    // 7. Start/End Detection (Multi-layer fallback)
    result.start = DetectStartVertex(processed, raw_text, result.vertices)
    result.end = DetectEndVertex(processed, raw_text, result.vertices)
    
    // Default start for traversal/shortest path
    IF result.start == null AND result.vertices.Count > 0:
        IF result.algorithm IN ["bfs", "dfs"] OR result.problem_group == "shortest_path":
            result.start = result.vertices[0]
    
    // ========== VALIDATION ==========
    Validate(result)
    
    RETURN result


// ========== HELPER FUNCTIONS ==========

FUNCTION Preprocess(input):
    s = input.Normalize(NFC)
    s = s.ToLowerInvariant()
    s = RemoveVietnameseDiacritics(s)
    s = NormalizeEdgeSymbols(s)  // →, —, – → -
    s = Regex.Replace(s, "[ \t]+", " ")
    s = s.Trim()
    RETURN s

FUNCTION TryParseAdjacencyList(text, result):
    lines = text.Split('\n')
    matches = []
    
    FOR each line IN lines:
        match = Regex.Match(line, "^(\w+)\s*(?:->|:|\|)\s*([^;]+)$")
        IF match.Success:
            matches.Add((match.Group(1), match.Group(2)))
    
    IF matches.Count < 2:
        RETURN false
    
    FOR each (from, list) IN matches:
        targets = ExtractVertexTokens(list)
        FOR each t IN targets:
            IF t != from:
                result.edges.Add(Edge(from, t, null))
    
    RETURN true

FUNCTION TryParseAdjacencyMatrix(text, result):
    lines = text.Split('\n')
    
    FOR i = 0 TO lines.Length - 2:
        header = Tokenize(lines[i])
        next = Tokenize(lines[i+1])
        
        IF next.Count == header.Count + 1 AND AllNumbers(next.Skip(1)):
            // Found matrix header
            FOR r = i+1 TO lines.Length - 1:
                row = Tokenize(lines[r])
                IF row.Count != header.Count + 1:
                    BREAK
                
                rowLabel = row[0]
                FOR c = 0 TO header.Count - 1:
                    weight = ParseWeight(row[c+1])
                    IF weight != 0:
                        result.edges.Add(Edge(rowLabel, header[c], weight))
            RETURN true
    
    RETURN false

FUNCTION ParseEdgesFromText(text, result):
    patterns = [
        // Tuple: (a,b,1) or [a,b,1]
        "[\(\[](\w+),(\w+),(\d+)[\)\]]",
        
        // Arrow with weight: a -> b : 1
        "(\w+)\s*(?:->|=>|-)\s*(\w+)\s*[:=]?\s*(\d+)",
        
        // Natural language: a nối với b có trọng số 1
        "(\w+)\s+(?:nối với|kết nối)\s+(\w+)\s+(?:có)?\s*(?:trọng số|chi phí)\s*(\d+)",
        
        // Between: giữa a và b có trọng số 1
        "giữa\s+(\w+)\s+và\s+(\w+)\s+(?:có)?\s*(?:trọng số)?\s*(\d+)"
    ]
    
    FOR each pattern IN patterns:
        matches = Regex.Matches(text, pattern)
        FOR each match IN matches:
            u = match.Group(1)
            v = match.Group(2)
            w = match.Group(3) ? ParseWeight(match.Group(3)) : null
            
            IF IsValidVertex(u) AND IsValidVertex(v):
                result.edges.Add(Edge(u, v, w))

FUNCTION DetectStartVertex(processed, raw, vertices):
    // Layer 1: Direct pattern "từ X" in processed text
    match = Regex.Match(processed, "\btu\s+(\w+)")
    IF match.Success AND IsVertex(match.Group(1)):
        RETURN match.Group(1)
    
    // Layer 2: Raw Vietnamese "từ X"
    match = Regex.Match(raw, "\btừ\s+(\w+)")
    IF match.Success AND IsVertex(match.Group(1)):
        RETURN match.Group(1)
    
    // Layer 3: Find first vertex after "tu" keyword
    IF vertices.Count > 0:
        candidate = FindFirstVertexAfter(processed, "tu", vertices)
        IF candidate != null:
            RETURN candidate
    
    // Layer 4: Try multiple normalized forms với pattern "tu X"
    FOR each form IN [rawSimplified, rawStrip, rawAscii]:
        match = Regex.Match(form, "\btu\s+(\w+)")
        IF match.Success AND IsVertex(match.Group(1)):
            RETURN match.Group(1)
    
    // Layer 5: First vertex in vertex list as ultimate fallback
    IF vertices.Count > 0 AND (algorithm IN ["bfs", "dfs"] OR problem_group == "shortest_path"):
        RETURN vertices[0]
    
    RETURN null

FUNCTION Validate(result):
    // Deduplicate edges
    result.edges = result.edges
        .GroupBy(e => e.from + "|" + e.to + "|" + e.weight)
        .Select(g => g.First())
        .ToList()
    
    // Sort vertices
    result.vertices = result.vertices.Distinct().Order().ToList()
```

---

## 4. Test Cases

### Test Case 1: Đồ thị vô hướng cơ bản
**Input:**
```
Cho đồ thị vô hướng có các đỉnh V = {A, B, C, D} và các cạnh E = {(A,B,1), (B,C,2), (C,D,3)}
Tìm đường đi ngắn nhất từ A đến D bằng thuật toán Dijkstra
```

**Expected Output:**
```json
{
  "algorithm": "dijkstra",
  "problem_group": "shortest_path",
  "direction": "undirected",
  "weighted": true,
  "vertices": ["A", "B", "C", "D"],
  "edges": [
    { "from": "A", "to": "B", "weight": 1 },
    { "from": "B", "to": "C", "weight": 2 },
    { "from": "C", "to": "D", "weight": 3 }
  ],
  "start": "A",
  "end": "D"
}
```

---

### Test Case 2: Đồ thị có hướng - Dạng danh sách kề
**Input:**
```
Đồ thị có hướng:
A -> B, C
B -> D
C -> D
Duyệt đồ thị bắt đầu từ A
```

**Expected Output:**
```json
{
  "algorithm": "bfs",
  "problem_group": "traversal",
  "direction": "directed",
  "weighted": false,
  "vertices": ["A", "B", "C", "D"],
  "edges": [
    { "from": "A", "to": "B", "weight": null },
    { "from": "A", "to": "C", "weight": null },
    { "from": "B", "to": "D", "weight": null },
    { "from": "C", "to": "D", "weight": null }
  ],
  "start": "A",
  "end": null
}
```

---

### Test Case 3: Ma trận kề (đối xứng)
**Input:**
```
Đồ thị có trọng số G:
    A   B   C
A   0   5   3
B   5   0   2
C   3   2   0
Tìm cây khung nhỏ nhất bắt đầu từ đỉnh A
```

**Expected Output:**
```json
{
  "algorithm": "prim",
  "problem_group": "mst",
  "direction": "undirected",
  "weighted": true,
  "vertices": ["A", "B", "C"],
  "edges": [
    { "from": "A", "to": "B", "weight": 5 },
    { "from": "B", "to": "A", "weight": 5 },
    { "from": "A", "to": "C", "weight": 3 },
    { "from": "C", "to": "A", "weight": 3 },
    { "from": "B", "to": "C", "weight": 2 },
    { "from": "C", "to": "B", "weight": 2 }
  ],
  "start": "A",
  "end": null
}
```

---

### Test Case 4: Tiếng Việt không dấu - Bellman-Ford
**Input:**
```
Do thi co huong co trong so am
Dinh: s, t, x, y, z
Canh: (s,t,6), (s,y,7), (t,x,5), (t,y,8), (t,z,-4), (x,t,-2)
Tim duong di ngan nhat tu s den z bang Bellman-Ford
```

**Expected Output:**
```json
{
  "algorithm": "bellman-ford",
  "problem_group": "shortest_path",
  "direction": "directed",
  "weighted": true,
  "vertices": ["s", "t", "x", "y", "z"],
  "edges": [
    { "from": "s", "to": "t", "weight": 6 },
    { "from": "s", "to": "y", "weight": 7 },
    { "from": "t", "to": "x", "weight": 5 },
    { "from": "t", "to": "y", "weight": 8 },
    { "from": "t", "to": "z", "weight": -4 },
    { "from": "x", "to": "t", "weight": -2 }
  ],
  "start": "s",
  "end": "z"
}
```

---

### Test Case 5: Ngôn ngữ tự nhiên đa dạng
**Input:**
```
Xét đồ thị gồm có 4 đỉnh là A, B, C, D
A nối với B có trọng số 10
B kết nối với C chi phí là 5
Giữa C và D có khoảng cách 8
Dùng thuật toán Kruskal tìm cây khung
```

**Expected Output:**
```json
{
  "algorithm": "kruskal",
  "problem_group": "mst",
  "direction": "undirected",
  "weighted": true,
  "vertices": ["A", "B", "C", "D"],
  "edges": [
    { "from": "A", "to": "B", "weight": 10 },
    { "from": "B", "to": "C", "weight": 5 },
    { "from": "C", "to": "D", "weight": 8 }
  ],
  "start": null,
  "end": null
}
```

---

### Test Case 6: DFS Traversal
**Input:**
```
Đồ thị vô hướng:
Đỉnh: 1, 2, 3, 4, 5
Cạnh: 1-2, 2-3, 3-4, 4-5, 5-1
Duyệt đồ thị theo chiều sâu bắt đầu từ đỉnh 1
```

**Expected Output:**
```json
{
  "algorithm": "dfs",
  "problem_group": "traversal",
  "direction": "undirected",
  "weighted": false,
  "vertices": ["1", "2", "3", "4", "5"],
  "edges": [
    { "from": "1", "to": "2", "weight": null },
    { "from": "2", "to": "3", "weight": null },
    { "from": "3", "to": "4", "weight": null },
    { "from": "4", "to": "5", "weight": null },
    { "from": "5", "to": "1", "weight": null }
  ],
  "start": "1",
  "end": null
}
```

---

### Test Case 7: Euler Path
**Input:**
```
Tìm chu trình Euler trong đồ thị:
Đỉnh: A, B, C, D
Cạnh: A-B, B-C, C-D, D-A, A-C
Đi qua tất cả các cạnh đúng một lần
```

**Expected Output:**
```json
{
  "algorithm": "euler",
  "problem_group": "traversal",
  "direction": "undirected",
  "weighted": false,
  "vertices": ["A", "B", "C", "D"],
  "edges": [
    { "from": "A", "to": "B", "weight": null },
    { "from": "B", "to": "C", "weight": null },
    { "from": "C", "to": "D", "weight": null },
    { "from": "D", "to": "A", "weight": null },
    { "from": "A", "to": "C", "weight": null }
  ],
  "start": null,
  "end": null
}
```

---

## 5. Cấu Trúc Project

```
DACN_Algorithms/
├── App/
│   └── Program.cs              # Entry point
├── Models/
│   ├── Edge.cs                 # Edge model (from, to, weight)
│   └── GraphExtractionResult.cs # Result model
├── Parsing/
│   ├── GraphExtractor.cs       # Core extraction logic
│   └── TokenManager.cs         # Keywords and token utilities
├── DACN_Algorithms.csproj      # Project file
└── README.md                   # This file
```

### Class Diagram

```
┌────────────────────────────────────────────────────────────────┐
│                    GraphExtractor                              │
│                     (static class)                             │
├────────────────────────────────────────────────────────────────┤
│ + Extract(string): GraphExtractionResult                       │
│ + ExtractToJson(string): string                                │
│ - Preprocess(string): string                                   │
│ - DetectAlgorithm(string, result)                            │
│ - DetectDirection(string, string, result)                    │
│ - DetectWeighted(string, result)                             │
│ - ExtractVertices(string, result)                              │
│ - ExtractEdges(string, result)                                 │
│ - DetectStartEnd(string, string, result)                     │
│ - TryParseAdjacencyList(string, result): bool                │
│ - TryParseAdjacencyMatrix(string, result): bool              │
│ - ParseEdgesFromText(string, result)                         │
│ - InferVerticesFromEdges(result)                             │
│ - Validate(result)                                             │
└────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│                    GraphExtractionResult                       │
├────────────────────────────────────────────────────────────────┤
│ + algorithm: string?                                           │
│ + problem_group: string?                                       │
│ + direction: string = "undirected"                           │
│ + weighted: bool = false                                     │
│ + vertices: List<string>                                       │
│ + edges: List<Edge>                                            │
│ + start: string?                                               │
│ + end: string?                                                 │
└────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│                         Edge                                   │
├────────────────────────────────────────────────────────────────┤
│ + from: string = ""                                            │
│ + to: string = ""                                              │
│ + weight: double?                                              │
└────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│                    KeywordRules                                │
│                     (static class)                             │
├────────────────────────────────────────────────────────────────┤
│ VertexKeywords, EdgeKeywords, DirectedKeywords...            │
│ BellmanKeywords, DijkstraKeywords, BfsKeywords...            │
└────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│                    TokenManager                                │
│                     (static class)                             │
├────────────────────────────────────────────────────────────────┤
│ + BlacklistTokens: HashSet<string>                           │
│ + CleanToken(string): string                                   │
│ + TokenizeAndClean(string): List<string>                     │
└────────────────────────────────────────────────────────────────┘
```

### Cách Sử Dụng

```csharp
// Từ console
> dotnet run

// Từ file
> dotnet run input.txt

// Từ code
using DACN_Algorithms;

var input = "Cho đồ thị V = {A, B, C}, E = {(A,B,1), (B,C,2)}";
var result = GraphExtractor.Extract(input);
var json = GraphExtractor.ExtractToJson(input);
```

---

## Keywords Reference

| Category | Keywords (Tiếng Việt / English) |
|----------|--------------------------------|
| **Vertex** | đỉnh, dinh, tập đỉnh, vertex, vertices, v, nodes |
| **Edge** | cạnh, canh, tập cạnh, edge, edges, e, cung |
| **Directed** | có hướng, co huong, directed, one-way, 1 chiều |
| **Undirected** | vô hướng, vo huong, undirected, 2 chiều |
| **Weighted** | có trọng số, trọng số, weighted, chi phí, độ dài |
| **Unweighted** | không trọng số, unweighted |
| **Start** | xuất phát, bắt đầu, start, source, từ |
| **End** | kết thúc, đến, đích, end, target, tới |
| **Algorithms** | dijkstra, bellman-ford, bfs, dfs, kruskal, prim, floyd, euler, hamilton |
