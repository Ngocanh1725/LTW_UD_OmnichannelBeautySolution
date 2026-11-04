# Omnichannel Beauty Solution 🌸💄
> **Hệ Thống Thương Mại Điện Tử & Quản Trị Chuỗi Bán Lẻ Mỹ Phẩm Cao Cấp Đa Kênh**  
> *Tiên phong công nghệ quản trị tồn kho FEFO (First-Expired, First-Out), Phân Quyền Hạt Nhân PBAC và Trải Nghiệm Mua Sắm Luxury.*

---

## 🌟 1. GIỚI THIỆU DỰ ÁN
**Omnichannel Beauty Solution** là giải pháp phần mềm toàn diện dành cho chuỗi bán lẻ mỹ phẩm cao cấp đa kênh (Omnichannel Retail & E-commerce). Hệ thống kết nối liền mạch giữa:
- **Khách hàng trực tuyến (Storefront Web)**: Trải nghiệm mua sắm sang trọng, tìm kiếm thông minh, tra cứu thành phần hoạt chất, giỏ hàng tức thì, tích điểm thành viên và thanh toán đa cổng (VNPay, MoMo, VietQR, COD).
- **Điểm bán lẻ trực tiếp (POS Checkout)**: Thu ngân bán lẻ tốc hành, quét mã vạch Barcode/SKU, quản lý ca làm việc (Shift), in hóa đơn nhiệt.
- **Hệ thống Quản trị & Vận hành (Admin Suite)**: Quản lý danh mục, đơn hàng, khách hàng, kho bãi FEFO theo lô & hạn dùng, điều chuyển kho liên chi nhánh, hộp thư nội bộ realtime, ma trận phân quyền hạt nhân và báo cáo tài chính P&L.

---

## 🚀 2. KIẾN TRÚC & CÔNG NGHỆ (TECH STACK)

