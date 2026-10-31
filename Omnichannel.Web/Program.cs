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


using Omnichannel.Infrastructure.Data.Interceptors;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình DbContext kết nối SQL Server (sa / 12345678) & Domain Events Interceptor
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' không được tìm thấy.");

builder.Services.AddSingleton<DomainEventDispatchInterceptor>();

builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<DomainEventDispatchInterceptor>();
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    })
    .AddInterceptors(interceptor);
});

// 2. Cấu hình Caching (MemoryCache L1 sẵn sàng + IDistributedCache L2)
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpContextAccessor();

// 3. Đăng ký Services Phân hệ Bảo mật
builder.Services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IStepUpElevationService, StepUpElevationService>();
// Đăng ký Service CMS & Menu Đa hình vào DI Container:
builder.Services.AddScoped<IMenuEngineService, MenuEngineService>();
builder.Services.AddScoped<IProductReviewService, ProductReviewService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IThemeService, ThemeService>();
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
// Đăng ký Service Quản lý Tài chính & Sổ cái:
builder.Services.AddScoped<IFinanceService, FinanceService>();
// Đăng ký Service AI Chatbot hỗ trợ khách hàng:
builder.Services.AddScoped<IChatbotService, ChatbotService>();
// Đăng ký Service AI Beauty Advisor - Smart Filter:
builder.Services.AddScoped<Omnichannel.Application.Interfaces.Catalog.ISmartFilterService, Omnichannel.Infrastructure.Services.Catalog.SmartFilterService>();
// Đăng ký Động cơ Phân tích Tương kỵ & Hiệp đồng Hoạt chất Da liễu AI:
builder.Services.AddScoped<Omnichannel.Application.Interfaces.BeautyTech.ISkincareIngredientInteractionEngine, Omnichannel.Infrastructure.Services.BeautyTech.SkincareIngredientInteractionEngine>();
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

// 4. Cấu hình Xác thực bằng Cookie (Cookie Authentication)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "OmnichannelBeauty.AuthCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// 5. Cấu hình Ủy quyền (Authorization)
builder.Services.AddAuthorization();

// 6. Đăng ký MVC Controllers & Views
var mvcBuilder = builder.Services.AddControllersWithViews();

if (builder.Environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}
builder.Services.AddSignalR();

// Tối ưu hiệu năng: Nén Response (Gzip & Brotli)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

var app = builder.Build();

// 7. Khởi tạo Database và Seed Data tự động khi bắt đầu
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await DbInitializer.SeedAsync(context);
        await DbInitializer.MigrateStaticImagesToDatabaseAsync(context, app.Environment);
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
}
else
{
    app.UseDeveloperExceptionPage();
}

// Xử lý mã trạng thái HTTP (404 Not Found, 403 Forbidden, 500)
app.UseStatusCodePagesWithReExecute("/Home/ErrorStatusCode", "?statusCode={0}");

// Kích hoạt nén dữ liệu Response
app.UseResponseCompression();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Middleware xử lý SEO 301 Redirect cho các URL/Slug cũ
app.UseMiddleware<SlugRedirectMiddleware>();

app.UseSession();

// Thứ tự bắt buộc: UseAuthentication trước UseAuthorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<Omnichannel.Web.Hubs.SystemHub>("/systemHub");
app.MapHub<Omnichannel.Web.Hubs.NotificationHub>("/notificationHub");

app.Run();