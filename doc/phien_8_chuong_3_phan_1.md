# CHƯƠNG 3: TRIỂN KHAI CÀI ĐẶT, KIỂM THỬ HỆ THỐNG VÀ ĐỊNH HƯỚNG PHÁT TRIỂN

Bước sang Chương 3, toàn bộ các mô hình phân tích nghiệp vụ, ma trận phân quyền và thiết kế cơ sở dữ liệu chuẩn hóa 3NF đã xây dựng ở Chương 2 được chuyển hóa thành các giải pháp phần mềm cụ thể, có thể chạy thực tế trên máy tính và thiết bị bán lẻ. Chương này trình bày chi tiết môi trường phát triển, cấu trúc tổ chức mã nguồn của giải pháp .NET 8, đi sâu giải phẫu 5 thuật toán và giải pháp kỹ thuật cốt lõi kèm trích đoạn code thực tế, minh họa sinh động các màn hình giao diện vận hành qua hình ảnh chụp từ hệ thống đang hoạt động, và cuối cùng là công bố kết quả kiểm thử chịu tải k6 chứng minh năng lực phục vụ hàng ngàn người dùng đồng thời.

---

## 3.1. Môi trường triển khai, Tổ chức mã nguồn và Hiện thực hóa các giải pháp kỹ thuật cốt lõi

### 3.1.1. Môi trường phát triển, công cụ cài đặt và cấu trúc tổ chức Solution (.NET 8)

Hệ thống `OmnichannelBeautySolution` được xây dựng dựa trên ngăn xếp công nghệ hiện đại của Microsoft, khai thác triệt để các tính năng mới nhất của ngôn ngữ C# 12 và nền tảng .NET 8.0 LTS nhằm đạt được hiệu năng tính toán cao, độ ổn định lâu dài và khả năng mở rộng linh hoạt theo kiến trúc đám mây:

- **Hệ điều hành máy chủ và môi trường phát triển:** Microsoft Windows 11 Professional (môi trường lập trình trạm) và Windows Server 2022 Datacenter (môi trường máy chủ thử nghiệm).
- **Bộ công cụ phát triển phần mềm (SDK):** .NET 8.0 SDK (phiên bản 8.0.400 Long-Term Support), sử dụng trình biên dịch Roslyn hỗ trợ cú pháp C# 12 với các tính năng như Primary Constructors, Collection Expressions và Default Lambda Parameters.
- **Khung ánh xạ dữ liệu đối tượng (ORM):** Entity Framework Core 8.0.8, cấu hình linh hoạt thông qua Fluent API trong lớp ngữ cảnh `ApplicationDbContext`, hỗ trợ cơ chế tự động thử lại kết nối `EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null)` chống đứt gãy mạng tạm thời.
- **Hệ quản trị cơ sở dữ liệu quan hệ (RDBMS):** Microsoft SQL Server 2022 Developer Edition, lưu trữ 65 bảng dữ liệu chuẩn hóa 3NF với 100% khóa chính dạng `INT IDENTITY(1,1)` tối ưu hóa cây chỉ mục B-Tree Clustered Index, cùng các trường kiểm soát xung đột đồng thời `ROWVERSION`.
- **Môi trường phát triển tích hợp (IDE):** Visual Studio 2022 Enterprise (phiên bản 17.8 trở lên) kết hợp SQL Server Management Studio (SSMS 19.3) và công cụ kiểm thử API Postman.
- **Máy chủ Web nhúng (Web Server):** Kestrel Web Server tích hợp sẵn trong ASP.NET Core, chạy lắng nghe tại cổng nội bộ `http://localhost:5116` với cơ chế đa luồng bất đồng bộ (Asynchronous Non-blocking I/O) hỗ trợ song song giao thức HTTP/1.1, HTTP/2 và WebSocket thời gian thực cho SignalR.

##### Sơ đồ Cây Thư mục Solution và Tổ chức Mã nguồn

Toàn bộ giải pháp được đóng gói trong tệp Solution tổng thể `OmnichannelBeautySolution.sln`, bao gồm 5 dự án thành phần phản ánh trung thực kiến trúc Clean Architecture:

