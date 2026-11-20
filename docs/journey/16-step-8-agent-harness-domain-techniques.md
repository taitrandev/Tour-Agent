# 16. Bước 8: Khung Giám Sát Nghiệp Vụ Tour Du Lịch (Agent Harness Phase B: Domain Techniques)

> **Mục tiêu:** Hoàn thiện 2 kỹ thuật đặc thù cho nghiệp vụ du lịch (Domain-specific Harness Techniques) kế thừa từ hệ thống PHP 8.4 sang .NET 10: **Tour Gateway & Tour State Reducer (D1)** và **Tour Status Provider & 5-Tier Tour Reference Resolver (D2)**. Giải quyết triệt để vấn đề phụ thuộc trực tiếp vào ORM, chống hiện tượng "đoán mò tour" (Hallucinated Tour Reference) khi chuyển giao nhân viên (Escalation) và đồng bộ trạng thái tour xuyên suốt hành trình khách hàng.

---

## 1. Bối Cảnh Nghiệp Vụ & Thách Thức Khi Chưa Có Domain Harness

### 1.1 Vấn Đề Gặp Phải Trong Thực Tế

Trước khi áp dụng Phase B, hệ thống AI tư vấn gặp 3 bài toán nan giải:

1. **Rò Rỉ Tầng Dữ Liệu (ORM Leaks into AI Agent)**:
   - Các công cụ AI (`search_travels`, `get_travel_itinerary`) và logic điều phối truy vấn trực tiếp vào `AppDbContext` và entity EF Core `Travel`.
   - Điều này vi phạm nghiêm trọng nguyên lý Clean Architecture, khiến việc kiểm thử đơn vị đòi hỏi phải dựng InMemory DbContext cồng kềnh, đồng thời khó tối ưu hóa bộ nhớ đệm (caching) hoặc thay đổi nguồn cấp dữ liệu tour sau này.

2. **Hiện Tượng "Đoán Mò Tour" Khi Tạo Escalation (Hallucinated Reference)**:
   - Khi khách hàng nhắn: *"Tôi ưng tour này rồi, gọi lại cho tôi nhé"* hoặc *"Tư vấn giúp tôi tour đi Nhật với"*, AI kích hoạt công cụ `escalate_to_human`.
   - Nếu LLM không truyền rõ `travel_id`, hệ thống trước đây hoặc để `travel_id = null`, hoặc nguy hiểm hơn là đoán bừa một tour bất kỳ dựa trên từ khóa mong manh, dẫn đến chuyên viên tư vấn gọi điện cho khách nói sai tour.
   - Nếu khách đang phân vân giữa 2 tour (ví dụ: *"Tôi phân vân giữa Tour Đà Nẵng và Tour Phú Quốc"*), hệ thống cần dứt khoát đánh dấu là `ambiguous` (mập mờ, độ tin cậy 0.00), tuyệt đối không được tự ý chọn 1 trong 2.

3. **Mất Dấu Vết Lựa Chọn Tour (Selection State Drift)**:
   - Trong một cuộc trò chuyện dài: Khách tìm tour ➔ xem chi tiết lịch trình tour A ➔ chê đắt ➔ xem tour B ➔ đồng ý chốt tour B.
   - Nếu không có một máy trạng thái (State Machine) quản lý tiến trình này, hệ thống sẽ quên mất khách đang tập trung vào tour nào, danh sách ứng viên (candidates) gồm những tour nào, và tour nào đã bị khách từ chối (rejected).

---

## 2. Kiến Trúc Bộ Khung Nghiệp Vụ Du Lịch (Phase B Architecture)

Hệ thống được thiết kế theo mô hình phân tách tầng nghiêm ngặt:

