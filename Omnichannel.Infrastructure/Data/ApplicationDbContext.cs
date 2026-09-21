using System;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // 1. PhÃ¢n há»‡ Äá»‹nh danh & Báº£o máº­t
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<User> Users => Set<User>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

        // 2. PhÃ¢n há»‡ CMS & Navigation
        public DbSet<Menu> Menus => Set<Menu>();
        public DbSet<MenuItem> MenuItems => Set<MenuItem>();
        public DbSet<Banner> Banners => Set<Banner>();
        public DbSet<PostCategory> PostCategories => Set<PostCategory>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<StaticPage> StaticPages => Set<StaticPage>();

        // 3. PhÃ¢n há»‡ HÃ ng hÃ³a & Kho FEFO
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductBatch> ProductBatches => Set<ProductBatch>();
        public DbSet<Store> Stores => Set<Store>();
        public DbSet<StoreInventory> StoreInventories => Set<StoreInventory>();
        public DbSet<InventoryLog> InventoryLogs => Set<InventoryLog>();

        // 4. PhÃ¢n há»‡ BÃ¡n láº» & TÃ i chÃ­nh
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
        public DbSet<Invoice> Invoices => Set<Invoice>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================================================================
            // 1. Cáº¤U HÃŒNH PHÃ‚N Há»† Äá»ŠNH DANH & Báº¢O Máº¬T (IDENTITY & SECURITY)
            // =========================================================================
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique().HasDatabaseName("IX_Users_Username");
                entity.HasIndex(u => u.Email).IsUnique().HasDatabaseName("IX_Users_Email");

                entity.HasOne(u => u.Store)
                      .WithMany(s => s.Users)
                      .HasForeignKey(u => u.StoreId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasIndex(r => r.Name).IsUnique().HasDatabaseName("IX_Roles_Name");
            });

            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasIndex(p => p.Code).IsUnique().HasDatabaseName("IX_Permissions_Code");
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });

                entity.HasOne(ur => ur.User)
                      .WithMany(u => u.UserRoles)
                      .HasForeignKey(ur => ur.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ur => ur.Role)
                      .WithMany(r => r.UserRoles)
                      .HasForeignKey(ur => ur.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(rp => new { rp.RoleId, rp.PermissionId });

                entity.HasOne(rp => rp.Role)
                      .WithMany(r => r.RolePermissions)
                      .HasForeignKey(rp => rp.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rp => rp.Permission)
                      .WithMany(p => p.RolePermissions)
                      .HasForeignKey(rp => rp.PermissionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserPermission>(entity =>
            {
                entity.HasKey(up => new { up.UserId, up.PermissionId });

                entity.HasOne(up => up.User)
                      .WithMany(u => u.UserPermissions)
                      .HasForeignKey(up => up.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(up => up.Permission)
                      .WithMany(p => p.UserPermissions)
                      .HasForeignKey(up => up.PermissionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // =========================================================================
            // 2. Cáº¤U HÃŒNH PHÃ‚N Há»† CMS & NAVIGATION
            // =========================================================================
            modelBuilder.Entity<Menu>(entity =>
            {
                entity.HasIndex(m => m.Code).IsUnique().HasDatabaseName("IX_Menus_Code");
            });

            modelBuilder.Entity<MenuItem>(entity =>
            {
                entity.HasOne(mi => mi.Menu)
                      .WithMany(m => m.MenuItems)
                      .HasForeignKey(mi => mi.MenuId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mi => mi.Parent)
                      .WithMany(mi => mi.Children)
                      .HasForeignKey(mi => mi.ParentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PostCategory>(entity =>
            {
                entity.HasIndex(pc => pc.Slug).IsUnique().HasDatabaseName("IX_PostCategories_Slug");
            });

            modelBuilder.Entity<Post>(entity =>
            {
                entity.HasIndex(p => p.Slug).IsUnique().HasDatabaseName("IX_Posts_Slug");

                entity.HasOne(p => p.PostCategory)
                      .WithMany(pc => pc.Posts)
                      .HasForeignKey(p => p.PostCategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Author)
                      .WithMany(u => u.Posts)
                      .HasForeignKey(p => p.AuthorId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<StaticPage>(entity =>
            {
                entity.HasIndex(sp => sp.Slug).IsUnique().HasDatabaseName("IX_StaticPages_Slug");
            });

            // =========================================================================
            // 3. Cáº¤U HÃŒNH PHÃ‚N Há»† HÃ€NG HÃ“A & KHO FEFO (INVENTORY & WAREHOUSE)
            // =========================================================================
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("IX_Categories_Slug");

                entity.HasOne(c => c.Parent)
                      .WithMany(c => c.SubCategories)
                      .HasForeignKey(c => c.ParentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.HasIndex(s => s.Code).IsUnique().HasDatabaseName("IX_Suppliers_Code");
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasIndex(p => p.Sku).IsUnique().HasDatabaseName("IX_Products_Sku");
                entity.HasIndex(p => p.Barcode).IsUnique().HasDatabaseName("IX_Products_Barcode");
                entity.HasIndex(p => p.Slug).IsUnique().HasDatabaseName("IX_Products_Slug");
                entity.HasIndex(p => new { p.CategoryId, p.IsActive }).HasDatabaseName("IX_Products_Category_Active");

                // Check constraint: SellingPrice >= 0
                entity.ToTable(t => t.HasCheckConstraint("CK_Products_SellingPrice_NonNegative", "[SellingPrice] >= 0"));

                entity.HasOne(p => p.Category)
                      .WithMany(c => c.Products)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Supplier)
                      .WithMany(s => s.Products)
                      .HasForeignKey(p => p.SupplierId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProductBatch>(entity =>
            {
                // Check constraint: ExpDate > ManufacturingDate
                entity.ToTable(t => t.HasCheckConstraint("CK_ProductBatches_ExpDate_ManufacturingDate", "[ExpDate] > [ManufacturingDate]"));

                // Non-clustered Index FEFO: Sáº¯p xáº¿p ExpDate ASC Ä‘á»ƒ truy xuáº¥t lÃ´ cáº­n date nhanh nháº¥t
                entity.HasIndex(pb => new { pb.ProductId, pb.ExpDate, pb.Status })
                      .HasDatabaseName("IX_ProductBatches_FEFO_Lookup");

                entity.HasIndex(pb => pb.BatchNumber).HasDatabaseName("IX_ProductBatches_BatchNumber");

                entity.HasOne(pb => pb.Product)
                      .WithMany(p => p.Batches)
                      .HasForeignKey(pb => pb.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Store>(entity =>
            {
                entity.HasIndex(s => s.Code).IsUnique().HasDatabaseName("IX_Stores_Code");
            });

            modelBuilder.Entity<StoreInventory>(entity =>
            {
                entity.HasIndex(si => new { si.StoreId, si.ProductBatchId })
                      .IsUnique()
                      .HasDatabaseName("IX_StoreInventories_Store_Batch");

                // Check constraint: PhysicalQuantity >= ReservedQuantity
                entity.ToTable(t => t.HasCheckConstraint("CK_StoreInventories_Physical_Reserved", "[PhysicalQuantity] >= [ReservedQuantity]"));

                entity.HasOne(si => si.Store)
                      .WithMany(s => s.StoreInventories)
                      .HasForeignKey(si => si.StoreId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(si => si.ProductBatch)
                      .WithMany(pb => pb.StoreInventories)
                      .HasForeignKey(si => si.ProductBatchId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<InventoryLog>(entity =>
            {
                // Non-clustered Index phá»¥c vá»¥ Audit Logs theo thá»i gian
                entity.HasIndex(il => new { il.StoreId, il.CreatedAt })
                      .HasDatabaseName("IX_InventoryLogs_Store_Created");

                entity.HasIndex(il => new { il.ProductBatchId, il.CreatedAt })
                      .HasDatabaseName("IX_InventoryLogs_Batch_Created");

                entity.HasOne(il => il.Store)
                      .WithMany(s => s.InventoryLogs)
                      .HasForeignKey(il => il.StoreId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(il => il.ProductBatch)
                      .WithMany(pb => pb.InventoryLogs)
                      .HasForeignKey(il => il.ProductBatchId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(il => il.User)
                      .WithMany(u => u.InventoryLogs)
                      .HasForeignKey(il => il.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // =========================================================================
            // 4. Cáº¤U HÃŒNH PHÃ‚N Há»† BÃN Láºº & TÃ€I CHÃNH (SALES & FINANCE)
            // =========================================================================
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasIndex(c => c.CartSessionId).HasDatabaseName("IX_Carts_CartSessionId");

                entity.HasOne(c => c.User)
                      .WithMany()
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasOne(ci => ci.Cart)
                      .WithMany(c => c.CartItems)
                      .HasForeignKey(ci => ci.CartId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ci => ci.Product)
                      .WithMany(p => p.CartItems)
                      .HasForeignKey(ci => ci.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasIndex(o => o.OrderCode).IsUnique().HasDatabaseName("IX_Orders_OrderCode");
                entity.HasIndex(o => new { o.StoreId, o.CreatedAt }).HasDatabaseName("IX_Orders_Store_Created");
                entity.HasIndex(o => new { o.OrderStatus, o.PaymentStatus }).HasDatabaseName("IX_Orders_Status");

                // NgÄƒn cháº·n Multiple Cascade Paths trong SQL Server
                entity.HasOne(o => o.User)
                      .WithMany(u => u.Orders)
                      .HasForeignKey(o => o.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Store)
                      .WithMany(s => s.Orders)
                      .HasForeignKey(o => o.StoreId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<OrderDetail>(entity =>
            {
                entity.HasOne(od => od.Order)
                      .WithMany(o => o.OrderDetails)
                      .HasForeignKey(od => od.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(od => od.Product)
                      .WithMany(p => p.OrderDetails)
                      .HasForeignKey(od => od.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(od => od.ProductBatch)
                      .WithMany(pb => pb.OrderDetails)
                      .HasForeignKey(od => od.ProductBatchId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasIndex(i => i.InvoiceCode).IsUnique().HasDatabaseName("IX_Invoices_InvoiceCode");

                entity.HasOne(i => i.Order)
                      .WithOne(o => o.Invoice)
                      .HasForeignKey<Invoice>(i => i.OrderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(i => i.Cashier)
                      .WithMany()
                      .HasForeignKey(i => i.CashierId)
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