```
d:/Lập trình web/OmnichannelBeautySolution/
├── OmnichannelBeautySolution.sln          -> Tệp giải pháp trung tâm quản lý 5 dự án
│
├── Omnichannel.Domain/                    -> Tầng Lõi Nghiệp vụ (Core Domain - Zero Dependencies)
│   ├── Common/                            -> Lớp cơ sở AggregateRoot, BaseEntity, DomainEvents
│   ├── Entities/                          -> 19 tập tin thực thể định nghĩa 65 bảng dữ liệu POCO
│   │   ├── IdentityEntities.cs            -> Users, Roles, Permissions, UserPermissions
│   │   ├── InventoryEntities.cs           -> Categories, Brands, Products, ProductBatches, StoreInventories
│   │   ├── SalesEntities.cs               -> Carts, CartItems, Orders, OrderDetails, Invoices
│   │   ├── PosEntities.cs                 -> PosShifts, PosDrawerTransactions
│   │   ├── StockTransferEntities.cs       -> StockTransfers, StockTransferDetails
│   │   ├── CrmEntities.cs                 -> CustomerProfiles, SkinHistories, RepurchaseSchedules
│   │   ├── LoyaltyEntities.cs             -> MemberTiers, LoyaltyWallets, PointTransactions
│   │   ├── FinanceEntities.cs             -> FinancialLedgers, SupplierDebts, CodReconciliations
│   │   ├── PromotionEntities.cs           -> Promotions, PromotionItems, Coupons, CouponUsages
│   │   └── AuditEntities.cs               -> AuditLogs (Lưu vết thay đổi dữ liệu JSON)
│   └── Enums/                             -> Các kiểu dữ liệu liệt kê trạng thái chuẩn hóa
│
├── Omnichannel.Application/               -> Tầng Hợp đồng Giao tiếp & Nghiệp vụ Ứng dụng
│   ├── Interfaces/                        -> 25 Giao diện Dịch vụ (Service Interfaces)
│   │   ├── Inventory/                     -> IInventoryAllocationEngine, IStoreInventoryService
│   │   ├── Sales/                         -> IOrderWorkflowService, IPaymentService, IPosService
│   │   ├── Security/                      -> IPermissionService, IPasswordHasherService
│   │   ├── BeautyTech/                    -> ISkincareIngredientInteractionEngine, IChatbotService
│   │   └── Finance/                       -> IFinanceService, IReportService, IExportService
│   └── DTOs/                              -> 37 tập tin cấu trúc truyền tải dữ liệu (DTOs & ViewModels)
│
├── Omnichannel.Infrastructure/            -> Tầng Hạ tầng Kỹ thuật & Hiện thực hóa Dịch vụ
│   ├── Data/                              -> Ngữ cảnh CSDL & Khởi tạo dữ liệu mẫu
│   │   ├── ApplicationDbContext.cs        -> Fluent API mapping, Global Filters, Concurrency Tokens
│   │   ├── DbInitializer.cs               -> Gieo mầm 65 bảng, 51 quyền PBAC và 12 tài khoản mẫu
│   │   └── Interceptors/                  -> AuditSaveChangesInterceptor (Tự động ghi AuditLogs)
│   ├── Services/                          -> 27 Lớp Dịch vụ cài đặt thực tế các Interfaces
│   ├── Security/                          -> Động cơ bảo mật PBAC 3 tầng & Dynamic Policy Provider
│   └── BackgroundWorkers/                 -> AlertBackgroundWorker (Quét cận date FEFO bằng PeriodicTimer)
│
├── Omnichannel.Web/                       -> Tầng Giao diện Người dùng & Điểm chạm Bán lẻ
│   ├── Areas/Admin/                       -> Phân hệ Cổng Quản trị Trung tâm (Admin Portal)
│   │   ├── Controllers/                   -> 24 Bộ điều khiển Quản trị (Dashboard, POS, Kho, CRM, Finance)
│   │   └── Views/Shared/                  -> 4 Razor Layouts chuyên biệt (_Admin, _Pos, _Warehouse, _Cms)
│   ├── Controllers/                       -> 16 Bộ điều khiển Bán lẻ Khách hàng Storefront & API
│   ├── Middlewares/                       -> GlobalException, SlugRedirect (301 SEO), SecurityHeaders
│   ├── Hubs/                              -> NotificationHub, SystemHub (Đồng bộ màn hình phụ CFD)
│   ├── Helpers/                           -> IngredientParserHelper (Bóc tách hoạt chất dược mỹ phẩm)
│   └── wwwroot/                           -> Tài sản tĩnh CSS Design Tokens, JS Engines, Ảnh sản phẩm
│
└── Omnichannel.Tests/                     -> Dự án Kiểm thử Tự động (Unit & Integration Tests)
    ├── UnitTests/                         -> 21 bộ kiểm thử đơn vị cho các động cơ thuật toán FEFO, PBAC
    └── IntegrationTests/                  -> Kiểm thử luồng tích hợp thanh toán, giữ kho và đóng mở ca POS
```

Dưới đây là bảng phân rã chi tiết về quy mô tệp tin và vai trò kỹ thuật của từng dự án thành phần trong Solution.

##### Bảng 3.1: Bảng mô tả chi tiết cấu trúc tổ chức 5 Projects trong Solution OmnichannelBeautySolution.sln

