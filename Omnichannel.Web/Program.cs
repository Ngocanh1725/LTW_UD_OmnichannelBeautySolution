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
using Omnichannel.Application.Interfaces.Inventory;
using Omnichannel.Application.Interfaces.Reports;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Application.Interfaces.Security;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;
using Omnichannel.Infrastructure.Services;
using System;


var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình DbContext kết nối SQL Server (sa / 12345678)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' không được tìm thấy.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    }));

// 2. Cấu hình Caching (MemoryCache L1 sẵn sàng + IDistributedCache L2)
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();

// 3. Đăng ký Services Phân hệ Bảo mật
builder.Services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
// Đăng ký Service CMS & Menu Đa hình vào DI Container:
builder.Services.AddScoped<IMenuEngineService, MenuEngineService>();
// Đăng ký Service Động cơ Phân bổ Kho FEFO:
builder.Services.AddScoped<IInventoryAllocationEngine, InventoryAllocationEngine>();
//  Đăng ký Session & Caching
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Đăng ký Services Bán lẻ & Đơn hàng
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderWorkflowService, OrderWorkflowService>();
// Đăng ký Services POS & Báo Cáo Tài Chính:
builder.Services.AddScoped<IPosService, PosService>();
builder.Services.AddScoped<IReportService, ReportService>();

// 4. Cấu hình Xác thực bằng Cookie (Cookie Authentication)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "OmnichannelBeauty.AuthCookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

// 5. Cấu hình Ủy quyền (Authorization)
builder.Services.AddAuthorization();

// 6. Đăng ký MVC Controllers & Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 7. Khởi tạo Database và Seed Data tự động khi bắt đầu
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await DbInitializer.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi xảy ra trong quá trình khởi tạo/Seed Database.");
    }
}

// 8. Cấu hình Request Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
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

app.Run();