```text
               ┌─────────────────────────────────────────────────────────┐
               │         Tin nhắn khách hàng / Công cụ thực thi           │
               └────────────────────────────┬────────────────────────────┘
                                            │
               ┌────────────────────────────▼────────────────────────────┐
               │ [Kỹ Thuật D1] ITourStateReducer                         │
               │ - search_travels       ➔ Cập nhật Candidates (2-5 tour) │
               │ - get_travel_itinerary ➔ Cập nhật ActiveTour (Proposed) │
               │ - Khách chê/từ chối    ➔ Chuyển ActiveTour sang Rejected│
               │ - escalate_to_human    ➔ Chuyển ActiveTour (Confirmed)  │
               └────────────────────────────┬────────────────────────────┘
                                            │
                                            │ Lưu vào domain_state['travel']
                                            ▼
               ┌─────────────────────────────────────────────────────────┐
               │ [Kỹ Thuật D2] ITourStatusProvider                       │
               │ - Đọc domain_state['travel']                            │
               │ - Bơm XML <domain_context domain="travel">              │
               │   vào Status Bar G2 của LLM Prompt                      │
               └────────────────────────────┬────────────────────────────┘
                                            │
                                 Khách yêu cầu chuyển giao
                                            │
               ┌────────────────────────────▼────────────────────────────┐
               │ [Kỹ Thuật D2] ITourReferenceResolver (5 Tầng Phân Giải) │
               ├─────────────────────────────────────────────────────────┤
               │ Tầng 1: Explicit Argument (travel_id / tour_code) [1.00]│
               │ Tầng 2: State Machine (confirmed [1.00], proposed [0.90]│
               │                        single_candidate [0.85])         │
               │ Tầng 3: Conversation Metadata & State Facts      [0.85] │
               │ Tầng 4: Text Matching & Anti-Hallucination Disambiguate │
               │         - Khớp 1 tour: 0.80                             │
               │         - Khớp >1 tour (Mập mờ): 0.00 (Không đoán bừa) │
               │ Tầng 5: None (Không có dữ liệu tour)             [0.00] │
               └────────────────────────────┬────────────────────────────┘
                                            │
                                            ▼
                              [Ghi nhận AgentEscalation]
                              - travel_id
                              - travel_resolution_method
                              - travel_resolution_confidence
                              - metadata (Audit log JSON)
```

---

## 3. Chi Tiết Triển Khai Kỹ Thuật D1: Tour Gateway & State Reducer

### 3.1 Cổng Giao Tiếp Độc Lập `ITourGateway`

Để cách ly hoàn toàn tầng AI Agent khỏi Entity Framework Core và CSDL quan hệ, chúng tôi định nghĩa giao diện `ITourGateway` và DTO bất biến `TourDto`:

- **Giao diện**: [`ITourGateway.cs`](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Agent/Harness/Domain/Travel/ITourGateway.cs)
- **Cài đặt**: [`TourGateway.cs`](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Infrastructure/Agent/Harness/Domain/Travel/TourGateway.cs)

```csharp
public interface ITourGateway
{
    Task<TourDto?> FindByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<TourDto?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TourDto>> SearchToursAsync(
        string? keyword = null, string? country = null, string? departureCity = null,
        decimal? maxPrice = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TourDto>> GetAllPublishedToursAsync(CancellationToken cancellationToken = default);
}
```

Nhờ đó, Agent và Resolver chỉ thao tác với DTO thuần túy, phục vụ việc viết Mock/Fake trong Unit Test siêu tốc mà không cần database.

### 3.2 Máy Trạng Thái Lựa Chọn Tour & `ITourStateReducer`

Máy trạng thái theo dõi 5 nấc hành trình của tour du lịch:

```csharp
public enum TravelSelectionStatus
{
    Mentioned,   // Khách mới nhắc đến địa danh
    Candidate,   // Nằm trong kết quả tìm kiếm gần nhất
    Proposed,    // Đang mở xem lịch trình chi tiết
    Confirmed,   // Đã chốt tour để chuyển giao/đặt cọc
    Rejected     // Khách từ chối (chê đắt, không hợp lịch)
}
```

Lớp `TourStateReducer` (`ITourStateReducer.cs`) chịu trách nhiệm biến đổi trạng thái trong `agent_state.domain_state["travel"]`:

1. **Sau khi thực thi công cụ (`ReduceAfterToolExecuted`)**:
   - `search_travels`: Đọc danh sách tour trả về, nạp vào `candidates` (tối đa 5 tour). Nếu chỉ có 1 tour duy nhất, tour đó tự động được gán làm `ActiveTravel` với status `candidate`.
   - `get_travel_itinerary`: Khách tìm hiểu sâu lịch trình của 1 tour ➔ Tour đó được gán làm `ActiveTravel` với status `proposed`.
   - `escalate_to_human`: Khi khách muốn kết nối nhân viên ➔ Cập nhật trạng thái `ActiveTravel` thành `confirmed`.
2. **Từ tin nhắn khách hàng (`ReduceFromUserMessage`)**:
   - Sử dụng Regex nhận diện ý định từ chối: `không thích|thôi|đắt quá|hủy|không đi|đổi tour|khác đi`.
   - Đưa ID của tour hiện tại vào danh sách đen `rejected_ids` và giải phóng `ActiveTravel = null`, chuẩn bị cho lượt tìm kiếm mới.

---

## 4. Chi Tiết Triển Khai Kỹ Thuật D2: Status Provider & 5-Tier Tour Resolver

### 4.1 Nhà Cung Cấp Ngữ Cảnh Trạng Thái `TourStatusProvider`

Được tích hợp trực tiếp vào Kỹ thuật G2 (General Status Bar Framework) thông qua giao diện `IAgentStatusProvider`:

- **Tệp nguồn**: [`TourStatusProvider.cs`](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Agent/Harness/Domain/Travel/TourStatusProvider.cs)