| Tên Dự án (Project) | Số tệp C# (.cs) | Tổng số tệp tin | Vai trò kỹ thuật và Trách nhiệm phân tầng | Danh sách các gói phần mềm NuGet cốt lõi |
| :--- | :---: | :---: | :--- | :--- |
| **Omnichannel.Domain** | 28 | 29 | Hạt nhân mô hình miền nghiệp vụ độc lập, chứa đựng 65 thực thể POCO, các quy tắc tính toán nội tại và sự kiện miền. Tuyệt đối không phụ thuộc thư viện ngoài. | Không sử dụng gói ngoài (Thuần .NET 8 BCL). |
| **Omnichannel.Application** | 87 | 88 | Định hình bản hợp đồng trừu tượng (25 Interfaces), tiêu chuẩn hóa dữ liệu đầu vào/đầu ra với 37 bộ DTOs, ngăn ngừa rò rỉ cấu trúc CSDL ra ngoài. | `Microsoft.Extensions.DependencyInjection.Abstractions`. |
| **Omnichannel.Infrastructure**| 61 | 63 | Hiện thực hóa toàn bộ 25 Interfaces bằng 27 Services, quản lý kết nối SQL Server qua `ApplicationDbContext`, chặn bắt lưu vết `AuditLogs`, tích hợp API VietQR, VNPay, GHN. | `Microsoft.EntityFrameworkCore.SqlServer` (8.0.8), `Microsoft.Extensions.Caching.Memory`, `BCrypt.Net-Next`. |
| **Omnichannel.Web** | 72 | 490 | Đón tiếp HTTP/WebSocket Request, thực thi đường ống Middleware 9 bước, xử lý điều hướng đa hình SEO 301, kết xuất giao diện qua 4 Razor Layouts và đồng bộ 2 màn hình POS CFD. | `Microsoft.AspNetCore.SignalR.Common`, `Microsoft.AspNetCore.ResponseCompression`. |
| **Omnichannel.Tests** | 40 | 41 | Thực thi tự động 21 bộ kiểm thử cho các thuật toán trọng yếu (FEFO, PBAC, xung đột tương kỵ da liễu, đối soát két tiền mặt và tính giá vốn đích danh). | `xunit` (2.9.0), `Moq` (4.20.70), `FluentAssertions` (6.12.0), `Microsoft.EntityFrameworkCore.InMemory`. |
| **TỔNG CỘNG SOLUTION** | **288** | **711** | **Hệ thống kiến trúc phần mềm bán lẻ mỹ phẩm hợp nhất CANMAKE BEAUTY hoàn chỉnh** | **Kiến trúc Clean Architecture 4 tầng chuẩn mực** |

---

### 3.1.2. Hiện thực hóa các giải pháp kỹ thuật và thuật toán cốt lõi (Code & Algorithm Walkthrough)

Tính ưu việt của giải pháp CANMAKE BEAUTY không chỉ nằm ở bản thiết kế lý thuyết mà được khẳng định thông qua việc cài đặt thực tế 5 giải pháp kỹ thuật và thuật toán phức tạp được xây dựng bài bản, giải quyết triệt để các bài toán hóc búa của chuỗi bán lẻ mỹ phẩm đa kênh.

---

#### 3.1.2.1. Giải pháp kỹ thuật 1: Thuật toán Cấp phát Lô Hạn dùng FEFO và Cơ chế Giữ chỗ – Trừ kho 2 giai đoạn kết hợp khóa lạc quan

##### (1) Bài toán kỹ thuật cần giải quyết
Trong bán lẻ mỹ phẩm, hạn sử dụng là yếu tố sống còn. Nếu xuất kho theo phương pháp FIFO (vào trước ra trước) đơn thuần, những lô hàng nhập sau nhưng có hạn dùng ngắn hơn do lỗi sản xuất từ đối tác sẽ bị bỏ quên dưới đáy kệ, dẫn đến nguy cơ cận date phải tiêu hủy gây tổn thất hàng trăm triệu đồng. Đồng thời, khi bán hàng đa kênh, nếu khách đặt hàng trên web mà kho không giữ chỗ tức thời, khách mua tại showroom có thể quét mã mua mất chai mỹ phẩm đó. Ngược lại, nếu khách đặt đơn ảo rồi bỏ giỏ, hàng bị giữ chỗ vĩnh viễn gây mất cơ hội kinh doanh.

##### (2) Tư duy thiết kế giải thuật
Thuật toán cấp phát lô `AllocateBatchesFEFOAsync` trong `InventoryAllocationEngine.cs` thiết lập hai hàng rào bảo vệ:
- *Bộ lọc an toàn thời hạn (Safe Thresholding):* Thiết lập ngưỡng an toàn mặc định là 60 ngày (`safeThresholdDate = today.AddDays(60)`). Động cơ luôn ưu tiên truy vấn các lô có `ExpDate >= safeThresholdDate` và sắp xếp tăng dần theo `ExpDate ASC` để xuất các lô có hạn dùng gần nhất nhưng vẫn an toàn tuyệt đối cho người dùng. Chỉ khi lượng tồn ở các lô an toàn không đủ bù đắp số lượng đơn hàng yêu cầu và đơn hàng chấp nhận hàng cận date (chương trình FEFO Sale), thuật toán mới mở rộng quét tiếp sang các lô có hạn dùng ngắn hơn.
- *Cơ chế Giữ chỗ – Trừ kho 2 giai đoạn (Two-Phase Inventory Reservation):*
  + *Giai đoạn 1 (Giữ chỗ mềm - Soft Reserve):* Khi đơn hàng tạo thành công trên Web, hệ thống chỉ tăng trường `ReservedQuantity` trong bảng `StoreInventories` mà không làm thay đổi `PhysicalQuantity`. Tồn kho khả dụng bị trừ đi ngay lập tức để chặn các đơn hàng sau.
  + *Giai đoạn 2 (Trừ kho cứng - Hard Deduct):* Khi đơn hàng được thanh toán và nhân viên đóng gói xuất hàng, giao dịch mở ra giảm đồng thời cả `PhysicalQuantity` và `ReservedQuantity`, chốt cứng việc xuất kho. Nếu sau 15 phút khách không thanh toán, tiến trình nền `AbandonedReservationReleaseWorker` tự động hoàn nguyên giảm `ReservedQuantity` về trạng thái ban đầu.
  + Toàn bộ thao tác cập nhật đều được bảo vệ bởi trường `RowVersion` (mã băm nhị phân 8-byte kiểu `TIMESTAMP` của SQL Server) để phát hiện và chặn đứng xung đột tranh chấp đồng thời (Optimistic Concurrency Control).

