# ANTIGRAVITY AGENT RULES & ARCHITECTURAL GUIDELINES
**Dự án:** `LTW_UD_OmnichannelBeautySolution`  
**Vị trí file:** `/rule.md` (Thư mục gốc của Solution, cùng cấp với `OmnichannelBeautySolution.slnx`)  
**Kiến trúc:** Clean Architecture (Domain-Driven Design inspired)  
**Nền tảng:** .NET 8 / ASP.NET Core MVC  

---

## 1. VỊ TRÍ FILE & PHẠM VI HOẠT ĐỘNG (LOCATION & BOUNDARIES)

File quy tắc này được đặt tại thư mục gốc của repository để Antigravity Agent tự động nhận diện làm tài liệu chỉ dẫn bắt buộc (Primary Guardrails) trước khi thực hiện bất kỳ tác vụ đọc, phân tích, sinh mã hoặc tái cấu trúc nào.

### 1.1. Sơ đồ vị trí trong cây thư mục Solution
```text
LTW_UD_OmnichannelBeautySolution/
│
├── rule.md                          <-- [FILE QUY TẮC GỐC DÀNH CHO ANTIGRAVITY]
├── OmnichannelBeautySolution.slnx   <-- Solution File
├── .gitignore
├── .gitattributes
│
├── Omnichannel.Domain/              <-- Core Entities, Enums, Value Objects
├── Omnichannel.Application/         <-- Business Contracts, DTOs, ViewModels, Interfaces
├── Omnichannel.Infrastructure/      <-- EF Core DbContext, Data Migrations, External Services
└── Omnichannel.Web/                 <-- Presentation Layer (MVC Controllers, Views, Areas/Admin)
```

---

### 1.2. Quyền hạn cho phép (Allowed Operations)
- Đọc và phân tích toàn bộ cây thư mục solution (`.slnx`, `.csproj`, mã nguồn `.cs`, Razor views `.cshtml`, cấu hình `.json`).
- Bổ sung hoặc tối ưu logic nghiệp vụ trong `Omnichannel.Application` và `Omnichannel.Infrastructure`.
- Chuẩn hóa validation (FluentValidation / DataAnnotations), ViewModel/DTO mapping, xử lý ngoại lệ tập trung.
- Rà soát và vá lỗi logic điều phối tồn kho (`InventoryAllocationEngine`), luồng xử lý đơn hàng (`OrderWorkflowService`), và phân quyền động (`PermissionService`).
- Bổ sung Unit Tests / Integration Tests để kiểm tra tính toàn vẹn hệ thống.

---

### 1.3. Giới hạn nghiêm ngặt (Strict Restrictions - Do Not Touch / Modify Without Confirmation)
1. **Không sửa đổi cấu trúc vendor frontend:**  
   Tuyệt đối không can thiệp, sửa đổi mã nguồn gốc hoặc format lại các thư viện tĩnh tại `Omnichannel.Web/wwwroot/lib/**` (`bootstrap`, `jquery`, `jquery-validation`, `jquery-validation-unobtrusive`).
2. **Không phá vỡ nguyên lý phụ thuộc của Clean Architecture:**
   - Tuyệt đối không để `Omnichannel.Domain` tham chiếu bất kỳ project hoặc thư viện ngoài nào (Domain phải độc lập hoàn toàn, POCO thuần túy).
   - Tuyệt đối không tham chiếu `Omnichannel.Infrastructure` từ `Omnichannel.Application`.
   - `Omnichannel.Web` chỉ được sử dụng `Omnichannel.Infrastructure` thông qua Dependency Injection tại `Program.cs`. Không được inject `ApplicationDbContext` trực tiếp vào Controllers nếu đã có Service/Repository tương ứng.
3. **Không hardcode thông tin nhạy cảm:**  
   Tuyệt đối không hardcode connection strings, mật khẩu, private key, JWT secrets vào mã nguồn C# hoặc các file `appsettings*.json`.
4. **Không tùy tiện thay đổi Database Schema / Migrations:**  
   Khi cần sửa đổi Domain Entity, phải phân tích rõ sự ảnh hưởng tới `ApplicationDbContext` và thông báo trước khi tạo migration mới.

