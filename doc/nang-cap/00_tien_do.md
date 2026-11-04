# BẢNG THEO DÕI TIẾN ĐỘ NÂNG CẤP HỆ THỐNG
## Dự án: Omnichannel Beauty Solution (.NET 8 Clean Architecture)

> **Mục tiêu:** Nâng cấp hệ thống bán lẻ mỹ phẩm đa kênh chuẩn ngành (Sephora, Olive Young, Hasaki), tối ưu cho kịch bản thuyết trình và bảo vệ đồ án 10 phút.  
> **Quy ước trạng thái:**  
> - ⬜ Chưa làm  
> - 🔄 Đang làm  
> - ✅ Xong  
> - ⏭️ Bỏ qua  

---

## 1. BẢNG TIẾN ĐỘ TỔNG THỂ CÁC PHIÊN

| Giai Đoạn | Phiên | Tên Phiên & Mục Tiêu Nghiệp Vụ | Loại Phiên | Trạng Thái | Ngày Hoàn Thành | Ghi Chú & Kết Quả |
| :--- | :---: | :--- | :---: | :---: | :---: | :--- |
| **Khởi động** | **Prompt 0** | Thiết lập bối cảnh, 4 vai trò & 10 Guardrails bắt buộc | Thiết lập | ✅ Xong | 04/11/2026 | Xác nhận 5 nguyên tắc cốt lõi |
| **Giai đoạn A: Khảo sát & Phân tích** | **A1** | Kiểm kê kiến trúc & Bản đồ chức năng hiện có | Chỉ đọc | ✅ Xong | 04/11/2026 | Quét 48 Controller, 12 nhóm G1-G12, phát hiện nợ kỹ thuật |
| | **A2** | Kiểm tra sức khỏe runtime (Smoke Test toàn bộ route) | Chỉ đọc + App | ✅ Xong | 04/11/2026 | Đo 41 route, test 5 luồng L1-L5, 233 automated test pass |
| | **A3** | Benchmark đối thủ & Ma trận khoảng trống (Gap Matrix) | Phân tích | ✅ Xong | 04/11/2026 | Đối chuẩn 8 đối thủ, đúc kết 6 USPs & 12 GAPs |
| | **A4** | Chấm điểm ưu tiên & Lộ trình nâng cấp hướng demo | Phân tích | ✅ Xong | 04/11/2026 | Lập lộ trình MoSCoW, kịch bản demo 10p, 1-migration |
| **Giai đoạn B: Ổn định nền tảng** | **B1** | Vá lỗi Blocker/P0 phát hiện ở A1–A2 | Code | ✅ Xong | 04/11/2026 | Fix timeout 30s, bật lại PBAC, khóa backdoor, fix cart form-data, test tăng lên 238/238 |
| | **B2** | Bộ dữ liệu demo chân thực & Nút Reset Demo Data | Code | ✅ Xong | 04/11/2026 | Seed 155 đơn 90 ngày, 210 review, 41 khách hàng (10 sinh nhật T11), voucher demo, nút 1-click Reset, test tăng 241/241 |
| **Giai đoạn C: Tính năng Storefront** | **C1** | Tìm kiếm tức thì (Instant Search Autocomplete) | Code | ✅ Xong | 04/11/2026 | Dropdown gợi ý tức thì chuẩn Sephora/Hasaki, latency < 15ms, bỏ dấu tiếng Việt, bàn phím ↑↓, lịch sử localStorage, test tăng 253/253 |
| | **C2** | Sản phẩm đã xem gần đây & "Thường mua cùng" | Code | ✅ Xong | 04/11/2026 | FBT co-occurrence + Skincare Routine synergy, bundle math, 1-click add bundle, Recently Viewed (PDP, Cart rỗng, Home), test tăng 257/257 |
| | **C3** | Khay so sánh mỹ phẩm chuyên sâu (VND/ml, EWG, FEFO)| Code | ✅ Xong | 04/11/2026 | Đối chiếu Side-by-Side 2-4 SP, Đơn giá/ml, Điểm EWG, Tương kỵ da liễu, test tăng 269/269 |
| | **C4** | Hỏi đáp sản phẩm (Q&A) & Đánh giá có ảnh / Skin Twin| Code | ✅ Xong | 04/11/2026 | Q&A chuyên gia, UGC Photo Swatch, Bộ lọc Skin Twin, Chống spam vote, test tăng 282/282 |
| | **C5** | Thông báo Có hàng lại / Giảm giá | Code | ✅ Xong | 04/11/2026 | Back-in-Stock & Price Drop Alert, SignalR chuông realtime, Trang Quản lý Theo dõi, test tăng 291/291 |
| | **C6** | Theo dõi đơn hàng Timeline & Hủy/Đổi trả tự phục vụ | Code | ✅ Xong | 04/11/2026 | Timeline dọc 6 bước + lô FEFO, Tra cứu vãng lai `/tra-cuu-don-hang`, Hủy đơn hoàn tồn FEFO, Đổi trả 7 ngày + SignalR, test tăng 296/296 |
| | **C7** | Gamification Loyalty: Điểm danh, Mã giới thiệu | Code | ✅ Xong | 04/11/2026 | Lịch điểm danh 7 ngày (UTC+7) + Confetti, Mã giới thiệu chống vòng lặp, Nhiệm vụ làm đẹp, Omni Rewards Bazaar đổi voucher dùng ngay tại Checkout, test tăng 300/300 |
| | **C8** | Thử màu son theo 4 tông da Châu Á & Undertone | Code | ✅ Xong | 05/11/2026 | Virtual Lip Try-On Studio 4 tông da + Split-Screen + Camera Fallback & AI Skin Analysis `/phan-tich-da`, test tăng 303/303 |
| **Giai đoạn D: Đa kênh & Quản trị** | **D1** | Click & Collect (Nhận tại Shop 1h) + Mã PIN tại POS | Code | ✅ Xong | 05/11/2026 | Checkout 2 Tab (Freeship 0₫ + Giữ lô FEFO 48h), Vé QR + Mã PIN 6 số có cấu trúc, Cổng bàn giao POS + Store Isolation + SignalR, test tăng 307/307 |
| | **D2** | Dashboard phân tích kinh doanh (Funnel, AOV, RFM) | Code | ✅ Xong | 05/11/2026 | Trung tâm ra quyết định (Executive Decision Center): 6 KPI có ▲▼% so sánh kỳ trước, 100% SQL thật loại bỏ hardcode, Heatmap Giờ×Thứ, Phễu chuyển đổi 5 bước, Phân khúc RFM 1-5 kèm Xuất CSV & 1-Click Tạo Voucher Win-back, Cảnh báo điều chuyển kho thông minh, Store Isolation & IMemoryCache 5p, test tăng 311/311 |
| **Giai đoạn E: Hoàn thiện & Demo** | **E1** | Demo Presenter HUD (Bảng điều khiển thuyết trình) | Code | ✅ Xong | 05/11/2026 | HUD ẩn 1-Click mở bằng Alt+D, đồng hồ đếm ngược 10p, 6 chặng kịch bản Persona "Lê Ngọc Linh", chuyển vai nhanh (Khách VIP, Thu ngân POS, SuperAdmin) không cần gõ mật khẩu, nạp giỏ hàng demo, 1-Click Reset Demo Data, test tăng 317/317 |
| | **E2** | Nghiệm thu toàn hệ thống & Tài liệu thuyết trình | QA + Tài liệu | ✅ Xong | 05/11/2026 | Build Release 0 Warning 0 Error, 317/317 tests pass 100%, Smoke test 64 route 0 lỗi 500, TTFB < 50ms, Chạy 2 chu kỳ demo liên tiếp ổn định, hoàn thiện Kịch bản demo 10p, Bộ 16 câu hỏi phản biện, và Báo cáo nâng cấp 05_bao_cao_ket_qua_nang_cap.md |
| **Giai đoạn F – Hình ảnh & Rebrand** | **F1** | Kiểm kê hình ảnh toàn hệ thống (Read-only) | Chỉ đọc | ✅ Xong | 05/11/2026 | Quét 258 file (138 SHA-256) trên 3 thư mục A/B/C, kiểm tra trực quan 100% ảnh, phát hiện 42 ảnh Unsplash giả ở B gây lỗi hiển thị (kể cả Bath & Body Works → Chanel Coco Mademoiselle) & 63 `MediaAssets` bị ghi đè, lập báo cáo `06_kiem_ke_hinh_anh.md` |
| | **F2** | Hợp nhất ảnh về `wwwroot\images\products` & Xóa thư mục trùng lặp | Code + DB | ✅ Xong | 05/11/2026 | Copy ảnh Hada Labo đúng dòng vào `products/skincare/`, cập nhật toàn bộ tham chiếu sang `/images/products/{sub}/{file}`, cập nhật DB (`98` Products, `75` MediaAssets, `1` OrderReturnRequests), xóa `real_products` & `Real_Products_Images` (`0` match `git grep`), Build 0 Error & 317/317 tests pass |
| | **F3** | Đồng bộ hình ảnh – dữ liệu & Tối ưu hiển thị (`IProductImageResolver`) | Code + Tối ưu | 🔄 Chờ duyệt DB/Xóa | 05/11/2026 | Tạo Single Source of Truth `IProductImageResolver` + ảnh fallback `/images/products/_placeholder.webp` tông hồng có logo mờ, chuẩn hóa 84 ảnh SP 1:1 `1000x1000px` `<= 150KB` + xuất 84 bản `.webp`, tối ưu `<picture>`/`lazy`/`fetchpriority`/CLS/SEO Alt, Build 0 Error & 317/317 tests pass |

---

## 2. NHẬT KÝ CHI TIẾT TỪNG PHIÊN

### Phiên A1: Kiểm kê Kiến trúc & Bản đồ Chức năng Hiện có (04/11/2026)
- **Đã làm:** Quét toàn diện 48 Controller (18 Storefront + 30 Admin), 35 Service, toàn bộ Domain Entities, đối chiếu 12 phiên cũ tại `prompt_sua_loi_theo_phien.md`, phân loại 12 nhóm G1–G12.
- **Phát hiện quan trọng:** 
  + Đã triển khai xong Phiên cũ 1.1 (xóa batch giả `99991`), 1.2 (bảng thành phần động), 2.1 (động cơ tương kỵ da liễu AI).
  + Lỗ hổng Blocker: `PermissionManagementController` bị comment mất `[Authorize]`; nhiều trang Admin gắn `[AllowAnonymous]`.
- **Tài liệu sinh ra:** `doc/nang-cap/01_ban_do_chuc_nang.md`.

### Phiên A2: Kiểm tra Sức khỏe Runtime (Smoke Test) (04/11/2026)
- **Đã làm:** Khởi chạy ứng dụng Web trên port 5116, thực hiện smoke test trên 41 route, kiểm thử 5 luồng nghiệp vụ trọng yếu (L1-L5), chạy `dotnet test`.
- **Kết quả nghiệm thu:**
  + `dotnet build`: 0 Error, 0 Warning.
  + `dotnet test`: 233/233 tests PASSED 100% (11s).
  + Khách & Superadmin đăng nhập thành công. Storefront & PDP nạp bình thường (200 OK).
  + Lỗi P0 phát hiện: SQL Timeout (30s) trên AuditLogs (90.362 dòng) khi worker chạy ngầm; `/Admin/Warehouse` nạp quá chậm (25,5s).
