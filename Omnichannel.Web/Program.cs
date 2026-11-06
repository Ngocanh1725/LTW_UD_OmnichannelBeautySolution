using Omnichannel.Web.Filters;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Omnichannel.Application.Interfaces.Cms;
using Omnichannel.Application.Interfaces.Notifications;
using Omnichannel.Infrastructure.Services.Notifications;
using Omnichannel.Application.Interfaces.Crm;
using Omnichannel.Application.Interfaces.Loyalty;
using Omnichannel.Application.Interfaces.Inventory;
using Omnichannel.Application.DTOs.Inventory;

using Omnichannel.Application.Interfaces.Finance;
using Omnichannel.Application.Interfaces.Reports;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Application.Interfaces.Security;
using Omnichannel.Application.Interfaces.Settings;
using Omnichannel.Application.Interfaces.Chat;
using Omnichannel.Application.Interfaces.Media;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Infrastructure.Services;
using Omnichannel.Web.Middlewares;
using System;


using Omnichannel.Application.Common;
using Omnichannel.Infrastructure.Data.Interceptors;

var builder = WebApplication.CreateBuilder(args);

// 0. Cấu hình Thương hiệu tập trung (Single Source of Truth cho CANMAKE BEAUTY)
builder.Services.Configure<BrandOptions>(builder.Configuration.GetSection(BrandOptions.SectionName));
builder.Services.AddSingleton(sp =>
{
    var opts = new BrandOptions();
    builder.Configuration.GetSection(BrandOptions.SectionName).Bind(opts);
    return opts;
});

// 1. Cấu hình DbContext kết nối SQL Server (sa / 12345678) & Domain Events Interceptor
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' không được tìm thấy.");

builder.Services.AddSingleton<DomainEventDispatchInterceptor>();
builder.Services.AddScoped<AuditSaveChangesInterceptor>();
builder.Services.AddScoped<BlockAdminShoppingAttribute>();

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<DomainEventDispatchInterceptor>();
    var auditInterceptor = sp.GetRequiredService<AuditSaveChangesInterceptor>();
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
    })
    .AddInterceptors(interceptor, auditInterceptor);
});

// 2. Cấu hình Caching (MemoryCache L1 sẵn sàng + IDistributedCache L2)
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpContextAccessor();

// 3. Đăng ký Services Phân hệ Bảo mật & Ủy quyền Phân cấp (Delegated RBAC)
builder.Services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IDelegationService, DelegationService>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IStepUpElevationService, StepUpElevationService>();
builder.Services.AddScoped<AdminAreaBoundaryFilter>();
builder.Services.AddScoped<Omnichannel.Application.Interfaces.HR.IStaffManagementService, Omnichannel.Infrastructure.Services.HR.StaffManagementService>();
// Đăng ký Service CMS & Menu Đa hình vào DI Container:
builder.Services.AddScoped<IMenuEngineService, MenuEngineService>();
builder.Services.AddScoped<IProductReviewService, ProductReviewService>();
builder.Services.AddScoped<IProductQnaService, ProductQnaService>();
builder.Services.AddScoped<IInventoryNotificationService, InventoryNotificationService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<Omnichannel.Application.Interfaces.Cms.ILayoutEngineService, Omnichannel.Infrastructure.Services.Cms.LayoutEngineService>();
builder.Services.AddScoped<Omnichannel.Infrastructure.Services.Cms.MediaStorageService>();
// Đăng ký Service Động cơ Phân bổ Kho FEFO:
builder.Services.AddScoped<IInventoryAllocationEngine, InventoryAllocationEngine>();
//  Đăng ký Session & Caching
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.IsEssential = true;
});

