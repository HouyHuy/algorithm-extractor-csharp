# Quy Trình Trích Xuất Dữ Liệu Đồ Thị (Rule-Based)

Tài liệu này mô tả chi tiết các bước xử lý từ văn bản thô (Raw Input) sang định dạng JSON chuẩn, sử dụng hoàn toàn các luật (rules) và biểu thức chính quy (Regex).

## 1. Pipeline Xử Lý Dữ Liệu

Quy trình tuân theo mô hình tuyến tính sau:

```mermaid
graph TD
    A[Raw Input] --> B[Normalize Unicode (NFC)]
    B --> C[Lowercase]
    C --> D[Remove Vietnamese Diacritics]
    D --> E[Keyword Mapping]
    E --> F[Rule / Algorithm Extraction]
    F --> G[JSON Output]
```

### Chi tiết từng bước:

1.  **Raw Input**: Văn bản đầu vào chưa qua xử lý.
2.  **Normalize Unicode**: Chuẩn hóa chuỗi về dạng NFC để đảm bảo tính nhất quán của các ký tự Unicode (đặc biệt là tiếng Việt).
3.  **Lowercase**: Chuyển toàn bộ văn bản về chữ thường để đơn giản hóa việc so khớp.
4.  **Remove Vietnamese Diacritics**: Loại bỏ dấu tiếng Việt (ví dụ: "đồ thị" -> "do thi", "có hướng" -> "co huong"). Đây là bước quan trọng để giảm thiểu sai sót do gõ dấu khác nhau.
5.  **Keyword Mapping**: Ánh xạ các từ đồng nghĩa về một từ khóa chuẩn (ví dụ: "start", "bat dau", "xuat phat" -> "start_keyword").
6.  **Rule / Algorithm Extraction**: Áp dụng các luật Regex để trích xuất thông tin cấu trúc.

---

## 2. Logic Trích Xuất (Extraction Details)

Giai đoạn trích xuất (Step 6 ở trên) được chia nhỏ thành các bước logic sau:

### Step 1: Detect Requested Algorithm (Nhận diện thuật toán)
*   **Mục tiêu**: Xác định thuật toán người dùng muốn sử dụng.
*   **Luật**: Tìm kiếm các từ khóa trong danh sách cho phép.
*   **Danh sách**: `bfs`, `dfs`, `dijkstra`, `bellman-ford`, `hamilton`, `euler`, `prim`, `kruskal`.
*   **Priority**: Nếu tìm thấy, gán vào trường `algorithm`. Nếu không, để `null` hoặc mặc định.

### Step 2: Graph Type Detection (Xác định loại đồ thị)
*   **Mục tiêu**: Xác định đồ thị có hướng hay vô hướng.
*   **Luật**:
    *   Nếu chứa từ khóa "co huong" (sau khi bỏ dấu) hoặc "directed" -> `direction: "directed"`.
    *   Nếu chứa "->" trong danh sách cạnh -> Ưu tiên `directed`.
    *   Mặc định (nếu không tìm thấy dấu hiệu): `direction: "undirected"`.

### Step 3: Weight Handling (Xử lý trọng số)
*   **Mục tiêu**: Xác định đồ thị có trọng số hay không.
*   **Luật**:
    *   Nếu chứa từ khóa "khong trong so" hoặc "unweighted" -> `weighted: false`.
    *   Nếu chứa "co trong so" hoặc "weighted" -> `weighted: true`.
    *   Kiểm tra định dạng cạnh: Nếu cạnh có dạng `(u, v, w)` -> Tự động suy luận `weighted: true`.

### Step 4: Extract Explicit Nodes (Trích xuất tập đỉnh V)
*   **Pattern**: Tìm chuỗi khớp với mẫu `V = { ... }` hoặc `tap dinh ... { ... }`.
*   **Regex**: `[v|tap dinh]\s*=\s*\{([^}]+)\}`
*   **Xử lý**: Tách chuỗi bên trong `{}` bằng dấu phẩy, loại bỏ khoảng trắng thừa.

### Step 5: Extract Edges (Trích xuất tập cạnh E)
*   **Pattern**: Tìm chuỗi khớp với mẫu `E = { ... }` hoặc `cac canh ... { ... }`.
*   **Regex**: `[e|cac canh]\s*=\s*\{(.*)\}` (Lấy nội dung trong ngoặc nhọn của tập E).
*   **Phân tích từng cạnh**: Duyệt qua chuỗi kết quả, tìm các mẫu con:
    *   Dạng 1: `(A, B)` hoặc `(A, B, 5)`
    *   Dạng 2: `A-B` hoặc `A->B`
    *   Regex con: `\(([\w]+)\s*,\s*([\w]+)(?:\s*,\s*(\d+))?\)`
*   **Normalization**: Chuẩn hóa dấu phân cách `–`, `—`, `->` về dạng chuẩn.

### Step 6: Node Inference (Suy luận đỉnh)
*   **Logic**: Đôi khi tập V không được liệt kê đầy đủ hoặc bị thiếu.
*   **Hành động**: Duyệt qua tất cả các cạnh đã trích xuất. Nếu có đỉnh xuất hiện trong cạnh mà chưa có trong danh sách `vertices`, thêm nó vào.

### Step 7: Start / End Node Detection (Điểm bắt đầu / kết thúc)
*   **Start Vertex**: Tìm từ khóa "xuat phat tu", "bat dau tai", "start at". Lấy token ngay sau đó.
*   **End Vertex**: Tìm từ khóa "ket thuc tai", "den dinh", "end at".

### Step 8: Validation & Formatting
*   Loại bỏ các cạnh trùng lặp.
*   Sắp xếp danh sách đỉnh (nếu cần).
*   Đóng gói thành JSON.