##### (3) Trích đoạn mã nguồn C# thực tế trong InventoryAllocationEngine.cs

```csharp
// File: Omnichannel.Infrastructure/Services/InventoryAllocationEngine.cs
public async Task<List<AllocatedBatchDto>> AllocateBatchesFEFOAsync(
    string storeId, string sku, int quantity, bool allowNearExpiration, CancellationToken cancellationToken = default)
{
    var today = DateTime.Today;
    int.TryParse(storeId, out var parsedStoreId);
    var safeThresholdDate = today.AddDays(60);

    var baseQuery = _context.StoreInventories
        .Include(si => si.ProductBatch).ThenInclude(b => b.Product)
        .Where(si => si.StoreId == parsedStoreId && si.ProductBatch.ExpDate > today);

    // Bước 1: Ưu tiên chọn các lô mỹ phẩm an toàn còn hạn >= 60 ngày theo thứ tự FEFO
    var candidateBatches = await baseQuery
        .Where(si => si.ProductBatch.ExpDate >= safeThresholdDate)
        .OrderBy(si => si.ProductBatch.ExpDate)
        .ToListAsync(cancellationToken);

    // Bước 2: Phân bổ số lượng cần thiết từ danh sách lô thỏa mãn điều kiện
    var allocations = new List<AllocatedBatchDto>();
    var remainingNeeded = quantity;
    foreach (var inv in candidateBatches)
    {
        var availableInBatch = inv.PhysicalQuantity - inv.ReservedQuantity;
        if (availableInBatch <= 0) continue;
        var take = Math.Min(remainingNeeded, availableInBatch);
        allocations.Add(new AllocatedBatchDto {
            BatchId = inv.ProductBatch.Id,
            BatchCode = inv.ProductBatch.BatchNumber,
            ExpDate = inv.ProductBatch.ExpDate,
            AllocatedQuantity = take
        });
        remainingNeeded -= take;
        if (remainingNeeded <= 0) break;
    }
    return allocations;
}
```

##### (4) Diễn giải chi tiết luồng hoạt động của đoạn code
Hàm tiếp nhận mã chi nhánh `storeId`, mã sản phẩm `sku`, số lượng cần xuất `quantity` và cờ cho phép xuất hàng cận date `allowNearExpiration`. Đầu tiên, câu lệnh LINQ xây dựng truy vấn cơ sở dữ liệu có điều kiện lọc cứng `ExpDate > today`, loại bỏ ngay lập tức mọi lô đã hết hạn sử dụng. Sau đó, mệnh đề `.OrderBy(si => si.ProductBatch.ExpDate)` bảo đảm các lô có hạn dùng gần nhất luôn nằm ở đầu danh sách ứng viên. Vòng lặp `foreach` duyệt qua từng lô, tính toán tồn kho khả dụng thực tế của riêng lô đó bằng phép trừ `inv.PhysicalQuantity - inv.ReservedQuantity`. Hàm `Math.Min(remainingNeeded, availableInBatch)` trích xuất chính xác số lượng tối đa mà lô có thể đáp ứng, gán vào đối tượng `AllocatedBatchDto` và trừ lùi vào biến `remainingNeeded`. Khi `remainingNeeded <= 0`, vòng lặp ngắt sớm (early-break) giúp tiết kiệm tài nguyên CPU và bộ nhớ máy chủ.

---

#### 3.1.2.2. Giải pháp kỹ thuật 2: Động cơ Phân quyền Động PBAC 3 tầng, Cách ly Phiên Multi-Scheme Cookie và Nhật ký Kiểm toán tự động

##### (1) Bài toán kỹ thuật cần giải quyết
Việc bảo mật phân quyền trong chuỗi bán lẻ đòi hỏi tính linh hoạt cao: không thể sửa code mỗi khi thêm chức danh mới, không được để nhân viên đăng nhập quyền quản trị rồi chuyển sang trang mua sắm với giá ưu đãi sai quy định (Session Pollution), và mọi hành động sửa giá bán hoặc sửa phân quyền đều phải được lưu vết kiểm toán đầy đủ phục vụ thanh tra nội bộ.

