# CHƯƠNG 3 (TIẾP THEO) – MỤC 3.2.2: PHÂN HỆ GIAO DIỆN QUẢN TRỊ ĐIỀU HÀNH VÀ BÁN LẺ ĐA KÊNH

## 3.2.2. Phân hệ Giao diện Quản trị Điều hành & Bán lẻ Đa kênh (Admin Back-Office, Smart POS, Kho Lô FEFO, Visual Site Customizer & Tài chính)

Nếu phân hệ Storefront là bộ mặt thương hiệu sang trọng tiếp xúc với người tiêu dùng, thì phân hệ Quản trị Điều hành và Bán lẻ Đa kênh (Admin Back-Office & Retail System) chính là bộ máy xương sống vận hành toàn bộ chuỗi bán lẻ CANMAKE BEAUTY. Được thiết kế chuyên biệt theo tiêu chuẩn công thái học phần mềm doanh nghiệp, phân hệ cung cấp các công cụ sắc bén cho từng cấp quản lý và nhân viên tác nghiệp: từ bảng điều khiển điều hành số (Digital Executive Dashboard), trình thiết kế giao diện không cần lập trình (Visual Site Customizer), hệ thống ma trận phân quyền động PBAC 51 quyền hạt nhân, công cụ giám sát hạn dùng kho dược mỹ phẩm FEFO, đến màn hình máy tính tiền quầy bán lẻ Smart POS màn hình đôi và sổ cái tài chính giá vốn đích danh theo từng số lô hàng.

Dưới đây là phân tích chi tiết 9 màn hình quản trị và vận hành cốt lõi của hệ thống, bao gồm hướng dẫn thực nghiệm chụp ảnh minh chứng từ hệ thống đang chạy thực tế, mô tả trực quan bố cục và các chỉ số giao diện, cùng phân tích chuyên sâu về nghiệp vụ, phân quyền PBAC và luồng xử lý dữ liệu tầng Backend kết nối cơ sở dữ liệu.

---

### 1. Hình 3.8 – Giao diện Trung tâm Điều hành Quản trị (Admin Dashboard), Biểu đồ Doanh thu Đa kênh & Trung tâm Thông báo Đẩy SignalR

Trung tâm Điều hành Quản trị là tháp chỉ huy số dành riêng cho Ban Giám đốc và các Quản lý Cửa hàng, tổng hợp toàn bộ các dòng vận động của dòng tiền, hàng hóa và khách hàng trên toàn chuỗi theo thời gian thực.

> **[HƯỚNG DẪN CHỤP & CHÈN HÌNH 3.8 - GIAO DIỆN TRUNG TÂM ĐIỀU HÀNH ADMIN DASHBOARD]**  
> - **Tệp hình ảnh có sẵn trong hệ thống:** `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_4_Giao_Dien_Bang_Dieu_Khien_Admin_Dashboard.png`.  
> - **Quy trình mở trình duyệt và chụp ảnh thực tế:**  
>   + *Bước 1:* Truy cập trang đăng nhập quản trị tại địa chỉ `http://localhost:5116/admin/auth/login`.  
>   + *Bước 2:* Đăng nhập bằng tài khoản Quản trị viên tối cao: Tên đăng nhập `superadmin` / Mật khẩu `Admin@123` (hoặc tài khoản Quản lý cửa hàng Cầu Giấy `manager_cg` / `Admin@123`).  
>   + *Bước 3:* Sau khi đăng nhập thành công, hệ thống tự động chuyển hướng về trang `http://localhost:5116/Admin/Dashboard`.  
>   + *Bước 4:* Quan sát thanh công cụ đầu trang với bộ lọc chi nhánh `StoreSelector`, 6 thẻ chỉ số KPI thời gian thực, biểu đồ đường cong xu hướng doanh thu Online vs POS, danh sách cảnh báo tồn kho lô hạn khẩn cấp và biểu tượng chuông thông báo đẩy SignalR ở góc phải trên cùng.  
>   + *Bước 5:* Dùng công cụ chụp ảnh màn hình lưu lại toàn cảnh giao diện làm việc này để chèn vào báo cáo.

```
+--------------------------------------------------------------------------------------------------------------------+
| CANMAKE BEAUTY ADMIN | Chi nhánh: [ Toàn chuỗi v ] | Kỳ báo cáo: [ Tháng này v ] | (🔔 3) [Tin nhắn: 2] [SuperAdmin v]|
+--------------------------------------------------------------------------------------------------------------------+
| [ THẺ 1: DOANH THU CHUỖI ]  | [ THẺ 2: ĐƠN CHỜ DUYỆT ]   | [ THẺ 3: DOANH SỐ POS ]    | [ THẺ 4: LÔ CẬN DATE FEFO] |
| 1.845.250.000 đ            | 28 Đơn hàng mới            | 48.650.000 đ (Hôm nay)     | 12 Lô cần xả hàng          |
| (▲ +14.8% so với tháng trước)| (15 Đơn Online, 13 O2O)    | (▲ +8.5% so với hôm qua)   | (Hạn dùng dưới 60 ngày)    |
+-----------------------------+----------------------------+----------------------------+----------------------------+
| [ THẺ 5: ĐIỀU CHUYỂN CHỜ ]  | [ THẺ 6: KHÁCH HÀNG MỚI ]  | TỶ SUẤT LỢI NHUẬN GMROI    | ĐÁNH GIÁ HÀI LÒNG KHÁCH    |
| 03 Phiếu yêu cầu chuyển kho | 342 Thành viên mới         | 3.42 x (Hiệu quả vốn tốt)  | ⭐⭐⭐⭐⭐ 4.92 / 5.0 (NPS: 86) |
+--------------------------------------------------------------------------------------------------------------------+
| BIỂU ĐỒ DIỄN BIẾN DOANH THU ĐA KÊNH THEO NGÀY (MULTI-CHANNEL REVENUE TREND - CHART.JS)                             |
| [Triệu VNĐ]                                                                                                        |
| 120M |                           /╲                                  ── Đơn hàng trực tuyến Storefront Web         |
| 100M |              /╲          /  ╲             /╲                  - - Doanh số bán trực tiếp tại quầy POS       |
|  80M |    /╲       /  ╲        /    ╲           /  ╲        /╲                                                     |
|  60M |   /  ╲-----/    ╲------/      ╲---------/    ╲------/  ╲                                                    |
|  40M |  /                                                                                                          |
|   0M +----+-------+-------+-------+-------+-------+-------+-------+----+----> [Ngày 01 đến Ngày 30 trong tháng]    |
+-------------------------------------------------------------------+------------------------------------------------+
| CẢNH BÁO TỒN KHO LÔ HẠN FEFO KHẨN CẤP (WATCHLIST)                 | LUỒNG GIAO DỊCH THỜI GIAN THỰC (SIGNALR FEED)  |
| - Lô LOT-2024-C12: Kem Chống Nắng Anessa (Còn 24 ngày) -> [XẢ DATE]| 14:35: Đơn ORD-8922: 420.000đ (POS Cầu Giấy)  |
| - Lô LOT-2024-A08: Serum B5 La Roche (Còn 42 ngày)     -> [XẢ DATE]| 14:32: Đơn ORD-8921: 643.000đ (O2O Nhận 1H)   |
| - Lô LOT-2024-B15: Son Kem 3CE Velvet (Còn 55 ngày)    -> [CHUYỂN] | 14:30: Khách hàng Lê Hoàng Yến thăng hạng VIP  |
| - Lô LOT-2023-F01: Mặt Nạ Banobagi (ĐÃ HẾT HẠN)        -> [TIÊU HỦY| 14:28: Phiếu chuyển kho TRF-003 đã được duyệt  |
+-------------------------------------------------------------------+------------------------------------------------+
```

#### Mô tả trực quan bố cục giao diện và các chỉ số hiển thị
- **Thanh điều hướng tối cao và lọc phạm vi hoạt động:** Bố trí dropdown chọn chi nhánh `StoreSelector`, hỗ trợ lọc dữ liệu toàn chuỗi hoặc thu hẹp vào từng Showroom cơ sở (Showroom Cầu Giấy, Showroom Ba Đình, Kho Tổng TP.HCM). Bên phải là biểu tượng chuông thông báo SignalR hiển thị số lượng sự kiện chưa đọc, nút truy cập hộp thư trao đổi nội bộ (`/Admin/Message`) và menu hồ sơ quản trị.
- **Hệ thống 6 Thẻ chỉ số hiệu năng cốt lõi (Core KPI Cards):**
  + *Thẻ 1 - Tổng doanh thu chuỗi:* Thể hiện doanh thu thuần hợp nhất sau khi trừ chiết khấu, kèm tỷ lệ phần trăm tăng trưởng so với cùng kỳ tháng trước.
  + *Thẻ 2 - Đơn hàng chờ xử lý:* Thống kê số lượng đơn hàng mới phát sinh cần duyệt, phân tách rõ giữa đơn giao tận nhà và đơn O2O nhận tại Showroom.
  + *Thẻ 3 - Doanh số POS hôm nay:* Đo lường tiền bán lẻ thu được trực tiếp tại quầy tính tiền trong ngày.
  + *Thẻ 4 - Lô cận date FEFO:* Cảnh báo số lượng lô mỹ phẩm có hạn dùng dưới 60 ngày cần giải phóng.
  + *Thẻ 5 - Phiếu điều chuyển chờ duyệt:* Báo cáo số lượng yêu cầu luân chuyển hàng hóa giữa các kho chưa được Giám đốc phê duyệt.
  + *Thẻ 6 - Khách hàng mới tích lũy:* Đo lường mức độ phát triển tệp khách hàng định danh thành viên.
- **Biểu đồ xu hướng doanh thu đa kênh (Interactive Multi-Channel Chart):** Sử dụng thư viện Chart.js hiển thị biểu đồ đường đôi so sánh tương quan giữa kênh Online và Offline, giúp nhận diện sự dịch chuyển hành vi mua sắm của khách hàng trong các dịp lễ và cuối tuần.
- **Luồng cảnh báo kho FEFO và Giao dịch thời gian thực (SignalR Live Stream):** Cập nhật từng giây các giao dịch mua hàng, mở ca thu ngân, thăng hạng VIP và phiếu điều chuyển kho mà không cần người dùng phải bấm F5 tải lại trang.