---

## 2. TIÊU CHUẨN CÔNG NGHỆ & QUY ƯỚC LẬP TRÌNH (TECH STACK & CONVENTIONS)

### 2.1. Tech Stack
- **Framework:** .NET (ASP.NET Core MVC)
- **ORM / Data Access:** Entity Framework Core (Code-First)
- **Database:** Relational Database (SQL Server / PostgreSQL / SQLite tùy connection string)
- **UI / Client:** Razor Pages/Views, HTML5, CSS3, JavaScript (ES6+), Bootstrap 5, jQuery & Unobtrusive Validation
- **Authentication & Authorization:** ASP.NET Core Cookie/Identity với Dynamic Claim-Based Permission Policy (`HasPermissionAttribute`, `PermissionAuthorizationHandler`)

### 2.2. Trách nhiệm chi tiết của 4 tầng kiến trúc

**1. Tầng Omnichannel.Domain:**
- **Trách nhiệm chính:** Chứa Core Entities và quy tắc doanh nghiệp gốc, không phụ thuộc bất kỳ layer nào khác. Là các POCO thuần túy.
- **Các thực thể trọng yếu:** `User`, `Role`, `Permission`, `UserRole`, `RolePermission`, `UserPermission`, `Product`, `ProductBatch`, `Category`, `Store`, `StoreInventory`, `Order`, `OrderDetail`, `Cart`, `CartItem`, `Invoice`, `Banner`, `Menu`, `MenuItem`, `Post`, `PostCategory`, `Supplier`, `InventoryLog`, `StaticPage`.

**2. Tầng Omnichannel.Application:**
- **Trách nhiệm chính:** Chứa Interfaces, DTOs, ViewModels và các hợp đồng nghiệp vụ (Business Contracts).
- **Hệ thống Interfaces:** `ICartService`, `IOrderWorkflowService`, `IPosService`, `IInventoryAllocationEngine`, `IMenuEngineService`, `IReportService`, `IPermissionService`, `IPasswordHasherService`.
- **Hệ thống DTOs và ViewModels:** Phân theo các phân hệ chức năng: Account, CMS, Inventory, Reports, Sales, Security.

**3. Tầng Omnichannel.Infrastructure:**
- **Trách nhiệm chính:** Cài đặt chi tiết các Interfaces từ tầng Application, quản lý truy xuất cơ sở dữ liệu và các dịch vụ hạ tầng kỹ thuật.
- **Phân hệ Dữ liệu (Data):** `ApplicationDbContext`, `DbInitializer`.
- **Phân hệ Bảo mật (Security):** `PermissionAuthorizationHandler`, `DynamicPermissionPolicyProvider`, `HasPermissionAttribute`, `PasswordHasherService`.
- **Phân hệ Triển khai Dịch vụ (Services):** `CartService`, `OrderWorkflowService`, `PosService`, `InventoryAllocationEngine`, `MenuEngineService`, `ReportService`.

**4. Tầng Omnichannel.Web:**
- **Trách nhiệm chính:** Tầng giao diện người dùng (Presentation Layer), đóng vai trò là điểm vào (Entry Point) của ứng dụng ASP.NET Core MVC.
- **Khu vực quản trị (Areas/Admin):** Các controllers và views phục vụ quản lý sản phẩm, ma trận phân quyền động, banner, quản lý kho hàng, báo cáo doanh thu, màn hình bán lẻ POS tại quầy.
- **Storefront Controllers:** `HomeController`, `CartController`, `CheckoutController`, `AccountController`.
- **ViewComponents tái sử dụng:** `HeaderMenuViewComponent`, `MiniCartViewComponent`.

