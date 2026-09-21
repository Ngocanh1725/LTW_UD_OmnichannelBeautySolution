using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // Đảm bảo Database đã được migrate trước khi seed
            await context.Database.MigrateAsync();

            // Nếu đã có dữ liệu sản phẩm, bỏ qua seeding để bảo toàn dữ liệu
            if (await context.Products.AnyAsync())
            {
                return;
            }

            // =========================================================================
            // 1. SEED PHÂN HỆ ĐỊNH DANH & BẢO MẬT (ROLES & PERMISSIONS)
            // =========================================================================
            var roleSuperAdmin = new Role { Name = "SuperAdmin", Description = "Quản trị viên toàn quyền hệ thống" };
            var roleBranchManager = new Role { Name = "BranchManager", Description = "Quản lý chi nhánh & kho hàng" };
            var roleCashier = new Role { Name = "Cashier", Description = "Thu ngân điểm bán lẻ POS" };
            var roleCustomer = new Role { Name = "Customer", Description = "Khách hàng mua lẻ đa kênh" };

            await context.Roles.AddRangeAsync(roleSuperAdmin, roleBranchManager, roleCashier, roleCustomer);
            await context.SaveChangesAsync();

            var permissions = new List<Permission>
            {
                new() { Code = "DASHBOARD_VIEW", Name = "Xem trang tổng quan", Module = "DASHBOARD" },
                new() { Code = "PRODUCT_VIEW", Name = "Xem danh sách sản phẩm", Module = "PRODUCT" },
                new() { Code = "PRODUCT_CREATE", Name = "Tạo mới sản phẩm", Module = "PRODUCT" },
                new() { Code = "PRODUCT_EDIT", Name = "Cập nhật sản phẩm", Module = "PRODUCT" },
                new() { Code = "PRODUCT_DELETE", Name = "Xóa sản phẩm", Module = "PRODUCT" },
                new() { Code = "INVENTORY_VIEW", Name = "Xem tồn kho FEFO", Module = "INVENTORY" },
                new() { Code = "INVENTORY_IMPORT", Name = "Nhập kho theo lô", Module = "INVENTORY" },
                new() { Code = "INVENTORY_ADJUST", Name = "Điều chỉnh kiểm kê kho", Module = "INVENTORY" },
                new() { Code = "POS_ACCESS", Name = "Truy cập giao diện POS", Module = "POS" },
                new() { Code = "POS_CHECKOUT", Name = "Thực hiện thanh toán POS", Module = "POS" },
                new() { Code = "ORDER_VIEW", Name = "Xem đơn hàng đa kênh", Module = "ORDER" },
                new() { Code = "ORDER_PROCESS", Name = "Xử lý & giao vận đơn", Module = "ORDER" },
                new() { Code = "USER_MANAGE", Name = "Quản lý người dùng & phân quyền", Module = "SECURITY" },
                new() { Code = "CMS_MANAGE", Name = "Quản lý Banner & Menu", Module = "CMS" },
                new() { Code = "REPORT_VIEW", Name = "Xem báo cáo doanh thu & hạn sử dụng", Module = "REPORT" }
            };

            await context.Permissions.AddRangeAsync(permissions);
            await context.SaveChangesAsync();

            // Gán Full quyền cho SuperAdmin
            foreach (var perm in permissions)
            {
                await context.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = roleSuperAdmin.Id,
                    PermissionId = perm.Id
                });
            }

            // Gán quyền cho BranchManager
            var managerPermCodes = new[] { "DASHBOARD_VIEW", "PRODUCT_VIEW", "INVENTORY_VIEW", "INVENTORY_IMPORT", "INVENTORY_ADJUST", "POS_ACCESS", "ORDER_VIEW", "ORDER_PROCESS", "REPORT_VIEW" };
            foreach (var perm in permissions.Where(p => managerPermCodes.Contains(p.Code)))
            {
                await context.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = roleBranchManager.Id,
                    PermissionId = perm.Id
                });
            }

            // Gán quyền cho Cashier
            var cashierPermCodes = new[] { "POS_ACCESS", "POS_CHECKOUT", "PRODUCT_VIEW", "ORDER_VIEW" };
            foreach (var perm in permissions.Where(p => cashierPermCodes.Contains(p.Code)))
            {
                await context.RolePermissions.AddAsync(new RolePermission
                {
                    RoleId = roleCashier.Id,
                    PermissionId = perm.Id
                });
            }

            await context.SaveChangesAsync();

            // =========================================================================
            // 2. SEED CHI NHÁNH CỬA HÀNG (STORES)
            // =========================================================================
            var flagshipStore = new Store
            {
                Code = "STR-HCM-01",
                Name = "Omnichannel Beauty Flagship Store Quận 1",
                Address = "128 Hai Bà Trưng, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh",
                PhoneNumber = "02838221199",
                Email = "flagship.q1@omnichannel.com",
                IsHeadquarters = true,
                IsActive = true
            };

            var hanoiStore = new Store
            {
                Code = "STR-HN-01",
                Name = "Omnichannel Beauty Cầu Giấy",
                Address = "245 Cầu Giấy, Phường Dịch Vọng, Quận Cầu Giấy, Hà Nội",
                PhoneNumber = "02437665588",
                Email = "caugiay.hn@omnichannel.com",
                IsHeadquarters = false,
                IsActive = true
            };

            await context.Stores.AddRangeAsync(flagshipStore, hanoiStore);
            await context.SaveChangesAsync();

            // =========================================================================
            // 3. SEED TÀI KHOẢN QUẢN TRỊ MẶC ĐỊNH (USERS)
            // Mật khẩu mặc định: Admin@123456 / Manager@123456 / Cashier@123456
            // =========================================================================
            var adminUser = new User
            {
                Username = "admin",
                Email = "admin@omnichannel.com",
                FullName = "Hệ Thống Super Admin",
                PasswordHash = HashPassword("Admin@123456"),
                PhoneNumber = "0909000001",
                AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=300&h=300&fit=crop&crop=face",
                IsActive = true,
                StoreId = flagshipStore.Id
            };

            var managerUser = new User
            {
                Username = "manager",
                Email = "manager@omnichannel.com",
                FullName = "Nguyễn Văn Quản Lý",
                PasswordHash = HashPassword("Manager@123456"),
                PhoneNumber = "0909000002",
                AvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=300&h=300&fit=crop&crop=face",
                IsActive = true,
                StoreId = flagshipStore.Id
            };

            var cashierUser = new User
            {
                Username = "cashier",
                Email = "cashier@omnichannel.com",
                FullName = "Trần Thị Thu Ngân",
                PasswordHash = HashPassword("Cashier@123456"),
                PhoneNumber = "0909000003",
                AvatarUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?w=300&h=300&fit=crop&crop=face",
                IsActive = true,
                StoreId = flagshipStore.Id
            };

            await context.Users.AddRangeAsync(adminUser, managerUser, cashierUser);
            await context.SaveChangesAsync();

            await context.UserRoles.AddRangeAsync(
                new UserRole { UserId = adminUser.Id, RoleId = roleSuperAdmin.Id },
                new UserRole { UserId = managerUser.Id, RoleId = roleBranchManager.Id },
                new UserRole { UserId = cashierUser.Id, RoleId = roleCashier.Id }
            );
            await context.SaveChangesAsync();

            // =========================================================================
            // 4. SEED NHÀ CUNG CẤP THỰC TẾ (SUPPLIERS)
            // =========================================================================
            var supLoreal = new Supplier
            {
                Code = "SUP-LOREAL",
                Name = "L'Oréal Paris Việt Nam",
                ContactPerson = "Phòng Phân Phối Thương Mại",
                PhoneNumber = "02839103968",
                Email = "order@loreal.vn",
                Address = "Tòa nhà Vincom Center, 72 Lê Thánh Tôn, Bến Nghé, Quận 1, TP.HCM",
                IsActive = true
            };

            var supRohto = new Supplier
            {
                Code = "SUP-ROHTO",
                Name = "Rohto-Mentholatum Việt Nam",
                ContactPerson = "Đại diện Phân Phối Kênh Bán Lẻ",
                PhoneNumber = "02743743400",
                Email = "sales@rohto.com.vn",
                Address = "Số 16 Đường số 5, VSIP 1, Thuận An, Bình Dương",
                IsActive = true
            };

            var supAmore = new Supplier
            {
                Code = "SUP-AMORE",
                Name = "Amorepacific Việt Nam (Laneige, Innisfree, Sulwhasoo)",
                ContactPerson = "Bộ phận Cung ứng Chuỗi Cửa Hàng",
                PhoneNumber = "02838246232",
                Email = "b2b@amorepacific.vn",
                Address = "Tòa nhà Mplaza Saigon, 39 Lê Duẩn, Bến Nghé, Quận 1, TP.HCM",
                IsActive = true
            };

            await context.Suppliers.AddRangeAsync(supLoreal, supRohto, supAmore);
            await context.SaveChangesAsync();

            // =========================================================================
            // 5. SEED DANH MỤC SẢN PHẨM MỸ PHẨM (CATEGORIES ĐA CẤP)
            // =========================================================================
            var catSkinCare = new Category
            {
                Name = "Chăm Sóc Da",
                Slug = "cham-soc-da",
                Description = "Sản phẩm skincare chuyên sâu làm sạch, dưỡng ẩm và chống nắng bảo vệ toàn diện",
                ImageUrl = "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=500&h=500&fit=crop",
                SortOrder = 1,
                IsActive = true
            };

            var catMakeup = new Category
            {
                Name = "Trang Điểm",
                Slug = "trang-diem",
                Description = "Mỹ phẩm trang điểm thời thượng mang phong cách trẻ trung, quyến rũ",
                ImageUrl = "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=500&h=500&fit=crop",
                SortOrder = 2,
                IsActive = true
            };

            var catHairCare = new Category
            {
                Name = "Chăm Sóc Tóc",
                Slug = "cham-soc-toc",
                Description = "Dòng dưỡng tóc suôn mượt, phục hồi hư tổn và tinh dầu bóng mượt chuẩn salon",
                ImageUrl = "https://images.unsplash.com/photo-1527799820374-dcf8d9d4a388?w=500&h=500&fit=crop",
                SortOrder = 3,
                IsActive = true
            };

            await context.Categories.AddRangeAsync(catSkinCare, catMakeup, catHairCare);
            await context.SaveChangesAsync();

            // Danh mục con
            var subCleanser = new Category { Name = "Nước Tẩy Trang", Slug = "nuoc-tay-trang", ParentId = catSkinCare.Id, SortOrder = 1, IsActive = true };
            var subFaceWash = new Category { Name = "Sữa Rửa Mặt", Slug = "sua-rua-mat", ParentId = catSkinCare.Id, SortOrder = 2, IsActive = true };
            var subSerum = new Category { Name = "Serum Phục Hồi", Slug = "serum-phuc-hoi", ParentId = catSkinCare.Id, SortOrder = 3, IsActive = true };
            var subSunscreen = new Category { Name = "Kem Chống Nắng", Slug = "kem-chong-nang", ParentId = catSkinCare.Id, SortOrder = 4, IsActive = true };

            var subLipstick = new Category { Name = "Son Môi Lì", Slug = "son-moi-li", ParentId = catMakeup.Id, SortOrder = 1, IsActive = true };
            var subCushion = new Category { Name = "Cushion Kiềm Dầu", Slug = "cushion-kiem-dau", ParentId = catMakeup.Id, SortOrder = 2, IsActive = true };
            var subEyeShadow = new Category { Name = "Phấn Mắt", Slug = "phan-mat", ParentId = catMakeup.Id, SortOrder = 3, IsActive = true };

            var subHairOil = new Category { Name = "Tinh Dầu Dưỡng Tóc", Slug = "tinh-dau-duong-toc", ParentId = catHairCare.Id, SortOrder = 1, IsActive = true };

            await context.Categories.AddRangeAsync(subCleanser, subFaceWash, subSerum, subSunscreen, subLipstick, subCushion, subEyeShadow, subHairOil);
            await context.SaveChangesAsync();

            // =========================================================================
            // 6. SEED SẢN PHẨM MỸ PHẨM THỰC TẾ & 2 LÔ HÀNG FEFO CHO MỖI SẢN PHẨM
            // 1 lô date xa (>2 năm) và 1 lô cận date (<60 ngày) kèm tồn kho chi nhánh
            // =========================================================================
            var now = DateTime.UtcNow;

            var productsData = new List<(Product Product, decimal Cost, decimal Sell, string BatchFar, string BatchNear)>
            {
                (
                    new Product
                    {
                        CategoryId = subCleanser.Id,
                        SupplierId = supLoreal.Id,
                        Sku = "LOR-MCL-400",
                        Barcode = "8935001201015",
                        Name = "Nước Tẩy Trang L'Oréal Paris Micellar Water 3-in-1 Deep Cleansing 400ml",
                        Slug = "nuoc-tay-trang-loreal-paris-micellar-water-400ml",
                        ShortDescription = "Làm sạch sâu lớp trang điểm lâu trôi và cặn bẩn ô nhiễm, công nghệ Mixen dịu nhẹ.",
                        Description = "Nước Tẩy Trang L'Oréal Paris Micellar Water 3-in-1 mang lại hiệu quả làm sạch tối ưu mà không gây khô rát làn da. Sản phẩm thích hợp cho cả làn da nhạy cảm.",
                        CostPrice = 145000m,
                        SellingPrice = 219000m,
                        DiscountPrice = 189000m,
                        ThumbnailUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=600&h=600&fit=crop",
                        Unit = "Chai",
                        IsActive = true,
                        IsFeatured = true
                    },
                    145000m, 219000m, "LOT-LOR-26A01", "LOT-LOR-24E09"
                ),
                (
                    new Product
                    {
                        CategoryId = subFaceWash.Id,
                        SupplierId = supRohto.Id,
                        Sku = "ROH-HDL-100",
                        Barcode = "8935001201022",
                        Name = "Kem Rửa Mặt Dưỡng Ẩm Chuyên Sâu Hada Labo Advanced Nourish 80g",
                        Slug = "kem-rua-mat-hada-labo-advanced-nourish-80g",
                        ShortDescription = "Công nghệ Hyaluronic Acid đa phân tử cấp ẩm dồi dào, rửa sạch sâu mà không căng rát.",
                        Description = "Hada Labo Advanced Nourish làm sạch dịu nhẹ bụi bẩn bã nhờn ẩn sâu trong lỗ chân lông đồng thời lưu lại màng ẩm mịn tự nhiên nhờ phức hợp HA, SHA, Nano HA.",
                        CostPrice = 62000m,
                        SellingPrice = 99000m,
                        DiscountPrice = 85000m,
                        ThumbnailUrl = "https://images.unsplash.com/photo-1556228722-d0b5be7490bf?w=600&h=600&fit=crop",
                        Unit = "Tuýp",
                        IsActive = true,
                        IsFeatured = true
                    },
                    62000m, 99000m, "LOT-ROH-26B02", "LOT-ROH-24F10"
                ),
                (
                    new Product
                    {
                        CategoryId = subSerum.Id,
                        SupplierId = supLoreal.Id,
                        Sku = "LOR-HYA-B5",
                        Barcode = "8935001201039",
                        Name = "Serum Phục Hồi Và Căng Mướt Da La Roche-Posay Hyalu B5 Pure 30ml",
                        Slug = "serum-phuc-hoi-da-la-roche-posay-hyalu-b5-30ml",
                        ShortDescription = "Chứa Panthenol B5 5% và Pure Hyaluronic Acid hỗ trợ phục hồi màng bảo vệ da suy yếu.",
                        Description = "Serum La Roche-Posay Hyalu B5 là giải pháp tái tạo và phục hồi hàng rào ẩm cho làn da mệt mỏi, da nhạy cảm sau peel hoặc laser.",
                        CostPrice = 680000m,
                        SellingPrice = 985000m,
                        DiscountPrice = 890000m,
                        ThumbnailUrl = "https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=600&h=600&fit=crop",
                        Unit = "Chai",
                        IsActive = true,
                        IsFeatured = true
                    },
                    680000m, 985000m, "LOT-LRP-26C03", "LOT-LRP-24G11"
                ),
                (
                    new Product
                    {
                        CategoryId = subSunscreen.Id,
                        SupplierId = supRohto.Id,
                        Sku = "ROH-SS-SKINA",
                        Barcode = "8935001201046",
                        Name = "Sữa Chống Nắng Kiềm Dầu Skin Aqua Tone Up UV Milk SPF50+ PA++++ 50g",
                        Slug = "sua-chong-nang-skin-aqua-tone-up-uv-milk-50g",
                        ShortDescription = "Hiệu chỉnh sắc tố da ánh tím Lavender rạng rỡ, kiềm dầu khô thoáng suốt 8 giờ.",
                        Description = "Skin Aqua Tone Up UV Milk bảo vệ làn da tối đa trước tia UVA, UVB và khói bụi mịn, đồng thời nâng tông trong trẻo tự nhiên.",
                        CostPrice = 125000m,
                        SellingPrice = 185000m,
                        DiscountPrice = 165000m,
                        ThumbnailUrl = "https://images.unsplash.com/photo-1598440947619-2c35fc9aa908?w=600&h=600&fit=crop",
                        Unit = "Chai",
                        IsActive = true,
                        IsFeatured = true
                    },
                    125000m, 185000m, "LOT-AQU-26D04", "LOT-AQU-24H12"
                ),
                (
                    new Product
                    {
                        CategoryId = subLipstick.Id,
                        SupplierId = supAmore.Id,
                        Sku = "AMO-ROM-VT02",
                        Barcode = "8935001201053",
                        Name = "Son Kem Lì Rom&nd Zero Velvet Tint - #02 Joyful 5.5g",
                        Slug = "son-kem-li-romand-zero-velvet-tint-02-joyful",
                        ShortDescription = "Chất son velvet xốp mịn che phủ vân môi tuyệt hảo, sắc đỏ cam gạch tôn da rạng rỡ.",
                        Description = "Rom&nd Zero Velvet Tint đem lại hiệu ứng làm mờ vân môi đỉnh cao, mềm mượt như bông và lâu trôi.",
                        CostPrice = 115000m,
                        SellingPrice = 179000m,
                        DiscountPrice = 159000m,
                        ThumbnailUrl = "https://images.unsplash.com/photo-1586495777744-4413f21062fa?w=600&h=600&fit=crop",
                        Unit = "Thỏi",
                        IsActive = true,
                        IsFeatured = true
                    },
                    115000m, 179000m, "LOT-ROM-26E05", "LOT-ROM-24I01"
                ),
                (
                    new Product
                    {
                        CategoryId = subCushion.Id,
                        SupplierId = supAmore.Id,
                        Sku = "AMO-LAN-CUSH21",
                        Barcode = "8935001201060",
                        Name = "Phấn Nước Kiềm Dầu Mịn Lì Laneige Neo Cushion Matte SPF42 15g #21N",
                        Slug = "phan-nuoc-laneige-neo-cushion-matte-spf42-21n",
                        ShortDescription = "Lớp nền che khuyết điểm bền màu 24h, mỏng nhẹ không lem dính khẩu trang.",
                        Description = "Laneige Neo Cushion Matte công nghệ hạt phấn siêu nhỏ thẩm thấu dầu thừa tức thì, mang đến bề mặt da mịn màng không tì vết suốt cả ngày dài năng động.",
                        CostPrice = 430000m,
                        SellingPrice = 650000m,
                        DiscountPrice = 580000m,
                        ThumbnailUrl = "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=600&h=600&fit=crop",
                        Unit = "Hộp",
                        IsActive = true,
                        IsFeatured = true
                    },
                    430000m, 650000m, "LOT-LAN-26F06", "LOT-LAN-24J02"
                ),
                (
                    new Product
                    {
                        CategoryId = subHairOil.Id,
                        SupplierId = supLoreal.Id,
                        Sku = "LOR-ELS-OIL",
                        Barcode = "8935001201077",
                        Name = "Dầu Dưỡng Tóc L'Oréal Paris Elseve Extraordinary Oil Chiết Xuất Hoa Tự Nhiên 100ml",
                        Slug = "dau-duong-toc-loreal-elseve-extraordinary-oil-100ml",
                        ShortDescription = "Chiết xuất 6 loại hoa quý nuôi dưỡng tóc mềm mượt diệu kỳ, không bết dính.",
                        Description = "L'Oréal Paris Elseve Extraordinary Oil thẩm thấu sâu nuôi dưỡng ngọn tóc xơ rối chẻ ngọn trở nên bóng mượt, ngát hương thơm quyến rũ chuẩn Paris.",
                        CostPrice = 170000m,
                        SellingPrice = 259000m,
                        DiscountPrice = 229000m,
                        ThumbnailUrl = "https://images.unsplash.com/photo-1608248597359-269201991823?w=600&h=600&fit=crop",
                        Unit = "Chai",
                        IsActive = true,
                        IsFeatured = false
                    },
                    170000m, 259000m, "LOT-ELS-26G07", "LOT-ELS-24K03"
                )
            };

            foreach (var item in productsData)
            {
                await context.Products.AddAsync(item.Product);
                await context.SaveChangesAsync();

                // 1. Lô Date Xa (> 2 năm kể từ hiện tại)
                var batchFar = new ProductBatch
                {
                    ProductId = item.Product.Id,
                    BatchNumber = item.BatchFar,
                    ManufacturingDate = now.AddMonths(-3),
                    ExpDate = now.AddYears(2).AddMonths(6), // Hạn còn ~2.5 năm
                    ImportCost = item.Cost,
                    InitialQuantity = 100,
                    CurrentQuantity = 95,
                    Status = "ACTIVE",
                    CreatedAt = now.AddMonths(-3)
                };

                // 2. Lô Cận Date (< 60 ngày kể từ hiện tại - Cốt lõi kiểm thử kho FEFO)
                var batchNear = new ProductBatch
                {
                    ProductId = item.Product.Id,
                    BatchNumber = item.BatchNear,
                    ManufacturingDate = now.AddYears(-2).AddMonths(-10),
                    ExpDate = now.AddDays(38), // Chỉ còn 38 ngày là hết hạn
                    ImportCost = item.Cost,
                    InitialQuantity = 50,
                    CurrentQuantity = 25,
                    Status = "NEAR_EXPIRATION",
                    CreatedAt = now.AddMonths(-10)
                };

                await context.ProductBatches.AddRangeAsync(batchFar, batchNear);
                await context.SaveChangesAsync();

                // Seed Tồn kho tại Flagship Store HCM
                var invFar = new StoreInventory
                {
                    StoreId = flagshipStore.Id,
                    ProductBatchId = batchFar.Id,
                    PhysicalQuantity = 95,
                    ReservedQuantity = 5, // 5 món đang trong giỏ chờ thanh toán
                    LastUpdated = now
                };

                var invNear = new StoreInventory
                {
                    StoreId = flagshipStore.Id,
                    ProductBatchId = batchNear.Id,
                    PhysicalQuantity = 25,
                    ReservedQuantity = 2, // 2 món đang trong đơn POS
                    LastUpdated = now
                };

                await context.StoreInventories.AddRangeAsync(invFar, invNear);

                // Ghi nhận Audit Log ban đầu
                await context.InventoryLogs.AddRangeAsync(
                    new InventoryLog
                    {
                        StoreId = flagshipStore.Id,
                        ProductBatchId = batchFar.Id,
                        UserId = adminUser.Id,
                        TransactionType = "INITIAL_IMPORT",
                        QuantityChanged = 100,
                        QuantityBefore = 0,
                        QuantityAfter = 100,
                        Note = $"Nhập kho ban đầu lô date xa {batchFar.BatchNumber}",
                        CreatedAt = now.AddMonths(-3)
                    },
                    new InventoryLog
                    {
                        StoreId = flagshipStore.Id,
                        ProductBatchId = batchNear.Id,
                        UserId = adminUser.Id,
                        TransactionType = "INITIAL_IMPORT",
                        QuantityChanged = 50,
                        QuantityBefore = 0,
                        QuantityAfter = 50,
                        Note = $"Nhập kho ban đầu lô cận date {batchNear.BatchNumber} (FEFO Priority)",
                        CreatedAt = now.AddMonths(-10)
                    }
                );
            }

            await context.SaveChangesAsync();

            // =========================================================================
            // 7. SEED CÂY MENU ĐA CẤP (HEADER_MAIN & FOOTER_MAIN)
            // =========================================================================
            var menuHeader = new Menu
            {
                Code = "HEADER_MAIN",
                Title = "Menu Điều Hướng Chính Đầu Trang",
                Position = "TOP",
                IsActive = true
            };

            var menuFooter = new Menu
            {
                Code = "FOOTER_MAIN",
                Title = "Menu Thông Tin Chân Trang",
                Position = "BOTTOM",
                IsActive = true
            };

            await context.Menus.AddRangeAsync(menuHeader, menuFooter);
            await context.SaveChangesAsync();

            var miHome = new MenuItem { MenuId = menuHeader.Id, Title = "Trang Chủ", Url = "/", SortOrder = 1, IsActive = true };
            var miSkincare = new MenuItem { MenuId = menuHeader.Id, Title = "Chăm Sóc Da", Url = "/category/cham-soc-da", SortOrder = 2, IsActive = true };
            var miMakeup = new MenuItem { MenuId = menuHeader.Id, Title = "Trang Điểm", Url = "/category/trang-diem", SortOrder = 3, IsActive = true };
            var miHaircare = new MenuItem { MenuId = menuHeader.Id, Title = "Chăm Sóc Tóc", Url = "/category/cham-soc-toc", SortOrder = 4, IsActive = true };
            var miFefoSale = new MenuItem { MenuId = menuHeader.Id, Title = "Ưu Đãi Cận Date", Url = "/clearance-fefo", SortOrder = 5, IsActive = true };

            await context.MenuItems.AddRangeAsync(miHome, miSkincare, miMakeup, miHaircare, miFefoSale);
            await context.SaveChangesAsync();

            // Menu con của Chăm sóc da
            await context.MenuItems.AddRangeAsync(
                new MenuItem { MenuId = menuHeader.Id, ParentId = miSkincare.Id, Title = "Nước Tẩy Trang", Url = "/category/nuoc-tay-trang", SortOrder = 1, IsActive = true },
                new MenuItem { MenuId = menuHeader.Id, ParentId = miSkincare.Id, Title = "Sữa Rửa Mặt", Url = "/category/sua-rua-mat", SortOrder = 2, IsActive = true },
                new MenuItem { MenuId = menuHeader.Id, ParentId = miSkincare.Id, Title = "Serum Phục Hồi", Url = "/category/serum-phuc-hoi", SortOrder = 3, IsActive = true },
                new MenuItem { MenuId = menuHeader.Id, ParentId = miSkincare.Id, Title = "Kem Chống Nắng", Url = "/category/kem-chong-nang", SortOrder = 4, IsActive = true }
            );

            // Menu Chân trang
            await context.MenuItems.AddRangeAsync(
                new MenuItem { MenuId = menuFooter.Id, Title = "Chính Sách Giao Hàng Đa Kênh", Url = "/page/chinh-sach-giao-hang", SortOrder = 1, IsActive = true },
                new MenuItem { MenuId = menuFooter.Id, Title = "Chính Sách Đổi Trả & FEFO", Url = "/page/chinh-sach-doi-tra", SortOrder = 2, IsActive = true },
                new MenuItem { MenuId = menuFooter.Id, Title = "Hệ Thống Chuỗi Cửa Hàng", Url = "/stores", SortOrder = 3, IsActive = true }
            );

            await context.SaveChangesAsync();

            // =========================================================================
            // 8. SEED BANNERS KHUYẾN MÃI HERO PASTEL
            // =========================================================================
            var banners = new List<Banner>
            {
                new()
                {
                    Title = "LUXURY BEAUTY SALE",
                    Subtitle = "Đắm Chìm Trong Thế Giới Nhan Sắc - Ưu Đãi Lên Đến 40%",
                    ImageUrl = "https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=1600&h=600&fit=crop",
                    LinkUrl = "/category/trang-diem",
                    ButtonText = "Khám Phá Ngay",
                    Position = "HERO_HOME",
                    SortOrder = 1,
                    IsActive = true,
                    StartDate = now.AddDays(-10),
                    EndDate = now.AddDays(90)
                },
                new()
                {
                    Title = "K-BEAUTY TREND 2026",
                    Subtitle = "Lớp Nền Mịn Lì Không Tì Vết Chuẩn Hàn Cùng Laneige & Rom&nd",
                    ImageUrl = "https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=1600&h=600&fit=crop",
                    LinkUrl = "/category/trang-diem",
                    ButtonText = "Mua Ngay",
                    Position = "HERO_HOME",
                    SortOrder = 2,
                    IsActive = true,
                    StartDate = now.AddDays(-5),
                    EndDate = now.AddDays(60)
                },
                new()
                {
                    Title = "FEFO SMART CLEARANCE",
                    Subtitle = "Săn Mỹ Phẩm Chính Hãng Cận Date Giá Cực Sốc - Minh Bạch Hạn Sử Dụng",
                    ImageUrl = "https://images.unsplash.com/photo-1570172619644-dfd03ed5d881?w=1600&h=600&fit=crop",
                    LinkUrl = "/clearance-fefo",
                    ButtonText = "Săn Deal Cận Date",
                    Position = "MID_PROMO",
                    SortOrder = 3,
                    IsActive = true,
                    StartDate = now,
                    EndDate = now.AddDays(30)
                }
            };

            await context.Banners.AddRangeAsync(banners);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Thuật toán băm mật khẩu sử dụng PasswordHasher chuẩn của ASP.NET Core Identity (giống với PasswordHasherService)
        /// </summary>
        private static string HashPassword(string password)
        {
            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<string>();
            return hasher.HashPassword("OmnichannelSystemUser", password);
        }
    }
}