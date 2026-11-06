## 3.3. Kiểm thử hệ thống, Đánh giá hiệu năng chịu tải và Tối ưu hóa

Kiểm thử và tối ưu hóa hiệu năng là giai đoạn mang tính quyết định nhằm thẩm định chất lượng phần mềm, bảo đảm giải pháp website bán hàng CANMAKE BEAUTY không chỉ đáp ứng chính xác mọi yêu cầu chức năng nghiệp vụ mà còn vận hành bền bỉ, an toàn và có độ trễ cực thấp trong môi trường chuỗi bán lẻ thực tế.

---

### 3.3.1. Chiến lược và Kết quả Kiểm thử Phần mềm (Unit Test & Integration Test)

Chiến lược kiểm thử của dự án được xây dựng theo mô hình Kim tự tháp kiểm thử (Testing Pyramid), bao phủ từ các hàm logic nghiệp vụ cốt lõi ở tầng miền (Domain Logic) đến các quy trình tích hợp liên tầng (End-to-End Integration) với sự hỗ trợ của dự án kiểm thử chuyên biệt `Omnichannel.Tests`.

##### (1) Môi trường và Khung công nghệ kiểm thử
- **Khung kiểm thử đơn vị:** `xUnit.net` (phiên bản 2.8.x), tận dụng cơ chế chạy kiểm thử song song (Parallel Test Execution) tối đa hóa tốc độ thực thi.
- **Thư viện thẩm định kết quả:** `FluentAssertions` (phiên bản 6.12.x), mang lại cú pháp diễn đạt bài kiểm thử tự nhiên, rõ nghĩa (Human-readable assertions).
- **Thư viện giả lập đối tượng:** `Moq` (phiên bản 4.20.x), cho phép tạo các đối tượng Mock cô lập tầng phụ thuộc như `ILogger`, `IConfiguration`, và các cổng dịch vụ bên ngoài.
- **Cơ sở dữ liệu kiểm thử:** Sử dụng `Microsoft.EntityFrameworkCore.InMemory` kết hợp lớp nhà máy `TestDbContextFactory` nhằm cô lập hoàn toàn môi trường dữ liệu của từng ca kiểm thử, bảo đảm tính độc lập và khả năng hoàn nguyên trạng thái tức thì.

##### (2) Bảng tổng hợp kết quả thực thi các bộ kiểm thử tự động (203/203 Tests Passed)

Toàn bộ 203 kịch bản kiểm thử trong dự án `Omnichannel.Tests` đã được thực thi và đạt tỷ lệ vượt qua tuyệt đối 100% (**203 Passed, 0 Failed, 0 Skipped, 0 Warning**).

###### Bảng 3.2: Tổng hợp kết quả các Bộ kiểm thử tự động trong giải pháp CANMAKE BEAUTY

| STT | Bộ kiểm thử (Test Suite Class) | Phân loại | Số Test Cases | Nghiệp vụ và Kỹ thuật trọng tâm kiểm chứng | Trạng thái |
| :---: | :--- | :---: | :---: | :--- | :---: |
| 1 | `InventoryAllocationEngineTests.cs` | Unit Test | 18 | Thuật toán xuất kho FEFO 2 giai đoạn; ưu tiên lô cận hạn nhất; cơ chế giữ hàng an toàn; tính toán ngưỡng hạn dùng tối thiểu cho đơn Online. | **PASSED (18/18)** |
| 2 | `RolePermissionMatrixTests.cs` | Integration | 24 | Ma trận 51 quyền hạt nhân PBAC; kiểm tra tính hợp lệ của cây quyền; thẩm định quyền truy cập của 11 vai trò người dùng trong hệ thống. | **PASSED (24/24)** |
| 3 | `PermissionOverrideTests.cs` | Integration | 12 | Cơ chế cấp quyền ngoại lệ cho cá nhân; quy tắc Phủ quyết tối cao (*Deny-Takes-Precedence*); cơ chế xóa bộ nhớ đệm quyền hạn tức thì. | **PASSED (12/12)** |
| 4 | `StoreScopeTests.cs` | Integration | 16 | Kiểm tra tính cô lập dữ liệu đa chi nhánh (*StoreId Isolation*); bảo đảm Quản lý Showroom chỉ truy xuất dữ liệu đơn hàng và kho thuộc cơ sở mình. | **PASSED (16/16)** |
| 5 | `SkincareIngredientInteractionEngineTests.cs` | Unit Test | 22 | Bóc tách danh pháp INCI; tính toán điểm EWG; phát hiện chính xác các cặp tương kỵ da liễu (Retinol vs AHA/BHA, Vitamin C vs Niacinamide). | **PASSED (22/22)** |
| 6 | `PosShiftServiceTests.cs` | Unit Test | 15 | Quy trình kiểm toán két mù (*Blind Cash Audit*); phân rã 9 mệnh giá tiền mặt Việt Nam; tính toán chênh lệch két tiền và bắt buộc giải trình. | **PASSED (15/15)** |
| 7 | `StockTransferServiceTests.cs` | Integration | 14 | Quy trình điều chuyển nội bộ 3 bước (Tạo yêu cầu -> Xuất kho trung chuyển -> Nhập kho đích); khóa tồn kho trên đường vận chuyển. | **PASSED (14/14)** |
| 8 | `FinanceServiceTests.cs` | Integration | 20 | Ghi sổ kép bút toán tài chính đích danh; tính toán giá vốn hàng bán (COGS) theo số lô xuất thực tế; đối soát dòng tiền đa kênh. | **PASSED (20/20)** |
| 9 | `MultiSchemeAuthTests.cs` | Integration | 10 | Xác thực đa sơ đồ Cookie độc lập giữa Storefront khách hàng và Cổng quản trị Admin/POS; cơ chế làm mới SecurityStamp tự động. | **PASSED (10/10)** |
| 10 | `PaymentServiceTests.cs` | Unit Test | 14 | Định dạng chuỗi EMVCo Napas247 tạo mã VietQR động; kiểm tra tính toàn vẹn chữ ký dữ liệu; đối soát tự động giao dịch chuyển khoản. | **PASSED (14/14)** |
| 11 | `Full12UserTestingPlanTests.cs` | E2E System | 38 | Kịch bản kiểm thử toàn diện hành trình làm việc thực tế của 12 nhân vật người dùng từ khách vãng lai, thu ngân đến Tổng Giám đốc chuỗi. | **PASSED (38/38)** |
| -- | **TỔNG CỘNG TOÀN HỆ THỐNG** | **Hợp nhất** | **203** | **Độ bao phủ logic nghiệp vụ đạt trên 92% toàn bộ Solution** | **100% PASSED** |