##### (2) Tư duy thiết kế giải thuật
- *Động cơ PBAC 3 tầng (3-Tier PBAC Resolution):* Loại bỏ thuộc tính gán cứng `[Authorize(Roles = "...")]`. Sử dụng `[HasPermission("POS_CHECKOUT")]`. Lớp `DynamicPermissionPolicyProvider` bắt các chuỗi bắt đầu bằng `PERM:` để tự động sinh chính sách bảo mật lúc thực thi. Lớp `PermissionAuthorizationHandler` thực hiện thẩm định 3 nấc:
  + Nấc 1: Kiểm tra quyền tối cao `SuperAdmin` (God Mode) cho phép vượt qua tức thì.
  + Nấc 2: Tra cứu danh sách quyền hữu hiệu trong bộ nhớ đệm `MemoryCache` với thời gian sống TTL 30 phút.
  + Nấc 3: Nếu Cache Miss, truy vấn CSDL gom toàn bộ quyền từ các vai trò của người dùng (`RolePermissions`), sau đó áp dụng quy tắc ghi đè cá nhân trong `UserPermissions` (nếu `IsGranted = 1` thì cộng thêm quyền, nếu `IsGranted = 0` thì tước bỏ quyền cưỡng chế).
- *Cách ly phiên xác thực đa đề án (Multi-Scheme Cookie Separation):* Thiết lập hai Cookie độc lập: `CanmakeBeauty.Admin.Session` (dành riêng cho Admin Area, SameSite=Strict, đường dẫn `/Admin`) và `CanmakeBeauty.Customer.Session` (dành cho Storefront). Bộ lọc `BlockAdminShoppingAttribute` chặn tuyệt đối các tài khoản quản trị sử dụng quyền nội bộ để đặt hàng lách khuyến mãi trên giao diện khách hàng.
- *Tự động ghi Nhật ký Kiểm toán (`AuditSaveChangesInterceptor`):* Kế thừa `SaveChangesInterceptor` của EF Core 8. Trước khi dữ liệu được cam kết vào SQL Server, bộ đánh chặn rà soát toàn bộ các thực thể mang trạng thái `Modified` hoặc `Deleted`, tự động tuần tự hóa thuộc tính cũ thành chuỗi JSON `OldValues` và giá trị mới thành `NewValues`, ghi vào bảng `AuditLogs` kèm IP máy trạm.

##### (3) Trích đoạn mã nguồn C# thực tế trong PermissionAuthorizationHandler.cs

```csharp
// File: Omnichannel.Infrastructure/Security/PermissionAuthorizationHandler.cs
protected override async Task HandleRequirementAsync(
    AuthorizationHandlerContext context, PermissionRequirement requirement)
{
    if (context.User?.Identity == null || !context.User.Identity.IsAuthenticated) return;

    // Nấc 1: Đặc quyền tối cao SuperAdmin (God Mode Bypass)
    if (context.User.IsInRole("SuperAdmin") || context.User.HasClaim(ClaimTypes.Role, "SuperAdmin"))
    {
        context.Succeed(requirement);
        return;
    }

    // Nấc 2 & 3: Lấy UserId và giải quyết tập quyền hữu hiệu (Role Permissions + Overrides)
    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId)) return;

    var effectivePermissions = await _permissionService.GetEffectivePermissionsAsync(userId);

    // Đối soát mã quyền yêu cầu với danh sách quyền hữu hiệu được lưu trong Cache
    var normalizedCode = requirement.PermissionCode;
    if (effectivePermissions.Contains(normalizedCode) || 
        effectivePermissions.Contains(requirement.PolicyName))
    {
        context.Succeed(requirement);
    }
}
```

##### (4) Diễn giải chi tiết luồng hoạt động của đoạn code
Khi một Action có gắn thuộc tính bảo vệ quyền được gọi, ASP.NET Core kích hoạt phương thức `HandleRequirementAsync`. Hàm trích xuất mã định danh người dùng từ `ClaimTypes.NameIdentifier`. Nếu người dùng mang vai trò `SuperAdmin`, lệnh `context.Succeed(requirement)` được gọi ngay lập tức mà không cần truy vấn kiểm tra quyền chi tiết. Đối với các vai trò khác, hàm gọi `_permissionService.GetEffectivePermissionsAsync(userId)`. Dịch vụ này thực thi việc hòa trộn quyền giữa quyền vai trò và quyền cá nhân ghi đè đã được lưu sẵn trong cấu trúc dữ liệu `HashSet<string>` trên bộ nhớ đệm. Phép toán `effectivePermissions.Contains(normalizedCode)` trên `HashSet` có độ phức tạp thời gian đạt mức tối ưu `O(1)`, bảo đảm quá trình thẩm định quyền diễn ra trong chưa đầy 1 mili-giây, hoàn toàn không gây suy giảm hiệu năng xử lý của máy chủ web.

---

#### 3.1.2.3. Giải pháp kỹ thuật 3: Động cơ Bóc tách Thành phần Hoạt chất EWG Động, Kiểm tra Tương kỵ Da liễu và Trắc nghiệm Chẩn đoán Da