#### Phân tích nghiệp vụ, phân quyền PBAC và luồng xử lý dữ liệu phía sau
- **Phân quyền PBAC:** Endpoint được bảo vệ nghiêm ngặt bởi thuộc tính `[Authorize]` và quyền hạt nhân `[HasPermission("DASHBOARD_VIEW")]`. Quản trị viên `SuperAdmin` có quyền xem số liệu toàn chuỗi; trong khi vai trò `StoreManager` bị giới hạn phạm vi dữ liệu tự động theo cơ chế cô lập chi nhánh `IStoreScopedEntity`, chỉ xem được doanh thu và tồn kho của Showroom mình phụ trách.
- **Luồng xử lý dữ liệu Backend:** Controller `DashboardController.Index()` trích xuất `StoreId` từ Claims của người dùng đăng nhập. Dịch vụ `IDashboardService.GetExecutiveMetricsAsync()` thực thi các câu lệnh LINQ to Entities tổng hợp song song:
  + Tính doanh thu: `.Where(o => o.Status == "COMPLETED" && o.CreatedAt >= startOfMonth).SumAsync(o => o.FinalTotal)`.
  + Đếm đơn chờ xử lý: `.CountAsync(o => o.Status == "PENDING" || o.Status == "PAID")`.
  + Quét lô cận date: `.CountAsync(b => b.Status == "ACTIVE" && b.ExpDate <= now.AddDays(60) && b.CurrentQuantity > 0)`.
- **Động cơ đẩy thông báo thời gian thực:** Lớp kết nối SignalR `NotificationHub` lắng nghe sự kiện từ các dịch vụ nghiệp vụ. Khi có một đơn hàng mới được tạo hoặc thanh toán thành công, phương thức `Clients.Group($"Store_{storeId}").SendAsync("ReceiveOrderNotification", payload)` lập tức gửi gói tin WebSocket tới trình duyệt của quản lý cửa hàng, cập nhật thẻ KPI và phát âm thanh thông báo chỉ trong 12 mili-giây.

---

### 2. Hình 3.9 – Giao diện Quản trị Cây Menu Đa hình & Tự động Hóa Chuyển hướng SEO 301

Giao diện quản trị menu mang lại khả năng tùy biến cấu trúc cây điều hướng trang web theo thời gian thực mà không phụ thuộc vào đội ngũ kỹ thuật, đồng thời tích hợp cơ chế bảo toàn điểm chất lượng tìm kiếm Google thông qua bảng chuyển hướng 301 tự động.

> **[HƯỚNG DẪN CHỤP & CHÈN HÌNH 3.9 - GIAO DIỆN QUẢN TRỊ CÂY MENU ĐA HÌNH VÀ SEO 301]**  
> - **Tệp hình ảnh có sẵn trong hệ thống:** `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_5_Giao_Dien_Quan_Tri_Menu_Da_Hinh_SEO.png`.  
> - **Quy trình mở trình duyệt và chụp ảnh thực tế:**  
>   + *Bước 1:* Đăng nhập tài khoản `superadmin` / `Admin@123` tại `http://localhost:5116/admin/auth/login`.  
>   + *Bước 2:* Trên menu thanh bên trái, chọn mục "Hệ Thống CMS" -> "Quản Trị Menu Đa Hình" hoặc truy cập trực tiếp `http://localhost:5116/Admin/Menu` (hoặc `http://localhost:5116/Admin/MenuManagement`).  
>   + *Bước 3:* Quan sát cấu trúc cây phân cấp danh mục kéo thả ở cột bên trái và bảng thiết lập thuộc tính nút menu ở cột bên phải.  
>   + *Bước 4:* Thử đổi đường dẫn thân thiện (Slug) của một danh mục và chuyển sang tab "Lịch Sử Chuyển Hướng SEO 301" để thấy hệ thống tự động ghi lại ánh xạ từ URL cũ sang URL mới.  
>   + *Bước 5:* Nhấn nút "Xóa Bộ Đệm Cache Menu" để kiểm tra thông báo Toast màu xanh đồng bộ dữ liệu ra Storefront và chụp ảnh màn hình chèn vào báo cáo.

```
+--------------------------------------------------------------------------------------------------------------------+
| QUẢN TRỊ MENU ĐA HÌNH VÀ BẢO TOÀN THỨ HẠNG SEO 301           [+ THÊM NÚT MENU MỚI] [🧹 XÓA CACHE MENU ĐỒNG BỘ]    |
+-------------------------------------------------------------------+------------------------------------------------+
| CÂY ĐIỀU HƯỚNG PHÂN CẤP KÉO THẢ (DRAG & DROP TREE VIEW)           | THIẾT LẬP THUỘC TÍNH NÚT MENU (SELECTED NODE)  |
|                                                                   | Tên hiển thị nút: [ Chăm Sóc Da (Skincare)   ] |
| [-] 1. CHĂM SÓC DA (SKINCARE)                        (Thứ tự: 10) | Vị trí menu:      [ Header Chính (MainMegaMenu)] |
|     |-- [+] 1.1. Làm Sạch Da                         (Thứ tự: 1)  | Cấp bậc cha:      [ Không có (Menu Gốc)      ] |
|     |-- [-] 1.2. Serum & Tinh Chất Đặc Trị           (Thứ tự: 2)  | Thứ tự sắp xếp:   [ 10                       ] |
|     |       |-- 1.2.1. Serum Trị Mụn (BHA/AHA)       (Thứ tự: 1)  | ---------------------------------------------- |
|     |       `-- 1.2.2. Serum Chống Lão Hóa (Retinol) (Thứ tự: 2)  | LIÊN KẾT ĐA HÌNH (POLYMORPHIC TARGET BINDING): |
|     `-- [+] 1.3. Kem Dưỡng Ẩm Phục Hồi               (Thứ tự: 3)  | (o) Danh mục sản phẩm (Category): [ Serum    ] |
| [+] 2. TRANG ĐIỂM (MAKEUP)                           (Thứ tự: 20) | ( ) Thương hiệu đối tác (Brand)                |
| [+] 3. BỘ SƯU TẬP CLEAN BEAUTY                       (Thứ tự: 30) | ( ) Trang nội dung tĩnh CMS (Static Page)      |
| [+] 4. CHUYÊN TRANG XẢ KHO FEFO                      (Thứ tự: 40) | ( ) Đường dẫn tùy biến ngoài (Custom External) |
|     `-- 4.1. Flash Sale Cận Date (-70%)              (Thứ tự: 1)  | Huy hiệu đính kèm: [ HOT ] (Màu đỏ rực rỡ)     |
+-------------------------------------------------------------------+------------------------------------------------+
| BẢNG QUẢN LÝ LỊCH SỬ CHUYỂN HƯỚNG SEO 301 TỰ ĐỘNG (SLUG REDIRECTS TABLE):                                          |
| STT | ĐƯỜNG DẪN CŨ (OLD URL SLUG)       | ĐƯỜNG DẪN MỚI (NEW TARGET)        | LOẠI HTTP | LƯỢT HIT | NGÀY TẠO    |
| 01  | /catalog/serum-tri-mun-bha-cu     | /catalog/serum-tri-mun-bha-aha    | 301 Perm  | 1.420    | 12/10/2026  |
| 02  | /san-pham/kem-chong-nang-anessa-v1| /san-pham/kem-chong-nang-anessa   | 301 Perm  | 3.850    | 05/09/2026  |
+--------------------------------------------------------------------------------------------------------------------+
```

#### Mô tả trực quan bố cục giao diện và các chỉ số hiển thị
- **Cột trái - Cây điều hướng phân cấp trực quan:** Biểu diễn trực quan cây thư mục menu đa tầng lồng nhau không giới hạn độ sâu. Cho phép người quản trị trực tiếp kéo thả chuột để thay đổi thứ tự hiển thị hoặc chuyển đổi nút menu thành mục con của một danh mục khác.
- **Cột phải - Trình cấu hình liên kết đa hình (Polymorphic Binding Panel):** Khả năng liên kết linh hoạt tới 4 thực thể nghiệp vụ khác nhau trong cơ sở dữ liệu: Danh mục sản phẩm (Category), Thương hiệu phân phối (Brand), Bài viết cẩm nang CMS (Post) hoặc Đường dẫn tĩnh tùy biến. Ngoài ra còn hỗ trợ gán nhãn huy hiệu nổi bật (HOT, SALE, NEW) và chọn biểu tượng FontAwesome.
- **Nút dọn sạch bộ nhớ đệm Cache đồng bộ:** Nút bấm màu xanh trên thanh tiêu đề cho phép xóa tức thì bộ đệm menu trong RAM máy chủ, đồng bộ ngay lập tức cấu trúc mới ra Storefront khách hàng.
- **Bảng lịch sử chuyển hướng SEO 301 tự động:** Lưu vết toàn bộ các lần thay đổi Slug danh mục hoặc sản phẩm, thống kê số lượng truy cập từ các liên kết cũ của Googlebot được chuyển hướng thành công sang liên kết mới, ngăn ngừa triệt để lỗi 404 làm tụt thứ hạng tìm kiếm.

#### Phân tích nghiệp vụ, phân quyền PBAC và luồng xử lý dữ liệu phía sau
- **Phân quyền PBAC:** Yêu cầu quyền hạt nhân `[HasPermission("MENU_MANAGE")]`. Chỉ vai trò `SuperAdmin` hoặc `ContentEditor` được cấp quyền thao tác.
- **Luồng xử lý Backend:** Khi người dùng lưu thay đổi trên cây menu, Controller `MenuController.SaveMenuStructure()` duyệt đệ quy các nút và cập nhật bảng `NavigationMenus` (các trường `ParentId`, `DisplayOrder`, `TargetType`, `TargetId`).
- **Cơ chế SEO 301 thông minh:** Nếu thuộc tính `Slug` của một nút menu trỏ tới danh mục bị thay đổi từ `old-slug` sang `new-slug`, hệ thống kiểm tra sự tồn tại trong bảng `SlugRedirects`. Nếu chưa có, phương thức tự động thêm một bản ghi mới với `OldUrl = "/catalog/old-slug"`, `NewUrl = "/catalog/new-slug"` và mã `StatusCode = 301`. Khi khách hàng truy cập vào URL cũ, middleware `SlugRedirectMiddleware` trong HTTP Pipeline 9 bước sẽ chặn lại tại Bước 2, tra cứu bảng chuyển hướng và phản hồi ngay mã chuyển hướng vĩnh viễn `HTTP 301 Moved Permanently` mà không cần xử lý các tầng sau.
- **Đồng bộ Cache:** Ngay sau khi commit dữ liệu, lệnh `_memoryCache.Remove("GlobalNavigationMenuTree")` được kích hoạt để làm sạch vùng nhớ đệm, bảo đảm tính nhất quán dữ liệu tức thời.

---

### 3. Hình 3.10 – Giao diện Trình Tùy biến Giao diện Trực quan (No-Code Visual Site Customizer & Live Preview)

Visual Site Customizer là công cụ đồ họa cao cấp cho phép đội ngũ Marketing tự do thiết kế, sắp đặt bố cục trang chủ và thay đổi nhận diện thương hiệu trực quan theo cơ chế WYSIWYG (What You See Is What You Get) mà không cần can thiệp mã nguồn HTML/CSS.

> **[HƯỚNG DẪN CHỤP & CHÈN HÌNH 3.10 - GIAO DIỆN VISUAL SITE CUSTOMIZER]**  
> - **Quy trình mở trình duyệt và chụp ảnh thực tế:**  
>   + *Bước 1:* Đăng nhập bằng tài khoản `superadmin` hoặc `editor01` (`Admin@123`) tại `http://localhost:5116/admin/auth/login`.  
>   + *Bước 2:* Truy cập trực tiếp đường dẫn `http://localhost:5116/admin/customizer` trên thanh địa chỉ trình duyệt.  
>   + *Bước 3:* Màn hình làm việc chia 2 cột xuất hiện: Cột bên trái (30%) là thanh công cụ cấu hình Sections và bảng màu; Cột bên phải (70%) là khung Live Preview hiển thị trực tiếp Storefront qua thẻ iframe.  
>   + *Bước 4:* Thử kéo thả chuột hoán đổi vị trí giữa khối "Flash Sale FEFO" và khối "Bộ Sưu Tập Nổi Bật", hoặc đổi màu chủ đạo Primary Color từ Rose Gold sang Deep Wine; quan sát khung Preview bên phải đổi màu tức thì trong vòng 50ms mà không tải lại trang.  
>   + *Bước 5:* Chụp toàn cảnh màn hình làm việc 2 cột này để chèn vào báo cáo khóa luận.