- **Tài liệu sinh ra:** `doc/nang-cap/02_suc_khoe_he_thong.md`.

### Phiên A3: Benchmark Đối thủ & Ma trận Khoảng trống (04/11/2026)
- **Đã làm:** So sánh năng lực hệ thống với 8 đối thủ tiêu biểu (Sephora, Ulta, Olive Young, Paula's Choice, The Ordinary, Hasaki, Watsons, Shopee Mall).
- **Đúc kết cốt lõi:**
  + Xác lập **6 Lợi thế khác biệt độc quyền (USPs)**: FEFO Clearance cận date minh bạch, Skincare Interaction Engine da liễu AI, Routine Quiz 1-click, Visual CMS Theme Customizer, Unified O2O Web-POS-Warehouse, Clean Architecture + 233 Tests.
  + Nhận diện **12 Khoảng trống năng lực (GAP-01 đến GAP-12)** làm cơ sở chọn lọc cho Giai đoạn C, D, E.
- **Tài liệu sinh ra:** `doc/nang-cap/03_ma_tran_canh_tranh.md`.


### Phiên A4: Chấm Điểm Ưu Tiên & Lộ Trình Nâng Cấp Hướng Demo (04/11/2026)
- **Đã làm:** Chấm điểm định lượng 12 GAP theo công thức MoSCoW ($0.35D + 0.25B + 0.15T + 0.15E + 0.10R$). Lập chiến lược gom toàn bộ thuộc tính CSDL mới vào 1 migration duy nhất. Xây dựng kịch bản persona 10 phút "Lê Ngọc Linh".
- **Tài liệu sinh ra:** `doc/nang-cap/04_lo_trinh_nang_cap.md`.

### Phiên B1: Vá Toàn Bộ Lỗi P0 / Blocker Từ Báo Cáo A1–A2 (04/11/2026)
- **Đã làm:**
  1. Khắc phục SQL Execution Timeout (30s) trên AuditLogs (90.362 dòng) và Review (gộp 4 câu lệnh count thành 1 câu lệnh GroupBy tối ưu).
  2. Bật lại phân quyền PBAC trên `PermissionManagementController` (`[Authorize(Roles = "SuperAdmin,SUPER_ADMIN")]` và `[HasPermission("PERMISSION_MANAGE")]`).
  3. Xóa bỏ toàn bộ cửa sau `[AllowAnonymous]` trên POS (`Index`, `GetVisualCatalog`, `SearchProducts`, `ScanBarcode`, `LookupCustomer`, `GetShiftStatus`), `WarehouseController`, `OrderController`, `MenuController`.
  4. Tối ưu thời gian tải trang `/Admin/Warehouse` từ 25.499ms xuống **749ms** (tăng tốc 34x) nhờ projection và giới hạn `Take(200)`.
  5. Sửa lỗi định dạng API `/api/cart/add`: Bổ sung `ReadJsonBodyAsync()`, hỗ trợ mượt mà cả JSON lẫn form urlencoded.
  6. Xóa bỏ `[IgnoreAntiforgeryToken]` trên API giỏ hàng và quiz; cấu hình tự động truyền CSRF token header trong `site.js` (HTMX hook) và `RoutineQuiz`.
  7. Loại bỏ phụ thuộc Mixkit CDN ngoài trên PDP; bổ sung fallback an toàn.
  8. Xóa bỏ hardcode `* 1.25m` tạo giá gốc giả trên live search, PDP và QuickView.
  9. Viết mới 5 automated unit tests trong `Omnichannel.Tests`.
- **Kết quả nghiệm thu:**
  + `dotnet build OmnichannelBeautySolution.sln`: **0 Error, 0 Warning**.
  + `dotnet test`: **238 / 238 tests PASSED (100%)** trong 11s (tăng 5 test so với ban đầu).
  + Toàn bộ 8 lỗi P0 được kiểm chứng thực tế bằng script tự động.
- **Tài liệu cập nhật:** `doc/nang-cap/02_suc_khoe_he_thong.md`.

### Phiên B2: Chuẩn Hóa Dữ Liệu Demo & Cơ Chế Khôi Phục Dữ Liệu Trực Quan (04/11/2026)
- **Đã làm:**
  1. **Chuẩn hóa bộ số liệu Demo hoàn chỉnh (Zero-Migration, Idempotent):**
     - **98 sản phẩm active** gắn ảnh thực tế 1-1, thương hiệu, danh mục, bảng thành phần đầy đủ.
     - **516 lô hàng FEFO** (trong đó **209 lô cận date** dưới 90 ngày) sẵn sàng phục vụ trang `/ClearanceFefo`.
     - **155 đơn hàng trải đều 90 ngày** (61% Website / 39% POS), phân bổ doanh thu ~486 triệu VNĐ có đỉnh bán cuối tuần và ngày lương, đa dạng trạng thái (87% Completed, 7% Processing, 4% Pending, 2% Cancelled).
     - **210 đánh giá sản phẩm tự nhiên bằng tiếng Việt**: Đầy đủ phân phối số sao (76% 5 sao, 19% 4 sao, 5% 3 sao), đầy đủ tiêu chí phụ da liễu (loại da, nhóm tuổi, mùi hương, độ bền, cảm giác da) và 73 đánh giá (35%) có phản hồi chính thức từ Chuyên viên CSKH.
     - **41 khách hàng & ví Loyalty**: Trải dài từ Bronze đến Diamond, trong đó có đúng **10 khách hàng có ngày sinh nhật rơi vào tháng hiện tại (Tháng 11)** để phục vụ thuyết trình tính năng Birthday Bonus.
     - **3 mã Voucher Demo dễ nhớ, không hết hạn**: `DEMO10` (giảm 10%), `FREESHIP` (miễn phí ship 30k), `COMBO15` (giảm 15% trọn bộ Routine).
  2. **Xây dựng Cơ chế Khôi phục Dữ liệu Demo (1-Click Reset State):**
     - Áp dụng kiến trúc Clean Architecture: DTOs (`DemoResetResultDto`), Interface (`IDemoResetService`), Implementation (`DemoResetService`), Controller Action (`SettingsController.ResetDemoData`).
     - Chiến lược Zero-Migration: Phân biệt đơn seed chuẩn bằng tiền tố `ORD-SEED-`. Khi khôi phục: tự động phát hiện và xóa toàn bộ đơn diễn tập (`!ORD-SEED-*`), hoàn nguyên tồn kho vật lý cho các lô FEFO, dọn sạch giỏ hàng tạm, bảo toàn 100% dữ liệu gốc và ghi Audit Log.
     - Giao diện Admin: Tích hợp nút và thẻ chuyên biệt tại `/Admin/Settings` kèm hộp thoại xác nhận 2 bước SweetAlert2 (bảo vệ bằng cấu hình `"DemoMode:Enabled": true` và quyền `SuperAdmin`).
  3. **Kiểm thử tự động & Tài liệu:**
     - Viết mới 3 automated unit tests trong `Omnichannel.Tests/Services/DemoResetServiceTests.cs`.
     - Cập nhật đầy đủ tài khoản mới, mã voucher và hướng dẫn reset tại `TEST_ACCOUNTS.md`.
- **Kết quả nghiệm thu:**
  + `dotnet build OmnichannelBeautySolution.sln`: **0 Error, 0 Warning**.
  + `dotnet test`: **241 / 241 tests PASSED (100%)** (tăng thêm 3 tests so với Phiên B1).
  + Kiểm thử thực tế: Tạo đơn thử nghiệm và giỏ hàng test $\rightarrow$ Bấm Reset Demo Data $\rightarrow$ Dọn sạch 12 đơn diễn tập, 22 giỏ hàng, hoàn nguyên 49 sản phẩm về kho, số đơn trở về 155 đơn chuẩn trong < 1 giây.
- **Tài liệu cập nhật:** `TEST_ACCOUNTS.md`, `doc/nang-cap/00_tien_do.md`.

### Phiên C1: Xây Dựng Ô Tìm Kiếm Tức Thì Chuẩn Sephora / Hasaki (04/11/2026)
- **Đã làm:**
  1. **Tầng Application & Domain:**
     - Thiết kế bộ DTOs: `SearchSuggestionDto`, `SearchProductSuggestionDto`, `SearchBrandSuggestionDto`, `SearchCategorySuggestionDto`.
     - Khai báo Interface `ISearchSuggestService` phục vụ tìm kiếm gợi ý tức thì.
  2. **Tầng Infrastructure (`SearchSuggestService`):**
     - Thuật toán bóc tách dấu tiếng Việt đa năng (`RemoveDiacritics`): chuẩn hóa `FormD`, khử dấu thanh, xử lý triệt để ký tự đặc thù `đ`/`Đ` thành `d`/`D`. Cho phép gõ `"kem chong nang"` tìm chuẩn `"Kem Chống Nắng"`, `"la roche"` tìm `"La Roche-Posay"`.
     - Cơ chế Snapshot In-Memory Cache (`IMemoryCache`, TTL 5 phút, `.AsNoTracking()`): gom toàn bộ Catalog sản phẩm, danh mục, thương hiệu và số lượng sản phẩm. Tối ưu thời gian phản hồi đạt **4ms - 15ms** (vượt xa yêu cầu < 150ms).
     - Thuật toán chấm điểm và xếp hạng (Scoring Algorithm): Khớp đầu chuỗi (Prefix match - score 100) > Khớp từ độc lập (Word boundary match - score 80) > Khớp chuỗi con (Substring match - score 50), kết hợp điểm thưởng cho thương hiệu (+45), danh mục (+35), mã SKU (+40), thành phần hoạt chất (+25), sản phẩm nổi bật (`IsFeatured` +10) và đang giảm giá (+5).
     - Bộ từ khóa thịnh hành (Trending/Hot Keywords) sẵn có và fallback thông minh khi chưa có kết quả.
  3. **Tầng Web & API Controller (`SearchApiController`):**
     - Endpoint: `GET /api/search/suggest?q=...` với giới hạn độ dài `Math.Clamp` (2-50 ký tự), lọc bỏ mã HTML/Script tags ngăn ngừa 100% XSS.
  4. **Tầng Giao Diện Người Dùng (Alpine.js Storefront Header & Mobile):**
     - Tích hợp Alpine.js component `instantSearch()` tại `_Header.cshtml` với debounce 250ms.
     - Hỗ trợ lưu trữ Lịch sử tìm kiếm gần đây (`localStorage`, tối đa 5 từ khóa, có nút xóa từng mục và nút "Xóa tất cả").
     - Hỗ trợ Từ khóa nổi bật (Hot Keywords) kèm biểu tượng ngọn lửa 🔥 click vào tìm ngay.
     - Điều hướng bàn phím chuẩn Desktop: phím Mũi tên Lên/Xuống (<kbd>↑</kbd> <kbd>↓</kbd>) để di chuyển chọn kết quả, phím <kbd>Enter</kbd> để truy cập ngay sản phẩm/danh mục/thương hiệu hoặc submit form, phím <kbd>Esc</kbd> để đóng nhanh.
     - Đánh dấu từ khóa khớp (Highlight match): Tự động bao bọc các ký tự khớp bằng thẻ `<mark class="bg-pink-100 text-magenta-accent font-bold px-0.5 rounded">` sau khi escape an toàn.
     - Trải nghiệm Mobile hoàn chỉnh: Nút kính lúp chuyên dụng trên mobile header mở khay tìm kiếm tức thì trượt xuống mượt mà, đồng bộ dữ liệu và tương tác cảm ứng.
  5. **Kiểm thử tự động & Nghiệm thu:**
     - Viết mới **12 Unit Tests** trong `Omnichannel.Tests/Services/SearchSuggestServiceTests.cs` (kiểm tra chuẩn hóa dấu, tìm kiếm tiếng Việt không dấu, xếp hạng ưu tiên, tìm theo thành phần da liễu, an toàn XSS).
     - Kiểm thử Runtime trực tiếp trên máy chủ đang chạy:
       + Latency đo được: **15ms** (truy vấn ấm **4ms - 7ms**).
       + XSS injection `<script>alert(1)</script>` được lọc sạch thành `alert(1)`.
       + Trang chủ Storefront nạp 200 OK với đầy đủ thành phần instant search.
- **Kết quả nghiệm thu:**
  + `dotnet build OmnichannelBeautySolution.sln`: **0 Error, 0 Warning**.
  + `dotnet test`: **253 / 253 tests PASSED (100%)** (tăng thêm 12 test so với Phiên B2).
- **Tài liệu cập nhật:** `doc/nang-cap/00_tien_do.md`.

### Phiên C2: Tăng Cross-Sell Với Recently Viewed & Frequently Bought Together (04/11/2026)
- **Đã làm:**
  1. **Tầng Application & Domain:**
     - Thiết kế bộ DTOs: `RecentlyViewedProductDto`, `FrequentlyBoughtTogetherDto`, `FbtProductItemDto` tại [`RecommendationDtos.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/Omnichannel.Application/DTOs/Catalog/RecommendationDtos.cs).
     - Khai báo Interface `IRecommendationService` với các phương thức `GetFrequentlyBoughtTogetherAsync` và `GetProductsByIdsAsync`.
  2. **Tầng Infrastructure (`RecommendationService`):**
     - **Thuật toán Đồng xuất hiện (Co-occurrence on Completed Orders):** Phân tích các đơn hàng hoàn tất (`OrderStatus.Completed`), đếm số đơn hàng đồng thời chứa cặp sản phẩm $A$ và $B$ ($Count(A \cap B)$), tính toán chỉ số tin cậy **Confidence** ($Count(A \cap B) / Count(A)$) và chỉ số đòn bẩy **Lift** ($\frac{Count(A \cap B) \times N}{Count(A) \times Count(B)}$). Chỉ đề xuất sản phẩm khi Lift $\ge 1.0$ hoặc tần suất mua kèm $\ge 2$ đơn.
     - **Thuật toán Bổ trợ Chu trình Skincare (Skincare Routine Synergy Fallback):** Khi thiếu lịch sử đơn hàng hoặc Lift thấp, tự động chuyển đổi sang gợi ý bước kế tiếp trong quy trình dưỡng da chuẩn da liễu: Sữa rửa mặt (`1_Cleanser`) $\rightarrow$ Nước hoa hồng (`2_Toner`) $\rightarrow$ Kem dưỡng ẩm (`4_Moisturizer`) $\rightarrow$ Serum (`3_Serum`) $\rightarrow$ Kem chống nắng (`5_Sunscreen`).
     - **Bộ nhớ đệm thông minh (`IMemoryCache`):** Lưu trữ kết quả phân tích combo trong 30 phút, tối ưu hóa tốc độ truy xuất.
  3. **Tầng Web Controllers & APIs:**
     - `ProductApiController`: Cung cấp 2 API RESTful:
       * `GET /api/products/by-ids?ids=1,2,3`: Trả về danh sách sản phẩm rút gọn, bảo toàn thứ tự ID đầu vào và tự động loại bỏ sản phẩm không hoạt động.
       * `GET /api/products/{id}/frequently-bought-together`: Trả về combo FBT kèm phân tích giá và độ tin cậy.
     - `CartController`: Bổ sung endpoint `POST /api/cart/add-bundle` (và `/cart/add-bundle`) cho phép thêm 1 lần toàn bộ combo sản phẩm vào giỏ hàng trong 1 request duy nhất.
     - `ProductController`: Tích hợp nạp trước dữ liệu FBT trong action `Detail()` (Server-Side Rendering không giật lag).
  4. **Tầng Giao Diện Storefront (PDP, Cart rỗng, Home):**
     - **Khối "Thường được mua cùng" (Frequently Bought Together) trên PDP:**
       * Phương trình mua sắm trực quan: $[SP\ Hiện\ tại] + [SP\ 1] + [SP\ 2] = [Tổng\ tiền\ Combo]$.
       * Checkbox linh hoạt cho từng sản phẩm gợi ý (sản phẩm hiện tại bị khóa cố định).
       * Tự động tính toán tổng tiền realtime, hiển thị số tiền tiết kiệm và nhãn uy tín ("Dựa trên N đơn hàng" hoặc "Chu trình chuẩn da liễu").
       * Nút 1-click "Thêm Cả N Sản Phẩm Vào Giỏ" tích hợp hiệu ứng xoay spinner, thông báo SweetAlert2 và tự động mở Mini-Cart drawer.
     - **Dải "Sản phẩm bạn đã xem gần đây" (Recently Viewed Products):**
       * Tự động ghi nhận ID sản phẩm vào `localStorage` (`'omni_recently_viewed_ids'`, tối đa 12 sản phẩm).
       * Hiển thị dạng Carousel/Grid sang trọng tại cuối trang chi tiết sản phẩm (PDP).
       * Hiển thị tại khối Giỏ hàng rỗng (`Cart/Index.cshtml`) giúp giữ chân và chuyển đổi khách hàng tiềm năng.
       * Hiển thị tại Trang chủ (`Home/Index.cshtml`) phía trên footer.
       * Hỗ trợ nút xóa lịch sử xem và nút 1-click "Thêm nhanh vào giỏ".
  5. **Kiểm thử tự động & Nghiệm thu Runtime:**
     - Viết mới **4 Unit Tests** tại `Omnichannel.Tests/Services/RecommendationServiceTests.cs` (kiểm tra tính toán Confidence/Lift, kiểm tra fallback routine skincare, kiểm tra bảo toàn thứ tự ID và lọc sản phẩm inactive).
     - Kiểm thử Runtime trực tiếp:
       + API `by-ids`: trả về đúng 3 sản phẩm theo thứ tự yêu cầu.
       + API `frequently-bought-together`: phân tích chính xác combo với tỷ lệ chiết khấu và lời giải thích.
       + API `add-bundle`: thêm thành công cả 2 sản phẩm vào giỏ hàng chỉ trong 1 request.
       + Trang chủ, giỏ hàng rỗng và PDP đều nạp 200 OK với đầy đủ khối giao diện mới.
- **Kết quả nghiệm thu:**
  + `dotnet build OmnichannelBeautySolution.sln`: **0 Error, 0 Warning**.
  + `dotnet test`: **257 / 257 tests PASSED (100%)** (tăng thêm 4 test so với Phiên C1).
- **Tài liệu cập nhật:** `doc/nang-cap/00_tien_do.md`.

### Phiên C3: Khay So Sánh Mỹ Phẩm Chuyên Sâu (Compare Drawer & Side-by-Side Matrix) (04/11/2026)
- **Đã làm:**
  1. **Tầng Application & Domain:**
     - Thiết kế DTOs chuyên biệt: `CompareItemSummaryDto`, `ComparisonActiveIngredientDto`, `ProductComparisonDetailDto`, `ProductComparisonMatrixDto` tại `ProductComparisonDtos.cs`.
     - Khai báo Interface `IProductComparisonService` gồm: `GetCompareQuickItemsAsync`, `GetDetailedComparisonAsync`, `GetSuggestedAlternativesAsync`.
  2. **Tầng Infrastructure (`ProductComparisonService`):**
     - **Trích xuất Thể Tích / Dung Tích Thông Minh:** Thuật toán Regex đa dạng `(?:\s|^|[(\[-])(\d+(?:[.,]\d+)?)\s*(ml|g|gram|gr|kg|l)\b` bóc tách chính xác thể tích và đơn vị (ml hoặc g), quy đổi chuẩn `1L = 1000ml`, `1.5kg = 1500g` và fallback theo đơn vị mỹ phẩm chuẩn (Thỏi -> 3.5g, Chai -> 50ml, Tuýp -> 50g, Hộp -> 50g).
     - **Tính Toán Đơn Giá Trên Thể Tích (Unit Price Ratio - VNĐ/ml hoặc VNĐ/g):** Đo lường tính kinh tế học mỹ phẩm, tự động nhóm và gắn nhãn vương miện **"Best Value / Tiết Kiệm Nhất"** cho sản phẩm có giá trên ml/g tối ưu nhất nhóm.
     - **Độ An Toàn EWG Skin Deep & Tiêu Chuẩn 5 KHÔNG:** Đánh giá điểm EWG (1-2 Xanh lá, 3-4 Vàng/Cam, 5+ Đỏ), tích hợp cờ Không Cồn Khô, Không Hương Liệu Nhân Tạo, An Toàn Cho Thai Kỳ.
     - **Tích Hợp Động Cơ Tương Kỵ & Hiệp Đồng Dược Mỹ Phẩm AI:** Tự động gọi `ISkincareIngredientInteractionEngine` phát hiện nguy cơ quá tải bong sừng khi dùng chung (Retinol vs BHA/AHA) để khuyến nghị Skin Cycling, hoặc chúc mừng các cặp hiệp đồng vàng (B5 + HA).
     - **Minh Bạch Hạn Dùng FEFO & PAO Mở Nắp:** Hiển thị số ngày còn lại của lô hàng gần nhất, trạng thái date (Date Mới, Date Ổn Định, Cận Date) và hạn dùng sau khi mở nắp (PAO 6M, 12M).
  3. **Tầng Web Controllers & Routing:**
     - `CompareController`: Cung cấp action `Index` tại các route `/so-sanh`, `/so-sanh-my-pham` (và `/Compare`), các endpoint `/api/compare/quick-items`, `/api/compare/matrix`, `/api/compare/suggested` và Partial Modal `/compare/modal-partial`.
  4. **Tầng Giao Diện Người Dùng (Storefront UI & Alpine.js):**
     - **Floating Compare Drawer (`_CompareDrawer.cshtml`):** Khay cố định đáy màn hình hiển thị 4 slots (slot có ảnh/giá/nút xóa, slot trống dấu cộng), nút thu nhỏ dạng pill góc phải dưới, nút "Xóa hết", nút "Xem nhanh Modal", nút "So Sánh Chi Tiết", quản lý state bằng Alpine.js + `localStorage` (`'omni_compare_product_ids'`).
     - **Nút So Sánh Trên Thẻ Sản Phẩm (`_ProductCard.cshtml`):** Icon cán cân `fa-scale-balanced` góc trên thumbnail container và cụm nút hành động nhanh.
     - **Nút So Sánh Trên PDP (`Detail.cshtml`):** Nút cán cân cạnh nút Yêu Thích và nút full-width "So Sánh Sản Phẩm Này (Giá/ml, EWG, FEFO)" dưới nút mua hàng.
     - **Trang Ma Trận So Sánh Toàn Diện (`Compare/Index.cshtml`):** Bảng đối chiếu Side-by-Side 9 hàng tiêu chí, khối khuyến nghị da liễu AI, nút "Thêm Cả N Sản Phẩm Vào Giỏ", khối gợi ý sản phẩm thay thế cùng loại nếu chưa đủ 4 SP.
     - **Popup Modal Quick Compare (`_CompareMatrixModalContent.cshtml`):** Xem bảng so sánh tức thì ngay trên trang hiện tại không mất context.
  5. **Kiểm thử tự động & Nghiệm thu Runtime:**
     - Viết mới **11 Unit Tests** tại `Omnichannel.Tests/Services/ProductComparisonServiceTests.cs` (kiểm tra Regex dung tích, tính đơn giá ml/g, đánh dấu Best Value, query bảo toàn thứ tự, phát hiện tương kỵ da liễu).
     - Kiểm thử Runtime trực tiếp:
       + Trang `/so-sanh`: Status 200 OK.
       + Trang `/so-sanh?ids=1,6,15`: Status 200 OK, hiển thị đầy đủ bảng ma trận side-by-side và badge Best Value.
       + API `/api/compare/quick-items?ids=1,6,15`: Status 200 OK, trả về JSON chuẩn xác.
       + Partial `/compare/modal-partial?ids=1,6,15`: Status 200 OK.
       + Trang PDP `/Product/Detail/6`: Status 200 OK, tích hợp nút so sánh mượt mà.
- **Kết quả nghiệm thu:**
  + `dotnet build OmnichannelBeautySolution.sln`: **0 Error, 0 Warning**.
  + `dotnet test`: **269 / 269 tests PASSED (100%)** (tăng thêm 12 test so với Phiên C2).
- **Tài liệu cập nhật:** `doc/nang-cap/00_tien_do.md`.

### Phiên C4: Hỏi đáp sản phẩm (Q&A) & Đánh giá có ảnh / Bộ lọc "Skin Twin" (04/11/2026)
- **Mục tiêu:** Xây dựng hệ sinh thái nội dung cộng đồng (UGC) trên trang sản phẩm chuẩn Sephora (Q&A, Helpful Votes), Olive Young (Customer Real Photo Swatch Gallery) và Hasaki (Dược sĩ giải đáp thắc mắc chính thức).
- **Đã làm:**
  1. **Domain & Database Schema:**
     - Thêm 3 Entity: `ProductQuestion`, `ProductAnswer`, `ReviewHelpfulVote` tại `Omnichannel.Domain/Entities/CmsEntities.cs` và cấu hình Unique Index chống spam vote (`IX_ReviewHelpfulVotes_Review_User`, `IX_ReviewHelpfulVotes_Review_AnonymousKey`) trong `ApplicationDbContext.cs`.
     - Áp dụng Migration `20261104140534_AddProductQnAAndHelpfulVotes` và seed idempotent 3 cặp câu hỏi - câu trả lời chính thức của Dược sĩ Omni Beauty.
  2. **Application & Infrastructure:**
     - Tạo `IProductQnaService`, `ProductQnaService` (chống XSS bằng `HtmlEncoder`, ưu tiên câu trả lời chính thức `IsOfficial` lên đầu).
     - Mở rộng `IProductReviewService`, `ProductReviewService` hỗ trợ lọc theo loại da **Skin Twin** (`IsSkinTwinMatch`), sắp xếp `helpful` / `newest` / `highest`, chống vote trùng lặp (`VoteHelpfulWithCheckAsync`) và trích xuất bộ sưu tập ảnh thật (`GetCustomerReviewImagesAsync`).
  3. **Web Controllers & Bảo mật Upload:**
     - `QnaController`: API `GET /api/qna/product/{id}`, `POST /api/qna/ask` (bắt buộc đăng nhập, gửi thông báo SignalR realtime tới group `Managers`).
     - `ReviewController`: Kiểm tra chữ ký nhị phân **Magic Bytes** (`IsValidImageFile`) chặn đứng tệp `.exe` (`0x4D, 0x5A`) giả mạo `.jpg`, giới hạn 2MB/ảnh.
     - Giao diện PDP `_ReviewSection.cshtml` và Admin `Areas/Admin/Views/Review/Index.cshtml`.
- **Kết quả nghiệm thu:**
  + `dotnet build`: **0 Error, 0 Warning**.
  + `dotnet test`: **282 / 282 tests PASSED (100%)** (+13 unit tests mới tại `ProductQnaAndReviewEnhancementTests.cs`).

### Phiên C5: Thông báo Có hàng lại & Giảm giá (Back-in-Stock & Price Drop Alerts) (04/11/2026)
- **Mục tiêu:** Giữ chân khách hàng khi sản phẩm hết hàng hoặc chờ giảm giá (Sephora "Notify Me When Available" & Shopee "Thông báo khi giảm giá"), kết hợp thông báo chuông Realtime qua SignalR khi Admin nhập kho hoặc giảm giá (GAP-05).
- **Đã làm:**
  1. **Domain & Database Schema:**
     - Thêm Entity `ProductSubscription` (`ProductId`, `UserId`, `Type`: `BACK_IN_STOCK` / `PRICE_DROP`, `TargetPrice`, `CustomerName`, `Email`, `PhoneNumber`, `Status`: `ACTIVE` / `NOTIFIED` / `CANCELLED`, `CreatedAt`, `NotifiedAt`, `Notes`) tại `Omnichannel.Domain/Entities/WishlistEntities.cs`.
     - Áp dụng Migration `20261104143949_AddProductSubscriptions` và seed idempotent dữ liệu mẫu trong `DbInitializer.cs`.
  2. **Application & Infrastructure (`InventoryNotificationService`):**
     - Tạo `ProductSubscriptionDtos.cs`, `IInventoryNotificationService.cs` và `InventoryNotificationService.cs`.
     - Chống đăng ký trùng lặp (idempotent), kiểm tra `TargetPrice < CurrentPrice`, chỉ gửi thông báo **1 lần duy nhất** mỗi đợt về hàng hoặc đợt giảm giá (`Status = "NOTIFIED"`).
     - Tự động tạo `Notification` trong DB và bắn **SignalR Realtime** qua `INotificationService` tới `Group("User_{UserId}")` (chuông thông báo khách hàng nhảy số +1 và hiện toast tức thì) kèm ghi log `[MOCK EMAIL DISPATCH]`.
  3. **Tích hợp Triggers Tự Động (Admin Warehouse & Catalog):**
     - `Areas/Admin/Controllers/WarehouseController.cs` (`ImportBatch`): Tự động gọi `TriggerBackInStockAlertsAsync(product.Id, model.Quantity)` ngay khi nhập lô hàng mới.
     - `Omnichannel.Infrastructure/Services/Catalog/ProductService.cs` (`UpdateProductAsync`): Tự động gọi `TriggerPriceDropAlertsAsync(product.Id, newEffectivePrice, oldEffectivePrice)` khi giá bán/giá khuyến mãi giảm xuống chạm hoặc thấp hơn `TargetPrice`.
  4. **Giao diện Storefront PDP & Customer Portal:**
     - PDP (`Views/Product/Detail.cshtml`): Khi `availableStock <= 0` tự động ẩn nút Mua và hiển thị nút nổi bật **"🔔 Báo Tôi Khi Có Hàng Trở Lại"**; khi `availableStock > 0` hiển thị nút **"📉 Theo Dõi Giảm Giá"** kèm 2 Modal chọn nhanh mức giảm (-5%, -10%, -15%).
     - Customer Portal (`Views/Customer/Subscriptions.cshtml` & `CustomerController.Subscriptions`): Tab **"Sản Phẩm Theo Dõi"** cho phép lọc theo loại cảnh báo, xem trạng thái (`Đang theo dõi` / `Đã gửi thông báo`) và hủy theo dõi (`POST /api/subscription/unsubscribe/{id}`).
- **Kết quả nghiệm thu:**
  + `dotnet build OmnichannelBeautySolution.sln`: **0 Error, 0 Warning**.
  + `dotnet test`: **291 / 291 tests PASSED (100%)** (+9 unit tests mới tại `ProductSubscriptionTests.cs`).
  + Runtime Smoke Test (`localhost:5116`): PDP 200 OK, API Back-in-Stock & Price Drop 200 OK, chống trùng lặp và chặn khách vãng lai thiếu thông tin liên hệ 400 OK.
- **Tài liệu cập nhật:** `doc/nang-cap/00_tien_do.md`.

### Phiên C6: Minh bạch hậu mua hàng – Order Tracking Timeline & Return Request (04/11/2026)
- **Mục tiêu:** Xây dựng hệ thống minh bạch hậu mua hàng chuẩn Hasaki / Watsons / Shopee: Timeline dọc 6 bước hiển thị thời gian thực & mã lô FEFO cấp phát, trang tra cứu đơn cho khách vãng lai (`/tra-cuu-don-hang`), khách tự hủy đơn khi chờ xác nhận (hoàn tồn đúng lô FEFO trong transaction), và quy trình đổi/trả hàng trong 7 ngày có thông báo SignalR realtime (GAP-07).
- **Đã làm:**
  1. **Domain & Database Schema:**
     - Thêm 2 Entity: `OrderStatusHistory` (`OrderId`, `Status`, `Title`, `Description`, `TrackingCode`, `CarrierName`, `ChangedByUserId`, `ChangedByName`, `CreatedAt`) và `OrderReturnRequest` (`OrderId`, `UserId`, `RequestCode`, `Reason`, `Description`, `EvidenceUrlsJson`, `Status`, `AdminNote`, `ProcessedByUserId`, `CreatedAt`, `ProcessedAt`) tại `Omnichannel.Domain/Entities/SalesEntities.cs`.
     - Áp dụng Migration `20261104160655_AddOrderStatusHistoryAndReturnRequests` và seed idempotent 234 mốc lịch sử trạng thái cho 40 đơn gần nhất + 1 yêu cầu đổi trả mẫu tại `DbInitializer.cs`.
  2. **Application & Infrastructure (`OrderManagementService` & `OrderWorkflowService`):**
     - Nâng cấp `CreateOrderWithSoftReserveAsync`: Khởi tạo đơn mới ở trạng thái `PENDING` (Chờ xác nhận) và tự động ghi mốc `OrderStatusHistory` đầu tiên kèm danh sách lô FEFO khóa tạm.
     - Nâng cấp `CancelOrderAndReleaseReserveAsync`: Chạy trong `CreateExecutionStrategy().ExecuteAsync` + Transaction, hoàn trả chính xác `ReservedQuantity` / `PhysicalQuantity` về đúng lô `ProductBatchId` ban đầu, ghi `InventoryLog` (`ORDER_CANCEL_RESTORE`) và `OrderStatusHistory` (`CANCELLED`).
     - Nâng cấp `UpdateOrderStatusAsync`: Tự động ghi lịch sử trạng thái khi Admin chuyển sang `CONFIRMED`, `PROCESSING` (kèm mã lô FEFO), `SHIPPING` (kèm sinh mã vận đơn GHN thực tế cho cả 2 mốc `HANDED_OVER` & `SHIPPING`) và `COMPLETED`.
     - Cài đặt `GetCustomerOrderDetailAsync` (kiểm tra quyền sở hữu chống IDOR), `LookupGuestOrderAsync` (chống lộ thông tin khi sai SĐT, che một phần SĐT/địa chỉ khi hiển thị vãng lai), `CancelOrderByCustomerAsync` (chỉ cho hủy khi `PENDING`), `SubmitReturnRequestAsync` (kiểm tra hạn 7 ngày sau khi giao) và `ProcessReturnRequestAsync` (Admin duyệt/từ chối + hoàn tồn kho FEFO + bắn SignalR Realtime cho khách).
  3. **Web Controllers & Views:**
     - `CustomerController`: Vá lỗ hổng IDOR trên `OrderDetail` (trả về `403 Forbidden` nếu Khách A truy cập đơn Khách B), thêm `/tra-cuu-don-hang` (`TrackOrder`), `POST /Customer/CancelOrder`, `POST /Customer/SubmitReturnRequest` (kiểm tra Magic Bytes ảnh ≤ 2MB) và `GET /Customer/GetOrderStatusJson/{id}` (phục vụ cập nhật realtime).
     - `Views/Customer/OrderDetail.cshtml`: Nâng cấp Timeline dọc 6 bước hiển thị thời gian thực, lô FEFO cấp phát tại Bước 3, mã vận đơn GHN tại Bước 4 & 5, Modal Hủy đơn, Modal Đổi/Trả 7 ngày và cơ chế tự động đồng bộ trạng thái mỗi 5 giây.
     - `Views/Customer/TrackOrder.cshtml`: Giao diện tra cứu đơn hàng không cần đăng nhập tại `/tra-cuu-don-hang` (được gắn link trực tiếp trên thanh Header).
     - `Areas/Admin/Views/Order/Details.cshtml` & `Areas/Admin/Controllers/OrderController.cs`: Bổ sung bảng xử lý **Yêu cầu Đổi/Trả hàng** (Duyệt hoàn kho / Từ chối) và **Nhật ký lịch sử trạng thái thực tế**.
- **Kết quả nghiệm thu:**
  + `dotnet build OmnichannelBeautySolution.sln`: **0 Error, 0 Warning**.
  + `dotnet test`: **296 / 296 tests PASSED (100%)** (+5 unit tests chuyên sâu tại `OrderTrackingAndReturnTests.cs`).
  + Runtime Smoke Test (`localhost:5116`): `/tra-cuu-don-hang` 200 OK, tra cứu sai SĐT chặn lộ thông tin, tra cứu đúng hiển thị đủ Timeline 6 bước & lô FEFO, kiểm tra Anti-IDOR trả về 403 Forbidden.
- **Tài liệu cập nhật:** `doc/nang-cap/00_tien_do.md`.

### Phiên C7: Tăng tương tác thành viên bằng Gamification (04/11/2026)
- **Mục tiêu:** Triển khai hệ sinh thái Gamification Loyalty chuẩn Shopee Xu / Sephora Beauty Insider / Hasaki: Điểm danh hằng ngày 7 ngày (`10–10–20–20–30–30–100`) theo múi giờ Việt Nam (`UTC+7`), Chương trình Giới thiệu bạn bè chống tự giới thiệu & vòng lặp, Nhiệm vụ làm đẹp (Beauty Missions), và Quầy đổi điểm lấy Voucher dùng ngay tại Checkout (Omni Rewards Bazaar) gắn chặt với Sổ cái giao dịch điểm (`PointTransactions`) (GAP-08).
- **Đã làm:**
  1. **Domain & Database Schema:**
     - Mở rộng `LoyaltyWallet` (`ReferralCode`, `CurrentStreak`, `LastCheckInDate`) và thêm 2 Entity mới: `LoyaltyCheckIn` (Unique Index trên `(UserId, CheckInDate)`) và `LoyaltyReferral` (Unique Index trên `ReferredUserId`) tại `Omnichannel.Domain/Entities/LoyaltyEntities.cs`.
     - Áp dụng Migration `20261104163831_AddLoyaltyGamificationEntities` và seed idempotent `EnsureLoyaltyGamificationSeededAsync` tại `DbInitializer.cs`.
  2. **Application & Infrastructure (`LoyaltyService` & `PromotionEngine`):**
     - `CheckInDailyAsync` & `GetDailyCheckInStatusAsync`: Tính ngày theo múi giờ Việt Nam (`UTC+7`), tăng chuỗi 7 ngày (`10, 10, 20, 20, 30, 30, 100` điểm), reset về Ngày 1 khi bỏ ngày, chặn điểm danh 2 lần/ngày, ghi bút toán `CHECKIN` vào `PointTransactions`.
     - `ApplyReferralCodeAsync` & `ProcessReferralFirstOrderCompletedAsync`: Cấp mã `OMNI-REF-{UserId:D4}`, chặn tự giới thiệu (`A -> A`) và giới thiệu vòng lặp hai chiều (`A <-> B`), tự động thưởng `+100` điểm cho người giới thiệu và `+50` điểm + Voucher `50.000₫` cho người mới khi hoàn tất đơn đầu tiên.
     - `GetBeautyMissionsAsync` & `ClaimMissionRewardAsync`: 3 nhiệm vụ thực tế (*Hoàn thành Skin Quiz AI +30 điểm*, *Viết đánh giá có ảnh +50 điểm*, *Mua đơn hàng đầu tiên +100 điểm*), chống nhận thưởng trùng lặp.
     - Đồng bộ `RedeemPointsAsync` và `PromotionEngine.ValidateCouponDetailAsync` / `ApplyCouponUsageAsync` để mọi voucher đổi từ điểm thưởng trên Omni Rewards Bazaar áp dụng được ngay lập tức tại `/Checkout`.
  3. **Web Controllers & Giao diện Omni Rewards (`Customer/Dashboard.cshtml`):**
     - Nâng cấp `CustomerController.Dashboard` hiển thị số dư điểm thực tế từ ví và thêm 4 endpoints: `CheckInDaily`, `RedeemReward`, `ApplyReferralCode`, `ClaimMission`.
     - Bổ sung trung tâm **Omni Rewards Gamification** trên `Views/Customer/Dashboard.cshtml` với hiệu ứng pháo giấy Confetti, cập nhật tức thì số dư điểm, thanh tiến độ hạng, danh sách Kho Voucher (kèm nút **"Dùng ngay"** tại `/Checkout`) và Sổ cái biến động điểm.
- **Kết quả nghiệm thu:**
  + `dotnet build OmnichannelBeautySolution.sln`: **0 Error, 0 Warning**.
  + `dotnet test`: **300 / 300 tests PASSED (100%)** (+4 unit tests chuyên sâu tại `LoyaltyGamificationTests.cs`: tính chuỗi qua nửa đêm `UTC+7`, mất chuỗi khi bỏ ngày, chặn tự giới thiệu & giới thiệu vòng lặp, và bất biến cân bằng sổ cái `PointTransactions`).
  + Runtime Smoke Test (`localhost:5116`): `/Customer/Dashboard` 200 OK, điểm danh lần 1 nhận `+10` điểm, điểm danh lần 2 trong ngày bị chặn, tự giới thiệu bị chặn, đổi 50 điểm nhận ngay Voucher giảm `50.000₫` dùng tại Checkout.
- **Tài liệu cập nhật:** `doc/nang-cap/00_tien_do.md`.


### Phiên D1: Khép Kín Vòng Lặp O2O – Đặt Online, Nhận Tại Showroom (Click & Collect) (05/11/2026)
- **Đã làm:**
  1. **Domain & Database Schema (`AddClickAndCollectOrderFields`):**
     - Mở rộng thực thể [`Order`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/Omnichannel.Domain/Entities/SalesEntities.cs) bổ sung 5 cột dữ liệu có cấu trúc (`DeliveryMethod`, `PickupStoreId`, `PickupCode`, `PickupExpiresAt`, `PickedUpAt`) kèm Filtered Index trên `PickupCode` và Composite Index `(DeliveryMethod, PickupStoreId, OrderStatus)` — tuyệt đối không nhồi mã PIN vào chuỗi `ShippingAddress`.
     - Tạo và áp dụng thành công Migration `20261104172250_AddClickAndCollectOrderFields` lên SQL Server.
  2. **Application & Infrastructure (`OrderWorkflowService.cs`):**
     - Bổ sung `GetStorePickupAvailabilityForCartAsync`: Kiểm tra tồn kho khả dụng (`PhysicalQuantity - ReservedQuantity`) theo lô FEFO tại từng Showroom cho **toàn bộ giỏ hàng**, trả về trạng thái `HasFullCartStock`, mã lô FEFO gần nhất, giờ mở cửa và link Google Maps.
     - Nâng cấp `CreateOrderWithSoftReserveAsync`: Khi `DeliveryMethod == "STORE_PICKUP"`, tự động tính `ShippingFee = 0`, cấp phát và khóa giữ hàng (`ReservedQuantity`) trực tiếp tại Showroom `PickupStoreId` trong 48 giờ, sinh mã PIN 6 số (`PickupCode`), và bắn thông báo SignalR theo `StoreId` (Store Isolation) tới màn hình thu ngân POS của đúng Showroom đó.
     - Bổ sung `HandoverPickupOrderAtPosAsync`: Xác thực Store Isolation (nhân viên cửa hàng khác bị từ chối), xác thực mã PIN 6 số (`PickupCode`), đảm bảo **tồn kho vật lý chỉ trừ đúng 1 lần duy nhất**, ghi nhận doanh thu & giao dịch vào ca làm việc POS (`PosShift`), chuyển trạng thái đơn sang `COMPLETED`, cộng điểm thưởng Loyalty và đẩy thông báo SignalR cho khách hàng.
     - Bổ sung `ReleaseExpiredPickupOrdersAsync`: Tự động hủy giữ hàng và hoàn trả `ReservedQuantity` về đúng lô FEFO cho các đơn Click & Collect quá 48h chưa nhận.
  3. **Web & POS UI (`Views/Checkout/Index.cshtml`, `OrderSuccess.cshtml`, `Areas/Admin/Views/Pos/Index.cshtml` & `_PosPickupOrdersModal.cshtml`):**
     - Thiết kế 2 Tab **"Giao tận nơi"** và **"Nhận tại cửa hàng (Click & Collect • 0₫)"** trên trang `/Checkout`.
     - Hiển thị **"Vé Nhận Hàng Ưu Tiên Tại Showroom (Click & Collect Pass)"** trên `/Checkout/OrderSuccess` với QR Code vẽ trực tiếp phía client, Mã PIN 6 số nổi bật, địa chỉ Showroom, nút mở Google Maps và cơ chế tự động cập nhật trạng thái khi thu ngân POS bấm Bàn giao.
     - Tích hợp nút **"Đơn nhận tại quầy (`n`)"** và Modal xác thực mã PIN / bàn giao hàng / chạy job hết hạn 48h ngay trên màn hình `/Admin/Pos`.
- **Kết quả nghiệm thu:**
  + `dotnet build OmnichannelBeautySolution.sln`: **0 Error, 0 Warning**.
  + `dotnet test`: **307 / 307 tests PASSED (100%)** (+4 unit tests chuyên sâu tại [`ClickAndCollectO2OTests.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/Omnichannel.Tests/Services/ClickAndCollectO2OTests.cs)).
- **Tài liệu cập nhật:** `doc/nang-cap/00_tien_do.md`.

### Phiên D2: Nâng Cấp Admin Dashboard Thành Trung Tâm Ra Quyết Định (Executive Decision Center) (05/11/2026)
- **Mục tiêu:** Chuyển đổi toàn diện Admin Dashboard thành trung tâm chỉ huy điều hành (Executive Decision Center) chuẩn Shopify Analytics / Haravan / Sapo: loại bỏ 100% số liệu hardcode giả lập để số liệu khớp tuyệt đối với câu lệnh SQL kiểm chứng thủ công, tích hợp bộ lọc toàn cục (7/30/90 ngày, Kênh Online/POS, Chi nhánh với Store Isolation), 6 thẻ KPI so sánh kỳ trước (▲▼ %), Biểu đồ Chart.js, Phễu chuyển đổi 5 bước, Ma trận Heatmap Giờ × Thứ, Phân khúc RFM 1–5 kèm Xuất CSV & 1-Click Tạo Voucher Win-Back, và Cảnh báo điều phối tồn kho thông minh (GAP-11).
- **Đã làm:**
  1. **Kiểm tra hiện trạng & Chuẩn hóa dữ liệu (Step 0 Audit):**
     - Phát hiện và loại bỏ toàn bộ nhánh ghi đè số giả lập (`if (monthOrdersRevenue < 10000000m)`) tại `DashboardController.cs` và khối text tĩnh HTML giả lập tại `Index.cshtml`.
     - Chuyển đổi 100% chỉ số sang truy vấn tổng hợp phía SQL Server (`GroupBy`, `AsNoTracking()`).
  2. **Bộ lọc toàn cục (Global Filter Bar) & Store Isolation:**
     - Cho phép chọn nhanh `7 ngày`, `30 ngày`, `90 ngày` hoặc tùy chọn ngày `fromDate – toDate`.
     - Bộ lọc kênh bán (`Tất cả / Online Storefront / Bán lẻ POS`).
     - Tôn trọng **Store Isolation**: Quản lý chi nhánh (`StoreManager` / `Staff`) bị khóa cứng vào chi nhánh mình ở tất cả KPI, Biểu đồ và Tồn kho; SuperAdmin có quyền xem toàn chuỗi hoặc chọn từng chi nhánh.
  3. **6 Thẻ KPI Trung Tâm Có So Sánh Kỳ Trước (Period-over-Period ▲▼ %):**
     - Doanh thu thuần (`PeriodNetRevenue` & `PeriodNetRevenueGrowth`).
     - Số đơn hàng hợp lệ (`PeriodOrderCount` & `PeriodOrderCountGrowth`).
     - Giá trị đơn trung bình (`PeriodAov` & `PeriodAovGrowth`).
     - Tỷ lệ hủy đơn (`PeriodCancellationRate %` & `PeriodCancellationRateDiff`).
     - Khách hàng mới (`PeriodNewCustomers` & `PeriodNewCustomersGrowth`).
     - Giá trị tồn kho cận date FEFO (`FefoRiskInventoryValue`) & số lô rủi ro (`FefoRiskBatchCount`).
  4. **Phễu chuyển đổi 5 bước & Heatmap Giờ × Thứ đặt hàng:**
     - Phễu chuyển đổi 5 bước: `Xem sản phẩm → Thêm giỏ → Checkout → Đặt hàng → Hoàn tất` tính trực tiếp từ dữ liệu thực trong DB kèm chú thích phương pháp minh bạch.
     - Ma trận Heatmap 7 Thứ × 6 Khung giờ (24h theo giờ VN UTC+7) với dải màu trực quan hóa khung giờ cao điểm để canh giờ Flash Sale.
  5. **Phân khúc RFM (Recency – Frequency – Monetary) Chấm Điểm 1–5:**
     - Chấm điểm R, F, M từ 1 đến 5 trên tập khách hàng thực tế và phân vào 4 nhóm: `Khách VIP`, `Trung thành`, `Có nguy cơ rời bỏ`, `Ngủ đông`.
     - Nút **Xuất danh sách nhóm ra file CSV** chuẩn định dạng UTF-8 BOM.
     - Nút **1-Click Tạo Voucher Nhắm Mục Tiêu**: Tự động sinh Coupon (`WINBACK20-XXXX`, `VIP25-XXXX`) và cấp phát trực tiếp vào ví Voucher cá nhân của khách hàng trong nhóm.
  6. **Cảnh báo thông minh & Tối ưu hiệu năng:**
     - Phân tích chênh lệch tồn kho thực tế giữa các chi nhánh để đề xuất lệnh `StockTransfer` kèm link 1-click sang `/Admin/StockTransfer?fromStoreId=...&toStoreId=...&productId=...`.
     - Caching 5 phút bằng `IMemoryCache` theo tổ hợp bộ lọc, tự động xóa cache khi Admin thao tác tạo voucher hoặc đẩy Flash Sale.
- **Kết quả nghiệm thu:**
  + `dotnet build OmnichannelBeautySolution.sln`: **0 Error, 0 Warning**.
  + `dotnet test`: **311 / 311 tests PASSED (100%)** (+4 unit tests chuyên sâu tại [`AdminDashboardDecisionCenterTests.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/Omnichannel.Tests/Services/AdminDashboardDecisionCenterTests.cs)).
  + Khớp số liệu 100% với truy vấn kiểm chứng thủ công; Store Isolation hoạt động nghiêm ngặt; file CSV xuất ra có UTF-8 BOM hiển thị chuẩn tiếng Việt.
- **Tài liệu cập nhật:** `doc/nang-cap/00_tien_do.md`, [`plan_d2_decision_center_dashboard.md`](file:///C:/Users/Admin/.gemini/antigravity/brain/5a8c83f4-29ad-4357-b6c4-f7e19a3da86f/plan_d2_decision_center_dashboard.md).

### Phiên E1: Bảng Điều Khiển Hỗ Trợ Thuyết Trình 1-Click (Demo Presenter HUD) (05/11/2026)
- **Mục tiêu:** Xây dựng bảng điều khiển thuyết trình Heads-Up Display (HUD) chuyên dụng, ẩn mình an toàn và chỉ kích hoạt khi `DemoMode:Enabled == true`, hỗ trợ người trình bày bám sát "Câu chuyện demo 10 phút" persona "Lê Ngọc Linh" mà không bao giờ bị lúng túng gõ sai URL, quên mật khẩu đăng nhập, hoặc mất nhịp thời gian (GAP-12).
- **Đã làm:**
  1. **Kiến trúc & Bảo mật Đa Môi Trường:**
     - Thiết kế giao diện Partial View `_DemoPresenterHud.cshtml`, kiểm tra chặt chẽ `IConfiguration["DemoMode:Enabled"]`. Khi cấu hình tắt (`false`) hoặc ở môi trường Production, HUD hoàn toàn không được render vào cây DOM và toàn bộ API endpoint tại `/api/demo/*` đều trả về `404 Not Found`.
     - Tích hợp phím tắt toàn cục **`Alt + D`** và nút kích hoạt nổi thu nhỏ ở góc trái dưới màn hình (`z-index: 10000`), có thể bật/tắt tức thì trên cả 3 giao diện: Web Storefront (`_Layout.cshtml`), Admin Dashboard (`_AdminLayout.cshtml`) và Màn hình Thu ngân POS (`_PosLayout.cshtml`).
  2. **Đồng Hồ Đếm Ngược 10 Phút & Thanh Tiến Độ Thuyết Trình:**
     - Tích hợp Timer đếm ngược trực quan `10:00` với nút Chạy/Tạm dừng/Reset, kèm chuông cảnh báo viền đỏ nhấp nháy khi thời gian dưới 2 phút.
     - Thanh tiến độ (Progress bar) tự động tính phần trăm các chặng đã hoàn thành (`0% - 100%`).
     - Tự động lưu trạng thái đánh dấu hoàn thành từng chặng vào `localStorage` (`omni_hud_step_*`) giúp duy trì tiến độ khi người thuyết trình chuyển trang hoặc tải lại trình duyệt.
  3. **Bản Đồ 6 Chặng Thuyết Trình Chuẩn Persona "Lê Ngọc Linh":**
     - **Chặng 1 (0:00 - 1:30):** Khởi động & Nhận diện Khách hàng VIP (`/`, `/Account/Profile`, `customer_vip` - Hạng Kim Cương, 1.250 điểm, sinh nhật Tháng 11).
     - **Chặng 2 (1:30 - 3:30):** Trợ lý AI Da Liễu & Cảnh Báo Tương Kỵ Hoạt Chất (`/RoutineQuiz`, `/phan-tich-da`, PDP son có Try-On ảo).
     - **Chặng 3 (3:30 - 5:30):** Trải Nghiệm Storefront Chuyên Sâu (`/`, `/so-sanh`, `/tra-cuu-don-hang`, `/ClearanceFefo`).
     - **Chặng 4 (5:30 - 7:00):** Gamification & Đổi Thưởng Omni Rewards (`/Customer/Loyalty`, `/Cart`).
     - **Chặng 5 (7:00 - 8:30):** Khép Kín Vòng Lặp O2O (Click & Collect) & Bàn Giao POS (`/Checkout`, `/Admin/Pos`).
     - **Chặng 6 (8:30 - 10:00):** Trung Tâm Ra Quyết Định & Điều Hành Chuỗi (`/Admin/Dashboard`, Báo cáo RFM, Cảnh báo kho).
  4. **Bộ Công Cụ Tác Vụ 1-Click (Demo Actions):**
     - **Chuyển vai nhanh (Quick Persona Switcher):** Cho phép đăng nhập 1-click thành `Khách VIP (Lê Ngọc Linh)`, `Thu ngân POS (Showroom 1)` hoặc `SuperAdmin`, tự động phát hành đúng Authentication Cookie (`CustomerCookieScheme` hoặc `AdminCookieScheme`) và gán đầy đủ Claim `StoreId` & Phân quyền PBAC tương ứng mà không làm gián đoạn bài thuyết trình. Ghi Audit Log tự động cho mọi thao tác chuyển vai.
     - **Nạp giỏ hàng mẫu:** 1-Click nạp combo Routine 3 bước (Sữa rửa mặt + Toner + Kem dưỡng) hoặc nạp combo Tương kỵ Da liễu AI (Retinol 1% + BHA 2%) để trình diễn cảnh báo lâm sàng đỏ rực trong trang giỏ hàng.
     - **Khôi phục dữ liệu Demo (1-Click Reset State):** Tích hợp nút Reset Demo Data gọi trực tiếp `IDemoResetService.ResetDemoDataAsync` với hộp thoại xác thực an toàn SweetAlert2.
  5. **Kiểm Thử Tự Động Toàn Diện:**
     - Viết mới 6 automated unit tests tại [`DemoPresenterControllerTests.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/Omnichannel.Tests/Controllers/DemoPresenterControllerTests.cs) kiểm thử:
       * Tắt DemoMode trả về `404 Not Found` cho toàn bộ endpoint.
       * Bật DemoMode trả về trạng thái hợp lệ.
       * Đăng nhập nhanh khách hàng cấp phát đúng `CustomerCookieScheme`.
       * Đăng nhập nhanh thu ngân cấp phát `AdminCookieScheme` và claim `StoreId=1`.
       * Nạp giỏ hàng tương kỵ thành công.
       * Gọi lệnh khôi phục dữ liệu demo thành công.
- **Kết quả nghiệm thu:**
  + `dotnet build OmnichannelBeautySolution.sln`: **0 Error, 0 Warning**.
  + `dotnet test`: **317 / 317 tests PASSED (100%)** (+6 unit tests).
  + Thao tác nhấn phím `Alt + D` mượt mà, phản hồi chuyển vai < 50ms, giao diện đẹp mắt chuẩn glassmorphism cao cấp.
- **Tài liệu cập nhật:** `doc/nang-cap/00_tien_do.md`, [`plan_e1_demo_presenter_hud.md`](file:///C:/Users/Admin/.gemini/antigravity/brain/5a8c83f4-29ad-4357-b6c4-f7e19a3da86f/plan_e1_demo_presenter_hud.md).

### Phiên E2: Nghiệm Thu Toàn Hệ Thống & Cập Nhật Tài Liệu Thuyết Trình (05/11/2026)
- **Mục tiêu:** Thực hiện tổng kiểm thử kỹ thuật và nghiệm thu runtime toàn diện trên toàn giải pháp `OmnichannelBeautySolution.sln` (-c Release, test suite, extended smoke test 64 route, 2 chu kỳ demo liên tiếp), rà soát hiệu năng/bảo mật/SEO/responsive, và hoàn thiện trọn bộ tài liệu thuyết trình bảo vệ đồ án trước Hội đồng (Kịch bản demo 10 phút, Bộ 16 câu hỏi phản biện, Báo cáo nâng cấp 12 nhóm G1–G12).
- **Đã làm:**
  1. **Kiểm thử Kỹ thuật (Technical Build & Test Suite):**
     - `dotnet build OmnichannelBeautySolution.sln -c Release`: **0 Error, 0 Warning** (biên dịch hoàn hảo chế độ Release).
     - `dotnet test OmnichannelBeautySolution.sln -c Release`: **317 / 317 tests PASSED (100%)** trong 17 giây (tăng trưởng ngoạn mục từ 233 tests ban đầu lên 317 tests, bao phủ toàn bộ 10 phân hệ tính năng mới).
  2. **Extended Smoke Test Runtime (64 Route / Tác vụ) & Sửa lỗi Runtime Razor:**
     - Soạn thảo và thực thi script [`scripts/test_extended_smoke_e2.ps1`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/scripts/test_extended_smoke_e2.ps1).
     - Phát hiện và xử lý dứt điểm lỗi Razor Runtime Compilation trên `_DemoPresenterHud.cshtml` (`IConfiguration.GetValue` $\rightarrow$ chuẩn hóa dùng indexer `Configuration["DemoMode:Enabled"]`).
     - Kết quả kiểm thử: **64 / 64 route và tác vụ tương tác PASSED 100% (0 lỗi 500)**. Thời gian phản hồi trang chủ 31ms, giỏ hàng 9ms, instant search 77ms, phân tích da 5ms, reset demo 59ms (vượt xa mục tiêu < 500ms).
  3. **Chạy Kịch Bản Demo 2 Vòng Liên Tiếp (Verification Loop):**
     - Thực thi liên hoàn 2 chu kỳ demo Persona Lê Ngọc Linh (Đăng nhập VIP $\rightarrow$ Nạp giỏ tương kỵ AI $\rightarrow$ Chuyển thu ngân POS kiểm tra đơn nhận tại quầy $\rightarrow$ Chuyển SuperAdmin xem Decision Center & Xuất CSV RFM $\rightarrow$ 1-Click Reset Demo Data). Cả 2 chu kỳ đều chạy mượt mà, ổn định tuyệt đối và hoàn nguyên CSDL sạch sẽ trong 70ms.
  4. **Rà soát Chất lượng Toàn diện (Chất lượng, SEO, An ninh, Responsive):**
     - **SEO & Structured Data:** Bổ sung các route mới (`/so-sanh`, `/tra-cuu-don-hang`, `/phan-tich-da`, `/clearance-fefo`, `/tu-van-da`) vào `sitemap.xml`; kiểm tra Schema.org `Product` JSON-LD và OpenGraph.
     - **Bảo mật:** CSRF bảo vệ toàn diện các form và AJAX POST; PBAC khóa chặt toàn bộ cửa sau Admin; Store Isolation cô lập dữ liệu chi nhánh nghiêm ngặt.
     - **Tiếng Việt & Encoding:** Toàn bộ font hiển thị chuẩn, file CSV xuất ra có tiền tố UTF-8 BOM hiển thị chuẩn xác trên Microsoft Excel.
  5. **Cập nhật & Hoàn thiện Hệ thống Tài liệu:**
     - **Cập nhật [`doc/KICH_BAN_DEMO_10_PHUT_VA_BO_HOI_DAP_PHAN_BIEN.md`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/doc/KICH_BAN_DEMO_10_PHUT_VA_BO_HOI_DAP_PHAN_BIEN.md):** Khớp trọn vẹn 6 chặng Persona Lê Ngọc Linh (có thao tác, lời thuyết trình mẫu, phím tắt `Alt + D`); bổ sung 10 câu hỏi phản biện chuyên sâu sắc bén (Cold Start FBT, AI Vision Fallback, Concurrency O2O/POS, Virtual Try-On Offline, RFM Win-back, Hủy đơn hoàn tồn FEFO, Gamification chống gian lận, Compare regex VND/ml, Instant search tiếng Việt, 1-Click Reset Data).
     - **Tạo mới [`doc/nang-cap/05_bao_cao_ket_qua_nang_cap.md`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/doc/nang-cap/05_bao_cao_ket_qua_nang_cap.md):** Bảng so sánh trước/sau theo 12 nhóm G1–G12 (điểm trung bình tăng từ 2.96 lên 4.90/5.0, +65.5%), biểu đồ so sánh với Hasaki, Sephora, Olive Young, danh mục toàn bộ tính năng mới, hạn chế và hướng phát triển.
     - **Cập nhật [`README.md`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/README.md) & [`TEST_ACCOUNTS.md`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/TEST_ACCOUNTS.md):** Bổ sung danh sách 10 tính năng đột phá, số lượng 317 tests passed 100%, và hướng dẫn sử dụng Demo Presenter HUD (`Alt + D`).
- **Kết quả nghiệm thu:**
  + `dotnet build OmnichannelBeautySolution.sln -c Release`: **0 Error, 0 Warning**.
  + `dotnet test OmnichannelBeautySolution.sln -c Release`: **317 / 317 tests PASSED (100%)**.
  + Extended Smoke Test: **64 / 64 passed (0 lỗi 500)**.
  + Toàn bộ 16 phiên (từ Prompt 0, A1–A4, B1–B2, C1–C8, D1–D2, E1–E2) đều đạt trạng thái **✅ Xong (100%)**.
  + **Hệ thống chính thức SẴN SÀNG 100% CHO BUỔI THUYẾT TRÌNH BẢO VỆ ĐỒ ÁN!**
- **Tài liệu cập nhật:** `doc/nang-cap/00_tien_do.md`, [`doc/nang-cap/05_bao_cao_ket_qua_nang_cap.md`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/doc/nang-cap/05_bao_cao_ket_qua_nang_cap.md), [`doc/KICH_BAN_DEMO_10_PHUT_VA_BO_HOI_DAP_PHAN_BIEN.md`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/doc/KICH_BAN_DEMO_10_PHUT_VA_BO_HOI_DAP_PHAN_BIEN.md), [`README.md`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/README.md), [`TEST_ACCOUNTS.md`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/TEST_ACCOUNTS.md).

### Phiên F1 (Giai đoạn F – Hình ảnh & Rebrand): Kiểm Kê Hình Ảnh Toàn Hệ Thống (Read-Only) (05/11/2026)
- **Mục tiêu:** Thực hiện kiểm kê toàn diện 100% hình ảnh sản phẩm trên 3 thư mục vật lý (`A = Omnichannel.Web\wwwroot\images\products\**`, `B = Omnichannel.Web\wwwroot\images\real_products\`, `C = Real_Products_Images\`) theo mã băm nội dung SHA-256, độ phân giải, định dạng thực tế (Magic Bytes), kiểm tra trực quan từng ảnh, và rà soát toàn bộ tham chiếu ảnh trong mã nguồn & CSDL `OmnichannelBeautyDb`.
- **Đã làm:**
  1. **Tạo nhánh Git riêng:** Chuyển sang nhánh `feature/canmake-rebrand-images` (giữ nguyên 100% trạng thái Read-Only, không xóa file/thư mục, không sửa DB).
  2. **Kiểm kê SHA-256 & Phân loại (a–d) trên 258 file vật lý (`A`: 90 file / 88 hash; `B`: 84 file / 59 hash; `C`: 84 file / 40 hash; tổng 138 hash duy nhất):**
     - **(a) Đã có bản giống hệt trong A:** **30 file** (15 ở B, 15 ở C) → An toàn tuyệt đối để xóa.
     - **(b) & (d) Khác hash với A / Xung đột cùng tên khác nội dung:** **74 tên file** (ứng với 138 file ở B/C; không có tên file mới nào chỉ tồn tại ở B/C mà thiếu ở A).
     - **(c) Trùng nội dung khác tên:** **2 cặp** trùng trong nội bộ A (`.png`/`.webp` của Bioderma và `.jpg`/`.webp` của LRP Hyalu B5), **1 cụm** giữa A (`.webp`) và B/C (`.jpg`) của CeraVe Foaming Cleanser, và **16 cụm ảnh Unsplash giả** bị nhân bản cho 2–11 sản phẩm khác nhau trong B và C.
  3. **Kiểm tra trực quan 100% ảnh (Visual Audit) & Tìm ra nguyên nhân gốc rễ (Root Cause):**
     - **Tại Thư mục chuẩn A (`wwwroot/images/products/**`):** **89/90 ảnh là ảnh thật ĐÚNG sản phẩm & thương hiệu** (bao gồm cả `xit-thom-toan-than-bath-and-body-works-into-the-night-236ml.jpg` trong A là ảnh chai Bath & Body Works thật!), chỉ có **1 ca lệch dòng** (`dung-dich-duong-am-hada-labo-gokujyun-premium-lotion-170ml.jpg`: A là chai vàng *Gokujyun Premium*, còn B/C là chai trắng *Advanced Nourish* khớp tên SP `Id=1` trong DB).
     - **Tại Thư mục B (`wwwroot/images/real_products/`) & Thư mục C (`Real_Products_Images/`):** Có **43/84 ảnh ở B** và **63/84 ảnh ở C** là **ảnh sai sản phẩm/ảnh Unsplash giả** (như Chanel Coco Mademoiselle dùng cho 6 sản phẩm, son Clinique đỏ dùng cho 9 sản phẩm, Bleu de Chanel dùng cho Dior Sauvage/YSL Y/Tom Ford...).
     - **Nguyên nhân lỗi hiển thị trên Web:** `dbo.Products.ThumbnailUrl` của **97/98 sản phẩm đang trỏ vào `/images/real_products/...` (Thư mục B)** thay vì `/images/products/{category}/...` (Thư mục A), và `DbInitializer.MigrateStaticImagesToDatabaseAsync` quét `real_products` sau `products` làm ghi đè `MediaAssets.BinaryData` của 63 sản phẩm bằng ảnh của thư mục B!
  4. **Phát hiện đầy đủ các lỗi đồng bộ:**
     - **24 nhóm URL tham chiếu gãy (404)** tại 58 vị trí (6 dòng `Products.ThumbnailUrl` trong DB, 1 dòng `OrderReturnRequests`, 5 mục trong `ProductImageSourceRegistry.cs`, 30 chỗ hardcode `/images/default-product.png` / `/images/branding/default-product.png` / `/images/placeholder-product.png`, và 9 ảnh demo 404 trong `CustomerController` & `WishlistController`).
     - **6 file mồ côi trong A**, **13 ảnh dùng chung cho 27 sản phẩm** (trong đó 3 SP khác thương hiệu là Cetaphil `Id=23`, Maybelline Fit Me `Id=32`, L'Oréal Micellar `Id=7`), **1 SP dùng placeholder** (`Id=98` Silcot 82 miếng).
     - **Chất lượng kỹ thuật:** 1 file trong A > 300 KB (`310.56 KB`), 8 file trong A có cạnh ngắn < 600px, 7 file trong A lệch đuôi mở rộng so với Magic Bytes (`WEBP` mang đuôi `.jpg`, `JPEG` mang đuôi `.png`/`.webp`).
- **Tài liệu sinh ra:** [`doc/nang-cap/06_kiem_ke_hinh_anh.md`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/doc/nang-cap/06_kiem_ke_hinh_anh.md), cập nhật [`doc/nang-cap/00_tien_do.md`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/doc/nang-cap/00_tien_do.md).


### Phiên F2 (Giai đoạn F – Hình ảnh & Rebrand): Hợp Nhất Ảnh Về `wwwroot\images\products` & Xóa Thư Mục Trùng Lặp (05/11/2026)
- **Mục tiêu:** Đưa ảnh còn thiếu vào thư mục chuẩn `Omnichannel.Web\wwwroot\images\products\{skincare|makeup|perfume|hair-body}`, cập nhật toàn bộ tham chiếu trong code và CSDL sang đường dẫn chuẩn `/images/products/{sub}/{file}`, và xóa vĩnh viễn 2 thư mục trùng lặp (`Omnichannel.Web\wwwroot\images\real_products` và `Real_Products_Images`).
- **Đã làm:**
  1. **Đưa ảnh còn thiếu vào thư mục chuẩn A:**
     - Copy ảnh chai trắng Hada Labo Advanced Nourish từ B vào `Omnichannel.Web\wwwroot\images\products\skincare\dung-dich-duong-am-hada-labo-advanced-nourish-170ml.jpg`.
     - Quy chuẩn tên file theo slug tiếng Việt không dấu, chữ thường, gạch nối, khớp `Product.Slug`.
  2. **Cập nhật toàn bộ tham chiếu trong Code, Registry & Script:**
     - Loại bỏ toàn bộ chuỗi hardcode `/images/real_products/...` và đường dẫn phẳng cũ trong `CartController.cs`, `Views/Cart/Index.cshtml`, `CustomerController.cs`, `WishlistController.cs`, `DbInitializer.cs`, `ProductImageSourceRegistry.cs`, `product_image_registry.json`, `scripts/download_and_rename_products.ps1`, `scripts/verify_real_product_images_e2e.ps1`, và `Omnichannel.Tests/Services/AdminProductCrudTests.cs`.
     - Đảm bảo `DbInitializer.cs` khi chạy lại không bao giờ ghi đè ngược về đường dẫn `/images/real_products/...`.
  3. **Cập nhật CSDL Idempotent (sau khi được duyệt tại Điểm dừng 1):**
     - Thực thi [`scripts/update_product_images_phien2.sql`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/scripts/update_product_images_phien2.sql) và [`scripts/sync_product_images_db_phien2.ps1`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/scripts/sync_product_images_db_phien2.ps1): cập nhật **98 dòng `dbo.Products`**, **75 dòng `dbo.MediaAssets`** (bao gồm đồng bộ lại `BinaryData`, `FileSize`, `MimeType` từ file chuẩn trong `wwwroot/images/products/**`), và **1 dòng `dbo.OrderReturnRequests.EvidenceUrlsJson`**.
  4. **Xóa 2 thư mục trùng lặp (sau khi được duyệt tại Điểm dừng 2):**
     - Xác nhận `git grep -i "real_products\|Real_Products_Images"` trong code trả về **0 kết quả**.
     - Xóa hoàn toàn `Omnichannel.Web\wwwroot\images\real_products` (84 file) và `Real_Products_Images` (84 file).
- **Kết quả nghiệm thu:**
  + `dotnet build OmnichannelBeautySolution.sln -c Release`: **0 Warning, 0 Error**.
  + `dotnet test OmnichannelBeautySolution.sln -c Release`: **317 / 317 tests PASSED (100%)**.
  + Quét E2E HTTP 404 trên toàn bộ URL DB + 13 trang HTML render: **0 ảnh 404**.

### Phiên F3 (Giai đoạn F – Hình ảnh & Rebrand): Đồng Bộ Hình Ảnh – Dữ Liệu & Tối Ưu Hiển Thị (05/11/2026)
- **Mục tiêu:** Thiết lập một nguồn sự thật duy nhất (`IProductImageResolver`) cho toàn bộ đường dẫn ảnh sản phẩm kèm ảnh fallback `/images/products/_placeholder.webp` tông hồng nhạt có logo mờ; tách ảnh dùng chung sai bằng placeholder; chuẩn hóa kỹ thuật ảnh (1:1 `1000x1000px`, nền sáng `#FFFFFF`, `<= 150 KB`, xuất bản `.webp` song song giữ `.jpg`/`.png` làm `<picture>` fallback); và tối ưu Core Web Vitals (`loading="lazy"`, `decoding="async"`, `fetchpriority="high"`, `width`/`height`, SEO Alt text tiếng Việt, Schema.org Product `image` URL tuyệt đối).
- **Đã làm:**
  1. **Một nguồn sự thật duy nhất (`IProductImageResolver` & `_placeholder.webp`):**
     - Thiết kế mới [`/images/products/_placeholder.webp`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/Omnichannel.Web/wwwroot/images/products/_placeholder.webp) (`5.73 KB`, `1000x1000px`, nền hồng phấn `#FFF8FA` → `#FBDCE6` với biểu tượng hoa/kim cương mờ `#E8618C`) và bản fallback [`/images/products/_placeholder.jpg`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/Omnichannel.Web/wwwroot/images/products/_placeholder.jpg).
     - Tạo [`IProductImageResolver`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/Omnichannel.Application/Interfaces/Catalog/IProductImageResolver.cs) ở tầng Application và triển khai [`ProductImageResolver`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/Omnichannel.Infrastructure/Services/Catalog/ProductImageResolver.cs) ở tầng Infrastructure, cung cấp `GetPrimaryImageUrl`, `GetWebpImageUrl`, `GetThumbnailUrl`, `GetGalleryImageUrls`, `GetAltText`, `GetAbsoluteImageUrl`, và `FallbackImageUrl`.
     - Đăng ký DI và tiêm `@inject IProductImageResolver ProductImageResolver` toàn cục trong `_ViewImports.cshtml` (Storefront & Admin); thay thế toàn bộ đoạn ghép chuỗi/fallback ảnh rải rác trên **38 file** Views, ViewComponents, Controllers và Services.
  2. **Sửa lỗi nội dung, tách ảnh dùng chung & đồng bộ Registry 98 SKU:**
     - Đồng bộ [`product_image_registry.json`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/Omnichannel.Infrastructure/Data/product_image_registry.json) và [`ProductImageSourceRegistry.cs`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/Omnichannel.Infrastructure/Data/ProductImageSourceRegistry.cs) cho đủ **98/98 SKU**: **84 sản phẩm** có ảnh thật khớp 1–1 tuyệt đối và **14 sản phẩm** (7 SP độc lập thiếu ảnh thật + 7 SP bản ghi trùng từ seed đầu) được tách sang `/images/products/_placeholder.webp`.
     - Soạn thảo script SQL idempotent [`scripts/update_product_data_phien3.sql`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/scripts/update_product_data_phien3.sql) và lập danh sách **3 file ảnh mồ côi** + **14 sản phẩm cần bổ sung ảnh thật** trình duyệt tại Điểm dừng.
  3. **Tối ưu hiệu năng & SEO ảnh (`scripts/optimize_images.ps1`):**
     - Chuẩn hóa **84/84 ảnh sản phẩm chính tắc** về đúng tỷ lệ **1:1 (`1000x1000px`)**, nền sáng `#FFFFFF`, định dạng chuẩn khớp Magic Bytes, dung lượng **`<= 150 KB`** (max `118.21 KB`, trung bình `34.98 KB`), và xuất **84 bản `.webp` song song** (max `76.57 KB`, trung bình `21.22 KB`).
     - Chuẩn hóa toàn bộ **4 ảnh `banners`** và **10 ảnh `categories`** (`<= 150 KB` + xuất `.webp` song song).
     - Áp dụng `<picture><source type="image/webp" srcset="..."><img ...></picture>`, `loading="lazy" decoding="async"` cho ảnh dưới màn hình đầu, `fetchpriority="high"` cho ảnh LCP (Hero banner & ảnh chính PDP), khai báo tường minh `width`/`height` chống CLS, Alt text tiếng Việt chứa Tên sản phẩm + Thương hiệu, và Schema.org `Product.image` xuất URL tuyệt đối chuẩn.
- **Kết quả nghiệm thu:**
  + `dotnet build OmnichannelBeautySolution.sln -c Release`: **0 Warning, 0 Error**.
  + `dotnet test OmnichannelBeautySolution.sln -c Release`: **317 / 317 tests PASSED (100%)**.
- **Tài liệu cập nhật:** [`doc/nang-cap/06_kiem_ke_hinh_anh.md`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/doc/nang-cap/06_kiem_ke_hinh_anh.md) (Mục VI), [`doc/nang-cap/00_tien_do.md`](file:///d:/L%E1%BA%ADp%20tr%C3%ACnh%20web/OmnichannelBeautySolution/doc/nang-cap/00_tien_do.md).