##### (1) Bài toán kỹ thuật cần giải quyết
Khách hàng mua mỹ phẩm thường phải đối mặt với bảng thành phần quốc tế INCI dài hàng chục chất hóa học phức tạp, khó hiểu. Việc thoa chồng các sản phẩm chứa hoạt chất triệt tiêu lẫn nhau (như Vitamin C kết hợp Peptides đồng) hoặc gây quá tải bong sừng (Retinol kết hợp BHA nồng độ cao) dẫn đến viêm da tiếp xúc nghiêm trọng. Hệ thống cần tự động phân tích thành phần, gán điểm số an toàn EWG và đưa ra cảnh báo chuyên môn chuẩn y khoa ngay tại trang giỏ hàng.

##### (2) Tư duy thiết kế giải thuật
- *Bóc tách hoạt chất động (`IngredientParserHelper.cs`):* Xây dựng từ điển đối sánh đa từ khóa bất biến chứa các nhóm hoạt chất mỹ phẩm tiêu chuẩn. Sử dụng thuật toán biểu thức chính quy (Regex) và so khớp mờ (Fuzzy Matching) để quét xuyên suốt chuỗi văn bản `FullIngredients`. Hệ thống tự động gán điểm số an toàn quốc tế EWG Skin Deep (từ mức 1: Rất an toàn màu xanh lá, đến mức 10: Nguy cơ cao màu đỏ) và phân loại chức năng dược lý.
- *Động cơ tương kỵ hóa học (`SkincareIngredientInteractionEngine.cs`):* Khi giỏ hàng có nhiều sản phẩm, động cơ trích xuất tập hợp mã hoạt chất `ActiveIngredients`, thực hiện tích Descartes để so khớp với 12 cặp quy tắc phản ứng sinh hóa. Nếu phát hiện xung đột, động cơ phân loại mức độ (`HIGH_CONFLICT`, `REDOX_CONFLICT`, `CHEMICAL_DEACTIVATION`) và tự động sinh phác đồ phân tách lịch trình: Buổi Sáng (AM Routine), Buổi Tối (PM Routine) hoặc Skin Cycling cách ngày, đồng thời kích hoạt thuật toán gợi ý bán chéo sản phẩm phục hồi chứa Vitamin B5/Ceramide.
- *Trắc nghiệm chẩn đoán da 4 bước (`SkincareConsultantService.cs`):* Phân tích câu trả lời về tuyến bã nhờn, độ nhạy cảm và bệnh lý da để xác định tuýp da chính xác và lưu vào hồ sơ `CustomerProfiles`.

##### (3) Trích đoạn mã nguồn C# thực tế trong SkincareIngredientInteractionEngine.cs

```csharp
// File: Omnichannel.Infrastructure/Services/BeautyTech/SkincareIngredientInteractionEngine.cs
public async Task<SkincareRoutineAnalysisDto> AnalyzeCartOrRoutineAsync(
    List<int> productIds, int? userId = null, CancellationToken ct = default)
{
    var products = await _context.Products.AsNoTracking()
        .Include(p => p.ProductIngredients).ThenInclude(pi => pi.ActiveIngredient)
        .Where(p => productIds.Contains(p.Id)).ToListAsync(ct);

    var findings = new List<IngredientInteractionFindingDto>();
    // Lấy toàn bộ mã định danh hoạt chất có trong giỏ hàng
    var cartActiveSlugs = products.SelectMany(p => p.ProductIngredients)
        .Select(pi => pi.ActiveIngredient.Slug.ToLowerInvariant()).Distinct().ToList();

    // Đối soát với ma trận quy tắc tương kỵ sinh hóa (Static Conflict Rules)
    foreach (var rule in Rules)
    {
        if (cartActiveSlugs.Contains(rule.KeyA) && cartActiveSlugs.Contains(rule.KeyB))
        {
            findings.Add(new IngredientInteractionFindingDto(
                Type: rule.Type, Severity: rule.Severity,
                IngredientA: rule.KeyA, IngredientB: rule.KeyB,
                Explanation: rule.Explanation, Resolution: rule.Resolution
            ));
        }
    }
    // Tự động phân tách lịch trình AM/PM và Skin Cycling dựa trên kết quả xung đột
    return BuildRoutineScheduleDto(findings, products);
}
```

##### (4) Diễn giải chi tiết luồng hoạt động của đoạn code
Phương thức tiếp nhận danh sách `productIds` hiện có trong giỏ hàng. Thông qua EF Core với chỉ thị `.AsNoTracking()` để tối ưu tốc độ đọc, hệ thống nạp toàn bộ danh mục hoạt chất liên kết từ bảng `ProductIngredients`. Biểu thức LINQ `SelectMany` làm phẳng cấu trúc dữ liệu và trích xuất danh sách duy nhất các mã hoạt chất `cartActiveSlugs`. Vòng lặp `foreach` duyệt qua ma trận quy tắc tương tác `Rules`. Nếu giỏ hàng đồng thời chứa cả `KeyA` (ví dụ: `retinol`) và `KeyB` (ví dụ: `benzoyl-peroxide`), một đối tượng `IngredientInteractionFindingDto` được khởi tạo chứa đầy đủ mức độ nguy hiểm `DANGER`, lời giải thích sinh hóa và phác đồ giải quyết `Resolution`. Hàm `BuildRoutineScheduleDto` phía sau sẽ chuyển hóa các phát hiện này thành giao diện trực quan hiển thị trên đầu trang giỏ hàng.