### Backend & Core
- **Nền tảng**: .NET 8.0 (C# 12)
- **Kiến trúc**: Clean Architecture / Domain-Driven Design (DDD)
  - `Omnichannel.Domain`: Entities, Enums, Value Objects, Domain Events
  - `Omnichannel.Application`: DTOs, Service Interfaces, CQRS & Workflow Logics
  - `Omnichannel.Infrastructure`: EF Core DbContext, Repositories, Third-party Integrations, PBAC Authorization
  - `Omnichannel.Web`: ASP.NET Core MVC, Areas (Admin), Controllers, Hubs, Middlewares, ViewComponents
  - `Omnichannel.Tests`: xUnit, FluentAssertions, Moq, Integration Fixtures (**317 tests passed 100%**)
- **Cơ sở dữ liệu**: Microsoft SQL Server / EF Core 8 (Code-First Migration & Dynamic Seeding)
- **Realtime Communication**: ASP.NET Core SignalR (`NotificationHub`, `SystemHub`)
- **Tối ưu hiệu năng**: Response Compression (Brotli + Gzip), Read-only `.AsNoTracking()`, Composite DB Indexes

### Frontend & UI/UX
- **Rendering Engine**: ASP.NET Core Razor Pages / Views
- **Styling**: Tailwind CSS & Vanilla CSS (Luxury Beauty Design System, Class-based Dark Mode)
- **Reactivity & Micro-Interactions**: Alpine.js, HTMX, NProgress Navigation Loader, SweetAlert2
- **Responsive Breakpoints**: Tối ưu toàn diện cho Desktop (1920px), Laptop (1366px), Tablet (768px), Mobile (375px)

### Tích hợp bên thứ ba (Third-party Sandbox)
- **Cổng thanh toán**: VNPay Sandbox, MoMo Payment Sandbox, VietQR động (chuyển khoản ngân hàng tức thì)
- **Vận chuyển & Logistics**: Giao Hàng Nhanh (GHN Express Sandbox API)
- **SEO & Search**: Auto Slug Generator, 301 Redirect Middleware, Schema.org Structured Data

---

## 💎 3. CÁC TÍNH NĂNG TRỌNG TÂM

1. **Quản Trị Tồn Kho FEFO Độc Bản (First-Expired, First-Out)**:
   - Theo dõi từng sản phẩm theo Lô sản xuất (Batch Number) và Hạn sử dụng (Expiry Date).
   - Tự động xuất kho lô cận date trước, phát cảnh báo AI khi hàng sắp hết hạn (30, 60, 90 ngày).
   - Chương trình khuyến mãi xả kho cận date tự động (Flash Sale Smart Clearance).
2. **Khép Kín Vòng Lặp O2O (Click & Collect 1H & Bàn Giao POS)**:
   - Đặt hàng online, nhận hàng trực tiếp tại Showroom (BOPIS/Click & Collect).
   - Giữ hàng theo lô FEFO vật lý trong 48h, miễn 100% phí vận chuyển.
   - Vé nhận hàng ưu tiên (Click & Collect Pass) với QR Code client-side & Mã PIN 6 số có cấu trúc.
   - Cổng thu ngân POS bàn giao nhanh bằng quét QR/PIN, tự động cập nhật đơn và tích điểm.
3. **Trợ Lý AI Da Liễu & Cảnh Báo Tương Kỵ Lâm Sàng (Clinical Conflict Engine)**:
   - Routine Quiz 1-click & Phân tích da bằng AI Vision (`/phan-tich-da`) chuẩn da liễu.
   - Động cơ phát hiện xung đột hoạt chất AI: Tự động cảnh báo tương kỵ đỏ rực tại giỏ hàng khi phối hợp các chất kích ứng mạnh (như Retinol + BHA) và gợi ý phân tách Skin Cycling.
4. **Phòng Thử Màu Son Ảo (Virtual Lip Try-On Studio)**:
   - Thử màu son môi realtime trên 4 tông da Châu Á (Sáng, Tự nhiên, Ngăm, Vàng ấm) và Camera trực tiếp.
   - Phân tích undertone (Cool/Warm/Neutral) và đối chiếu Split-screen trước/sau.
5. **Khay So Sánh Mỹ Phẩm Đa Chiều (Compare Drawer & Matrix)**:
   - Ghim khay so sánh nổi, đối chiếu 2–4 sản phẩm: Đơn giá/ml (VNĐ/ml), Điểm an toàn EWG, Huy hiệu Clean Beauty 5 KHÔNG, và Hạn mở nắp PAO.
6. **Trung Tâm Ra Quyết Định Điều Hành Chuỗi (Executive Decision Center)**:
   - Loại bỏ 100% số liệu hardcode: 6 KPI tăng trưởng ▲▼ % so với kỳ trước tính trực tiếp từ SQL Server.
   - Phễu chuyển đổi 5 bước, Ma trận Heatmap Giờ × Thứ săn sale, Phân khúc RFM 1–5 kèm Xuất CSV & 1-Click Tạo Voucher cá nhân hóa.
   - Cảnh báo điều phối tồn kho thông minh liên chi nhánh.
7. **Gamification Khách Hàng Thân Thiết (Omni Rewards)**:
   - Lịch điểm danh 7 ngày liên tiếp giờ Việt Nam (UTC+7) kèm hiệu ứng pháo hoa Confetti.
   - Mã giới thiệu bạn bè chống vòng lặp, Nhiệm vụ làm đẹp (Beauty Missions), Quầy đổi voucher Rewards Bazaar dùng ngay tại Checkout.
8. **Tìm Kiếm Tức Thì (Instant Search Autocomplete)**:
   - Gợi ý sản phẩm, thương hiệu, danh mục dưới 15ms. Bóc tách tiếng Việt không dấu triệt để (`đ`/`Đ`).
9. **Theo Dõi Đơn Hàng Timeline & Đổi Trả Tự Phục Vụ**:
   - Timeline dọc 6 bước minh bạch số hiệu lô FEFO, Cổng tra cứu vãng lai không cần đăng nhập `/tra-cuu-don-hang`.
   - Khách tự hủy đơn hoàn tồn kho tự động trong transaction; Gửi yêu cầu đổi trả 7 ngày với SignalR realtime.
10. **Demo Presenter HUD (`Alt + D`)**:
    - Bảng điều khiển thuyết trình 1-click tích hợp đồng hồ đếm ngược 10 phút, tự động ánh xạ 6 chặng kịch bản Persona "Lê Ngọc Linh", chuyển vai siêu tốc và nạp giỏ hàng mẫu tức thì.
11. **Phân Quyền Hạt Nhân (PBAC Matrix & Store Isolation)**:
    - Quản trị quyền chi tiết theo `MODULE` và `ACTION`.
    - Store Isolation Engine: Nhân viên và quản lý chi nhánh chỉ xem và tác nghiệp trên dữ liệu của chi nhánh mình; SuperAdmin có tầm nhìn toàn chuỗi.
12. **Điều Chuyển Kho Liên Chi Nhánh (Stock Transfer Workflow)**:
    - Cửa hàng trưởng tạo phiếu yêu cầu xuất hàng (`TRANSFER_CREATE`).
    - Phê duyệt và điều phối tập trung bởi SuperAdmin (`TRANSFER_APPROVE`).
    - Nhân viên kho chi nhánh nhận xác nhận nhập kho thực tế (`TRANSFER_RECEIVE`).

---

## 📦 4. HƯỚNG DẪN CÀI ĐẶT & CHẠY DỰ ÁN

### Yêu cầu tiên quyết (Prerequisites)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Microsoft SQL Server](https://www.microsoft.com/sql-server) (2019 trở lên hoặc SQL LocalDB / Express)
- [Node.js](https://nodejs.org) (tùy chọn để build CSS nếu tùy biến Tailwind)

### Các bước cài đặt

1. **Clone repository và di chuyển vào thư mục dự án**:
   ```bash
   git clone https://github.com/Ngocanh1725/LTW_UD_OmnichannelBeautySolution.git
   cd LTW_UD_OmnichannelBeautySolution
   ```

2. **Cấu hình Connection String**:
   Mở file `Omnichannel.Web/appsettings.json` và cập nhật chuỗi kết nối SQL Server:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=localhost;Database=OmnichannelBeautyDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;MultipleActiveResultSets=True;"
   }
   ```

3. **Cập nhật Database & Tự động Seed Data**:
   Khi ứng dụng khởi chạy lần đầu tiên, `DbInitializer` sẽ tự động thực thi migrations và nạp toàn bộ danh mục, 12 tài khoản nhân sự/khách hàng, sản phẩm mẫu và kho hàng.

4. **Khởi chạy ứng dụng**:
   ```bash
   dotnet run --project "Omnichannel.Web/Omnichannel.Web.csproj"
   ```
   Ứng dụng sẽ lắng nghe tại:
   - HTTP: `http://localhost:5116`
   - HTTPS: `https://localhost:7142` (tùy cấu hình)

5. **Chạy bộ kiểm thử tự động (Unit & Integration Tests)**:
   ```bash
   dotnet test "Omnichannel.Tests/Omnichannel.Tests.csproj"
   ```

6. **Chạy kịch bản kiểm thử hiệu năng tự động (System Performance Benchmark)**:
   ```powershell
   pwsh -File ./scripts/test_system_performance_benchmark.ps1 -RequestsPerEndpoint 50
   ```
   > 📑 Xem báo cáo chi tiết kỹ thuật tại [PERFORMANCE_OPTIMIZATION_REPORT.md](doc/PERFORMANCE_OPTIMIZATION_REPORT.md).

---

## 🔑 5. DANH SÁCH TÀI KHOẢN KIỂM THỬ (TEST ACCOUNTS)

> Mật khẩu mặc định của tất cả các tài khoản test: **`Admin@123`**  
> *(Xem chi tiết tại file `TEST_ACCOUNTS.md`)*

| Username | Vai trò | Chi nhánh | Mô tả quyền hạn |
|:---|:---|:---|:---|
| `superadmin` | **SuperAdmin** | Toàn hệ thống | Toàn quyền, cấu hình hệ thống, duyệt chuyển kho, xem P&L |
| `admin` | **SuperAdmin** | Toàn hệ thống | Quản trị viên kế thừa, hỗ trợ cả `Admin@123` và `Admin@123456` |
| `manager_hq` | **StoreManager** | Kho Tổng HQ-001 | Quản lý chi nhánh HQ-001, tạo phiếu chuyển kho |
| `manager_cg` | **StoreManager** | Showroom Cầu Giấy | Quản lý chi nhánh SH-CG, phân lập dữ liệu kho riêng biệt |
| `cashier01` | **Cashier** | Kho Tổng HQ-001 | Bán lẻ POS, mở/đóng ca, thanh toán đơn hàng |
| `cashier02` | **Cashier** | Showroom Cầu Giấy | Bán lẻ POS tại chi nhánh Cầu Giấy |
| `warehouse01` | **WarehouseStaff** | Kho Tổng HQ-001 | Quản lý lô hạn dùng FEFO, nhập kho, nhận điều chuyển |
| `warehouse02` | **WarehouseStaff** | Showroom Cầu Giấy | Quản lý kho chi nhánh Cầu Giấy |
| `editor01` | **ContentEditor** | Toàn chuỗi | Quản trị CMS: Bài viết, Banner, Menu, Đánh giá |
| `customer01` | **Customer** | Khách cá nhân | Hạng **Silver** (50 điểm tích lũy) |
| `customer02` | **Customer** | Khách cá nhân | Hạng **Gold** (400 điểm tích lũy) |
| `customer_vip` | **Customer VIP** | Khách cá nhân | Hạng **Diamond** (1.800 điểm tích lũy, cấn trừ thanh toán) |

---

## 📄 6. GIẤY PHÉP & BẢN QUYỀN
Dự án được xây dựng phục vụ môn học Lập trình Web và ứng dụng chuyển đổi số bán lẻ đa kênh. Mọi quyền sở hữu thuộc về nhóm phát triển.