```
+--------------------------------------------------------------------------------------------------------------------+
| TRÌNH TÙY BIẾN GIAO DIỆN TRỰC QUAN (VISUAL SITE CUSTOMIZER)          [ LƯU BẢN NHÁP (DRAFT) ] [ 🚀 XUẤT BẢN THỰC TẾ ] |
+--------------------------------------+-----------------------------------------------------------------------------+
| SIDEBAR CẤU HÌNH (30% WIDTH)         | KHUNG LIVE PREVIEW TRỰC QUAN (70% WIDTH - CHUẨN POSTMESSAGE < 50MS)        |
| Chọn trang: [ Trang Chủ (Home) v ]   | [ Chế độ xem: (🖥️ Máy tính)  (📱 Điện thoại) ] | URL: https://canmakebeauty.vn|
| ------------------------------------ | --------------------------------------------------------------------------- |
| 1. BẢNG MÀU THƯƠNG HIỆU (PALETTE):   | +-------------------------------------------------------------------------+ |
| - Màu chủ đạo: [ ■ #B76E79 ] (Rose)  | | [Thanh thông báo] Freeship đơn từ 500k | Hotline 1800 6868               | |
| - Màu điểm nhấn:[ ■ #1B4D3E ] (Green)| | [LOGO] CANMAKE BEAUTY  [ Tìm kiếm...              ]  [Wishlist] [Giỏ: 3]     | |
| - Màu nền trang:[ ■ #FFFDFB ] (Pearl)| | ----------------------------------------------------------------------- | |
| ------------------------------------ | | HERO BANNER MÙA THU (ẢNH MỚI CẬP NHẬT TỨC THÌ)                          | |
| 2. SẮP ĐẶT THỨ TỰ KHỐI (SECTIONS):   | | "ĐẠI TIỆC DƯỢC MỸ PHẨM CHUẨN FEFO - SALE ĐẾN 70%"                        | |
| [::] 1. Hero Banner Slider     [x]Bật| | [ KHÁM PHÁ NGAY ]                                                       | |
| [::] 2. FEFO Smart Clearance   [x]Bật| | ----------------------------------------------------------------------- | |
| [::] 3. Routine Quiz 4 Bước    [x]Bật| | FLASH SALE XẢ KHO CẬN DATE FEFO (ĐÃ ĐƯỢC KÉO LÊN VỊ TRÍ SỐ 2)           | |
| [::] 4. Lưới Sản Phẩm Nổi Bật  [x]Bật| | [HSD: 03/2027] Kem Nắng Anessa 60ml | Giá: 357.500 đ (-45%)             | |
| [::] 5. Cẩm Nang Da Liễu Blog  [ ]Tắt| | [HSD: 04/2027] Serum B5 La Roche    | Giá: 275.000 đ (-50%)             | |
| ------------------------------------ | +-------------------------------------------------------------------------+ |
| TRẠNG THÁI: Bản nháp đang sửa đổi    | ⚡ Phản hồi giao diện Live Preview bằng Window.postMessage() cực mượt      |
+--------------------------------------+-----------------------------------------------------------------------------+
```

#### Mô tả trực quan bố cục giao diện và các chỉ số hiển thị
- **Bố cục chia đôi không gian làm việc (Split-View Workspace):** Tỷ lệ 30:70 tối ưu cho thao tác vừa chỉnh sửa vừa quan sát kết quả. Cột bên trái tập trung toàn bộ các trường nhập liệu và điều khiển; cột bên phải mô phỏng chính xác khung nhìn của khách hàng với công cụ chuyển đổi nhanh giữa chế độ Desktop và Mobile.
- **Thanh công cụ cấu hình màu sắc và bố cục Section:** Cho phép thay đổi mã màu Hex của toàn bộ giao diện; danh sách các khối Sections hỗ trợ thao tác kéo thả Reorder mượt mà kèm nút công tắc Bật/Tắt (Toggle Visibility) cho phép ẩn tạm thời các khối sự kiện đã kết thúc mà không cần xóa dữ liệu.
- **Cơ chế tách biệt Bản Nháp và Bản Xuất Bản (Draft vs Published):** Cung cấp 2 nút bấm riêng biệt: nút "Lưu Bản Nháp" lưu giữ các thử nghiệm đang làm dở mà khách hàng bên ngoài không nhìn thấy; và nút "Xuất Bản Thực Tế" chính thức áp dụng cấu hình mới cho toàn bộ người dùng Storefront.

#### Phân tích nghiệp vụ, phân quyền PBAC và luồng xử lý dữ liệu phía sau
- **Phân quyền PBAC:** Yêu cầu quyền hạt nhân `[HasPermission("THEME_MANAGE")]`.
- **Luồng xử lý Backend:** Controller `ThemeCustomizerController.Index()` gọi `ILayoutEngineService.GetLayoutAsync(pageKey, isDraft: true)`. Dữ liệu cấu hình được lưu trữ trong bảng `ThemeConfigs` dưới dạng chuỗi JSON có cấu trúc.
- Khi người dùng chỉnh sửa trên thanh bên, JavaScript lắng nghe sự kiện thay đổi và gửi bản tin qua hàm `iframe.contentWindow.postMessage({ type: 'THEME_UPDATE', payload: draftConfig }, '*')`. Mã script `_LivePreviewScript.cshtml` nhúng trong iframe nhận thông điệp, cập nhật các biến CSS (CSS Custom Properties `--color-primary`, `--color-secondary`) và hoán đổi vị trí các phần tử DOM trong vòng chưa đầy 50 mili-giây mà hoàn toàn không gửi request nạp lại trang.
- Khi bấm "Lưu Bản Nháp", dữ liệu được gửi đến Action `POST /admin/customizer/save-draft`, hệ thống cập nhật vào cột `ThemeConfig.DraftConfigJson`. Khi bấm "Xuất Bản", Action `POST /admin/customizer/publish` sao chép dữ liệu từ `DraftConfigJson` sang `PublishedConfigJson`, đồng thời xóa bộ nhớ đệm `_memoryCache.Remove("ActiveThemeConfig")`, kích hoạt giao diện mới cho hàng ngàn khách hàng đồng thời.

---

### 4. Hình 3.11 – Giao diện Ma trận Phân quyền Hạt nhân Động (PBAC Permission Matrix 51 Quyền x 6 Vai trò) & Nhật ký Kiểm toán (`AuditLog`)

Hệ thống phân quyền PBAC kết hợp Nhật ký Kiểm toán là bức tường thành an ninh mạng vững chắc của chuỗi CANMAKE BEAUTY, đảm bảo kiểm soát truy cập theo nguyên tắc đặc quyền tối thiểu và truy vết mọi biến động dữ liệu quan trọng.

> **[HƯỚNG DẪN CHỤP & CHÈN HÌNH 3.11 - GIAO DIỆN MA TRẬN PHÂN QUYỀN PBAC VÀ AUDIT LOG]**  
> - **Tệp hình ảnh có sẵn trong hệ thống:** `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_6_Giao_Dien_Ma_Tran_Phan_Quyen_PBAC.png`.  
> - **Quy trình mở trình duyệt và chụp ảnh thực tế:**  
>   + *Bước 1:* Đăng nhập bằng tài khoản `superadmin` / `Admin@123` tại `http://localhost:5116/admin/auth/login`.  
>   + *Bước 2:* Truy cập đường dẫn `http://localhost:5116/Admin/PermissionManagement`.  
>   + *Bước 3:* Chọn vai trò `StoreManager` trong dropdown để thấy bảng lưới 51 quyền hạt nhân chia theo 17 module nghiệp vụ với các công tắc Toggle Switch xanh lá.  
>   + *Bước 4:* Mở tab mới truy cập `http://localhost:5116/Admin/AuditLog`, quan sát bảng nhật ký theo dõi lịch sử thao tác của các tài khoản và nhấp vào nút "Xem JSON Diff" để mở modal so sánh dữ liệu trước và sau khi sửa.  
>   + *Bước 5:* Chụp màn hình ma trận phân quyền hoặc cửa sổ modal JSON Diff để chèn vào báo cáo.