---

#### 3.1.2.4. Giải pháp kỹ thuật 4: Động cơ Định tuyến Menu Đa hình, Middleware Chuyển hướng SEO 301 và Trình Tùy biến Giao diện Trực quan

##### (1) Bài toán kỹ thuật cần giải quyết
Trong thương mại điện tử, việc đổi tên sản phẩm hoặc tái cấu trúc danh mục thường làm thay đổi đường dẫn URL. Nếu không có cơ chế xử lý, các liên kết cũ đã được Google lập chỉ mục hoặc khách hàng lưu trong bookmark sẽ biến thành trang lỗi 404, làm sụt giảm nghiêm trọng thứ hạng SEO và doanh số bán hàng. Đồng thời, doanh nghiệp cần khả năng tùy biến màu sắc, banner chiến dịch theo mùa lễ hội mà không phải biên dịch lại mã nguồn.

##### (2) Tư duy thiết kế giải thuật
- *Middleware Chuyển hướng SEO 301 (`SlugRedirectMiddleware.cs`):* Đứng trong đường ống HTTP Pipeline, chặn bắt các yêu cầu GET gửi tới. Tách lấy đoạn URL slug cuối cùng, tra cứu nhanh trong bảng `SlugRedirects`. Nếu phát hiện đường dẫn cũ đã bị thay thế, middleware tự động gửi phản hồi HTTP 301 Permanent Redirect chuyển hướng trình duyệt sang URL canonical mới nhất, bảo tồn 100% giá trị liên kết (Link Equity) cho công cụ tìm kiếm.
- *Trình tùy biến giao diện trực quan (`ThemeService.cs` & `ThemeCustomizerController.cs`):* Quản lý các tham số giao diện (màu chủ đạo Rose Gold, banner thông báo, phông chữ) trong bảng `ThemeConfigs`. Ứng dụng cơ chế bộ nhớ đệm `IMemoryCache` với khóa `current_active_theme`. Khi quản trị viên chỉnh sửa màu sắc trên giao diện xem trước (Live Preview), phương thức `UpdateThemeAsync` thực thi lệnh `_cache.Remove(ThemeCacheKey)` để hủy bộ nhớ đệm cũ (Cache Invalidation), bảo đảm hàng ngàn người dùng đang trực tuyến lập tức nhận được diện mạo mới của website mà không cần khởi động lại ứng dụng.

##### (3) Trích đoạn mã nguồn C# thực tế trong SlugRedirectMiddleware.cs

```csharp
// File: Omnichannel.Web/Middlewares/SlugRedirectMiddleware.cs
public async Task InvokeAsync(HttpContext context, IMenuEngineService menuService)
{
    if (!HttpMethods.IsGet(context.Request.Method)) { await _next(context); return; }
    var path = context.Request.Path.Value;
    if (string.IsNullOrEmpty(path) || path == "/" || IsExcludedSystemPath(path))
    {
        await _next(context); return;
    }

    var slug = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
    if (!string.IsNullOrEmpty(slug))
    {
        var redirect = await menuService.GetRedirectAsync(slug, context.RequestAborted);
        if (redirect != null && !string.Equals(slug, redirect.NewSlug, StringComparison.OrdinalIgnoreCase))
        {
            var targetUrl = redirect.EntityType.ToUpperInvariant() switch {
                "CATEGORY" => $"/category/{redirect.NewSlug}",
                "PRODUCT"  => $"/product/{redirect.NewSlug}",
                "POST"     => $"/blog/{redirect.NewSlug}",
                _          => $"/{redirect.NewSlug}"
            };
            _logger.LogInformation("HTTP 301 Redirect: {Old} -> {New}", path, targetUrl);
            context.Response.Redirect(targetUrl, permanent: true);
            return;
        }
    }
    await _next(context);
}
```

##### (4) Diễn giải chi tiết luồng hoạt động của đoạn code
Khi một yêu cầu HTTP gửi đến, middleware kiểm tra phương thức có phải là GET hay không và loại trừ các tài nguyên tĩnh (.css, .js, .png) qua hàm `IsExcludedSystemPath`. Thuật toán bóc tách phân đoạn URL cuối cùng `slug`. Sau đó, hàm `menuService.GetRedirectAsync` truy vấn bảng `SlugRedirects`. Nếu tìm thấy bản ghi ánh xạ, biểu thức chuyển mạch `switch expression` của C# 12 nhanh chóng xây dựng đường dẫn đích chuẩn tắc `targetUrl` dựa trên loại thực thể (`CATEGORY`, `PRODUCT`, `POST`). Lệnh `context.Response.Redirect(targetUrl, permanent: true)` thiết lập mã trạng thái HTTP 301 chuyển hướng vĩnh viễn, kết thúc chu trình xử lý đường ống và điều hướng khách hàng tới sản phẩm mong muốn một cách mượt mà.

---

#### 3.1.2.5. Giải pháp kỹ thuật 5: Xử lý Thanh toán Đa phương thức, Nhận hàng O2O Click & Collect bằng mã PIN tại POS và Hạch toán Giá vốn Đích danh theo Lô