### 2.3. Quy ước viết mã (Coding Standards)
- **Bất đồng bộ (Async/Await):** Tất cả các thao tác I/O (Database queries, file operations) bắt buộc phải sử dụng `async` / `await` và hỗ trợ `CancellationToken`.
- **Naming Conventions:**
  - PascalCase cho Class, Method, Interface (tiền tố `I`), Property, ViewModel.
  - camelCase với tiền tố gạch dưới (`_camelCase`) cho private readonly dependencies được inject qua Constructor.
  - DTO/ViewModel phải đặt tên tường minh phản ánh mục đích (`CreateProductDto`, `OrderWorkflowResultDto`, `PermissionMatrixViewModel`).
- **Giao dịch (Database Transactions):** Các thao tác xử lý nghiệp vụ phức tạp liên quan đến nhiều bảng (ví dụ: Chốt đơn hàng $\rightarrow$ trừ tồn kho chi nhánh $\rightarrow$ ghi log lịch sử kho $\rightarrow$ tạo hóa đơn) bắt buộc phải được bọc trong một Transaction (`IDbContextTransaction`) để đảm bảo tính toàn vẹn ACID.

---

## 3. PHƯƠNG ÁN KIỂM TRA DỰ ÁN DÀNH CHO ANTIGRAVITY (AUDIT & INSPECTION PLAN)

Khi được yêu cầu kiểm tra, đánh giá hoặc rà soát dự án `LTW_UD_OmnichannelBeautySolution`, Antigravity phải thực hiện tuần tự theo quy trình 6 bước sau:

- Bước 1: Build & Dependency Health
- Bước 2: Architectural Conformance (Kiểm tra ranh giới Clean Architecture)
- Bước 3: Dynamic Security & Permission Matrix Audit
- Bước 4: Core Business Engine Audit (Kho, Đơn hàng, POS đa kênh)
- Bước 5: Entity Framework Core & Data Integrity Verification
- Bước 6: Presentation Layer, Routing & CSRF Protection Check

### Chi tiết các bước thực hiện:

**Bước 1: Kiểm tra tính sẵn sàng & Build Health**
- Kiểm tra file solution `OmnichannelBeautySolution.slnx` và tất cả các file `*.csproj`.
- Chạy lệnh kiểm tra build:
  ```bash
  dotnet build OmnichannelBeautySolution.slnx --verbosity minimal
  ```
- Rà soát các cảnh báo (Warnings) về `nullable reference types`, packages lỗi thời hoặc deprecated APIs.

**Bước 2: Kiểm tra tuân thủ kiến trúc Clean Architecture**
- Domain Layer: Đảm bảo không có using directive nào liên quan đến EF Core, ASP.NET Core MVC hay các tầng khác.
- Application Layer: Kiểm tra các DTO/Interface không chứa trực tiếp `DbSet<T>` hoặc Entity Framework dependency.
- Infrastructure Layer: Đảm bảo tất cả services implement đúng interface từ `Omnichannel.Application`.
- Web Layer: Quét các Controllers trong `Areas/Admin/Controllers` và storefront `Controllers` để phát hiện nếu có controller nào inject trực tiếp `ApplicationDbContext` thay vì thông qua Service layer.

**Bước 3: Rà soát cơ chế Phân quyền (Security & Dynamic Permission Audit)**
- Kiểm tra HasPermissionAttribute & DynamicPermissionPolicyProvider:
  - Kiểm tra xem các Admin Action có được bảo vệ bởi `[HasPermission(...)]` hoặc `[Authorize]` tương ứng không.
  - Kiểm tra `PermissionAuthorizationHandler.cs` xử lý logic kiểm tra Claim/Database có bị leak quyền hoặc dính lỗi N+1 query không.
- Kiểm tra Password Hasher:
  - Đảm bảo `IPasswordHasherService` sử dụng thuật toán băm an toàn (PBKDF2/BCrypt/Argon2/ASP.NET Identity hasher) kèm Salt, tuyệt đối cấm MD5 hoặc SHA256 đơn thuần không có salt.
- Phòng chống lỗ hổng bảo mật:
  - Kiểm tra tất cả action `[HttpPost]` đều có `[ValidateAntiForgeryToken]`.
  - Đảm bảo chống tấn công IDOR (Insecure Direct Object References) khi xem đơn hàng, giỏ hàng giữa các tài khoản khác nhau.