```
+--------------------------------------------------------------------------------------------------------------------+
| MA TRẬN PHÂN QUYỀN HẠT NHÂN ĐỘNG (PBAC) | Chọn Vai Trò: [ Quản Lý Cửa Hàng (StoreManager) v ] [💾 LƯU MA TRẬN QUYỀN]|
+--------------------------------------------------------------------------------------------------------------------+
| PHẠM VI DỮ LIỆU CHI NHÁNH: [x] Giới hạn truy xuất dữ liệu trong chi nhánh phân công (Store Scope Isolation)        |
+--------------------------------------------------------------------------------------------------------------------+
| PHÂN HỆ NGHIỆP VỤ         | MÃ QUYỀN NGUYÊN TỬ           | MÔ TẢ Ý NGHĨA QUYỀN HẠN       | TRẠNG THÁI CẤP PHÉP     |
|---------------------------+------------------------------+-------------------------------+-------------------------|
| QUẢN TRỊ KHO & ĐIỀU CHUYỂN| INVENTORY_VIEW               | Xem danh sách tồn kho lô hạn  | [ ON  (v) Bật màu xanh] |
|                           | INVENTORY_ADJUST             | Điều chỉnh số lượng kiểm kê   | [ OFF ( ) Tắt màu xám ] |
|                           | STOCK_TRANSFER_CREATE        | Tạo phiếu yêu cầu chuyển kho  | [ ON  (v) Bật màu xanh] |
|                           | STOCK_TRANSFER_APPROVE       | Phê duyệt xuất kho điều chuyển| [ ON  (v) Bật màu xanh] |
|---------------------------+------------------------------+-------------------------------+-------------------------|
| BÁN HÀNG TẠI QUẦY (POS)   | POS_ORDER_CREATE             | Tạo đơn bán hàng trực tiếp    | [ ON  (v) Bật màu xanh] |
|                           | POS_APPLY_DISCOUNT           | Chiết khấu hóa đơn thủ công   | [ ON  (v) Bật màu xanh] |
|                           | POS_O2O_HANDOVER             | Bàn giao đơn hàng O2O bằng PIN| [ ON  (v) Bật màu xanh] |
|                           | POS_BLIND_AUDIT_VIEW         | Xem số dư thực tế kiểm toán   | [ OFF ( ) Tắt cảnh báo] |
+--------------------------------------------------------------------------------------------------------------------+
| NHẬT KÝ KIỂM TOÁN HỆ THỐNG (AUDIT LOG & JSON DIFF):                                                                |
| Thời gian: 04/11/2026 14:15 | Người thực hiện: manager_cg | Thao tác: UPDATE trên bảng ProductBatches (ID: 101)    |
| MODAL SO SÁNH BIẾN ĐỘNG DỮ LIỆU (JSON DIFF MODAL):                                                                 |
|   DỮ LIỆU CŨ (OLD VALUES):                       DỮ LIỆU MỚI (NEW VALUES):                                         |
|   {                                              {                                                                 |
|     "ClearanceDiscountPercent": 0,                 "ClearanceDiscountPercent": 45,  <-- [ĐÃ THAY ĐỔI XẢ HÀNG]      |
|     "Status": "ACTIVE"                             "Status": "ACTIVE"                                              |
|   }                                              }                                                                 |
+--------------------------------------------------------------------------------------------------------------------+
```

#### Mô tả trực quan bố cục giao diện và các chỉ số hiển thị
- **Thanh chọn vai trò và chế độ cô lập chi nhánh:** Dropdown chọn 6 vai trò chính trong hệ thống (SuperAdmin, StoreManager, Cashier, WarehouseStaff, ContentEditor, CustomerSupport). Bên cạnh là tùy chọn "Store Scope" kiểm soát việc nhân viên có bị đóng khung quyền hạn trong Showroom mình làm việc hay không.
- **Bảng ma trận phân quyền 2 chiều với 51 quyền hạt nhân:** Phân chia thành 17 cụm nghiệp vụ mạch lạc. Mỗi dòng thể hiện mã quyền viết hoa chuẩn quốc tế (như `STOCK_TRANSFER_APPROVE`), phần giải thích tiếng Việt và công tắc Toggle Switch cho phép bật/tắt quyền hạn chỉ bằng một cú nhấp chuột.
- **Khung cấp quyền ngoại lệ cho người dùng cá nhân (User Permissions Override):** Cho phép gắn quyền hoặc thu hồi quyền riêng biệt cho một nhân viên cụ thể mà không cần tạo thêm vai trò mới, tuân thủ quy tắc phủ quyết tối cao (Deny-Takes-Precedence).
- **Trình tra cứu Nhật ký Kiểm toán và Modal so sánh JSON Diff:** Ghi nhận đầy đủ lịch sử: ai làm gì, vào thời điểm nào, từ địa chỉ IP nào, trên bảng dữ liệu nào. Modal JSON Diff tô màu đỏ phần dữ liệu cũ và màu xanh lá phần dữ liệu mới giúp việc thanh tra nội bộ cực kỳ minh bạch.

#### Phân tích nghiệp vụ, phân quyền PBAC và luồng xử lý dữ liệu phía sau
- **Phân quyền PBAC:** Chỉ duy nhất vai trò `SuperAdmin` sở hữu quyền `[HasPermission("ROLE_PERMISSION_MANAGE")]` mới được phép truy cập và chỉnh sửa ma trận phân quyền.
- **Luồng xử lý Backend:** Khi lưu ma trận, Controller `PermissionManagementController.SaveRoleMatrix()` mở giao dịch cơ sở dữ liệu, xóa các dòng quyền cũ của vai trò trong bảng `RolePermissions` và ghi nhận danh sách mã quyền mới. Sau đó, hệ thống kích hoạt hàm `_permissionService.InvalidatePermissionCacheForRole(roleId)` để giải phóng bộ nhớ đệm phân quyền `HashSet<string>`.
- **Bộ chặn kiểm toán tự động (Audit Interceptor):** Lớp `AuditSaveChangesInterceptor` tích hợp trong Entity Framework Core lắng nghe mọi thao tác `SaveChanges` trên toàn bộ hệ thống. Đối với các thực thể có gắn thuộc tính theo dõi kiểm toán, bộ chặn tự động trích xuất các thuộc tính bị thay đổi, tuần tự hóa thành chuỗi JSON và ghi vào bảng `AuditLogs` với các trường `Action`, `TableName`, `RecordId`, `OldValuesJson`, `NewValuesJson`, `UserId`, `Timestamp` mà lập trình viên không cần viết mã thủ công trong từng Controller.

---

### 5. Hình 3.12 – Giao diện Giám sát Tồn kho Lô Hạn dùng FEFO & Cảnh báo Màu sắc Trực quan

Giao diện giám sát kho là công cụ thiết yếu của Thủ kho, giải quyết triệt để vấn đề mỹ phẩm hư hỏng do hết hạn sử dụng thông qua hệ thống cảnh báo màu sắc đèn giao thông và chức năng điều chuyển kho nội bộ 3 bước.

> **[HƯỚNG DẪN CHỤP & CHÈN HÌNH 3.12 - GIAO DIỆN GIÁM SÁT KHO LÔ HẠN FEFO]**  
> - **Tệp hình ảnh có sẵn trong hệ thống:** `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_7_Giao_Dien_Giam_Sat_Ton_Kho_FEFO.png`.  
> - **Quy trình mở trình duyệt và chụp ảnh thực tế:**  
>   + *Bước 1:* Đăng nhập bằng tài khoản Thủ kho Kho Tổng `warehouse01` / `Admin@123` (hoặc `superadmin`) tại `http://localhost:5116/admin/auth/login`.  
>   + *Bước 2:* Truy cập đường dẫn `http://localhost:5116/Admin/Warehouse` trên thanh menu trái.  
>   + *Bước 3:* Chọn bộ lọc chi nhánh "Kho Tổng TP.HCM" hoặc "Showroom Cầu Giấy", chọn bộ lọc hạn dùng "Cận hạn < 90 ngày".  
>   + *Bước 4:* Quan sát bảng lưới mật độ cao với các cột số lô, hạn sử dụng và hệ thống tem nhãn màu sắc: Màu Đỏ (Hết hạn), Màu Cam (Cận date <90 ngày) và Màu Xanh (An toàn).  
>   + *Bước 5:* Bấm vào nút "Tạo Phiếu Điều Chuyển Kho" trên một lô hàng cận date để thấy modal lập phiếu điều chuyển 3 bước xuất hiện và chụp ảnh màn hình chèn vào báo cáo.

```
+--------------------------------------------------------------------------------------------------------------------+
| GIÁM SÁT TỒN KHO LÔ HẠN FEFO | Chi nhánh: [ Showroom Cầu Giấy v ] | Lọc Date: [ Cận date <90 ngày v ] [TÌM KIẾM 🔍]|
+--------------------------------------------------------------------------------------------------------------------+
| TỔNG QUAN TỒN KHO: 519 Sản phẩm | 🔴 01 Lô hết hạn (Khóa bán) | 🟠 02 Lô cận date (Cần xả) | 🟢 02 Lô an toàn      |
+--------------------------------------------------------------------------------------------------------------------+
| MÃ SKU   | TÊN MỸ PHẨM CHÍNH HÃNG   | SỐ LÔ SẢN XUẤT | HẠN SỬ DỤNG  | TRẠNG THÁI HẠN DÙNG | TỒN THỰC | GIỮ | KHẢ DỤNG|
|----------+--------------------------+----------------+--------------+---------------------+----------+-----+---------|
| SKU-0129 | Kem Chống Nắng Anessa 60m| LOT-2023-A09   | 15/12/2026   | 🔴 [ ĐỎ: HẾT HẠN ]  | 10 hộp   | 0   | 0 (Khóa)|
| SKU-0482 | Serum B5 La Roche-Posay  | LOT-2024-C12   | 28/02/2027   | 🟠 [ CAM: CÒN 42N ] | 28 hộp   | 4   | 24 hộp  |
| SKU-0751 | Nước Tẩy Trang Bioderma  | LOT-2024-D05   | 10/04/2027   | 🟠 [ CAM: CÒN 83N ] | 50 chai  | 5   | 45 chai |
| SKU-0933 | Son Kem 3CE Velvet Lip   | LOT-2025-E01   | 20/11/2027   | 🟢 [ XANH: AN TOÀN ]| 160 cây  | 10  | 150 cây |
| SKU-1044 | Mặt Nạ Dưỡng Banobagi    | LOT-2025-F18   | 15/05/2028   | 🟢 [ XANH: AN TOÀN ]| 310 miếng| 10  | 300 miếng
+--------------------------------------------------------------------------------------------------------------------+
| THAO TÁC XỬ LÝ NHANH TRÊN LÔ CẬN DATE:                                                                             |
| [ ⚡ THIẾT LẬP GIÁ XẢ HÀNG CẬN DATE (-45%) ]  [ 🚚 TẠO PHIẾU ĐIỀU CHUYỂN KHO NỘI BỘ ]  [ 🏷️ IN TEM MÃ VẠCH LÔ HÀNG ]|
+--------------------------------------------------------------------------------------------------------------------+
```