// Đăng ký Services Bán lẻ & Đơn hàng
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderWorkflowService, OrderWorkflowService>();
builder.Services.AddScoped<Omnichannel.Application.Interfaces.Sales.IOrderManagementService, Omnichannel.Infrastructure.Services.Sales.OrderManagementService>();
builder.Services.AddHttpClient<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPaymentGatewayService>(sp => (IPaymentGatewayService)sp.GetRequiredService<IPaymentService>());
builder.Services.AddHttpClient<IShippingService, GhnShippingService>();
builder.Services.AddScoped<IIdempotencyService, IdempotencyService>();
// Đăng ký Services POS & Báo Cáo Tài Chính:
builder.Services.AddScoped<IPosService, PosService>();
builder.Services.AddScoped<IPosShiftService, PosShiftService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<Omnichannel.Application.Interfaces.Catalog.IProductService, Omnichannel.Infrastructure.Services.Catalog.ProductService>();
builder.Services.AddSingleton<Omnichannel.Application.Interfaces.Catalog.IProductImageResolver, Omnichannel.Infrastructure.Services.Catalog.ProductImageResolver>();
// Đăng ký Service CRM & Hồ Sơ Sắc Đẹp:
builder.Services.AddScoped<ICustomerProfileService, CustomerProfileService>();
builder.Services.AddScoped<Omnichannel.Application.Interfaces.Crm.ICustomerCrmService, Omnichannel.Infrastructure.Services.Crm.CustomerCrmService>();
// Đăng ký Service Ví Điểm & Thăng Hạng Thành Viên:
builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();
// Đăng ký Động cơ Khuyến mãi & Coupon:
builder.Services.AddScoped<IPromotionEngine, PromotionEngine>();
// Đăng ký Service Điều chuyển kho liên chi nhánh:
builder.Services.AddScoped<IStockTransferService, StockTransferService>();
// Đăng ký Service Cài đặt động toàn hệ thống:
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<Omnichannel.Application.Interfaces.Settings.IDemoResetService, Omnichannel.Infrastructure.Services.DemoResetService>();
// Đăng ký Service Quản lý Tài chính & Sổ cái:
builder.Services.AddScoped<IFinanceService, FinanceService>();
// Đăng ký Service AI Chatbot hỗ trợ khách hàng:
builder.Services.AddScoped<IChatbotService, ChatbotService>();
// Đăng ký Service AI Beauty Advisor - Smart Filter:
builder.Services.AddScoped<Omnichannel.Application.Interfaces.Catalog.ISmartFilterService, Omnichannel.Infrastructure.Services.Catalog.SmartFilterService>();
// Đăng ký Service Tìm kiếm gợi ý tức thì chuẩn Sephora/Hasaki (Phiên C1):
builder.Services.AddScoped<Omnichannel.Application.Interfaces.Catalog.ISearchSuggestService, Omnichannel.Infrastructure.Services.Catalog.SearchSuggestService>();
// Đăng ký Service Gợi ý FBT & Đã xem gần đây chuẩn Sephora/Olive Young (Phiên C2):
builder.Services.AddScoped<Omnichannel.Application.Interfaces.Catalog.IRecommendationService, Omnichannel.Infrastructure.Services.Catalog.RecommendationService>();
// Đăng ký Service Khay So Sánh Mỹ Phẩm Chuyên Sâu Side-by-Side (Phiên C3 - GAP-03):
builder.Services.AddScoped<Omnichannel.Application.Interfaces.Catalog.IProductComparisonService, Omnichannel.Infrastructure.Services.Catalog.ProductComparisonService>();
// Đăng ký Động cơ Phân tích Tương kỵ & Hiệp đồng Hoạt chất Da liễu AI:
builder.Services.AddScoped<Omnichannel.Application.Interfaces.BeautyTech.ISkincareIngredientInteractionEngine, Omnichannel.Infrastructure.Services.BeautyTech.SkincareIngredientInteractionEngine>();
// Đăng ký Service Chuyên gia Da liễu AI & Routine Quiz:
builder.Services.AddScoped<Omnichannel.Application.Interfaces.Beauty.ISkincareConsultantService, Omnichannel.Infrastructure.Services.Beauty.SkincareConsultantService>();
// Đăng ký Service Phân tích Da bằng Ảnh AI Vision + Deterministic Fallback (Phiên C8):
builder.Services.AddHttpClient("GeminiVisionClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddScoped<Omnichannel.Infrastructure.Services.Beauty.MockSkinAnalysisService>();
builder.Services.AddScoped<Omnichannel.Application.Interfaces.Beauty.ISkinAnalysisService, Omnichannel.Infrastructure.Services.Beauty.AiVisionSkinAnalysisService>();
// Đăng ký Service Tồn kho Đa Kênh Showroom (O2O) & Flash Sale:
builder.Services.AddScoped<Omnichannel.Application.Interfaces.Inventory.IStoreInventoryService, Omnichannel.Infrastructure.Services.Inventory.StoreInventoryService>();
// Đăng ký Service AI Cảnh Báo Tự Động & Background Worker:
builder.Services.AddScoped<Omnichannel.Application.Interfaces.Alerts.IAlertService, Omnichannel.Infrastructure.Services.AlertService>();
builder.Services.AddHostedService<Omnichannel.Web.BackgroundServices.AlertBackgroundWorker>();
// Đăng ký Service Thông Báo Realtime & SignalR Dispatcher:
builder.Services.AddScoped<Omnichannel.Application.Interfaces.Notifications.INotificationDispatcher, Omnichannel.Web.Services.SignalRNotificationDispatcher>();
builder.Services.AddScoped<Omnichannel.Application.Interfaces.Notifications.INotificationService, Omnichannel.Infrastructure.Services.NotificationService>();
// Đăng ký Service Hộp Thư Nội Bộ:
builder.Services.AddScoped<Omnichannel.Application.Interfaces.Messages.IInternalMessageService, Omnichannel.Infrastructure.Services.InternalMessageService>();

// Cấu hình AutoMapper
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// 4. Cấu hình Xác thực Multi-Scheme Cookie Authentication Độc Lập
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = AuthConstants.DynamicScheme;
    options.DefaultAuthenticateScheme = AuthConstants.DynamicScheme;
    options.DefaultChallengeScheme = AuthConstants.DynamicScheme;
})
.AddPolicyScheme(AuthConstants.DynamicScheme, "Dynamic Admin/Customer Scheme Forwarder", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase))
        {
            return AuthConstants.AdminCookieScheme;
        }
        return AuthConstants.CustomerCookieScheme;
    };
})
.AddCookie(AuthConstants.CustomerCookieScheme, options =>
{
    options.Cookie.Name = AuthConstants.CustomerCookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = AuthConstants.CustomerLoginPath;
    options.LogoutPath = AuthConstants.CustomerLogoutPath;
    options.AccessDeniedPath = AuthConstants.CustomerAccessDeniedPath;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
})
.AddCookie(AuthConstants.AdminCookieScheme, options =>
{
    options.Cookie.Name = AuthConstants.AdminCookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = AuthConstants.AdminLoginPath;
    options.LogoutPath = AuthConstants.AdminLogoutPath;
    options.AccessDeniedPath = AuthConstants.AdminAccessDeniedPath;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);

    // Kiểm tra SecurityStamp, trạng thái IsActive/IsDeleted và tách bạch UserType >= 2 ở mọi request
    options.Events = new CookieAuthenticationEvents
    {
        OnValidatePrincipal = async context =>
        {
            var principal = context.Principal;
            if (principal?.Identity == null || !principal.Identity.IsAuthenticated)
            {
                return;
            }

            var userIdStr = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            {
                context.RejectPrincipal();
                return;
            }

            var db = context.HttpContext.RequestServices.GetService<ApplicationDbContext>();
            if (db == null)
            {
                return;
            }

            var dbUser = await db.Users
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            // Chặn ngay lập tức nếu tài khoản không tồn tại, bị khóa, xóa mềm hoặc là Khách hàng (UserType = 1)
            if (dbUser == null || !dbUser.IsActive || dbUser.IsDeleted || dbUser.UserType < 2)
            {
                context.RejectPrincipal();
                return;
            }

            var cookieStamp = principal.FindFirst("SecurityStamp")?.Value;
            var currentStamp = dbUser.SecurityStamp ?? string.Empty;

            if (!string.Equals(cookieStamp, currentStamp, StringComparison.Ordinal))
            {
                var refreshedClaims = new System.Collections.Generic.List<System.Security.Claims.Claim>
                {
                    new(System.Security.Claims.ClaimTypes.NameIdentifier, dbUser.Id.ToString()),
                    new(System.Security.Claims.ClaimTypes.Name, dbUser.Username),
                    new(System.Security.Claims.ClaimTypes.GivenName, dbUser.FullName ?? dbUser.Username),
                    new(System.Security.Claims.ClaimTypes.Email, dbUser.Email ?? string.Empty),
                    new("UserType", dbUser.UserType.ToString()),
                    new("IsStaff", "true"),
                    new("SecurityStamp", currentStamp)
                };

                if (dbUser.StoreId.HasValue)
                {
                    refreshedClaims.Add(new("StoreId", dbUser.StoreId.Value.ToString()));
                }

                if (dbUser.DepartmentId.HasValue)
                {
                    refreshedClaims.Add(new("DepartmentId", dbUser.DepartmentId.Value.ToString()));
                }

                foreach (var ur in dbUser.UserRoles)
                {
                    if (ur.Role != null && ur.Role.IsActive && !string.Equals(ur.Role.Name, "Customer", StringComparison.OrdinalIgnoreCase))
                    {
                        refreshedClaims.Add(new(System.Security.Claims.ClaimTypes.Role, ur.Role.Name));
                    }
                }

                var refreshedIdentity = new System.Security.Claims.ClaimsIdentity(refreshedClaims, AuthConstants.AdminCookieScheme);
                context.ReplacePrincipal(new System.Security.Claims.ClaimsPrincipal(refreshedIdentity));
                context.ShouldRenew = true;
            }
        },
        OnRedirectToAccessDenied = async context =>
        {
            var req = context.Request;
            bool isAjaxOrJson =
                string.Equals(req.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase) ||
                (req.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase) &&
                 !req.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase)) ||
                (req.ContentType != null && req.ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase));

            if (isAjaxOrJson)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Từ chối truy cập (HTTP 403): Tài khoản của bạn không có quyền thực hiện thao tác này.",
                    errorCode = "FORBIDDEN_403"
                });
                return;
            }

            context.Response.Redirect(context.RedirectUri);
        }
    };
});