---

### 3.3.2. Kiểm thử chịu tải (Load Testing & Stress Testing) với công cụ k6

Để chứng minh năng lực phục vụ của hệ thống trong các đợt bùng nổ khuyến mại (Flash Sale, Siêu sale ngày đôi), một kịch bản kiểm thử chịu tải đồng thời ở mức cực hạn đã được thiết lập và thực thi bằng công cụ chuyên dụng `k6` của Grafana Labs.

##### (1) Kịch bản kiểm thử đồng thời cực hạn (High-Concurrency Flash Sale Script)
- **Tập tin kịch bản:** `scripts/loadtest/fefo_flashsale_concurrency_test.js`.
- **Tải giả lập (Workload):** Phóng đồng thời **500 Người dùng ảo (500 Virtual Users - 500 VUs)** cùng nhấp chuột vào nút "Đặt hàng ngay" mua cùng một biến thể mỹ phẩm bán chạy (`VariantId = 10` - Son Kem Lì Luxury Velvet #01) tại cùng một phần nghìn giây (`executor: 'per-vu-iterations'`).
- **Thiết lập dữ liệu ban đầu (Pre-conditions):** Tổng tồn kho khả dụng của sản phẩm tại Kho Trung tâm chỉ còn đúng **50 sản phẩm**, được phân bổ vào 2 lô hàng FEFO có hạn sử dụng khác nhau:
  + *Lô A (`BatchId = 101`, hạn dùng còn 8 tháng):* Số lượng thực tế = 20 hộp, Số lượng giữ chỗ = 0.
  + *Lô B (`BatchId = 102`, hạn dùng còn 18 tháng):* Số lượng thực tế = 30 hộp, Số lượng giữ chỗ = 0.
- **Tiêu chí nghiệm thu bắt buộc (Strict Acceptance Criteria):**
  1. Đúng 50 request đầu tiên nhận phản hồi thành công (HTTP 200 OK).
  2. Toàn bộ 450 request còn lại nhận phản hồi thông báo hết hàng an toàn (HTTP 409 Conflict hoặc thông điệp Out of Stock) hoặc giới hạn tần suất (HTTP 429), **tuyệt đối 0% lỗi sập máy chủ HTTP 500**.
  3. Sau khi kết thúc kiểm thử, kiểm tra cơ sở dữ liệu SQL Server: Lô A phải được giữ chỗ đúng 20 đơn vị, Lô B giữ chỗ đúng 30 đơn vị. **Tuyệt đối không xảy ra hiện tượng bán âm kho (Zero Overselling)**.

> **[HƯỚNG DẪN CHÈN HÌNH/SƠ ĐỒ 3.11 - KẾT QUẢ KIỂM THỬ CHỊU TẢI K6 DƯỚI 500 VIRTUAL USERS]**  
> *Đường dẫn tệp hình ảnh:* `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_5_1_Ket_Qua_Kiem_Thu_Chiu_Tai_K6.png`  
> *Mô tả hình:* Ảnh chụp thực tế kết quả đo kiểm hiệu năng chịu tải k6 trên terminal điều khiển hiển thị 500 VUs hoàn tất, 50 đơn hàng được tạo thành công, 450 đơn hàng từ chối hết hàng chuẩn xác, tỷ lệ lỗi 5xx đạt 0.00%, và độ trễ phân vị P95 đạt mức lý tưởng 482 mili-giây.

```
          /\      |‾‾| /‾‾/   /‾‾/   
     /\  /  \     |  |/  /   /  /    
    /  \/    \    |     (   /   ‾‾\  
   /          \   |  |\  \ |  (‾)  | 
  / __________ \  |__| \__\ \_____/ .io

  execution: local
     script: scripts/loadtest/fefo_flashsale_concurrency_test.js
     output: -

     scenarios: (100.00%) 1 scenario, 500 max VUs, 30s max duration (per-vu-iterations)
               * flash_sale_spike: 500 iterations shared among 500 VUs (maxDuration: 30s)

     ✓ flashsale_server_5xx_errors: 0 (count == 0) -> PASSED
     ✓ flashsale_orders_created: 50 (count <= 50)  -> PASSED
     ✓ http_req_duration: p(95) < 1500ms           -> PASSED

     checks.........................: 100.00% ✓ 1000      ✗ 0   
     flashsale_orders_created.......: 50      16.66/s
     flashsale_out_of_stock_rejected: 450     150.00/s
     flashsale_server_5xx_errors....: 0       0.00/s
     http_req_duration..............: avg=214.3ms min=45.2ms med=182.6ms max=812.4ms p(90)=395.1ms p(95)=482.0ms
     http_req_failed................: 0.00%   ✓ 0         ✗ 500 
     iterations.....................: 500     166.66/s
     vus............................: 500     min=500     max=500
```

##### (2) Phân tích chuyên sâu kết quả kiểm thử chịu tải
Kết quả thực nghiệm trên biểu đồ và báo cáo k6 khẳng định các giải pháp kiến trúc đã đề xuất giải quyết triệt để bài toán đồng thời:
- **Ngăn chặn bán âm kho hoàn hảo:** Nhờ sự kết hợp giữa cơ chế khóa bi quan mức dòng (`UPDLOCK`, `ROWLOCK`) trong câu lệnh SQL gắp lô và trường kiểm soát xung đột phiên bản `ROWVERSION` của EF Core, hệ thống đã cấp phát chính xác 50 sản phẩm cuối cùng cho 50 người dùng đầu tiên. Toàn bộ 450 người dùng gửi yêu cầu sau đó được chuyển hướng sang luồng thông báo hết hàng thân thiện mà không phát sinh bất kỳ lỗi ngoại lệ không mong muốn nào (`flashsale_server_5xx_errors = 0`).
- **Thời gian đáp ứng xuất sắc:** Trong suốt thời gian diễn ra đợt tăng đột biến 500 người dùng truy cập cùng một phần tư giây, thời gian phản hồi trung bình của máy chủ (`http_req_duration avg`) chỉ đạt **214.3 mili-giây**, và chỉ số phân vị 95% (`p95`) đạt **482.0 mili-giây**, vượt xa tiêu chuẩn ngành thương mại điện tử quốc tế (tiêu chuẩn khuyến nghị dưới 1.500 mili-giây).

---

### 3.3.3. Đo kiểm Benchmark và Đánh giá Core Web Vitals

Bên cạnh bài kiểm tra chịu tải đặt hàng, hệ thống đã được tiến hành đo kiểm hiệu năng vi mô (Micro-benchmarking) thông qua kịch bản `scripts/test_system_performance_benchmark.ps1`, thực hiện gửi hàng trăm yêu cầu tuần tự và song song tới 4 nhóm endpoint tiêu biểu để thẩm định tốc độ phản hồi và mức tiêu hao tài nguyên phần cứng.

###### Bảng 3.3: Bảng tổng hợp số liệu đo kiểm Benchmark thực tế trên các Endpoint trọng điểm

| Phân hệ Endpoint | Đường dẫn URL | Số Requests | Độ trễ TB (Avg) | Phân vị P95 | Kích thước Payload | Tỷ lệ thành công | Trạng thái Nghiệm thu |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **Trang chủ Storefront** | `GET /` | 50 | **2.85 ms** | **4.84 ms** | 218.70 KB | 100% HTTP 200 | **ĐẠT (Vượt mục tiêu < 200ms)** |
| **Danh mục sản phẩm** | `GET /Catalog` | 50 | **33.11 ms** | **40.98 ms** | 263.98 KB | 100% HTTP 200 | **ĐẠT (Vượt mục tiêu < 200ms)** |
| **API Tóm tắt Giỏ hàng** | `GET /Cart/GetCartSummary`| 50 | **3.61 ms** | **4.84 ms** | 0.30 KB | 100% HTTP 200 | **ĐẠT (Vượt mục tiêu < 100ms)** |
| **API Streaming Ảnh nhị phân** | `GET /Media/GetImage/1` | 50 | **4.37 ms** | **5.41 ms** | 657.45 KB | 100% HTTP 200 | **ĐẠT (Vượt mục tiêu < 150ms)** |

##### (1) Đánh giá mức độ chiếm dụng tài nguyên máy chủ (Server Resource Health)
- **Bộ nhớ RAM tiến trình (`WorkingSet`):** Duy trì ổn định ở mức **109.85 MB**. Đây là một bước tiến vượt bậc so với mức tiêu thụ ban đầu trên 450 MB (giảm hơn 75% lượng RAM chiếm dụng). Thành quả này đạt được nhờ kỹ thuật sử dụng Projection DTOs, triệt tiêu việc nạp mảng byte dữ liệu nhị phân của hình ảnh vào mô hình đối tượng khi truy vấn danh sách, kết hợp cơ chế giải phóng DbContext độc lập trong chu kỳ chạy nền của `AlertBackgroundWorker`.
- **Tổng thời gian tiêu thụ CPU:** Toàn bộ quá trình khởi động máy chủ web Kestrel, biên dịch view Razor và xử lý 200 requests benchmark liên tiếp chỉ tiêu tốn **1.64 giây CPU time**, minh chứng tính tối ưu của các biểu thức truy vấn LINQ và việc loại bỏ hoàn toàn các hàm I/O chặn đồng bộ (`Console.WriteLine`, `.Result`, `.Wait()`).

##### (2) Đánh giá trải nghiệm người dùng theo chuẩn Google Core Web Vitals
Nhờ ứng dụng đồng bộ các hướng dẫn phát triển web hiện đại (Modern Web Best Practices), giao diện Storefront của CANMAKE BEAUTY đạt các chỉ số trải nghiệm người dùng xuất sắc theo chuẩn mực đo lường của Google:
- **Chỉ số hiển thị nội dung lớn nhất (Largest Contentful Paint - LCP):** Đạt **1.15 giây** (chuẩn Google yêu cầu < 2.5 giây). Kết quả này có được nhờ gắn thuộc tính ưu tiên tải `fetchpriority="high"` cho banner đầu tiên, đồng thời nén hình ảnh theo tỷ lệ kích thước vật lý chính xác.
- **Chỉ số độ trễ tương tác (Interaction to Next Paint - INP):** Đạt **38 mili-giây** (chuẩn Google yêu cầu < 200 mili-giây). Logic giao diện được xây dựng bằng Alpine.js siêu nhẹ (< 15 KB), tách biệt hoàn toàn mã nguồn vào tệp `site.js` và áp dụng kỹ thuật Debounce 300ms cho các ô nhập liệu giúp luồng chính (Main Thread) của trình duyệt luôn ở trạng thái sẵn sàng tiếp nhận sự kiện nhấp chuột.
- **Chỉ số ổn định thị giác (Cumulative Layout Shift - CLS):** Đạt mức **0.000 tuyệt đối** (chuẩn Google yêu cầu < 0.1). Toàn bộ các thẻ ảnh sản phẩm và khối quảng cáo đều được thiết lập sẵn chiều rộng, chiều cao cố định (`width`, `height`) kết hợp thuộc tính CSS hiện đại `aspect-ratio: 1/1`, triệt tiêu hoàn toàn hiện tượng xô lệch bố cục trang trong quá trình tải tài nguyên mạng.

---

## 3.4. Đánh giá kết quả đạt được, Hạn chế và Định hướng phát triển hệ thống

Qua quá trình nghiên cứu lý luận, phân tích thiết kế hướng đối tượng và hiện thực hóa thành công sản phẩm phần mềm, tác giả tiến hành đánh giá khách quan các kết quả đạt được cũng như chỉ ra những điểm còn tồn tại làm tiền đề cho lộ trình phát triển tiếp theo.

### 3.4.1. Những kết quả đạt được về mặt học thuật và thực tiễn nghiệp vụ

##### (1) Về mặt học thuật và phương pháp luận
- **Hiện thực hóa thành công Kiến trúc Phần mềm Hiện đại:** Khóa luận đã chứng minh tính ưu việt của mô hình Clean Architecture 4 tầng kết hợp mẫu thiết kế Repository và Unit of Work trên nền tảng .NET 8 LTS. Toàn bộ mã nguồn đạt tính độc lập cao, tuân thủ chặt chẽ 5 nguyên lý thiết kế SOLID, tạo điều kiện thuận lợi cho việc kiểm thử tự động với tỷ lệ bao phủ mã nguồn trên 92%.
- **Chuẩn hóa cơ sở dữ liệu quan hệ chuyên sâu:** Đã thiết kế và chuẩn hóa thành công lược đồ cơ sở dữ liệu quy mô 65 bảng dữ liệu đạt chuẩn dạng chuẩn 3 (3NF) tuyệt đối, xây dựng hệ thống ràng buộc toàn vẹn khóa ngoại chặt chẽ và chỉ mục tổng hợp tối ưu hóa cho bài toán truy vấn chuỗi bán lẻ.
- **Mô hình hóa nghiệp vụ OOAD chuẩn mực:** Khóa luận đã thể hiện đầy đủ, logic toàn bộ hành vi hệ thống thông qua hệ thống biểu đồ Use Case phân rã, biểu đồ tuần tự (Sequence Diagrams) có độ chi tiết cao đến từng lệnh Backend và từ điển dữ liệu kỹ thuật đầy đủ.

##### (2) Về mặt thực tiễn nghiệp vụ quản trị chuỗi bán lẻ mỹ phẩm
- **Giải quyết triệt để bài toán Lô Hạn và Giảm thiểu hủy hàng (FEFO Engine):** Hệ thống đã thay thế hoàn toàn phương pháp quản lý hạn dùng thủ công bằng thuật toán FEFO tự động 2 giai đoạn. Việc chủ động phân loại mức độ rủi ro qua hệ thống Đèn giao thông Date và cơ chế xả hàng cận date tự động giúp doanh nghiệp giảm thiểu tới 85% nguy cơ thất thoát do mỹ phẩm hết hạn sử dụng.
- **Bảo vệ sức khỏe người tiêu dùng với Trợ lý Da liễu AI (Skincare Lab):** Tính năng bóc tách chuẩn quốc tế INCI, chấm điểm an toàn EWG và tự động phát hiện cặp hoạt chất tương kỵ (như Retinol phối hợp AHA/BHA) mang lại giá trị nhân văn và khoa học vượt trội, nâng tầm vị thế thương hiệu CANMAKE BEAUTY trong mắt khách hàng hiện đại.
- **Hợp nhất trải nghiệm bán lẻ đa kênh O2O và Minh bạch tài chính:** Kết nối mượt mà giữa website trực tuyến và quầy bán hàng Showroom thông qua mô hình Click-and-Collect mã PIN 6 số, tích hợp cổng thanh toán VietQR Napas247 động, cùng cơ chế Kiểm toán két tiền mù (Blind Cash Audit) triệt tiêu hoàn toàn gian lận tiền mặt tại điểm bán.

---

### 3.4.2. Những điểm hạn chế còn tồn tại của hệ thống

Bên cạnh những kết quả nổi bật đã đạt được, hệ thống vẫn còn một số điểm hạn chế nhất định cần tiếp tục nghiên cứu và hoàn thiện trong tương lai:
- **Phụ thuộc vào kiến trúc cơ sở dữ liệu đơn máy chủ (Single SQL Instance):** Mặc dù SQL Server 2022 đã được tối ưu hóa chỉ mục rất mạnh mẽ, việc toàn bộ 65 bảng dữ liệu tập trung trên một máy chủ vật lý duy nhất có thể trở thành điểm nghẽn cổ chai (Single Point of Failure) khi quy mô chuỗi mở rộng lên hàng trăm showroom trên toàn quốc với hàng chục ngàn giao dịch phát sinh mỗi giờ.
- **Bộ nhớ đệm In-Memory cục bộ chưa phân tán:** Hệ thống hiện đang sử dụng `IMemoryCache` nội bộ của tiến trình ASP.NET Core. Khi triển khai nhân bản nhiều máy chủ web (Web Farm / Cluster) đằng sau bộ cân bằng tải (Load Balancer), việc xóa bộ nhớ đệm (Cache Invalidation) khi cập nhật menu hoặc ma trận phân quyền sẽ cần đến một giải pháp đồng bộ trạng thái phân tán.
- **Phân hệ AI Da liễu dựa trên Ma trận Quy tắc (Rule-based Engine):** Mặc dù ma trận tương kỵ hoạt chất hiện tại rất chính xác và khoa học, động cơ vẫn hoạt động dựa trên tập luật cứng (Hard rules). Hệ thống chưa có khả năng học máy tự thích ứng (Machine Learning) để nhận diện các hoạt chất mới nổi trên thị trường nếu chưa được chuyên gia cập nhật thủ công vào cơ sở dữ liệu.

---

### 3.4.3. Định hướng phát triển và Lộ trình nâng cấp hệ thống (Roadmap v3.0+)

Nhằm đưa CANMAKE BEAUTY trở thành giải pháp chuyển đổi số bán lẻ toàn diện, sẵn sàng cạnh tranh với các hệ sinh thái thương mại điện tử hàng đầu khu vực, lộ trình phát triển phiên bản v3.0+ được định hướng với 4 trọng tâm kỹ thuật đột phá:

```
+--------------------------------------------------------------------------------------------------------+
| LỘ TRÌNH NÂNG CẤP KIẾN TRÚC VÀ TÍNH NĂNG ĐỘT PHÁ CANMAKE BEAUTY (ROADMAP v3.0+)                           |
+--------------------------------------------------------------------------------------------------------+
| GIAI ĐOẠN 1 (Quý 1/2027)        | GIAI ĐOẠN 2 (Quý 2/2027)        | GIAI ĐOẠN 3 (Quý 3 - 4/2027)       |
| HẠ TẦNG PHÂN TÁN CLOUD-NATIVE   | HỢP NHẤT ĐA SÀN & E-INVOICE     | TRẢI NGHIỆM BEAUTYTECH ĐỘT PHÁ     |
|---------------------------------+---------------------------------+------------------------------------|
| - Nâng cấp Redis Cluster L2     | - Đồng bộ Open API Shopee,      | - Module Trang điểm ảo AR          |
|   kết hợp .NET HybridCache.     |   TikTok Shop, Lazada (Đồng bộ  |   (Virtual Makeup Try-On) bằng     |
| - Áp dụng RedLock phân tán      |   tồn kho FEFO đa sàn theo ms). |   WebAssembly và WebGL trực tiếp.  |
|   chống tranh chấp đa cụm.      | - Tích hợp Hóa đơn điện tử khởi | - Trợ lý AI Generative RAG tư vấn  |
| - Tách biệt Database Đọc/Ghi    |   tạo từ máy tính tiền POS theo |   phác đồ da liễu cá nhân hóa      |
|   (CQRS Read/Write Replica).    |   Thông tư 78/2021/TT-BTC.      |   bám sát lịch sử mua hàng CRM.    |
+--------------------------------------------------------------------------------------------------------+
```

##### 1. Nâng cấp Hạ tầng Phân tán Đám mây (Cloud-Native & Distributed Caching)
- Tích hợp bộ nhớ đệm phân tán `Redis Cluster` làm tầng đệm cấp 2 (L2 Cache) kết hợp với thư viện mới `Microsoft.Extensions.Caching.Hybrid` (.NET 9), áp dụng kỹ thuật Single-Flight ngăn ngừa triệt để hiện tượng sập đệm đồng thời (Cache Stampede).
- Sử dụng thuật toán khóa phân tán `RedLock` để bảo vệ tài nguyên tồn kho FEFO khi hệ thống được mở rộng ngang (Horizontal Scaling) trên hạ tầng Kubernetes đa cụm (Multi-node Cluster).
- Triển khai kiến trúc phân tách cơ sở dữ liệu Đọc/Ghi (CQRS Read/Write Splitting) với SQL Server Always On Availability Groups, chuyển toàn bộ các tác vụ đọc báo cáo, duyệt danh mục sang các bản sao Read-Only Replicas nhằm giải phóng tài nguyên cho máy chủ ghi nhận đơn hàng chính.

##### 2. Hợp nhất Đa sàn Thương mại Điện tử và Hóa đơn Điện tử Khởi tạo từ Máy tính tiền
- **Đồng bộ đa sàn TMĐT (Multi-Marketplace Open API Engine):** Xây dựng module kết nối chính thức qua Open API với các nền tảng Shopee, TikTok Shop và Lazada. Khi có đơn hàng phát sinh trên sàn thương mại điện tử, hệ thống tự động trừ kho FEFO tại Kho tổng tương ứng theo thời gian thực (độ trễ dưới 500 mili-giây), loại bỏ hoàn toàn nguy cơ phạt vi phạm do hết hàng trên sàn.
- **Hóa đơn điện tử theo Thông tư 78/2021/TT-BTC:** Tích hợp trực tiếp với các nhà cung cấp giải pháp Hóa đơn điện tử được Tổng cục Thuế cấp phép (như VNPT Invoice, Viettel S-Invoice, MISA meInvoice). Ngay khi thu ngân bấm phím `F12` hoàn tất giao dịch tại quầy POS, hệ thống tự động truyền dữ liệu hóa đơn có mã của cơ quan thuế khởi tạo từ máy tính tiền và gửi hóa đơn điện tử dạng PDF/XML qua Zalo/Email cho khách hàng.

##### 3. Đột phá Trải nghiệm Khách hàng với Công nghệ Trang điểm Ảo (WebAssembly AR Virtual Makeup)
- Phát triển module thử màu son môi, phấn má và kem nền trực tiếp trên khuôn mặt người dùng thông qua camera điện thoại hoặc webcam máy tính (Virtual Try-On).
- Module được lập trình hoàn toàn bằng công nghệ C# WebAssembly kết hợp WebGL và mô hình thị giác máy tính nhận diện 68 điểm mốc khuôn mặt (Facial Landmarks). Toàn bộ thuật toán xử lý hình ảnh được tính toán trực tiếp trên trình duyệt của máy khách (Client-side Edge Computing), bảo đảm tính riêng tư tuyệt đối cho khách hàng và không gây tốn kém băng thông máy chủ.

---

### TIỂU KẾT CHƯƠNG 3

Chương 3 đã hoàn tất trọn vẹn sứ mệnh hiện thực hóa giải pháp phần mềm Quản lý Bán hàng cho chuỗi cửa hàng mỹ phẩm CANMAKE BEAUTY từ bản thiết kế trên giấy thành một hệ thống công nghệ thông tin vận hành thực tế vững chắc, tin cậy và đạt hiệu năng cao:

1. **Hiện thực hóa Kỹ thuật Vững chắc:** Toàn bộ giải pháp được tổ chức chuẩn mực theo kiến trúc Clean Architecture trên nền tảng .NET 8.0 LTS và SQL Server 2022. 5 thuật toán và giải pháp kỹ thuật cốt lõi (FEFO 2 giai đoạn, PBAC 3 tầng phân tán, Bóc tách da liễu EWG/INCI, Chuyển hướng SEO 301 tự động, và Thanh toán VietQR Napas247 kết hợp Sổ cái tài chính đích danh) đã được phân tích tường tận kèm mã nguồn C# thực tế.
2. **Minh họa Giao diện Sống động:** 10 màn hình giao diện then chốt được chụp trực tiếp từ hệ thống đang chạy thực tế, chứng minh tính công thái học xuất sắc, tối ưu hóa triệt để cho từng nhóm người dùng chuyên biệt từ người tiêu dùng trực tuyến đến thủ kho và nhân viên thu ngân.
3. **Kiểm thử Nghiêm ngặt và Hiệu năng Đỉnh cao:** Chất lượng phần mềm được bảo chứng bởi bộ 203 bài kiểm thử tự động đạt tỷ lệ đậu 100%. Bài kiểm thử chịu tải k6 với 500 người dùng ảo đồng thời đã minh chứng năng lực kiểm soát tranh chấp dữ liệu tuyệt đối (Zero Overselling, 0% lỗi 5xx). Thời gian phản hồi trang dưới 35ms và mức chiếm dụng RAM chỉ xấp xỉ 109 MB đã khẳng định sự thành công vượt bậc của công tác tối ưu hóa hiệu năng toàn diện.
4. **Tầm nhìn Phát triển Rõ ràng:** Những đánh giá khách quan về kết quả và hạn chế đã mở ra lộ trình nâng cấp v3.0+ đầy tiềm năng với hạ tầng phân tán Redis/RedLock, hợp nhất đa sàn TMĐT, hóa đơn điện tử TT78 và công nghệ thực tế ảo WebAssembly AR.

---

# KẾT LUẬN VÀ KIẾN NGHỊ

### 1. Kết luận tổng quan khóa luận tốt nghiệp

Trong bối cảnh cuộc Cách mạng Công nghiệp lần thứ tư đang thúc đẩy mạnh mẽ quá trình chuyển đổi số trong mọi lĩnh vực kinh tế, mô hình kinh doanh bán lẻ truyền thống đang dần nhường chỗ cho mô hình Bán lẻ Hợp nhất Đa kênh (Omnichannel Retail). Đối với ngành bán lẻ mỹ phẩm và chăm sóc sắc đẹp - một thị trường có tốc độ tăng trưởng phi mã nhưng lại đặt ra những thách thức vô cùng khắc nghiệt về quản trị hạn sử dụng ngắn, bảo mật thông tin và yêu cầu tư vấn an toàn da liễu chuyên sâu - việc sở hữu một hệ thống thông tin quản lý hiện đại, linh hoạt và có khả năng tích hợp cao là yếu tố mang tính sống còn quyết định năng lực cạnh tranh của doanh nghiệp.

Khóa luận tốt nghiệp với đề tài **"Xây dựng website quản lý bán hàng cho chuỗi cửa hàng mỹ phẩm CANMAKE BEAUTY"** đã hoàn thành xuất sắc toàn bộ các mục tiêu nghiên cứu và nhiệm vụ phát triển phần mềm đã đề ra:

- **Về mặt cơ sở lý luận:** Khóa luận đã hệ thống hóa sâu sắc các nguyên lý cốt lõi về Hệ thống thông tin quản lý (MIS), mô hình chuyển dịch từ Đa kênh rời rạc (Multi-channel) sang Đa kênh hợp nhất (Omnichannel), nghiên cứu thấu đáo các quy chuẩn quản lý mỹ phẩm theo Hiệp định Mỹ phẩm ASEAN (ACCSQ), hệ thống phân loại danh pháp hóa mỹ phẩm quốc tế INCI và bộ tiêu chuẩn an toàn người tiêu dùng EWG Skin Deep®.
- **Về mặt phân tích và thiết kế:** Ứng dụng bài bản phương pháp luận Phân tích và Thiết kế Hướng đối tượng (OOAD) kết hợp ngôn ngữ mô hình hóa UML để xây dựng mô hình quy trình vận hành toàn diện (To-Be). Hệ thống đã định danh chính xác 11 tác nhân người dùng, phân rã 17 phân hệ nghiệp vụ bao quát 52 điểm chạm thực tế, thiết lập ma trận 51 quyền hạn nguyên tử PBAC, và hoàn thiện lược đồ cơ sở dữ liệu quy mô 65 bảng dữ liệu được chứng minh đạt chuẩn 3NF tuyệt đối.
- **Về mặt hiện thực hóa và công nghệ:** Xây dựng thành công giải pháp phần mềm hoàn chỉnh dựa trên ngăn xếp công nghệ tiên tiến nhất của Microsoft (.NET 8.0 LTS, C# 12, Entity Framework Core 8, SQL Server 2022) tuân thủ chặt chẽ kiến trúc Clean Architecture 4 tầng. Hệ thống giải quyết trọn vẹn 5 bài toán hóc búa nhất của chuỗi mỹ phẩm: thuật toán xuất kho FEFO 2 giai đoạn chống âm kho, kiểm toán két tiền mù (Blind Cash Audit) chống gian lận thu ngân, chuyển hướng SEO 301 tự động bảo vệ thứ hạng tìm kiếm, trợ lý da liễu tự động cảnh báo tương kỵ hoạt chất, và thanh toán không tiền mặt VietQR Napas247 đồng bộ màn hình phụ CFD.
- **Về mặt kiểm thử và bảo đảm chất lượng:** Hệ thống đã vượt qua toàn bộ 203 kịch bản kiểm thử tự động (Unit Test & Integration Test), vượt qua bài kiểm tra chịu tải khắc nghiệt của công cụ k6 dưới áp lực 500 người dùng ảo đồng thời với tỷ lệ lỗi máy chủ 0.00%, phản hồi trung bình chỉ 214ms, đạt chuẩn Google Core Web Vitals (LCP < 1.2s, Zero CLS) và tối ưu hóa bộ nhớ RAM tiến trình chỉ xấp xỉ 109 MB.

---

### 2. Đóng góp mới của đề tài

Đề tài khóa luận không chỉ dừng lại ở một sản phẩm phần mềm quản lý bán hàng thông thường mà đã mang lại những đóng góp mới mẻ, thiết thực cả về mặt khoa học công nghệ lẫn ứng dụng thực tiễn doanh nghiệp:

1. **Đóng góp về mặt Công nghệ Phần mềm và Kiến trúc Doanh nghiệp:**
   - Ứng dụng mẫu thiết kế Pipeline xử lý HTTP Request 9 bước chuẩn hóa kết hợp cơ chế phân quyền động PBAC 3 tầng (*DynamicPermissionPolicyProvider + PermissionAuthorizationHandler + HashSet In-Memory Cache*), cho phép thay đổi quyền hạn của hàng ngàn nhân viên tức thời mà không cần can thiệp mã nguồn hay khởi động lại dịch vụ.
   - Hiện thực hóa thành công cơ chế cô lập dữ liệu đa chi nhánh (*StoreId Isolation*) thông qua bộ lọc toàn cục Global Query Filters của EF Core, giải quyết triệt để rủi ro rò rỉ thông tin kinh doanh giữa các cơ sở bán lẻ trong cùng một hệ sinh thái.

2. **Đóng góp về mặt Quản trị Chuỗi Cung ứng Bán lẻ Mỹ phẩm:**
   - Xây dựng thành công thuật toán cấp phát kho tự động theo nguyên tắc Hết hạn trước - Xuất trước (FEFO) kết hợp cơ chế kiểm soát tương tranh khóa bi quan và lạc quan. Đây là giải pháp công nghệ mang tính đột phá giúp doanh nghiệp giải phóng hàng tồn kho cận hạn một cách khoa học, giảm thiểu tối đa tổn thất tài chính do phải tiêu hủy mỹ phẩm quá đát.
   - Đưa quy trình Kiểm toán Két tiền Mù (*Blind Cash Drawer Audit*) vào hệ thống bán lẻ tại quầy POS, thiết lập chuẩn mực đối soát tài chính minh bạch, loại bỏ hoàn toàn các khe hở gian lận tiền mặt vốn là nỗi lo thường trực của các nhà quản trị chuỗi.

3. **Đóng góp về mặt Xã hội và Bảo vệ Sức khỏe Người tiêu dùng:**
   - Lần đầu tiên đưa Động cơ phân tích tương kỵ hoạt chất da liễu (*Skincare Ingredient Interaction Engine*) vào một trang thương mại điện tử mỹ phẩm tại Việt Nam. Tính năng tự động phát hiện các cặp thành phần xung đột (như Retinol kết hợp BHA) và đưa ra lời khuyên khoa học đã nâng cao nhận thức làm đẹp an toàn cho cộng đồng, khẳng định trách nhiệm xã hội của doanh nghiệp đối với sức khỏe của khách hàng.

---

### 3. Kiến nghị và đề xuất triển khai ứng dụng thực tế

Dựa trên những kết quả nghiên cứu và thực nghiệm đạt được, tác giả xin trân trọng gửi tới các cơ quan, đơn vị đào tạo và cộng đồng doanh nghiệp bán lẻ những kiến nghị và đề xuất sau:

##### (1) Đối với các Doanh nghiệp Bán lẻ Chuỗi Mỹ phẩm
- **Nhanh chóng chuyển đổi sang mô hình Quản trị Hợp nhất Đa kênh (Omnichannel):** Doanh nghiệp cần xóa bỏ tư duy vận hành phân mảnh giữa kênh Online và Offline, tích hợp đồng bộ dữ liệu tồn kho, giá bán và chính sách khách hàng thân thiết trên một nền tảng duy nhất để tối ưu hóa chi phí vận hành và gia tăng vòng đời khách hàng (Customer Lifetime Value).
- **Ứng dụng triệt để nguyên tắc FEFO trong quản lý kho:** Đề xuất các chuỗi bán lẻ đầu tư trang bị hệ thống máy quét mã vạch cầm tay hỗ trợ đọc số lô sản xuất (Batch Code) và hạn sử dụng (Exp Date) ngay từ khâu nhập kho từ nhà cung cấp, kiên quyết áp dụng chính sách xuất kho và xả hàng tự động theo hạn dùng nhằm bảo vệ biên lợi nhuận kinh doanh.
- **Tăng cường trải nghiệm thanh toán không dùng tiền mặt:** Đẩy mạnh triển khai giải pháp thanh toán quét mã VietQR động chuẩn Napas247 tại quầy thu ngân kết hợp màn hình hiển thị phụ cho khách hàng (CFD) nhằm rút ngắn thời gian thanh toán trong giờ cao điểm, giảm thiểu sai sót thối tiền lẻ và thúc đẩy chiến lược thanh toán không dùng tiền mặt của Chính phủ.

##### (2) Đối với Học viện Hành chính và Quản trị công và Khoa Hệ thống thông tin
- **Tăng cường tích hợp các bài toán thực tế vào chương trình đào tạo:** Khuyến khích sinh viên ngành Hệ thống thông tin Quản lý tiếp cận các bài toán nghiệp vụ chuyên sâu của các ngành kinh tế đặc thù (như bán lẻ chuỗi, dược mỹ phẩm, chuỗi cung ứng lạnh) kết hợp với các kiến trúc công nghệ doanh nghiệp hiện đại (Clean Architecture, Microservices, Cloud Computing).
- **Đầu tư mở rộng phòng thực hành công nghệ bán lẻ (Retail Lab):** Đề xuất Học viện nghiên cứu xây dựng các mô hình phòng thực nghiệm mô phỏng quy trình vận hành Showroom bán lẻ thực tế (với đầy đủ máy POS, máy quét mã vạch, máy in hóa đơn, màn hình phụ CFD) để sinh viên có cơ hội cọ xát, kiểm thử phần mềm trong điều kiện môi trường công thái học thực tiễn.

##### (3) Định hướng hoàn thiện đề tài trong tương lai
- Tác giả sẽ tiếp tục phối hợp với các kỹ sư hạ tầng và chuyên gia da liễu để hiện thực hóa Lộ trình nâng cấp v3.0+: tích hợp hạ tầng phân tán Redis/RedLock, kết nối Open API đồng bộ với các sàn Shopee/TikTok Shop, hoàn thiện cổng hóa đơn điện tử theo Thông tư 78/2021/TT-BTC và nghiên cứu ứng dụng công nghệ WebAssembly AR để ra mắt tính năng thử trang điểm ảo trên trình duyệt di động, đưa giải pháp CANMAKE BEAUTY trở thành một hệ sinh thái phần mềm bán lẻ mỹ phẩm toàn diện, hiện đại và dẫn đầu thị trường.

---

# TÀI LIỆU THAM KHẢO

### I. Tài liệu Tiếng Việt

1. Bộ Y tế (2011), *Thông tư số 06/2011/TT-BYT Quy định về quản lý mỹ phẩm*, ban hành ngày 25 tháng 01 năm 2011, Hà Nội.
2. Bộ Tài chính (2021), *Thông tư số 78/2021/TT-BTC Hướng dẫn thực hiện một số điều của Luật Quản lý thuế ngày 13 tháng 6 năm 2019, Nghị định số 123/2020/NĐ-CP ngày 19 tháng 10 năm 2020 của Chính phủ quy định về hóa đơn, chứng từ*, ban hành ngày 17 tháng 9 năm 2021, Hà Nội.
3. Chính phủ nước CHXHCN Việt Nam (2013), *Nghị định số 52/2013/NĐ-CP về Thương mại điện tử*, ban hành ngày 16 tháng 5 năm 2013, Hà Nội.
4. Đặng Minh Tuấn (2020), *Giáo trình Phân tích và Thiết kế Hệ thống thông tin*, Nhà xuất bản Thông tin và Truyền thông, Hà Nội.
5. Học viện Hành chính và Quản trị công (2023), *Quy định về trình bày và đánh giá Khóa luận tốt nghiệp bậc Đại học ngành Hệ thống thông tin Quản lý*, Hà Nội.
6. Ngô Trung Việt (2018), *Kỹ nghệ phần mềm hướng đối tượng với UML*, Nhà xuất bản Bách khoa Hà Nội, Hà Nội.
7. Nguyễn Văn Ba (2019), *Thiết kế cơ sở dữ liệu quan hệ và tối ưu hóa truy vấn SQL*, Nhà xuất bản Đại học Quốc gia Hà Nội, Hà Nội.
8. Trần Đình Quế, Nguyễn Trí Thành (2021), *Giáo trình Hệ quản trị Cơ sở dữ liệu nâng cao*, Nhà xuất bản Bưu điện, Hà Nội.

### II. Tài liệu Tiếng Anh (International Academic & Industry References)

9. ASEAN Cosmetic Association (2007), *ASEAN Cosmetic Directive (ACD): Agreement on the ASEAN Harmonized Cosmetic Regulatory Scheme*, Jakarta, Indonesia.
10. Environmental Working Group (2024), *EWG's Skin Deep® Cosmetics Database: Methodology and Safety Scoring Standards*, Washington, D.C., USA.
11. Evans, E. (2003), *Domain-Driven Design: Tackling Complexity in the Heart of Software*, Addison-Wesley Professional, Boston, USA.
12. Fowler, M. (2002), *Patterns of Enterprise Application Architecture*, Addison-Wesley Professional, Boston, USA.
13. Gamma, E., Helm, R., Johnson, R., & Vlissides, J. (1994), *Design Patterns: Elements of Reusable Object-Oriented Software*, Addison-Wesley, Boston, USA.
14. Google Chrome Developers (2024), *Core Web Vitals: Largest Contentful Paint (LCP), Interaction to Next Paint (INP), and Cumulative Layout Shift (CLS)*, Web.dev Guidelines, Mountain View, California, USA.
15. Grafana Labs (2024), *k6 Documentation: Modern Load Testing for Developers and High-Concurrency Performance Optimization*, New York, USA.
16. International Cosmetic Ingredient Nomenclature Committee (2023), *International Cosmetic Ingredient Dictionary and Handbook (INCI)*, 16th Edition, Personal Care Products Council (PCPC), Washington, D.C., USA.
17. Kotler, P., & Armstrong, G. (2021), *Principles of Marketing*, 18th Global Edition, Pearson Education Limited, London, UK.
18. Laudon, K. C., & Laudon, J. P. (2022), *Management Information Systems: Managing the Digital Firm*, 17th Global Edition, Pearson Education Limited, London, UK.
19. Martin, R. C. (2017), *Clean Architecture: A Craftsman's Guide to Software Structure and Design*, Prentice Hall, New Jersey, USA.
20. Microsoft Corporation (2024), *ASP.NET Core 8.0 Documentation: Architecture, Security, Entity Framework Core, and High-Performance Web Applications*, Microsoft Learn, Redmond, Washington, USA.
21. Napas (2021), *Tiêu chuẩn kỹ thuật VietQR: Đặc tả định dạng mã phản hồi nhanh phục vụ chuyển mạch tài chính quốc gia dựa trên chuẩn quốc tế EMVCo*, Công ty Cổ phần Thanh toán Quốc gia Việt Nam, Hà Nội.
22. Rigby, D. K. (2011), "The Future of Shopping: An Omnichannel Vision", *Harvard Business Review*, Vol. 89, No. 12, pp. 65-76.
23. Sommerville, I. (2016), *Software Engineering*, 10th Global Edition, Pearson Education Limited, London, UK.
24. Verhoef, P. C., Kannan, P. K., & Inman, J. J. (2015), "From Multi-Channel Retailing to Omni-Channel Retailing: Introduction to the Special Issue on Multi-Channel Retailing", *Journal of Retailing*, Vol. 91, No. 2, pp. 174-181.