#### Mô tả trực quan bố cục giao diện và các chỉ số hiển thị
- **Bảng dữ liệu mật độ cao chuẩn Logistics (Compact Grid):** Bố trí 8 cột thông tin chuẩn mực, sử dụng phông chữ đơn khoảng cách `JetBrains Mono` cho các cột Số lô sản xuất, Ngày tháng và Số lượng tồn, bảo đảm các chữ số luôn thẳng hàng tuyệt đối giúp mắt thủ kho không bị mỏi khi kiểm kê.
- **Hệ thống nhãn màu cảnh báo hạn dùng trực quan (Traffic Light Expiry Badges):**
  + *Nhãn Màu Đỏ (Hết hạn):* Lô hàng có `ExpDate <= DateTime.UtcNow`. Hệ thống tự động khóa bán trên toàn bộ POS và Web, chuyển tồn khả dụng về 0 và chỉ cho phép lập biên bản xuất hủy hàng hỏng.
  + *Nhãn Màu Cam / Vàng (Cận date < 90 ngày):* Hiển thị số ngày còn lại chính xác đến từng ngày (ví dụ: "CÒN 42N"), kích hoạt gợi ý áp dụng giá xả hàng thanh lý hoặc tạo lệnh điều chuyển sang các showroom trung tâm có sức mua nhanh hơn.
  + *Nhãn Màu Xanh Lá (An toàn > 90 ngày):* Lô hàng có hạn dùng an toàn, bảo quản bình thường.
- **Phân tách 3 trạng thái tồn kho:** Cung cấp thông tin rành mạch giữa Tồn vật lý (Physical Quantity), Tồn đang giữ chỗ cho đơn online (Reserved Quantity) và Tồn thực sự khả dụng để bán tại quầy (Available Quantity = Physical - Reserved).

#### Phân tích nghiệp vụ, phân quyền PBAC và luồng xử lý dữ liệu phía sau
- **Phân quyền PBAC:** Yêu cầu quyền `[HasPermission("INVENTORY_VIEW")]`. Để lập phiếu điều chuyển cần quyền `[HasPermission("STOCK_TRANSFER_CREATE")]`; để duyệt phiếu cần quyền `[HasPermission("STOCK_TRANSFER_APPROVE")]`.
- **Luồng xử lý Backend:** Controller `WarehouseController.Index()` nhận các tham số lọc `storeId`, `keyword`, `expiryFilter`. Tầng dữ liệu thực hiện truy vấn bảng `StoreInventories` kết hợp `ProductBatches`:
  + Khi chọn lọc `EXPIRED`: `.Where(si => si.ProductBatch.ExpDate.Date < today && si.PhysicalQuantity > 0)`.
  + Khi chọn lọc `CRITICAL_30`: `.Where(si => si.ProductBatch.ExpDate.Date <= today.AddDays(30) && si.PhysicalQuantity > 0)`.
  + Khi chọn lọc `WARNING_60`: `.Where(si => si.ProductBatch.ExpDate.Date > today.AddDays(30) && si.ProductBatch.ExpDate.Date <= today.AddDays(60))`.
- **Quy trình điều chuyển kho 3 bước:** Khi thủ kho bấm "Tạo phiếu điều chuyển", dịch vụ `StockTransferService.CreateTransferRequestAsync()` khởi tạo bản ghi trong bảng `StockTransfers` với trạng thái `PENDING`. Khi cấp quản lý bấm duyệt qua Action `Approve()`, hệ thống tăng trường `ReservedQuantity` tại kho xuất, chuyển trạng thái phiếu sang `IN_TRANSIT` (hàng đang trên đường vận chuyển). Khi kho đích nhận hàng và quét mã vạch xác nhận qua Action `Receive()`, hệ thống trừ tồn vật lý kho xuất và cộng tồn vật lý kho nhập, bảo đảm số lượng hàng hóa toàn chuỗi luôn cân bằng tuyệt đối từng đơn vị.

---

### 6. Hình 3.13 – Giao diện Quản lý Đơn hàng Đa kênh & Truy vết Cấp phát Lô Hàng FEFO

Giao diện quản lý đơn hàng kết nối thông suốt giữa các giao dịch bán lẻ trên mọi kênh bán hàng với quy trình hoàn tất đơn hàng vật lý tại kho, hỗ trợ nhân viên soạn hàng nhặt đúng số lô và tích hợp đẩy đơn sang đơn vị vận chuyển Giao Hàng Nhanh (GHN).

> **[HƯỚNG DẪN CHỤP & CHÈN HÌNH 3.13 - GIAO DIỆN QUẢN LÝ ĐƠN HÀNG VÀ CẤP PHÁT LÔ FEFO]**  
> - **Tệp hình ảnh có sẵn trong hệ thống:** `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_8_Giao_Dien_Quan_Ly_Don_Hang_Cap_Phat_Lo.png`.  
> - **Quy trình mở trình duyệt và chụp ảnh thực tế:**  
>   + *Bước 1:* Đăng nhập bằng tài khoản Quản lý cửa hàng Cầu Giấy `manager_cg` / `Admin@123` tại `http://localhost:5116/admin/auth/login`.  
>   + *Bước 2:* Truy cập đường dẫn `http://localhost:5116/Admin/Order`.  
>   + *Bước 3:* Nhấp chuột vào mã đơn hàng `#ORD-20261104-7892` hoặc đơn O2O mới phát sinh để mở trang chi tiết đơn hàng `http://localhost:5116/Admin/Order/Details/7`.  
>   + *Bước 4:* Quan sát bảng chi tiết sản phẩm hiển thị mã số lô FEFO đích danh `BatchNumber`, ngày hết hạn và vị trí ngăn kệ kho; kiểm tra mã vận đơn đối tác Giao Hàng Nhanh GHN và nút "In Phiếu Đóng Gói Kèm Barcode".  
>   + *Bước 5:* Chụp ảnh toàn cảnh màn hình chi tiết đơn hàng để chèn vào báo cáo.

```
+--------------------------------------------------------------------------------------------------------------------+
| CHI TIẾT ĐƠN HÀNG: #ORD-20261104-7892 | Kênh: [ O2O CLICK-AND-COLLECT ] | Trạng thái đơn: [ ĐÃ XÁC NHẬN - CHỜ LẤY ]|
+-------------------------------------------------------------------+------------------------------------------------+
| THÔNG TIN KHÁCH HÀNG & GIAO NHẬN:                                 | ĐỊA ĐIỂM BÀN GIAO HÀNG HÓA TẠI SHOWROOM:       |
| - Khách hàng: Nguyễn Thu Hà | SĐT: 0987.321.654 (Hạng VÀNG)       | - Showroom tiếp nhận: Showroom 123 Cầu Giấy, HN|
| - Email: thuha.nguyen@gmail.com                                   | - Thời gian khách hẹn lấy: Trong vòng 24 giờ   |
| - Trạng thái thanh toán: ĐÃ THANH TOÁN (VietQR Napas247 - Khớp)   | - Mã PIN bảo mật nhận hàng: [ OB - 8 4 9 2 0 1]|
+-------------------------------------------------------------------+------------------------------------------------+
| BẢNG TRUY VẾT CẤP PHÁT LÔ HÀNG ĐÍCH DANH FEFO (ALLOCATED BATCHES BREAKDOWN):                                       |
| DÒNG MỸ PHẨM ĐẶT MUA      | SL | MÃ SỐ LÔ GẮN ĐÍCH DANH| HẠN SỬ DỤNG  | GIÁ BÁN   | THÀNH TIỀN | VỊ TRÍ KỆ KHO     |
|---------------------------+----+-----------------------+--------------+-----------+------------+-------------------|
| Serum B5 La Roche-Posay   | 01 | LOT-2024-C12          | 28/02/2027   | 650.000 đ | 650.000 đ  | Kệ Mỹ Phẩm A2-Tầng3|
| Dung Dịch Tẩy Da Chết BHA | 01 | LOT-2024-D05          | 10/04/2027   | 399.000 đ | 399.000 đ  | Kệ Dược Liệu B1-T2|
+-------------------------------------------------------------------+------------------------------------------------+
| THÔNG TIN VẬN ĐƠN ĐỐI TÁC GIAO HÀNG NHANH (GHN LOGISTICS):                                                         |
| - Mã vận đơn GHN: [ GHN-VN-8839201 ] (Mã vạch chuẩn Code 128)      | - Tiền thu hộ COD: 0 đ (Đã thanh toán Online)  |
| - Trạng thái vận đơn: Đã đồng bộ API sang GHN, Shipper đang tới lấy| - Phí vận chuyển: 0 đ (Áp dụng chính sách O2O) |
+--------------------------------------------------------------------------------------------------------------------+
| [ 🖨️ IN PHIẾU ĐÓNG GÓI CÓ BARCODE ]   [ 📦 XÁC NHẬN ĐÓNG GÓI XONG ]   [ 🤝 HOÀN TẤT BÀN GIAO O2O (XÁC THỰC MÃ PIN) ] |
+--------------------------------------------------------------------------------------------------------------------+
```