// 5. Cấu hình Ủy quyền (Authorization)
builder.Services.AddAuthorization();

// 6. Đăng ký MVC Controllers & Views kèm Guard tách bạch Admin/Customer
var mvcBuilder = builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<AdminAreaBoundaryFilter>();
});

if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}
builder.Services.AddSignalR();

// Tối ưu hiệu năng: Nén Response (Gzip & Brotli với CompressionLevel.Fastest)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

var app = builder.Build();

// 7. Khởi tạo Database và Seed Data tự động khi bắt đầu
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var dbInitLogger = services.GetService<ILogger<ApplicationDbContext>>();
        await DbInitializer.SeedAsync(context, dbInitLogger);
        await DbInitializer.MigrateStaticImagesToDatabaseAsync(context, app.Environment, dbInitLogger);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi xảy ra trong quá trình khởi tạo/Seed Database.");
    }
}

// 8. Cấu hình Request Pipeline
// Global Exception Logging & API Handler Middleware
app.UseMiddleware<Omnichannel.Web.Middlewares.GlobalExceptionMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error500");
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    app.UseDeveloperExceptionPage();
}

// Xử lý mã trạng thái HTTP (404 Not Found, 403 Forbidden, 500)
app.UseStatusCodePagesWithReExecute("/Home/ErrorStatusCode", "?statusCode={0}");

// Kích hoạt nén dữ liệu Response
app.UseResponseCompression();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Cache các file tĩnh (CSS, JS, hình ảnh, webp, svg, woff2) trong 7 ngày (604800 giây)
        ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=604800");
    }
});
app.UseRouting();

// Middleware xử lý SEO 301 Redirect cho các URL/Slug cũ
app.UseMiddleware<SlugRedirectMiddleware>();

app.UseSession();

// Thứ tự bắt buộc: UseAuthentication trước UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

// Alias Route cho Quản lý Kho FEFO: /Admin/Inventory -> WarehouseController
app.MapAreaControllerRoute(
    name: "admin_inventory_alias",
    areaName: "Admin",
    pattern: "Admin/Inventory/{action=Index}/{id?}",
    defaults: new { controller = "Warehouse" });

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<Omnichannel.Web.Hubs.SystemHub>("/systemHub");
app.MapHub<Omnichannel.Web.Hubs.NotificationHub>("/notificationHub");

app.Run();