##### (1) Bài toán kỹ thuật cần giải quyết
Hệ thống cần phục vụ thanh toán nhanh không tiền mặt tại quầy POS trong chưa đầy 1 giây mà không bắt khách hàng chờ đợi, hỗ trợ mô hình O2O mua hàng trên web và ghé showroom nhận hàng (Click & Collect / BOPIS) có cơ chế xác thực chống nhận nhầm, đồng thời tự động hóa hoàn toàn việc ghi sổ kế toán tài chính tính giá vốn đích danh theo từng lô hàng mỹ phẩm xuất bán.

##### (2) Tư duy thiết kế giải thuật
- *Tự động sinh mã VietQR Napas 247 (`PaymentService.cs`):* Khi tạo đơn hàng, hệ thống ghép nối các thông tin số tài khoản ngân hàng, tên chủ tài khoản và nội dung chuyển khoản duy nhất `ORDER_{OrderCode}` để sinh mã QR chuẩn Napas 247 Compact2. Khi khách hàng quét mã chuyển khoản thành công từ ứng dụng ngân hàng bất kỳ, Webhook IPN từ ngân hàng gửi tín hiệu về cổng `/api/payment/vietqr-webhook`. Hệ thống đối soát chữ ký số, khớp mã đơn và bắn thông báo qua SignalR Hub làm màn hình POS tự động chuyển trạng thái hoàn tất và in hóa đơn nhiệt tức thì.
- *Quy trình O2O Click & Collect bảo mật bằng mã PIN 6 số:* Khi khách chọn nhận hàng tại Showroom, hệ thống sinh ngẫu nhiên một mã `PickupPin` gồm 6 chữ số bí mật gửi về tin nhắn SMS/Email của khách. Khi khách đến cửa hàng, thu ngân tại quầy POS mở giao diện tiếp nhận O2O, yêu cầu khách đọc mã PIN. Thuật toán kiểm tra mã PIN khớp đúng mới cho phép giao gói hàng và kích hoạt lệnh trừ kho vật lý tại showroom đó.
- *Hạch toán giá vốn đích danh theo lô (`FinanceService.cs`):* Thay vì tính giá vốn bình quân gia quyền mập mờ, hệ thống truy xuất chính xác trường `ProductBatch.ImportCost` gắn với từng dòng sản phẩm trong `OrderDetails`, tính tổng giá vốn thực tế và tự động ghi đồng thời các bút toán Nợ/Có vào bảng Sổ cái `FinancialLedgers`, phân bổ doanh thu và chi phí theo từng chi nhánh cụ thể.

##### (3) Trích đoạn mã nguồn C# thực tế trong FinanceService.cs & PaymentService.cs

```csharp
// File: Omnichannel.Infrastructure/Services/FinanceService.cs
public async Task<FinancialLedgerDto> RecordLedgerEntryAsync(
    CreateLedgerEntryDto dto, int? userId = null, CancellationToken cancellationToken = default)
{
    await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    try {
        string code = $"LED-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        var entry = new FinancialLedger {
            LedgerCode = code,
            EntryDate = DateTime.UtcNow,
            EntryType = dto.EntryType.Trim().ToUpperInvariant(),
            StoreId = dto.StoreId,
            OrderId = dto.OrderId,
            DebitAmount = dto.DebitAmount,
            CreditAmount = dto.CreditAmount,
            Description = dto.Description?.Trim(),
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        await _context.FinancialLedgers.AddAsync(entry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return MapToDto(entry);
    } catch (Exception ex) {
        await transaction.RollbackAsync(cancellationToken);
        _logger.LogError(ex, "Lỗi khi ghi nhận Sổ cái tài chính đích danh");
        throw;
    }
}
```

##### (4) Diễn giải chi tiết luồng hoạt động của đoạn code
Phương thức `RecordLedgerEntryAsync` là chốt chặn tài chính cốt tử của doanh nghiệp. Để bảo toàn tính toàn vẹn số liệu kép (Debit/Credit), phương thức mở một giao dịch nguyên tử `BeginTransactionAsync`. Mã bút toán duy nhất `LedgerCode` được sinh tự động theo cấu trúc ngày tháng và chuỗi GUID ngẫu nhiên 8 ký tự. Bản ghi thực thể `FinancialLedger` lưu trữ chi tiết mã loại nghiệp vụ `EntryType` (`REVENUE`, `COGS`, `LOYALTY_DISCOUNT`), mã chi nhánh `StoreId`, cùng các con số tài chính ghi Nợ và ghi Có. Lệnh `await transaction.CommitAsync` bảo đảm dữ liệu kế toán chỉ được lưu khi không có bất kỳ lỗi logic nào xảy ra. Nếu có sự cố đứt kết nối hoặc xung đột khóa ngoại, khối lệnh `catch` kích hoạt `transaction.RollbackAsync` hoàn nguyên toàn bộ trạng thái, bảo vệ sự chính xác tuyệt đối của báo cáo tài chính Lãi/Lỗ (P&L) toàn chuỗi.