#### Mô tả trực quan bố cục giao diện và các chỉ số hiển thị
- **Khối thông tin hành trình đơn hàng đa kênh:** Phân định rõ ràng nguồn gốc phát sinh đơn hàng: Đơn mua trực tuyến giao hàng tận nhà, Đơn O2O Click-and-Collect đặt trước nhận tại Showroom, hoặc Đơn bán trực tiếp tại quầy. Thể hiện trạng thái thanh toán và thông tin khách hàng thân thiết.
- **Bảng cấp phát lô hàng đích danh FEFO (Batch Traceability Table):** Điểm khác biệt lớn nhất so với các hệ thống ERP thương mại thông thường. Mỗi dòng sản phẩm được thuật toán FEFO tự động gắn chặt với một mã số lô sản xuất `BatchNumber` và Hạn sử dụng cụ thể. Cột "Vị trí kệ kho" chỉ dẫn chính xác tọa độ vật lý giúp nhân viên nhặt hàng lấy đúng sản phẩm cận hạn nhất mà không mất thời gian tìm kiếm.
- **Khối tích hợp vận chuyển Giao Hàng Nhanh (GHN):** Hiển thị mã vận đơn đối tác kèm mã vạch Code 128, cập nhật trạng thái lộ trình bưu tá theo thời gian thực qua Webhook.
- **Bộ công cụ đóng gói và in ấn:** Nút in phiếu giao hàng chuẩn khổ A5 có sẵn mã vạch đơn hàng và mã vạch số lô; nút xác nhận hoàn tất soạn hàng chuyển trạng thái đơn sang sẵn sàng giao; nút bàn giao hàng O2O yêu cầu đối chiếu mã PIN của khách trước khi trao kiện hàng.

#### Phân tích nghiệp vụ, phân quyền PBAC và luồng xử lý dữ liệu phía sau
- **Phân quyền PBAC:** Yêu cầu quyền hạt nhân `[HasPermission("ORDER_VIEW")]` để xem danh sách và `[HasPermission("ORDER_PROCESS")]` để thao tác duyệt hoặc đóng gói đơn hàng.
- **Luồng xử lý Backend:** Khi mở trang chi tiết, Controller `OrderController.Details(id)` nạp thông tin đơn hàng từ bảng `Orders`, kết hợp phép nối bảng `Include(o => o.OrderDetails).ThenInclude(od => od.ProductBatch)` để lấy thông tin lô hàng tương ứng.
- **Tích hợp API đối tác vận chuyển GHN:** Khi đơn hàng chuyển sang trạng thái sẵn sàng xuất giao, dịch vụ `GhnShippingService.CreateOrderAsync()` đóng gói dữ liệu kích thước 3 chiều và trọng lượng, gửi yêu cầu HTTP POST tới cổng Open API của GHN. Hệ thống nhận lại mã bưu gửi `GHN-VN-xxxx`, lưu vào trường `Order.TrackingCode` và tự động sinh ảnh mã vạch Code 128 phục vụ in nhãn dán kiện hàng thông qua Action `PrintShippingLabel(id)`.
- Khi nhân viên bấm "Hoàn tất bàn giao O2O", hệ thống đối soát mã PIN nhập vào với trường `Order.PickupPinHash`. Nếu trùng khớp, hệ thống cập nhật `Order.Status = "COMPLETED"`, ghi nhận người giao hàng và thời gian hoàn tất chính xác đến từng giây.

---

### 7. Hình 3.14 – Giao diện Màn hình Bán hàng Tại quầy Smart POS (`_PosLayout`), Quét Mã vạch & Cổng Giao Đơn O2O Click & Collect

Màn hình POS là công cụ làm việc cường độ cao của Nhân viên thu ngân tại các Showroom, được thiết kế theo chuẩn Kiosk toàn màn hình cảm ứng, tích hợp máy quét mã vạch và cổng bàn giao đơn hàng O2O thần tốc.

> **[HƯỚNG DẪN CHỤP & CHÈN HÌNH 3.14 - GIAO DIỆN BÁN HÀNG TẠI QUẦY SMART POS]**  
> - **Tệp hình ảnh có sẵn trong hệ thống:** `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_9_Giao_Dien_Man_Hinh_Ban_Hang_POS.png`.  
> - **Quy trình mở trình duyệt và chụp ảnh thực tế:**  
>   + *Bước 1:* Đăng nhập bằng tài khoản Nhân viên thu ngân Cầu Giấy: Tên đăng nhập `cashier02` (hoặc `cashier01`) / Mật khẩu `Admin@123` tại `http://localhost:5116/admin/auth/login`.  
>   + *Bước 2:* Truy cập vào đường dẫn `http://localhost:5116/Admin/Pos` (hệ thống tự động sử dụng bố cục chuyên dụng `_PosLayout.cshtml` cố định 100vh không cuộn trang).  
>   + *Bước 3:* Nhập mã vạch sản phẩm (ví dụ `8935028401923` hoặc SKU `SKU-0129`) vào ô quét hàng để kiểm tra hệ thống tự động nhận diện sản phẩm và gán lô FEFO gần nhất vào hóa đơn bán lẻ.  
>   + *Bước 4:* Bấm phím tắt `F2` hoặc gõ số điện thoại `0912.345.678` để nạp thông tin khách hàng VIP và kiểm tra số điểm tích lũy có thể tiêu điểm; bấm nút "Giao Đơn O2O Click & Collect" để mở modal nhập mã PIN nhận hàng.  
>   + *Bước 5:* Bấm phím `F4` bật phương thức thanh toán VietQR động và chụp ảnh toàn bộ giao diện bán hàng Kiosk để chèn vào báo cáo.

```
+--------------------------------------------------------------------------------------------------------------------+
| SMART POS KIOSK - CANMAKE BEAUTY | Ca: SÁNG (Thu ngân: Hoàng Thu Ngân) | Điểm bán: SHOWROOM CẦU GIẤY | [🔒 ĐÓNG CA F10]  |
+-------------------------------------------------------------------+------------------------------------------------+
| [F1] QUÉT MÃ VẠCH EAN-13 / SKU: [ 8935028401923               🔍] | [F2] KHÁCH HÀNG: [ 0912.345.678             🔍] |
+-------------------------------------------------------------------+ Tên: Lê Hoàng Yến | Hạng: KIM CƯƠNG (VIP)      |
| GIỎ HÀNG BÁN LẺ TẠI QUẦY (HÓA ĐƠN ĐANG PHỤC VỤ)                   | Điểm khả dụng: 1.250 Điểm (Tương đương 1.25M)  |
| STT | TÊN MỸ PHẨM            | LÔ XUẤT FEFO   | SL | ĐƠN GIÁ | T.TIỀN   | ---------------------------------------------- |
| 01  | Kem Nắng Anessa 60ml   | LOT-2024-C12   | 01 | 650.000 | 650.000  | TỔNG TIỀN HÀNG:                      930.000 đ |
| 02  | Sữa Rửa Mặt Cerave     | LOT-2024-D05   | 01 | 350.000 | 350.000  | Giảm giá thành viên VIP (-5%):       -46.500 đ |
|     |                        |                |    |         |          | Tiêu điểm Loyalty (200 điểm):        -20.000 đ |
|     |                        |                |    |         |          | KHÁCH PHẢI THANH TOÁN:               863.500 đ |
|     |                        |                |    |         |          | ---------------------------------------------- |
|     |                        |                |    |         |          | PHƯƠNG THỨC THANH TOÁN NHANH:                 |
|     |                        |                |    |         |          | [ TIỀN MẶT ]  [ QUẸT THẺ ]  [ VIETQR ĐỘNG F4 ] |
|     |                        |                |    |         |          | ---------------------------------------------- |
|     |                        |                |    |         |          | [ 🤝 GIAO ĐƠN O2O CLICK & COLLECT (MÃ PIN) ]   |
|     |                        |                |    |         |          | [ 🖨️ F12: THANH TOÁN & IN BILL NHIỆT K80 ]    |
+-------------------------------------------------------------------+------------------------------------------------+
| THANH PHÍM TẮT THAO TÁC NHANH: [F1: Quét] [F2: Khách] [F3: Chiết khấu] [F4: VietQR] [F8: Giữ đơn] [F12: Thu tiền]   |
+--------------------------------------------------------------------------------------------------------------------+
```

#### Mô tả trực quan bố cục giao diện và các chỉ số hiển thị
- **Bố cục Kiosk cố định toàn màn hình (Fixed 100vh Layout):** Loại bỏ hoàn toàn thanh cuộn trang, chia không gian làm việc thành 2 cột tương tác tỷ lệ 60:40: cột bên trái xử lý danh mục hàng hóa trong hóa đơn và cột bên phải xử lý khách hàng cùng các phương thức thanh toán.
- **Ô quét mã vạch duy trì tiêu điểm liên tục (Auto-focus Barcode Input):** Hỗ trợ nhận diện tức thời mã vạch quốc tế EAN-13, mã Barcode nội bộ và mã SKU từ máy quét cầm tay cổng USB/Bluetooth.
- **Cổng giao hàng O2O Click & Collect chuyên biệt:** Nút bấm nổi bật cho phép thu ngân mở nhanh cửa sổ nhập mã PIN 6 số hoặc quét mã QR từ điện thoại của khách hàng đặt mua trên website để hoàn tất bàn giao kiện hàng trong vòng chưa đầy 15 giây.
- **Thanh phím chức năng tiêu chuẩn F1 - F12:** Tối ưu hóa cho thao tác bấm phím cơ học, loại bỏ sự phụ thuộc vào chuột máy tính, nâng cao tốc độ thanh toán trong giờ cao điểm lên gấp 3 lần so với giao diện web truyền thống.
- **Đồng bộ màn hình phụ khách hàng (Customer-Facing Display - CFD):** Hệ thống tích hợp kênh phát sóng trình duyệt `BroadcastChannel("pos_customer_display")`. Khi thu ngân thao tác, màn hình phụ quay về phía người mua lập tức hiển thị danh sách sản phẩm và mã QR VietQR động với số tiền thanh toán chính xác đến từng đồng.

