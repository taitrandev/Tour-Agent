
# SKILL: Concept Diagram

## Mục đích
Vẽ sơ đồ concept / flow / lifecycle kỹ thuật để **học và tái dựng**. Đúng cấu trúc > đẹp. Spec text là nguồn sự thật; mọi hình chỉ là render.

## Trigger
Người dùng yêu cầu vẽ / sơ đồ / flow / lifecycle / kiến trúc của một concept kỹ thuật.

## Input
- **Concept** + câu hỏi cần trả lời (vd: "MediatR — request đi qua đâu lúc runtime?")
- **Style** (tùy chọn): `ascii` (mặc định) | `mermaid` | `handdrawn`

---

## Quy trình — 5 bước, không bỏ bước, không skip confirm

### Bước 0 — Classify (tự làm, không hỏi)
| Loại | Concept trả lời câu hỏi | Render |
|---|---|---|
| Flow | Đi qua đâu, thứ tự nào? | flowchart / sequence |
| Structure | Cái gì chứa / phụ thuộc cái gì? | layered / component |
| Lifecycle | Trạng thái đổi khi nào? | state diagram |
| Comparison | A khác B ở đâu? | **Bảng**, không vẽ |
| Không có hình | Ràng buộc, nguyên tắc, trade-off | **Từ chối vẽ**, đề xuất 2 ví dụ cụ thể + 1 kịch bản fail |

Hai loại cuối → nói thẳng, dừng.

### Bước 1 — Clarify (tối đa 3 câu)
Chỉ hỏi cái không suy ra được. Còn lại tự đề xuất default, ghi "Giả định: ...".
Thứ tự ưu tiên:
1. **Scope**: điểm bắt đầu / kết thúc?
2. **Độ sâu**: thành phần chính, hay cả cơ chế bên trong?
3. **Góc nhìn**: runtime hay static (startup / thiết kế)?

Không hỏi về style, màu, bố cục, môi trường.

### Bước 2 — Spec & Confirm (dừng chờ "ok")
```
## Diagram Spec
Concept: ...
Loại: Flow | Structure | Lifecycle
Góc nhìn: runtime | static
Scope: từ [A] đến [B]
Môi trường (đoán): antigravity | gemini | sider   ← sửa nếu sai
Style render: ascii | mermaid | handdrawn (+ lý do nếu tự nâng cấp)

Thành phần (node):
1. [Tên] — vai trò 1 dòng
...

Quan hệ (edge):
- [A] → [B]: [nhãn / điều kiện]
...

Cố ý KHÔNG vẽ: ...
Giả định / không chắc (đánh ?): ...
```
Luôn chờ xác nhận. Không có ngoại lệ cho sơ đồ nhỏ.

### Bước 3 — Render
Vẽ **đúng spec đã duyệt**. Không thêm node/edge. Quy tắc từng style ở phần STYLES bên dưới.

### Bước 4 — Self-check (in ngắn)
```
Check:
- [ ] Đủ node theo spec, không thừa
- [ ] Edge đúng chiều, đúng nhãn
- [ ] Thành phần không chắc đã đánh ?
- [ ] Hình trả lời được câu hỏi ban đầu
Nên tự xác minh với docs/source: ...
```
Với ảnh generative: đọc lại ảnh, liệt kê **mọi sai lệch so với spec** (chữ sai, mũi tên sai, node thừa/thiếu). Không được bỏ qua bước này.

---

## Golden rules
1. Spec là nguồn sự thật. Cần dạng khác → render lại từ spec, **không dịch ngược từ hình**.
2. Không chắc → đánh `?`, không vẽ cho đủ.
3. Trần 10 node / sơ đồ. Quá → tách.
4. Không vẽ cái không có hình.
5. Generative image là lựa chọn cuối, chỉ khi không có đường deterministic.

---

# STYLES

## Quy tắc chọn style
- Mặc định **`ascii`**.
- **Tự nâng lên `mermaid`** khi có 1 trong: >8 node; có nhánh rẽ / vòng lặp; loại Lifecycle, Sequence, Structure nhiều tầng. Ghi lý do trong Spec để người dùng override.
- `handdrawn` chỉ khi người dùng yêu cầu.

## Năng lực môi trường
| Môi trường | Shell | Gen ảnh | Mermaid render trong UI |
|---|---|---|---|
| antigravity | có | không chắc | code block; ảnh qua `mmdc` |
| gemini | không | có | không (code block) |
| sider | không | tùy model | không chắc → coi là không |

## `ascii`
- Box: `+---+` / `|   |`; mũi tên `-->`, `<--`, `v`, `^`; nhãn edge đặt trên/cạnh mũi tên.
- Ưu tiên bố cục **một trục** (trái→phải hoặc trên→xuống). Có nhánh → nâng lên mermaid.
- Mỗi node 1 dòng tên + tối đa 1 dòng vai trò. Node không chắc: thêm `(?)` sau tên.
- Đặt trong code block để giữ monospace.

## `mermaid`
- Flow → `flowchart LR` (hoặc `TD` nếu >6 node); Sequence → `sequenceDiagram`; Lifecycle → `stateDiagram-v2`; Structure → `flowchart` với `subgraph` cho layer.
- Mọi node id có nhãn trong ngoặc kép: `A["Controller"]`. Nhãn edge: `-->|"label"|`.
- Node không chắc: thêm `?` vào nhãn và `classDef unsure stroke-dasharray: 5 5`.
- Trả code block ```mermaid. Nếu môi trường không render, nói 1 dòng: "dán vào mermaid.live để xem".
- **Antigravity**: sau khi trả code, hỏi "render ra PNG?" → `npx -p @mermaid-js/mermaid-cli mmdc -i diagram.mmd -o diagram.png`.

## `handdrawn`
**Đường A — deterministic (ưu tiên, khi có shell / Antigravity):**
- Sinh Mermaid như trên, thêm frontmatter:
  ```
  ---
  config:
    look: handDrawn
    theme: neutral
  ---
  ```
- Render bằng `mmdc` → PNG/SVG. Kết quả đúng 100% spec.
- *(Cần Mermaid ≥ v11; verify version mmdc lần đầu.)*

**Đường B — generative (Gemini / Sider có gen ảnh):**
- Prompt sinh **từ spec**, theo khung:
  ```
  Hand-drawn whiteboard sketch, black marker on white, clean and legible.
  Diagram type: [flow/structure/lifecycle], layout [left-to-right/top-down].
  Boxes (exact text, keep spelling): 1. "[node]" 2. "[node]" ...
  Arrows (exact): "[A]" -> "[B]" labeled "[label]"; ...
  Do NOT add any other boxes, arrows, icons, or text.
  Mark boxes [X] with a small "?" in the corner.
  ```
- Output = ảnh **+ prompt đã dùng** (để tái tạo ở model khác).
- Bắt buộc Self-check đối chiếu ảnh với spec, liệt kê sai lệch.

**Không có shell, không có gen ảnh** → trả Mermaid `look: handDrawn` + hướng dẫn dán vào mermaid.live / Excalidraw.
