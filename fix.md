# Fix & Cải thiện — Graph Extractor

## Có nên bỏ GraphSolver không?

**Có, nếu mục tiêu chỉ là trích xuất dữ liệu.**

`GraphSolver.cs` hiện tại:
- Không liên quan gì đến chất lượng extraction
- Floyd-Warshall, Euler, Hamilton được detect nhưng **không có implement** → dễ gây crash
- Để lại trong project gây nhầm lẫn về scope

→ **Nên tách ra hoặc xoá**, chỉ giữ lại `GraphExtractionResult` làm output cuối cùng.

---

## Bug nghiêm trọng cần fix ngay

### 1. `IsVertexToken` không nhận đỉnh viết hoa

```csharp
// HIỆN TẠI — chỉ nhận a-z và số
return Regex.IsMatch(token, @"^[a-z][a-z0-9]*$|^\d+$");

// FIX — thêm A-Z
return Regex.IsMatch(token, @"^[a-zA-Z][a-zA-Z0-9]*$|^\d+$");
```

Đề thi hay dùng đỉnh `A, B, C, D` hoặc `V1, V2, V3`. Hiện tại **mất sạch** toàn bộ.  
Sau khi fix nhớ cập nhật `StopTokens` sang lowercase để tránh conflict.

---

### 2. False positive Bellman-Ford

```csharp
// HIỆN TẠI — bất kỳ số âm nào cũng trigger Bellman
if (ContainsAny(text, BellmanKeywords) || ... || Regex.IsMatch(text, @"-\s*\d"))

// FIX — chỉ trigger khi có từ khoá rõ ràng, bỏ regex số âm
if (ContainsAny(text, BellmanKeywords) || ContainsAny(text, BellmanHintKeywords))
```

Đề Dijkstra mà có trọng số `-3` thì bị nhảy thành Bellman oan.

---

### 3. `"den"` vừa là StopToken vừa dùng để detect đỉnh đích

`"den"` có trong `StopTokens` → khi parse đỉnh bị lọc mất, nhưng lại dùng regex `\bden\s+(\w+)\b` để tìm đỉnh đích.  
→ Kết quả detection end-node bấp bênh tùy thứ tự xử lý.

```csharp
// FIX — xoá "den" khỏi StopTokens, thay bằng blacklist riêng cho vertex extraction
// Tạo VertexBlacklist tách biệt với StopTokens
private static readonly HashSet<string> VertexBlacklist = new() {
    "den", "tu", "co", "va", "voi", "noi", "ket", ...
};
```

---

### 4. Kruskal vs Prim đều fallback về Prim

```csharp
// HIỆN TẠI — MstKeywords luôn trả về Prim
if (ContainsAny(text, MstKeywords))
{
    result.algorithm = "prim"; // ← sai nếu đề có hint về Kruskal
    ...
}
```

Nên thêm logic: nếu đề nhắc đến "nhiều cạnh", "sắp xếp cạnh", "union-find" → ưu tiên Kruskal.

---

## Cải thiện trung hạn (~80% → 90%)

### 5. Thêm parser cho dạng danh sách kề

Dạng phổ biến trong đề thi VN nhưng chưa được hỗ trợ:

```
1 → 2, 3, 4
2 → 3, 5
3 → 4
```

Regex gợi ý:
```csharp
var adjList = Regex.Matches(text, @"(\w+)\s*(?:->|:|\|)\s*([\w,\s]+)(?:\n|$)");
```

---

### 6. Thêm parser cho dạng ma trận kề

Dạng bảng số thường thấy trong đề in sẵn:

```
  1  2  3
1 0  5  0
2 0  0  3
3 0  0  0
```

Cần detect header hàng/cột → build edges từ các ô khác 0.

---

### 7. Mở rộng StopTokens cho đỉnh viết hoa

Sau khi fix IsVertexToken nhận chữ hoa, cần thêm blacklist các từ viết hoa hay xuất hiện trong đề mà không phải đỉnh:

```csharp
// Ví dụ: "BFS", "DFS", "MST", "G", "V", "E" (khi dùng làm ký hiệu đồ thị)
// Phân biệt: "V = {A, B}" thì V là ký hiệu tập, không phải đỉnh
// Nhưng trong E = {(A,B,5)} thì A, B là đỉnh thật
```

---

### 8. Cải thiện detect trọng số ngầm định

Hiện tại nếu đề không ghi "có trọng số" nhưng tuple có 3 phần tử `(u, v, w)` thì code đã tự detect. Tuy nhiên nếu dạng `u - v: w` thì chưa tự set `weighted = true`. Nên thêm:

```csharp
// Sau khi parse xong edges
if (result.edges.Any(e => e.weight.HasValue))
    result.weighted = true;
```

---

## Cải thiện dài hạn (~90% → 95%)

### 9. Tách keyword detection thành scoring thay vì first-match

Hiện tại `DetectAlgorithm` dùng `return` ngay khi match đầu tiên. Nếu đề có nhiều gợi ý mâu thuẫn thì lấy cái sai.

Gợi ý: tính điểm cho từng thuật toán, lấy cao nhất:

```csharp
var scores = new Dictionary<string, int>();
foreach (var kw in BfsKeywords) if (text.Contains(kw)) scores["bfs"] = scores.GetValueOrDefault("bfs") + 1;
foreach (var kw in DfsKeywords) if (text.Contains(kw)) scores["dfs"] = scores.GetValueOrDefault("dfs") + 1;
// ... tương tự cho các thuật toán khác
result.algorithm = scores.OrderByDescending(x => x.Value).First().Key;
```

---

### 10. Thêm confidence score vào GraphExtractionResult

Cho phép caller biết extraction tin cậy hay không:

```csharp
public sealed class GraphExtractionResult
{
    // ... các field hiện tại ...
    public double confidence { get; set; } = 1.0; // 0.0 → 1.0
    public List<string> warnings { get; set; } = new(); // mô tả điểm không chắc
}
```

Ví dụ: nếu không tìm được tập đỉnh rõ ràng mà phải infer từ cạnh → `confidence -= 0.2`, `warnings.Add("Vertices inferred from edges")`.

---

## Tóm tắt thứ tự ưu tiên

| Thứ tự | Việc cần làm | Tác động |
|--------|-------------|----------|
| 1 | Fix `IsVertexToken` nhận A-Z | +10% ngay lập tức |
| 2 | Bỏ `GraphSolver.cs` | Giảm scope, tránh nhầm lẫn |
| 3 | Fix false positive Bellman | +5% độ chính xác |
| 4 | Tách `"den"` khỏi StopTokens | +3% detect start/end |
| 5 | Auto-detect weighted từ edges | +3% |
| 6 | Thêm adjacency list parser | +8% coverage dạng đề |
| 7 | Thêm ma trận kề parser | +5% coverage dạng đề |
| 8 | Scoring thay first-match | +5% chính xác thuật toán |
| 9 | Thêm confidence score | Dễ debug và test hơn |