#### Phân tích nghiệp vụ, phân quyền PBAC và luồng xử lý dữ liệu phía sau
- **Phân quyền PBAC:** Yêu cầu quyền `[HasPermission("POS_ORDER_CREATE")]` để bán hàng, quyền `[HasPermission("POS_APPLY_DISCOUNT")]` để chiết khấu hóa đơn và quyền `[HasPermission("POS_O2O_HANDOVER")]` để bàn giao đơn hàng online.
- **Luồng xử lý Backend:** Khi thu ngân quét mã vạch, JavaScript gửi yêu cầu `POST /Admin/Pos/ScanItem`. Controller `PosController` kích hoạt thuật toán xuất kho `InventoryAllocationEngine.AllocateBatchFefoAsync(productId, storeId, 1)`. Động cơ tự động truy vấn bảng `StoreInventories` thuộc đúng Showroom của thu ngân, tìm lô hàng có `ExpDate` gần nhất đang có sẵn và trả về kết quả gán số lô đích danh vào dòng hóa đơn.
- Khi bấm chọn thanh toán VietQR qua phím `F4`, Action `GenerateVietQr()` tính toán chuỗi dữ liệu theo định dạng quốc tế EMVCo gồm mã ngân hàng thụ hưởng, tài khoản định danh chi nhánh và số tiền lẻ chính xác `863.500 đ`, trả về chuỗi ảnh Base64 của mã QR để hiển thị lên cả màn hình thu ngân và màn hình phụ CFD.
- Khi hoàn tất đơn hàng, hệ thống phát lệnh in trực tiếp tới máy in hóa đơn nhiệt qua giao thức ESC/POS, tự động cắt giấy khổ K80 và mở ngăn kéo đựng tiền mặt (Cash Drawer Kick).

---

### 8. Hình 3.15 – Giao diện Quản lý Mở/Đóng Ca Thu ngân POS & Kiểm toán Đối soát Két tiền mặt

Giao diện quản lý ca bán hàng hiện thực hóa quy trình kiểm toán két tiền mù (Blind Cash Drawer Audit), thiết lập kỷ luật tài chính nghiêm ngặt nhất cho toàn bộ mạng lưới cửa hàng bán lẻ của CANMAKE BEAUTY.

> **[HƯỚNG DẪN CHỤP & CHÈN HÌNH 3.15 - GIAO DIỆN KIỂM TOÁN ĐÓNG CA THU NGÂN]**  
> - **Tệp hình ảnh có sẵn trong hệ thống:** `d:\Lập trình web\OmnichannelBeautySolution\doc\Hinh_4_10_Giao_Dien_Dong_Mo_Ca_Va_Doi_Soat_Ket.png`.  
> - **Quy trình mở trình duyệt và chụp ảnh thực tế:**  
>   + *Bước 1:* Trên giao diện bán hàng POS `http://localhost:5116/Admin/Pos` (đăng nhập bằng `cashier02` / `Admin@123`), nhấp chuột vào nút "Đóng Ca [F10]" ở góc trên cùng bên phải màn hình.  
>   + *Bước 2:* Cửa sổ modal "Kiểm Toán Két Tiền Mù Đóng Ca Bán Hàng" xuất hiện, lưu ý thông điệp cảnh báo hệ thống ẩn hoàn toàn doanh thu phần mềm.  
>   + *Bước 3:* Nhập số lượng tờ tiền thực đếm trong két cho 9 mệnh giá tiền mặt Việt Nam từ 1.000đ đến 500.000đ (ví dụ: 12 tờ 500k, 25 tờ 200k, 30 tờ 100k...) để hệ thống tự động tính tổng tiền mặt thực tế.  
>   + *Bước 4:* Bấm nút "Xác Nhận Nộp Tiền & Khóa Sổ Ca Bán Hàng" để hệ thống mở khóa bảng kết quả đối soát tự động, hiển thị số tiền kỳ vọng, số tiền thực tế và khoản chênh lệch két tiền kèm ô nhập giải trình.  
>   + *Bước 5:* Chụp ảnh toàn cảnh cửa sổ kiểm toán két mù này để chèn vào báo cáo khóa luận.

```
+--------------------------------------------------------------------------------------------------------------------+
| ĐÓNG CA BÁN HÀNG VÀ KIỂM ĐẾM TIỀN MẶT KÉT THU NGÂN (BLIND CASH AUDIT) | Ca: SÁNG (08h00 - 15h30) | Showroom Cầu Giấy|
+--------------------------------------------------------------------------------------------------------------------+
| ⚠️ QUY CHẾ KẾT MÙ BẮT BUỘC: Hệ thống KHÔNG hiển thị doanh thu phần mềm ghi nhận trong ca trước khi kiểm đếm.       |
| Thu ngân bắt buộc phải đếm tiền vật lý thực tế trong ngăn kéo và nhập đúng số tờ theo từng mệnh giá dưới đây:      |
+--------------------------------------------------------------------------------------------------------------------+
| BẢNG KÊ KIỂM ĐẾM 9 MỆNH GIÁ TIỀN MẶT VIỆT NAM (DENOMINATION BREAKDOWN GRID):                                       |
| MỆNH GIÁ TIỀN (VNĐ) | SỐ TỜ THỰC ĐẾM | THÀNH TIỀN (VNĐ) || MỆNH GIÁ TIỀN (VNĐ) | SỐ TỜ THỰC ĐẾM | THÀNH TIỀN (VNĐ) |
| 500.000 đ           | [     12     ] |   6.000.000 đ    || 20.000 đ            | [     15     ] |     300.000 đ    |
| 200.000 đ           | [     25     ] |   5.000.000 đ    || 10.000 đ            | [     20     ] |     200.000 đ    |
| 100.000 đ           | [     30     ] |   3.000.000 đ    || 5.000 đ             | [     10     ] |      50.000 đ    |
| 50.000 đ            | [     18     ] |     900.000 đ    || 1.000 đ / 2.000 đ   | [     15     ] |      25.000 đ    |
+--------------------------------------------------------------------------------------------------------------------+
| TỔNG TIỀN MẶT THỰC TẾ TRONG KÉT KIỂM ĐẾM (ACTUAL CASH AT CLOSE): 15.475.000 đ                                      |
|                                                                                                                    |
| [ 🔒 XÁC NHẬN NỘP TIỀN & KHÓA SỔ CA BÁN HÀNG ]                                                                     |
+--------------------------------------------------------------------------------------------------------------------+
| KẾT QUẢ ĐỐI SOÁT TỰ ĐỘNG CỦA HỆ THỐNG (CHỈ XUẤT HIỆN SAU KHI ĐÃ KHÓA SỔ VĨNH VIỄN):                                |
| - Số dư tiền mặt đầu ca bàn giao (Opening Balance):                     2.000.000 đ                                |
| - Doanh số tiền mặt hệ thống ghi nhận từ đơn hàng POS:                 13.500.000 đ                                |
| - Tổng tiền mặt hệ thống kỳ vọng phải có trong két (Expected Cash):    15.500.000 đ                                |
| - Tổng tiền mặt thực tế thu ngân đã kiểm đếm (Actual Cash):            15.475.000 đ                                |
| - CHÊNH LỆCH KÉT TIỀN (DISCREPANCY AMOUNT):                               -25.000 đ (THIẾU HỤT - CẢNH BÁO MÀU ĐỎ)  |
| ------------------------------------------------------------------------------------------------------------------ |
| Giải trình bắt buộc của Thu ngân: [ Bù 25.000 đ do thối nhầm tiền lẻ cho khách mua đơn #8921 lúc 11h30 ]           |
| Xác nhận của Cửa hàng trưởng:     [ Lê Quản Lý CG - Đã duyệt ký nhận biên bản bàn giao ca lúc 15:35 ]              |
+--------------------------------------------------------------------------------------------------------------------+
```

#### Mô tả trực quan bố cục giao diện và các chỉ số hiển thị
- **Thông điệp thực thi nguyên tắc Két Mù (Blind Audit Mandate):** Hộp cảnh báo màu vàng nổi bật trên cùng khẳng định kỷ luật bảo mật: nhân viên thu ngân hoàn toàn không được biết trước con số doanh thu phần mềm tính toán để triệt tiêu mọi hành vi gian lận hoặc tự ý bù trừ tiền thừa/thiếu.
- **Bảng kê 9 mệnh giá tiền mặt Việt Nam đối xứng:** Trình bày khoa học 2 cột cân đối bao gồm đủ các mệnh giá polymer và cotton từ 1.000 đồng đến 500.000 đồng. Các ô nhập số lượng tờ tự động nhân thành tiền tức thời bằng JavaScript và cập nhật tổng tiền mặt thực tế kiểm đếm (`ActualCashAtClose`) nổi bật ở chân bảng.
- **Bảng đối soát tài chính sau khóa sổ (Reconciliation Panel):** Chỉ hiển thị duy nhất sau khi thu ngân đã nhấn nút xác nhận khóa ca. Trình bày rõ ràng công thức đối soát kế toán: `ExpectedCash = InitialCash + CashSales - CashRefunds`.
- **Chỉ số chênh lệch két tiền và ô giải trình bắt buộc:** Trường `DiscrepancyAmount` được tô màu trực quan: màu xanh nếu số liệu khớp hoàn toàn (bằng 0), màu vàng nếu thừa tiền (> 0) và màu đỏ rực rỡ nếu thiếu hụt (< 0) kèm ô bắt buộc nhập lý do giải trình trước khi nộp báo cáo cho Cửa hàng trưởng ký duyệt.

#### Phân tích nghiệp vụ, phân quyền PBAC và luồng xử lý dữ liệu phía sau
- **Phân quyền PBAC:** Yêu cầu quyền `[HasPermission("POS_SHIFT_CLOSE")]`. Để xem lại số liệu kiểm toán lịch sử của các ca trước cần quyền giám sát `[HasPermission("POS_BLIND_AUDIT_VIEW")]`.
- **Luồng xử lý Backend:** Khi thu ngân nhấn xác nhận nộp tiền, Controller `PosController.CloseShift([FromBody] CloseShiftDto dto)` tiếp nhận dữ liệu bảng kê mệnh giá.
- Dịch vụ `PosShiftService.CloseShiftAsync(dto)` mở một giao dịch cơ sở dữ liệu `BeginTransactionAsync()`:
  + Truy vấn bảng `Orders` lọc các đơn hàng hoàn tất trong ca có phương thức thanh toán tiền mặt `PaymentMethod == "CASH"`, tính tổng số tiền thu được.
  + Cộng số dư bàn giao đầu ca `InitialCashBalance` để xác định `ExpectedCashAtClose`.
  + Lấy con số `ActualCashAtClose` do thu ngân khai báo trừ đi số kỳ vọng để tính `DiscrepancyAmount`.
  + Lưu bản ghi ca bán hàng vào bảng `PosShifts` với trạng thái `CLOSED`, thời gian kết thúc `ClosedAt` và lưu chi tiết số lượng từng tờ tiền vào bảng `PosShiftDenominations`.
