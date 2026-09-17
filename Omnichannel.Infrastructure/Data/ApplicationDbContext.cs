using Microsoft.EntityFrameworkCore;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<User> Users => Set<User>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
        public DbSet<Menu> Menus => Set<Menu>();
        public DbSet<MenuItem> MenuItems => Set<MenuItem>();
        public DbSet<Banner> Banners => Set<Banner>();
        public DbSet<PostCategory> PostCategories => Set<PostCategory>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<StaticPage> StaticPages => Set<StaticPage>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductBatch> ProductBatches => Set<ProductBatch>();
        public DbSet<Store> Stores => Set<Store>();
        public DbSet<StoreInventory> StoreInventories => Set<StoreInventory>();
        public DbSet<InventoryLog> InventoryLogs => Set<InventoryLog>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
        public DbSet<Invoice> Invoices => Set<Invoice>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Roles
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("Roles");
                entity.HasKey(e => e.RoleId);
                entity.Property(e => e.RoleId).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.RoleName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.IsSystemRole).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            });

            // 2. Permissions
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.ToTable("Permissions");
                entity.HasKey(e => e.PermissionId);
                entity.Property(e => e.ModuleCode).HasMaxLength(50).IsUnicode(false).IsRequired();
                entity.Property(e => e.ActionCode).HasMaxLength(50).IsUnicode(false).IsRequired();
                entity.Property(e => e.PermissionName).HasMaxLength(150).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.HasIndex(e => new { e.ModuleCode, e.ActionCode }).IsUnique().HasDatabaseName("UQ_Permissions_Module_Action");
            });

            // 3. Users
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.UserId).HasMaxLength(450).IsUnicode(false);
                entity.Property(e => e.Username).HasMaxLength(100).IsUnicode(false).IsRequired();
                entity.Property(e => e.NormalizedUsername).HasMaxLength(100).IsUnicode(false).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(256).IsUnicode(false).IsRequired();
                entity.Property(e => e.PhoneNumber).HasMaxLength(20).IsUnicode(false);
                entity.Property(e => e.FullName).HasMaxLength(150).IsRequired();
                entity.Property(e => e.UserType).HasDefaultValue(1);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasIndex(e => e.Username).IsUnique().HasDatabaseName("UQ_Users_Username");
                entity.HasIndex(e => e.NormalizedUsername).IsUnique().HasDatabaseName("UQ_Users_NormalizedUsername");
                entity.HasIndex(e => e.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
                entity.HasIndex(e => e.PhoneNumber).IsUnique().HasFilter("[PhoneNumber] IS NOT NULL").HasDatabaseName("UQ_Users_PhoneNumber");
            });

            // 4. UserRoles
            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");
                entity.HasKey(e => new { e.UserId, e.RoleId });
                entity.Property(e => e.UserId).HasMaxLength(450).IsUnicode(false);
                entity.Property(e => e.RoleId).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.AssignedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(e => e.User).WithMany(u => u.UserRoles).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Role).WithMany(r => r.UserRoles).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => e.RoleId).HasDatabaseName("IX_UserRoles_RoleId").IncludeProperties(e => e.UserId);
            });

            // 5. RolePermissions
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.ToTable("RolePermissions");
                entity.HasKey(e => e.RolePermissionId);
                entity.Property(e => e.RoleId).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.GrantedBy).HasMaxLength(450).IsUnicode(false);
                entity.Property(e => e.IsGranted).HasDefaultValue(true);
                entity.Property(e => e.GrantedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(e => e.Role).WithMany(r => r.RolePermissions).HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Permission).WithMany(p => p.RolePermissions).HasForeignKey(e => e.PermissionId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Granter).WithMany().HasForeignKey(e => e.GrantedBy).OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique().HasDatabaseName("UQ_RolePermissions_Role_Perm");
                entity.HasIndex(e => e.RoleId).HasDatabaseName("IX_RolePermissions_RoleId").IncludeProperties(e => new { e.PermissionId, e.IsGranted });
            });

            // 6. UserPermissions
            modelBuilder.Entity<UserPermission>(entity =>
            {
                entity.ToTable("UserPermissions");
                entity.HasKey(e => e.UserPermissionId);
                entity.Property(e => e.UserId).HasMaxLength(450).IsUnicode(false);
                entity.Property(e => e.ModifiedBy).HasMaxLength(450).IsUnicode(false);
                entity.Property(e => e.ModifiedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(e => e.User).WithMany(u => u.UserPermissions).HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Permission).WithMany(p => p.UserPermissions).HasForeignKey(e => e.PermissionId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Modifier).WithMany().HasForeignKey(e => e.ModifiedBy).OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(e => new { e.UserId, e.PermissionId }).IsUnique().HasDatabaseName("UQ_UserPermissions_User_Perm");
                entity.HasIndex(e => e.UserId).HasDatabaseName("IX_UserPermissions_UserId").IncludeProperties(e => new { e.PermissionId, e.IsGranted });
            });

            // 7. Menus & MenuItems
            modelBuilder.Entity<Menu>(entity =>
            {
                entity.ToTable("Menus");
                entity.HasKey(e => e.MenuId);
                entity.Property(e => e.MenuCode).HasMaxLength(50).IsUnicode(false).IsRequired();
                entity.Property(e => e.MenuName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.HasIndex(e => e.MenuCode).IsUnique().HasDatabaseName("UQ_Menus_MenuCode");
            });

            modelBuilder.Entity<MenuItem>(entity =>
            {
                entity.ToTable("MenuItems");
                entity.HasKey(e => e.MenuItemId);
                entity.Property(e => e.Title).HasMaxLength(150).IsRequired();
                entity.Property(e => e.TargetId).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.TargetSlug).HasMaxLength(200).IsUnicode(false);
                entity.Property(e => e.CustomUrl).HasMaxLength(500).IsUnicode(false);
                entity.Property(e => e.IconClass).HasMaxLength(100).IsUnicode(false);
                entity.Property(e => e.CssClass).HasMaxLength(100).IsUnicode(false);
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                entity.Property(e => e.OpenInNewTab).HasDefaultValue(false);
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasOne(e => e.Menu).WithMany(m => m.MenuItems).HasForeignKey(e => e.MenuId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Parent).WithMany(p => p.Children).HasForeignKey(e => e.ParentId).OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(e => new { e.MenuId, e.DisplayOrder }).HasDatabaseName("IX_MenuItems_Menu_Display")
                      .IncludeProperties(e => new { e.ParentId, e.Title, e.TargetType, e.TargetSlug });
            });

            // 8. Banners
            modelBuilder.Entity<Banner>(entity =>
            {
                entity.ToTable(tb => tb.HasCheckConstraint("CK_Banners_Validity", "[ValidTo] >= [ValidFrom]"));
                entity.ToTable("Banners");
                entity.HasKey(e => e.BannerId);
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.DesktopImageUrl).HasMaxLength(500).IsUnicode(false).IsRequired();
                entity.Property(e => e.MobileImageUrl).HasMaxLength(500).IsUnicode(false);
                entity.Property(e => e.LinkUrl).HasMaxLength(500).IsUnicode(false);
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                entity.Property(e => e.TrackingClickCount).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // 9. PostCategories & Posts
            modelBuilder.Entity<PostCategory>(entity =>
            {
                entity.ToTable("PostCategories");
                entity.HasKey(e => e.CategoryId);
                entity.Property(e => e.CategoryName).HasMaxLength(150).IsRequired();
                entity.Property(e => e.Slug).HasMaxLength(150).IsUnicode(false).IsRequired();
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("UQ_PostCategories_Slug");
            });

            modelBuilder.Entity<Post>(entity =>
            {
                entity.ToTable("Posts");
                entity.HasKey(e => e.PostId);
                entity.Property(e => e.Title).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Slug).HasMaxLength(255).IsUnicode(false).IsRequired();
                entity.Property(e => e.Summary).HasMaxLength(500);
                entity.Property(e => e.ContentHtml).IsRequired();
                entity.Property(e => e.ThumbnailUrl).HasMaxLength(500).IsUnicode(false).IsRequired();
                entity.Property(e => e.AuthorId).HasMaxLength(450).IsUnicode(false);
                entity.Property(e => e.Status).HasDefaultValue(1);
                entity.Property(e => e.MetaTitle).HasMaxLength(255);
                entity.Property(e => e.MetaKeywords).HasMaxLength(255);
                entity.Property(e => e.MetaDescription).HasMaxLength(500);
                entity.Property(e => e.ViewCount).HasDefaultValue(0);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(e => e.Category).WithMany(c => c.Posts).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Author).WithMany(u => u.Posts).HasForeignKey(e => e.AuthorId).OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("UQ_Posts_Slug");
            });

            // 10. StaticPages
            modelBuilder.Entity<StaticPage>(entity =>
            {
                entity.ToTable("StaticPages");
                entity.HasKey(e => e.PageId);
                entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
                entity.Property(e => e.Slug).HasMaxLength(200).IsUnicode(false).IsRequired();
                entity.Property(e => e.ContentHtml).IsRequired();
                entity.Property(e => e.IsSystemLocked).HasDefaultValue(false);
                entity.Property(e => e.MetaTitle).HasMaxLength(255);
                entity.Property(e => e.MetaDescription).HasMaxLength(500);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("UQ_StaticPages_Slug");
            });

            // 11. Categories, Suppliers & Products
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("Categories");
                entity.HasKey(e => e.CategoryId);
                entity.Property(e => e.CategoryName).HasMaxLength(150).IsRequired();
                entity.Property(e => e.Slug).HasMaxLength(150).IsUnicode(false).IsRequired();
                entity.Property(e => e.IconUrl).HasMaxLength(500).IsUnicode(false);
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                entity.HasOne(e => e.Parent).WithMany(p => p.SubCategories).HasForeignKey(e => e.ParentId).OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("UQ_Categories_Slug");
            });

            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.ToTable("Suppliers");
                entity.HasKey(e => e.SupplierId);
                entity.Property(e => e.SupplierId).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.SupplierName).HasMaxLength(200).IsRequired();
                entity.Property(e => e.TaxCode).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.PhoneNumber).HasMaxLength(20).IsUnicode(false).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(150).IsUnicode(false);
                entity.Property(e => e.Address).HasMaxLength(255).IsRequired();
                entity.Property(e => e.TotalDebt).HasPrecision(18, 2).HasDefaultValue(0.00m);
                entity.Property(e => e.Status).HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.HasIndex(e => e.TaxCode).IsUnique().HasFilter("[TaxCode] IS NOT NULL").HasDatabaseName("UQ_Suppliers_TaxCode");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_Products_SellingPrice", "[SellingPrice] >= 0.00");
                    tb.HasCheckConstraint("CK_Products_CostPrice", "[CostPrice] >= 0.00");
                });
                entity.ToTable("Products");
                entity.HasKey(e => e.ProductId);
                entity.Property(e => e.ProductId).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.SupplierId).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.Barcode).HasMaxLength(50).IsUnicode(false).IsRequired();
                entity.Property(e => e.ProductName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Slug).HasMaxLength(255).IsUnicode(false).IsRequired();
                entity.Property(e => e.SellingPrice).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.CostPrice).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.SafetyStock).HasDefaultValue(10);
                entity.Property(e => e.Unit).HasMaxLength(50).HasDefaultValue("Hộp");
                entity.Property(e => e.Status).HasDefaultValue(1);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(e => e.Category).WithMany(c => c.Products).HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(e => e.Supplier).WithMany(s => s.Products).HasForeignKey(e => e.SupplierId).OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(e => e.Barcode).IsUnique().HasDatabaseName("UQ_Products_Barcode");
                entity.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("UQ_Products_Slug");
            });

            // 12. ProductBatches & Inventories
            modelBuilder.Entity<ProductBatch>(entity =>
            {
                entity.ToTable(tb => tb.HasCheckConstraint("CK_ProductBatches_Dates", "[ExpDate] > [ManufacturingDate]"));
                entity.ToTable("ProductBatches");
                entity.HasKey(e => e.BatchId);
                entity.Property(e => e.ProductId).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.BatchNumber).HasMaxLength(100).IsUnicode(false).IsRequired();
                entity.Property(e => e.ManufacturingDate).HasColumnType("date").IsRequired();
                entity.Property(e => e.ExpDate).HasColumnType("date").IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(e => e.Product).WithMany(p => p.ProductBatches).HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.ProductId, e.BatchNumber }).IsUnique().HasDatabaseName("UQ_ProductBatches_SKU_Batch");
                entity.HasIndex(e => new { e.ProductId, e.ExpDate }).HasDatabaseName("IX_ProductBatches_ExpDate")
                      .IncludeProperties(e => new { e.BatchId, e.BatchNumber });
            });

            modelBuilder.Entity<Store>(entity =>
            {
                entity.ToTable("Stores");
                entity.HasKey(e => e.StoreId);
                entity.Property(e => e.StoreId).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.StoreName).HasMaxLength(150).IsRequired();
                entity.Property(e => e.Address).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Phone).HasMaxLength(20).IsUnicode(false).IsRequired();
                entity.Property(e => e.IsCentralWarehouse).HasDefaultValue(false);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            modelBuilder.Entity<StoreInventory>(entity =>
            {
                entity.ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_StoreInventories_Physical", "[PhysicalQuantity] >= 0");
                    tb.HasCheckConstraint("CK_StoreInventories_Reserved", "[ReservedQuantity] >= 0");
                    tb.HasCheckConstraint("CK_StoreInventories_Available", "[PhysicalQuantity] >= [ReservedQuantity]");
                });
                entity.ToTable("StoreInventories");
                entity.HasKey(e => e.InventoryId);
                entity.Property(e => e.StoreId).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.PhysicalQuantity).HasDefaultValue(0);
                entity.Property(e => e.ReservedQuantity).HasDefaultValue(0);
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(e => e.Store).WithMany(s => s.StoreInventories).HasForeignKey(e => e.StoreId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Batch).WithMany(b => b.StoreInventories).HasForeignKey(e => e.BatchId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.StoreId, e.BatchId }).IsUnique().HasDatabaseName("UQ_StoreInventories_Store_Batch");
                entity.HasIndex(e => new { e.StoreId, e.BatchId }).HasDatabaseName("IX_StoreInventories_FEFO")
                      .IncludeProperties(e => new { e.PhysicalQuantity, e.ReservedQuantity });
            });

            modelBuilder.Entity<InventoryLog>(entity =>
            {
                entity.ToTable("InventoryLogs");
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.StoreId).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.ReferenceDocNo).HasMaxLength(100).IsUnicode(false).IsRequired();
                entity.Property(e => e.CreatedBy).HasMaxLength(450).IsUnicode(false);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.Notes).HasMaxLength(500);

                entity.HasOne(e => e.Store).WithMany(s => s.InventoryLogs).HasForeignKey(e => e.StoreId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(e => e.Batch).WithMany(b => b.InventoryLogs).HasForeignKey(e => e.BatchId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(e => e.Creator).WithMany(u => u.InventoryLogs).HasForeignKey(e => e.CreatedBy).OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(e => new { e.StoreId, e.BatchId, e.CreatedAt }).HasDatabaseName("IX_InventoryLogs_Audit");
            });

            // 13. Carts & CartItems
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.ToTable("Carts");
                entity.HasKey(e => e.CartId);
                entity.Property(e => e.CartId).HasMaxLength(100).IsUnicode(false);
                entity.Property(e => e.UserId).HasMaxLength(450).IsUnicode(false);
                entity.Property(e => e.LastActive).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.ToTable(tb => tb.HasCheckConstraint("CK_CartItems_Quantity", "[Quantity] > 0"));
                entity.ToTable("CartItems");
                entity.HasKey(e => e.CartItemId);
                entity.Property(e => e.CartId).HasMaxLength(100).IsUnicode(false);
                entity.Property(e => e.ProductId).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.AddedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(e => e.Cart).WithMany(c => c.CartItems).HasForeignKey(e => e.CartId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Product).WithMany(p => p.CartItems).HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(e => new { e.CartId, e.ProductId }).IsUnique().HasDatabaseName("UQ_CartItems_Cart_Product");
            });

            // 14. Orders & OrderDetails
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_Orders_SubTotal", "[SubTotal] >= 0.00");
                    tb.HasCheckConstraint("CK_Orders_DiscountAmount", "[DiscountAmount] >= 0.00");
                    tb.HasCheckConstraint("CK_Orders_ShippingFee", "[ShippingFee] >= 0.00");
                    tb.HasCheckConstraint("CK_Orders_FinalTotal", "[FinalTotal] >= 0.00");
                });
                entity.ToTable("Orders");
                entity.HasKey(e => e.OrderId);
                entity.Property(e => e.OrderId).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.StoreId).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.CustomerId).HasMaxLength(450).IsUnicode(false);
                entity.Property(e => e.CreatedByStaffId).HasMaxLength(450).IsUnicode(false);
                entity.Property(e => e.RecipientName).HasMaxLength(150).IsRequired();
                entity.Property(e => e.RecipientPhone).HasMaxLength(20).IsUnicode(false).IsRequired();
                entity.Property(e => e.ShippingAddress).HasMaxLength(255).IsRequired();
                entity.Property(e => e.SubTotal).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.DiscountAmount).HasPrecision(18, 2).HasDefaultValue(0.00m);
                entity.Property(e => e.ShippingFee).HasPrecision(18, 2).HasDefaultValue(0.00m);
                entity.Property(e => e.FinalTotal).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.PaymentStatus).HasDefaultValue(1);
                entity.Property(e => e.OrderStatus).HasDefaultValue(1);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(e => e.Store).WithMany(s => s.Orders).HasForeignKey(e => e.StoreId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(e => e.Customer).WithMany(u => u.CustomerOrders).HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(e => e.Staff).WithMany(u => u.StaffOrders).HasForeignKey(e => e.CreatedByStaffId).OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(e => new { e.OrderChannel, e.OrderStatus, e.CreatedAt }).HasDatabaseName("IX_Orders_Channel_Status");
            });

            modelBuilder.Entity<OrderDetail>(entity =>
            {
                entity.ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_OrderDetails_Quantity", "[Quantity] > 0");
                    tb.HasCheckConstraint("CK_OrderDetails_UnitPrice", "[UnitPrice] >= 0.00");
                    tb.HasCheckConstraint("CK_OrderDetails_DiscountRate", "[DiscountRate] >= 0.00 AND [DiscountRate] <= 100.00");
                    tb.HasCheckConstraint("CK_OrderDetails_LineTotal", "[LineTotal] >= 0.00");
                });
                entity.ToTable("OrderDetails");
                entity.HasKey(e => e.OrderDetailId);
                entity.Property(e => e.OrderId).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.ProductId).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.DiscountRate).HasPrecision(5, 2).HasDefaultValue(0.00m);
                entity.Property(e => e.LineTotal).HasPrecision(18, 2).IsRequired();

                entity.HasOne(e => e.Order).WithMany(o => o.OrderDetails).HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.Product).WithMany(p => p.OrderDetails).HasForeignKey(e => e.ProductId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(e => e.Batch).WithMany(b => b.OrderDetails).HasForeignKey(e => e.BatchId).OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(e => e.OrderId).HasDatabaseName("IX_OrderDetails_OrderId")
                      .IncludeProperties(e => new { e.ProductId, e.BatchId, e.Quantity, e.LineTotal });
            });

            // 15. Invoices
            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.ToTable(tb =>
                {
                    tb.HasCheckConstraint("CK_Invoices_TotalBeforeTax", "[TotalBeforeTax] >= 0.00");
                    tb.HasCheckConstraint("CK_Invoices_VatRate", "[VatRate] >= 0.00");
                    tb.HasCheckConstraint("CK_Invoices_VatAmount", "[VatAmount] >= 0.00");
                    tb.HasCheckConstraint("CK_Invoices_TotalAfterTax", "[TotalAfterTax] >= 0.00");
                });
                entity.ToTable("Invoices");
                entity.HasKey(e => e.InvoiceId);
                entity.Property(e => e.InvoiceId).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.OrderId).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.InvoiceSeries).HasMaxLength(20).IsUnicode(false).IsRequired();
                entity.Property(e => e.TaxLookupCode).HasMaxLength(100).IsUnicode(false).IsRequired();
                entity.Property(e => e.TotalBeforeTax).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.VatRate).HasPrecision(5, 2).HasDefaultValue(10.00m);
                entity.Property(e => e.VatAmount).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.TotalAfterTax).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.CustomerTaxCode).HasMaxLength(50).IsUnicode(false);
                entity.Property(e => e.CompanyBillingName).HasMaxLength(255);
                entity.Property(e => e.IssuedByStaffId).HasMaxLength(450).IsUnicode(false);
                entity.Property(e => e.PdfStorageUrl).HasMaxLength(500).IsUnicode(false);
                entity.Property(e => e.IssuedAt).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.IsCancelled).HasDefaultValue(false);

                entity.HasOne(e => e.Order).WithOne(o => o.Invoice).HasForeignKey<Invoice>(e => e.OrderId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(e => e.Issuer).WithMany(u => u.IssuedInvoices).HasForeignKey(e => e.IssuedByStaffId).OnDelete(DeleteBehavior.NoAction);
                entity.HasIndex(e => e.OrderId).IsUnique().HasDatabaseName("UQ_Invoices_OrderId");
                entity.HasIndex(e => e.TaxLookupCode).IsUnique().HasDatabaseName("UQ_Invoices_TaxLookupCode");
            });
        }
    }
}