**Bước 4: Kiểm tra Logic nghiệp vụ Đa kênh (Omnichannel Engines)**
- Phân hệ Tồn kho (`IInventoryAllocationEngine` / `StoreInventory`):
  - Kiểm tra logic giữ hàng (holding/reservation) khi khách đặt online so với bán lẻ tại quầy (`PosController`).
  - Kiểm tra trường hợp race condition (nhiều đơn hàng cùng mua số lượng tồn cuối cùng của sản phẩm tại một store).
  - Kiểm tra việc ghi chép `InventoryLog` mỗi khi có biến động tăng/giảm kho.
- Quy trình Đơn hàng (`IOrderWorkflowService` / `PosService`):
  - Kiểm tra trạng thái vòng đời của `Order`: Chờ xác nhận $\rightarrow$ Đã phân bổ kho $\rightarrow$ Đang giao $\rightarrow$ Hoàn thành / Hủy.
  - Kiểm tra luồng POS: In hóa đơn (`PrintReceipt.cshtml`), cập nhật doanh thu và trừ kho tức thì.
- Dynamic CMS & Menu Engine (`IMenuEngineService`):
  - Kiểm tra logic render phân cấp (Tree Hierarchy) của `Menu` và `MenuItem`. Đảm bảo không đệ quy vô hạn khi dữ liệu cha-con bị vòng lặp.

**Bước 5: Kiểm tra tầng dữ liệu (Data & EF Core Integrity)**
- Kiểm tra các cấu hình fluent API / navigation properties trong `ApplicationDbContext.cs`:
  - Khóa chính, Khóa ngoại của các quan hệ nhiều-nhiều (`UserRole`, `RolePermission`, `UserPermission`).
  - Cấu hình Cascade Delete: Đảm bảo không cascade delete ngoài ý muốn làm mất dữ liệu lịch sử giao dịch (`Order`, `Invoice`, `InventoryLog`).
- Kiểm tra file `DbInitializer.cs`:
  - Kiểm tra dữ liệu seed mặc định (SuperAdmin account, Roles, Root Permissions, Default Stores).
  - Đảm bảo việc khởi tạo không gây lỗi `Unique Constraint Violation` khi ứng dụng khởi động lại nhiều lần.

**Bước 6: Kiểm tra UI & Presentation Layer**
- Kiểm tra Area Routing: Đảm bảo các controller trong Admin có thuộc tính `[Area("Admin")]` và route tương thích.
- Kiểm tra ViewComponents:
  - `HeaderMenuViewComponent`: Phải hoạt động an toàn kể cả khi DB chưa có dữ liệu menu (tránh null reference).
  - `MiniCartViewComponent`: Phải hiển thị đúng số lượng và tổng tiền giỏ hàng dựa trên session/cookie.
- Kiểm tra tính tương thích Responsive của các View (`Index.cshtml`, `Create.cshtml`) và Admin Layout (`_AdminLayout.cshtml`).

---

## 4. BẢNG MẪU BÁO CÁO ĐÁNH GIÁ (AUDIT REPORT TEMPLATE)

Mỗi khi Antigravity hoàn thành đợt kiểm tra dự án, agent phải xuất kết quả theo mẫu văn bản sau:

```text
KẾT QUẢ KIỂM TRA DỰ ÁN: OmnichannelBeautySolution
- Thời gian thực hiện: [YYYY-MM-DD]
- Tình trạng Build: [SUCCESS / FAILED]
- Mức độ tuân thủ kiến trúc (Clean Architecture Conformance): [Tốt / Cần cải thiện / Vi phạm]

1. Các vấn đề nghiêm trọng (Critical Issues):
- [Vấn đề bảo mật / Race condition / Vi phạm ranh giới tầng kiến trúc]

2. Cảnh báo & Điểm nghẽn (Warnings & Code Smells):
- [N+1 Query / Thiếu ValidateAntiForgeryToken / Hardcoded string]

3. Khuyến nghị cải tiến (Actionable Recommendations):
- [Hành động 1 kèm file path cụ thể]
- [Hành động 2 kèm file path cụ thể]
```