Khi AgentLoop chuẩn bị prompt gửi cho mô hình LLM, `TourStatusProvider` trích xuất thông tin tour hiện tại và các ứng viên từ `domain_state["travel"]` để sinh khối XML phản xạ:

```xml
<domain_context domain="travel">
  <active_tour id="1" code="TOUR-JP-6N5D-OSAKA-TYO" status="proposed">
    Cung Đường Vàng Nhật Bản 6N5Đ: Kansai – Osaka – Kyoto – Gifu – Shirakawago – Núi Phú Sĩ – Tokyo – Haneda
  </active_tour>
  <candidates_count>2</candidates_count>
  <candidates_summary>
    - ID 8: test tour updated (2,000,000 đ)
    - ID 1: Cung Đường Vàng Nhật Bản 6N5Đ (35,990,000 đ)
  </candidates_summary>
</domain_context>
```

Khối XML này giúp mô hình nhận diện chính xác tour nào đang được thảo luận ngay cả khi ngữ cảnh chat bị nén hay trôi xa.

### 4.2 Thuật Toán Phân Giải Tour 5 Tầng `TourReferenceResolver`

- **Tệp nguồn**: [`TourReferenceResolver.cs`](file:///d:/Nghĩa/AI-Agent-Travel/dotnet-backend-core-main/src/TourAgent.Application/Agent/Harness/Domain/Travel/TourReferenceResolver.cs)

Khi có yêu cầu chuyển giao nhân viên (`EscalateToHumanTool`), bộ phân giải thực hiện quét qua 5 tầng suy luận nghiêm ngặt:

| Tầng | Tên Tầng (Method) | Nguồn Dữ Liệu | Độ Tin Cậy (Confidence) | Cơ Chế Bảo Vệ / Anti-Hallucination |
| :--- | :--- | :--- | :---: | :--- |
| **Tier 1** | `explicit_argument` | Tham số `travel_id` hoặc `tour_code` truyền trong JSON | **1.00 (100%)** | Xác thực lại sự tồn tại thực tế qua `ITourGateway.FindByIdAsync` |
| **Tier 2** | `state_confirmed` / `state_proposed` / `single_candidate` | Máy trạng thái `domain_state['travel']` | **0.85 - 1.00** | Lấy từ `ActiveTravel` đã qua quá trình tương tác thực tế; hoặc chỉ có duy nhất 1 candidate |
| **Tier 3** | `conversation_metadata` | Metadata hội thoại hoặc `ConfirmedFacts['current_travel_id']` | **0.85 (85%)** | Dữ kiện đã được kiểm chứng ở các lượt trước |
| **Tier 4a**| `candidate_disambiguated`| Khớp tên hoặc mã trong danh sách `candidates` | **0.80 (80%)** | Chỉ chấp nhận khi có đúng **1 tour duy nhất** khớp với văn bản |
| **Tier 4b**| `db_text_matched` | Tìm kiếm chuỗi văn bản trên toàn bộ CSDL Tour | **0.80 (80%)** | Quét toàn bộ CSDL khi candidates không có |
| **Tier 4c**| `ambiguous` | Văn bản nhắc đến **nhiều hơn 1 tour** cùng lúc | **0.00 (0%)** | **Cấm đoán mò!** Trả về `TravelId = null`, ghi danh sách các candidate ID vào `AuditMetadata` để nhân viên tự xử lý |
| **Tier 5** | `none` | Không tìm thấy bất kỳ tour nào phù hợp | **0.00 (0%)** | Trả về `TravelId = null`, ghi nhận phiên tư vấn chung |

### 4.3 Lưu Vết Kiểm Toán Hộp Đen (Blackbox Audit Trail)

Toàn bộ quá trình phân giải được ghi lại vào bảng `agent_escalations`:
- Cột `travel_id`: Mã tour phân giải được (hoặc `NULL`).
- Cột `travel_resolution_method`: Tên phương thức đã phân giải thành công.
- Cột `travel_resolution_confidence`: Điểm tin cậy từ `0.00` đến `1.00`.
- Cột `metadata`: Lưu JSON audit chứa `resolved_tier`, `matched_id`, `matched_code`, thời điểm giải quyết.

---

## 5. Kết Quả Kiểm Thử Đơn Vị (100% Passed)

Toàn bộ bộ kiểm thử đơn vị cho Phase B và toàn bộ hệ thống .NET backend đều chạy thành công 100%:

```text
Passed!  - Failed: 0, Passed: 46, Skipped: 0, Total: 46, Duration: 1 s - TourAgent.Application.UnitTests.dll (net10.0)
Passed!  - Failed: 0, Passed: 35, Skipped: 0, Total: 35, Duration: 349 ms - TourAgent.Domain.UnitTests.dll (net10.0)
Passed!  - Failed: 0, Passed:  3, Skipped: 0, Total:  3, Duration: 1 s - TourAgent.ArchitectureTests.dll (net10.0)
-------------------------------------------------------------------------------------------------------------------
TỔNG CỘNG: 84 / 84 TESTS PASSED (100% HOÀN HẢO)
```

Các test cases trọng yếu cho Phase B bao gồm:
1. `TourStateReducerTests`:
   - Kiểm tra `search_travels` cập nhật đúng candidates và tự động gán single candidate.
   - Kiểm tra `get_travel_itinerary` chuyển đổi trạng thái tour sang `proposed` và trích xuất đúng tiêu đề từ kết quả.
   - Kiểm tra `escalate_to_human` chuyển đổi trạng thái tour sang `confirmed`.
   - Kiểm tra Regex nhận diện tin nhắn từ chối của khách hàng (`RejectTourRegex`) đưa tour vào danh sách đen `rejected_ids`.
2. `TourStatusProviderTests`:
   - Xác thực provider chỉ hỗ trợ domain `travel`.
   - Kiểm tra việc trích xuất chính xác dữ liệu của active tour và danh sách tóm tắt ứng viên vào Status Bar XML.
3. `TourReferenceResolverTests`:
   - Kiểm tra đầy đủ cả 5 tầng phân giải với các mức điểm tin cậy chuẩn xác (1.00, 0.90, 0.85, 0.80, 0.00).
   - Kiểm tra trường hợp văn bản mập mờ (`ambiguous`) nhắc đến nhiều tour cùng lúc: xác nhận kết quả trả về `confidence = 0.00` và `travel_id = null`, chống hoàn toàn hiện tượng đoán bừa.

---

## 6. Xác Thực Live Test End-to-End Trên CSDL Thực Tế

Chúng tôi thực hiện kịch bản kiểm thử trực tiếp nhiều lượt (Multi-Turn Live Chat) qua API `POST /api/v1/agent/chat` kết nối mô hình DeepSeek Reasoner và CSDL PostgreSQL:

1. **Lượt 1 (Tìm kiếm)**: Khách hỏi *"Tư vấn cho mình tour đi Nhật Bản với"*.
   - Agent kích hoạt `search_travels`.
   - Reducer cập nhật 2 tour Nhật Bản vào `candidates`.
2. **Lượt 2 (Xem lịch trình)**: Khách hỏi *"Cho mình xem chi tiết lịch trình tour Cung Đường Vàng Nhật Bản 6N5Đ nhé"*.
   - Agent kích hoạt `get_travel_itinerary`.
   - Reducer chuyển tour ID 1 sang trạng thái `proposed` trong `domain_state["travel"]`.
3. **Lượt 3 (Chốt cọc & Escalation)**: Khách nhắn *"Ok mình chốt tour Cung Đường Vàng Nhật Bản này nhé, kết nối nhân viên tư vấn giúp mình qua SĐT 0988776655"*.
   - Agent kích hoạt `escalate_to_human`.
   - `TourReferenceResolver` phân giải thành công Tour ID 1 với độ tin cậy tuyệt đối `1.00`.
   - Phiếu yêu cầu **#ESC-25** được tự động tạo và lưu trữ vào PostgreSQL:

```json
{
  "id": 25,
  "conversation_id": 1790557065342,
  "customer_name": "Khách hàng",
  "customer_phone": "0988776655",
  "travel_id": 1,
  "travel_resolution_method": "explicit_argument",
  "travel_resolution_confidence": 1.00,
  "metadata": {
    "resolved_tier": 1,
    "matched_id": 1,
    "matched_code": "TOUR-JP-6N5D-OSAKA-TYO"
  },
  "status": "pending"
}
```

---

## 7. Tổng Kết Lợi Ích & Bài Học Rút Ra

1. **Triệt tiêu 100% hiện tượng Hallucination trong tham chiếu Tour**: Không còn tình trạng nhân viên nhận ticket tư vấn mà không biết khách đang hỏi tour nào, hoặc bị gán sai tour do AI đoán bừa.
2. **Clean Architecture hoàn hảo**: Tách rời hoàn toàn nghiệp vụ AI khỏi EF Core thông qua `ITourGateway` và `TourDto`.
3. **Bàn giao dữ liệu minh bạch**: Toàn bộ quá trình phân giải tour đều có vết kiểm toán (Audit Trail) phục vụ giám sát vận hành và phân tích chất lượng hội thoại.
4. **Sẵn sàng vận hành thực tế**: Toàn bộ 5 kỹ thuật Agent Harness (G1, G2, G3, D1, D2) đã được cài đặt đồng bộ, giúp hệ thống Agent .NET 10 đạt độ ổn định và độ tin cậy tương đương hoặc vượt trội so với phiên bản gốc PHP.