- Nếu phát sinh chênh lệch tiền mặt, hệ thống tự động gọi `FinanceService` tạo một bút toán điều chỉnh trong sổ cái tài chính `FinancialLedgers` ghi nhận vào tài khoản tổn thất hoặc thu nhập khác, bảo đảm số liệu kế toán của chuỗi luôn minh bạch và chuẩn xác tuyệt đối.

---

### 9. Hình 3.16 – Giao diện Quản trị Sổ cái Tài chính, Báo cáo Kết quả Kinh doanh P&L (Theo Giá vốn Đích danh Lô FEFO) & Đối soát COD

Giao diện tài chính là công cụ tối cao của Giám đốc Tài chính và Kế toán trưởng, cung cấp bức tranh tài chính chuẩn mực về doanh thu, chi phí, lợi nhuận gộp tính theo giá vốn đích danh của từng số lô xuất kho và quy trình đối soát tiền thu hộ COD với bưu chính.

> **[HƯỚNG DẪN CHỤP & CHÈN HÌNH 3.16 - GIAO DIỆN BÁO CÁO TÀI CHÍNH P&L VÀ SỔ CÁI]**  
> - **Quy trình mở trình duyệt và chụp ảnh thực tế:**  
>   + *Bước 1:* Đăng nhập bằng tài khoản Quản trị viên tối cao: Tên đăng nhập `superadmin` / Mật khẩu `Admin@123` tại `http://localhost:5116/admin/auth/login`.  
>   + *Bước 2:* Truy cập đường dẫn `http://localhost:5116/Admin/Finance/ProfitLoss` để xem Báo cáo Kết quả Kinh doanh P&L hoặc `http://localhost:5116/Admin/Finance/Ledger` để xem Sổ cái tài chính.  
>   + *Bước 3:* Chọn bộ lọc kỳ báo cáo "Tháng này" và chọn chi nhánh "Toàn chuỗi" hoặc "Showroom Cầu Giấy".  
>   + *Bước 4:* Quan sát bảng cân đối P&L với dòng Doanh thu thuần, dòng Giá vốn hàng bán (COGS) tính chuẩn xác theo từng số lô hàng FEFO xuất bán, Lợi nhuận gộp và Lợi nhuận ròng.  
>   + *Bước 5:* Nhấp nút "Xuất Báo Cáo Excel (.xlsx)" để kiểm tra tính năng xuất file và chụp ảnh màn hình báo cáo tài chính chèn vào báo cáo khóa luận.

```
+--------------------------------------------------------------------------------------------------------------------+
| BÁO CÁO KẾT QUẢ HOẠT ĐỘNG KINH DOANH (P&L STATEMENT) | Kỳ: [ Tháng Này v ] | Chi nhánh: [ Toàn chuỗi v ] [📊 XUẤT EXCEL] |
+--------------------------------------------------------------------------------------------------------------------+
| CHỈ TIÊU TÀI CHÍNH DOANH NGHIỆP                           | MÃ SỐ | KỲ BÁO CÁO NÀY (VNĐ)   | TỶ TRỌNG (%) | SO CÙNG KỲ |
|-----------------------------------------------------------+-------+------------------------+--------------+------------|
| 1. Doanh thu bán lẻ gộp (Gross Revenue)                   | 01    |       2.015.450.000 đ  |   109.2%     |  ▲ +16.2%  |
|    - Doanh thu trực tuyến Storefront Web                  | 01.1  |         845.250.000 đ  |    41.9%     |  ▲ +22.5%  |
|    - Doanh số bán hàng trực tiếp tại quầy POS Showroom    | 01.2  |       1.170.200.000 đ  |    58.1%     |  ▲ +12.1%  |
| 2. Các khoản giảm trừ doanh thu (Voucher, Loyalty, Trả)   | 02    |        -170.200.000 đ  |    -9.2%     |  ▼ -2.1%   |
| 3. DOANH THU THUẦN (NET REVENUE) (01 - 02)                | 10    |       1.845.250.000 đ  |   100.0%     |  ▲ +14.8%  |
| 4. GIÁ VỐN HÀNG BÁN ĐÍCH DANH THEO LÔ FEFO (COGS)         | 11    |        -922.625.000 đ  |   -50.0%     |  ▼ -1.5%   |
| 5. LỢI NHUẬN GỘP BÁN HÀNG (GROSS PROFIT) (10 - 11)        | 20    |         922.625.000 đ  |    50.0%     |  ▲ +18.2%  |
| 6. Chi phí bán hàng & Phí vận chuyển đối tác GHN          | 21    |         -95.400.000 đ  |    -5.2%     |  ▲ +4.1%   |
| 7. Chi phí mặt bằng Showroom & Khấu hao thiết bị POS      | 22    |        -180.000.000 đ  |    -9.8%     |    0.0%    |
| 8. LỢI NHUẬN THUẦN TRƯỚC THUẾ (NET PROFIT BEFORE TAX)     | 50    |         647.225.000 đ  |    35.0%     |  ▲ +24.6%  |
+--------------------------------------------------------------------------------------------------------------------+
| TRÍCH LỤC SỔ CÁI TÀI CHÍNH BÚT TOÁN KÉP (FINANCIAL LEDGER STREAM):                                                 |
| MÃ BÚT TOÁN          | NGÀY GHI SỔ       | LOẠI NGHIỆP VỤ | TÀI KHOẢN NỢ | TÀI KHOẢN CÓ | SỐ TIỀN PHÁT SINH (VNĐ) |
| LED-20261104-A1902   | 04/11/2026 14:32  | REVENUE_POS    | 1111 (Tiền mặt)| 5111 (Doanh thu)|          863.500 đ |
| LED-20261104-B8812   | 04/11/2026 14:32  | COGS_FEFO_LOT  | 6321 (Giá vốn) | 1561 (Hàng hóa) |          431.750 đ |
+--------------------------------------------------------------------------------------------------------------------+
```

#### Mô tả trực quan bố cục giao diện và các chỉ số hiển thị
- **Báo cáo Kết quả Kinh doanh P&L chuẩn mực kế toán doanh nghiệp:** Trình bày theo biểu mẫu phân tích tài chính hiện đại gồm 8 chỉ tiêu lớn, cột mã số kế toán, số tiền phát sinh, tỷ trọng phần trăm trên doanh thu thuần và cột so sánh tăng trưởng cùng kỳ.
- **Giá vốn hàng bán tính đích danh theo số lô FEFO (Specific Identification COGS):** Đây là thành quả kỹ thuật đột phá của giải pháp. Thay vì sử dụng phương pháp giá vốn bình quân gia quyền truyền thống thường bị sai lệch lớn khi giá nhập các lô mỹ phẩm biến động, hệ thống tính toán giá vốn chính xác đến từng xu dựa trên giá nhập thực tế của từng số lô đã xuất kho trong kỳ.
- **Trích lục Sổ cái tài chính bút toán kép (Double-Entry General Ledger):** Danh sách các bút toán tài chính tự động phát sinh từ các giao dịch bán hàng, hủy đơn hoặc thanh lý hàng cận date, thể hiện đầy đủ mã giao dịch duy nhất `LED-xxxx`, tài khoản kế toán Nợ/Có, diễn giải nghiệp vụ và số tiền phát sinh.
- **Công cụ xuất dữ liệu đa định dạng:** Hỗ trợ xuất toàn bộ bảng số liệu ra tệp bảng tính Microsoft Excel (`.xlsx`) và tệp tài liệu số (`.pdf`) chỉ qua một cú nhấp chuột.

#### Phân tích nghiệp vụ, phân quyền PBAC và luồng xử lý dữ liệu phía sau
- **Phân quyền PBAC:** Đây là phân hệ nhạy cảm nhất của doanh nghiệp, yêu cầu các quyền hạt nhân cao cấp `[HasPermission("FINANCE_PNL_VIEW")]` để xem báo cáo P&L và `[HasPermission("FINANCE_LEDGER_VIEW")]` để xem sổ cái tài chính. Nhân viên bán hàng và thủ kho hoàn toàn bị chặn truy cập.
- **Luồng xử lý Backend:** Khi người dùng xem báo cáo P&L, Controller `FinanceController.ProfitLoss()` gọi dịch vụ `IFinanceService.GetProfitLossReportAsync()`. Tầng dữ liệu thực hiện tính toán giá vốn thông qua câu lệnh tổng hợp LINQ tối ưu:
  `cogs = await _context.OrderDetails.Where(od => od.Order.Status == "COMPLETED" && od.Order.CreatedAt >= fromDate && od.Order.CreatedAt <= toDate).SumAsync(od => od.Quantity * od.ProductBatch.UnitCost)`.
- Khi một đơn hàng hoàn tất tại quầy POS hoặc giao thành công qua GHN, phương thức `FinanceService.RecordOrderFinancialEntriesAsync(orderId)` được kích hoạt trong cùng giao dịch cơ sở dữ liệu. Hệ thống tự động ghi 2 bút toán kép đối ứng vào bảng `FinancialLedgers`: một bút toán ghi nhận Doanh thu (Nợ TK 1111/1121, Có TK 5111) và một bút toán ghi nhận Giá vốn hàng bán đích danh (Nợ TK 6321, Có TK 1561), bảo đảm nguyên tắc phù hợp giữa doanh thu và chi phí của chuẩn mực kế toán Việt Nam (VAS).

---

### TIỂU KẾT MỤC 3.2.2

Mục 3.2.2 đã hoàn thành việc mô tả và phân tích chi tiết toàn diện 9 màn hình giao diện cốt lõi của Phân hệ Giao diện Quản trị Điều hành & Bán lẻ Đa kênh. Từ tháp chỉ huy số Executive Dashboard, công cụ tùy biến trang web No-Code Visual Customizer, ma trận bảo mật PBAC 51 quyền hạt nhân, cho đến quy trình kiểm soát kho lô hạn FEFO, bán lẻ Kiosk Smart POS và sổ cái tài chính giá vốn đích danh, hệ thống đã chứng minh năng lực hợp nhất xuất sắc giữa công nghệ phần mềm hiện đại và các nguyên lý quản trị chuỗi cung ứng bán lẻ thực tiễn.
