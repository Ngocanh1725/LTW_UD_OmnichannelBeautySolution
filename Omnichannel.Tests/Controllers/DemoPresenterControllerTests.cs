using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Omnichannel.Application.Common;
using Omnichannel.Application.DTOs.Sales;
using Omnichannel.Application.DTOs.Settings;
using Omnichannel.Application.Interfaces.Sales;
using Omnichannel.Application.Interfaces.Security;
using Omnichannel.Application.Interfaces.Settings;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Web.Controllers;
using Xunit;

namespace Omnichannel.Tests.Controllers
{
    public class DemoPresenterControllerTests
    {
        private ApplicationDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"OmniDb_Demo_{Guid.NewGuid()}")
                .Options;

            return new ApplicationDbContext(options);
        }

        private void SeedUsersAndProducts(ApplicationDbContext context)
        {
            var customer = new User
            {
                Id = 1,
                Username = "customer_vip",
                FullName = "Lê Ngọc Linh (VIP)",
                Email = "customer.vip@gmail.com",
                UserType = 1,
                IsActive = true
            };

            var cashier = new User
            {
                Id = 2,
                Username = "cashier01",
                FullName = "Thu Ngân Showroom 01",
                Email = "cashier01@omnichannelbeauty.vn",
                UserType = 3,
                StoreId = 1,
                IsActive = true
            };

            var admin = new User
            {
                Id = 3,
                Username = "superadmin",
                FullName = "Nguyễn Quản Trị (SuperAdmin)",
                Email = "superadmin@omnichannelbeauty.vn",
                UserType = 5,
                IsActive = true
            };

            context.Users.AddRange(customer, cashier, admin);

            var p1 = new Product { Id = 101, Name = "Sữa Rửa Mặt Dịu Lành Cetaphil Gentle Cleanser", Sku = "CET-01", SellingPrice = 250000m, IsActive = true };
            var p2 = new Product { Id = 102, Name = "Dung Dịch Tẩy Tế Bào Chết BHA 2% Paula's Choice", Sku = "BHA-02", SellingPrice = 900000m, FullIngredients = "Water, Salicylic Acid (BHA 2%), Methylpropanediol", IsActive = true };
            var p3 = new Product { Id = 103, Name = "Kem Dưỡng Phục Hồi La Roche-Posay Cicaplast Baume B5", Sku = "B5-03", SellingPrice = 450000m, FullIngredients = "Water, Panthenol (Vitamin B5), Madecassoside", IsActive = true };
            var p4 = new Product { Id = 104, Name = "Tinh Chất Trẻ Hóa Da Retinol 1% Clinical Paula's Choice", Sku = "RET-04", SellingPrice = 1200000m, FullIngredients = "Water, Retinol 1%, Dimethicone", IsActive = true };

            context.Products.AddRange(p1, p2, p3, p4);
            context.SaveChanges();
        }

        private IConfiguration CreateConfig(bool isDemoMode)
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "DemoMode:Enabled", isDemoMode.ToString().ToLower() }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public async Task DemoModeDisabled_AllEndpointsReturn404NotFound()
        {
            // Arrange
            var context = CreateInMemoryDbContext();
            var config = CreateConfig(isDemoMode: false);
            var envMock = new Mock<IWebHostEnvironment>();
            var cartMock = new Mock<ICartService>();
            var resetMock = new Mock<IDemoResetService>();
            var loggerMock = new Mock<ILogger<DemoPresenterController>>();

            var controller = new DemoPresenterController(config, envMock.Object, context, cartMock.Object, resetMock.Object, loggerMock.Object);

            // Act & Assert
            var statusRes = controller.GetStatus() as NotFoundObjectResult;
            Assert.NotNull(statusRes);
            Assert.Equal(404, statusRes.StatusCode);

            var switchRes = await controller.QuickSwitchPersona(new QuickSwitchPersonaRequestDto { Persona = "customer" }) as NotFoundObjectResult;
            Assert.NotNull(switchRes);
            Assert.Equal(404, switchRes.StatusCode);

            var seedRoutineRes = await controller.SeedCartRoutine(CancellationToken.None) as NotFoundObjectResult;
            Assert.NotNull(seedRoutineRes);
            Assert.Equal(404, seedRoutineRes.StatusCode);

            var seedConflictRes = await controller.SeedCartConflict(CancellationToken.None) as NotFoundObjectResult;
            Assert.NotNull(seedConflictRes);
            Assert.Equal(404, seedConflictRes.StatusCode);

            var resetRes = await controller.ResetData(CancellationToken.None) as NotFoundObjectResult;
            Assert.NotNull(resetRes);
            Assert.Equal(404, resetRes.StatusCode);
        }

        [Fact]
        public void DemoModeEnabled_GetStatus_Returns200WithDemoInfo()
        {
            // Arrange
            var context = CreateInMemoryDbContext();
            var config = CreateConfig(isDemoMode: true);
            var envMock = new Mock<IWebHostEnvironment>();
            envMock.Setup(e => e.EnvironmentName).Returns("Development");
            var cartMock = new Mock<ICartService>();
            var resetMock = new Mock<IDemoResetService>();
            var loggerMock = new Mock<ILogger<DemoPresenterController>>();

            var controller = new DemoPresenterController(config, envMock.Object, context, cartMock.Object, resetMock.Object, loggerMock.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var res = controller.GetStatus() as OkObjectResult;

            // Assert
            Assert.NotNull(res);
            Assert.Equal(200, res.StatusCode);
            dynamic data = res.Value!;
            Assert.True((bool)data.GetType().GetProperty("demoMode")!.GetValue(data)!);
            Assert.Equal("Development", (string)data.GetType().GetProperty("environment")!.GetValue(data)!);
        }

        [Fact]
        public async Task DemoModeEnabled_QuickSwitchPersona_Customer_SignsInSuccessfully()
        {
            // Arrange
            var context = CreateInMemoryDbContext();
            SeedUsersAndProducts(context);
            var config = CreateConfig(isDemoMode: true);
            var envMock = new Mock<IWebHostEnvironment>();
            var cartMock = new Mock<ICartService>();
            var resetMock = new Mock<IDemoResetService>();
            var loggerMock = new Mock<ILogger<DemoPresenterController>>();
            var auditMock = new Mock<IAuditLogService>();

            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock.Setup(a => a.SignInAsync(
                It.IsAny<HttpContext>(),
                AuthConstants.CustomerCookieScheme,
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            var httpContext = new DefaultHttpContext { RequestServices = serviceProviderMock.Object };

            var controller = new DemoPresenterController(config, envMock.Object, context, cartMock.Object, resetMock.Object, loggerMock.Object, auditMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var res = await controller.QuickSwitchPersona(new QuickSwitchPersonaRequestDto { Persona = "customer" }) as OkObjectResult;

            // Assert
            Assert.NotNull(res);
            Assert.Equal(200, res.StatusCode);
            dynamic data = res.Value!;
            Assert.True((bool)data.GetType().GetProperty("success")!.GetValue(data)!);
            Assert.Equal("customer_vip", (string)data.GetType().GetProperty("username")!.GetValue(data)!);
            Assert.Equal("/", (string)data.GetType().GetProperty("redirectUrl")!.GetValue(data)!);

            // Verify Audit Log was recorded
            auditMock.Verify(a => a.LogAsync(
                "DEMO_QUICK_SWITCH_PERSONA",
                "User",
                "1",
                null,
                It.Is<string>(s => s.Contains("customer_vip")),
                null,
                "DemoPresenterHUD",
                null,
                null,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DemoModeEnabled_QuickSwitchPersona_Cashier_SignsInWithStoreIsolation()
        {
            // Arrange
            var context = CreateInMemoryDbContext();
            SeedUsersAndProducts(context);
            var config = CreateConfig(isDemoMode: true);
            var envMock = new Mock<IWebHostEnvironment>();
            var cartMock = new Mock<ICartService>();
            var resetMock = new Mock<IDemoResetService>();
            var loggerMock = new Mock<ILogger<DemoPresenterController>>();

            ClaimsPrincipal? signedInPrincipal = null;
            var authServiceMock = new Mock<IAuthenticationService>();
            authServiceMock.Setup(a => a.SignInAsync(
                It.IsAny<HttpContext>(),
                AuthConstants.AdminCookieScheme,
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
                .Callback<HttpContext, string, ClaimsPrincipal, AuthenticationProperties>((ctx, scheme, principal, props) =>
                {
                    signedInPrincipal = principal;
                })
                .Returns(Task.CompletedTask);

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock.Setup(s => s.GetService(typeof(IAuthenticationService)))
                .Returns(authServiceMock.Object);

            var httpContext = new DefaultHttpContext { RequestServices = serviceProviderMock.Object };

            var controller = new DemoPresenterController(config, envMock.Object, context, cartMock.Object, resetMock.Object, loggerMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var res = await controller.QuickSwitchPersona(new QuickSwitchPersonaRequestDto { Persona = "cashier" }) as OkObjectResult;

            // Assert
            Assert.NotNull(res);
            Assert.Equal(200, res.StatusCode);
            dynamic data = res.Value!;
            Assert.True((bool)data.GetType().GetProperty("success")!.GetValue(data)!);
            Assert.Equal("cashier01", (string)data.GetType().GetProperty("username")!.GetValue(data)!);
            Assert.Equal("/Admin/Pos", (string)data.GetType().GetProperty("redirectUrl")!.GetValue(data)!);

            // Assert Claims: StoreId = 1, IsStaff = true
            Assert.NotNull(signedInPrincipal);
            Assert.Equal("1", signedInPrincipal.FindFirst("StoreId")?.Value);
            Assert.Equal("true", signedInPrincipal.FindFirst("IsStaff")?.Value);
        }

        [Fact]
        public async Task DemoModeEnabled_SeedCartConflict_AddsRetinolAndBhaProducts()
        {
            // Arrange
            var context = CreateInMemoryDbContext();
            SeedUsersAndProducts(context);
            var config = CreateConfig(isDemoMode: true);
            var envMock = new Mock<IWebHostEnvironment>();
            var resetMock = new Mock<IDemoResetService>();
            var loggerMock = new Mock<ILogger<DemoPresenterController>>();

            var addedProductIds = new List<int>();
            var cartMock = new Mock<ICartService>();
            cartMock.Setup(c => c.AddToCartAsync(
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<int>(),
                It.IsAny<int?>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
                .Callback<string?, string?, int, int?, int, CancellationToken>((sid, uid, pid, bid, qty, ct) =>
                {
                    addedProductIds.Add(pid);
                })
                .ReturnsAsync(new CartDto { Id = 1 });

            var controller = new DemoPresenterController(config, envMock.Object, context, cartMock.Object, resetMock.Object, loggerMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // Act
            var res = await controller.SeedCartConflict(CancellationToken.None) as OkObjectResult;

            // Assert
            Assert.NotNull(res);
            Assert.Equal(200, res.StatusCode);
            dynamic data = res.Value!;
            Assert.True((bool)data.GetType().GetProperty("success")!.GetValue(data)!);
            Assert.Equal(2, (int)data.GetType().GetProperty("count")!.GetValue(data)!);
            Assert.Equal("/cart", (string)data.GetType().GetProperty("cartUrl")!.GetValue(data)!);

            // Assert added products: BHA (102) and Retinol (104)
            Assert.Contains(102, addedProductIds);
            Assert.Contains(104, addedProductIds);
        }

        [Fact]
        public async Task DemoModeEnabled_ResetData_CallsDemoResetService()
        {
            // Arrange
            var context = CreateInMemoryDbContext();
            var config = CreateConfig(isDemoMode: true);
            var envMock = new Mock<IWebHostEnvironment>();
            var cartMock = new Mock<ICartService>();
            var resetMock = new Mock<IDemoResetService>();
            var loggerMock = new Mock<ILogger<DemoPresenterController>>();

            resetMock.Setup(r => r.ResetDemoDataAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DemoResetResultDto
                {
                    Success = true,
                    Message = "Khôi phục dữ liệu Demo thành công!"
                });

            var controller = new DemoPresenterController(config, envMock.Object, context, cartMock.Object, resetMock.Object, loggerMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // Act
            var res = await controller.ResetData(CancellationToken.None) as OkObjectResult;

            // Assert
            Assert.NotNull(res);
            Assert.Equal(200, res.StatusCode);
            dynamic data = res.Value!;
            Assert.True((bool)data.GetType().GetProperty("success")!.GetValue(data)!);
            Assert.Contains("Khôi phục", (string)data.GetType().GetProperty("message")!.GetValue(data)!);
            resetMock.Verify(r => r.ResetDemoDataAsync(It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
