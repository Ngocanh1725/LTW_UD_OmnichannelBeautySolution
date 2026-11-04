using System;
using Microsoft.EntityFrameworkCore;
using Omnichannel.Domain.Entities;

namespace Omnichannel.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor? _httpContextAccessor;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            Microsoft.AspNetCore.Http.IHttpContextAccessor? httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
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
        public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
        public DbSet<ReviewHelpfulVote> ReviewHelpfulVotes => Set<ReviewHelpfulVote>();
        public DbSet<ProductQuestion> ProductQuestions => Set<ProductQuestion>();
        public DbSet<ProductAnswer> ProductAnswers => Set<ProductAnswer>();
        public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
        public DbSet<ThemeConfig> ThemeConfigs => Set<ThemeConfig>();
        public DbSet<UiSection> UiSections => Set<UiSection>();
        public DbSet<PageLayout> PageLayouts => Set<PageLayout>();
        public DbSet<LayoutSection> LayoutSections => Set<LayoutSection>();
        public DbSet<SectionItem> SectionItems => Set<SectionItem>();

        // 3. Phân hệ Hàng hóa & Kho FEFO
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Brand> Brands => Set<Brand>();
        public DbSet<Supplier> Suppliers => Set<Supplier>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductBatch> ProductBatches => Set<ProductBatch>();
        public DbSet<Store> Stores => Set<Store>();
        public DbSet<StoreInventory> StoreInventories => Set<StoreInventory>();
        public DbSet<InventoryLog> InventoryLogs => Set<InventoryLog>();
        public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
        public DbSet<ProductSwatch> ProductSwatches => Set<ProductSwatch>();
        public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
        public DbSet<ProductAttributeValue> ProductAttributeValues => Set<ProductAttributeValue>();
        public DbSet<ProductVariantAttributeValue> ProductVariantAttributeValues => Set<ProductVariantAttributeValue>();
        public DbSet<ActiveIngredient> ActiveIngredients => Set<ActiveIngredient>();
        public DbSet<ProductIngredient> ProductIngredients => Set<ProductIngredient>();
        public DbSet<SkinType> SkinTypes => Set<SkinType>();
        public DbSet<ProductSkinType> ProductSkinTypes => Set<ProductSkinType>();
        public DbSet<SkinConcern> SkinConcerns => Set<SkinConcern>();
        public DbSet<ProductConcern> ProductConcerns => Set<ProductConcern>();

        // 4. PhÃ¢n há»‡ BÃ¡n láº» & TÃ i chÃ­nh
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
        public DbSet<OrderReturnRequest> OrderReturnRequests => Set<OrderReturnRequest>();

        // 5. Phân hệ Nhật ký & Kiểm toán (Audit Log)
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        // 6. Phân hệ Quản lý Ca bán hàng POS (Shift Management)
        public DbSet<PosShift> PosShifts => Set<PosShift>();
        public DbSet<PosDrawerTransaction> PosDrawerTransactions => Set<PosDrawerTransaction>();

        // 7. Phân hệ CRM & Hồ sơ sắc đẹp (Beauty Profile)
        public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
        public DbSet<SkinHistory> SkinHistories => Set<SkinHistory>();
        public DbSet<RepurchaseSchedule> RepurchaseSchedules => Set<RepurchaseSchedule>();

        // 8. Phân hệ Ví điểm thành viên & Thăng hạng (Loyalty & Tier)
        public DbSet<MemberTier> MemberTiers => Set<MemberTier>();
        public DbSet<LoyaltyWallet> LoyaltyWallets => Set<LoyaltyWallet>();
        public DbSet<PointTransaction> PointTransactions => Set<PointTransaction>();
        public DbSet<Voucher> Vouchers => Set<Voucher>();
        public DbSet<LoyaltyCheckIn> LoyaltyCheckIns => Set<LoyaltyCheckIn>();
        public DbSet<LoyaltyReferral> LoyaltyReferrals => Set<LoyaltyReferral>();

        // 9. Phân hệ Khuyến mãi & Coupon (Promotion Engine)
        public DbSet<Promotion> Promotions => Set<Promotion>();
        public DbSet<PromotionItem> PromotionItems => Set<PromotionItem>();
        public DbSet<Coupon> Coupons => Set<Coupon>();
        public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();

        // 10. Phân hệ Điều chuyển kho liên chi nhánh (Stock Transfer)
        public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
        public DbSet<StockTransferDetail> StockTransferDetails => Set<StockTransferDetail>();

        // 11. Phân hệ Cài đặt động & SEO Redirects
        public DbSet<Setting> Settings => Set<Setting>();
        public DbSet<SlugRedirect> SlugRedirects => Set<SlugRedirect>();

        // 12. Phân hệ Tài chính, Sổ cái & Công nợ (Finance & Ledger)
        public DbSet<FinancialLedger> FinancialLedgers => Set<FinancialLedger>();
        public DbSet<SupplierDebt> SupplierDebts => Set<SupplierDebt>();
        public DbSet<CodReconciliation> CodReconciliations => Set<CodReconciliation>();

        // 13. Phân hệ Chatbot AI & Hỗ trợ khách hàng (Customer Service AI Chatbot)
        public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

        // 14. Phân hệ Cảnh báo Thông minh (AI Alert System)
        public DbSet<Alert> Alerts => Set<Alert>();

        // 15. Phân hệ Thông Báo Realtime (Realtime Notifications)
        public DbSet<Notification> Notifications => Set<Notification>();

        // 16. Phân hệ Hộp Thư Nội Bộ (Internal Messages)
        public DbSet<InternalMessage> InternalMessages => Set<InternalMessage>();

        // 17. Phân hệ Chăm sóc khách hàng & Giữ chân (Wishlist & Back-In-Stock Alerts)
        public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
        public DbSet<StockAlert> StockAlerts => Set<StockAlert>();
        public DbSet<ProductSubscription> ProductSubscriptions => Set<ProductSubscription>();

        // 18. Phân hệ Outbox & Chống trùng lặp (Transactional Outbox & Idempotency)
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

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

                entity.Property(mi => mi.LinkType).HasMaxLength(20).HasDefaultValue("CUSTOM");
            });

            modelBuilder.Entity<SlugRedirect>(entity =>
            {
                entity.HasIndex(sr => new { sr.EntityType, sr.OldSlug }).IsUnique().HasDatabaseName("IX_SlugRedirects_EntityType_OldSlug");
            });

            modelBuilder.Entity<Setting>(entity =>
            {
                entity.HasIndex(s => s.SettingKey).IsUnique().HasDatabaseName("IX_Settings_SettingKey");

                entity.HasOne(s => s.UpdatedByUser)
                      .WithMany()
                      .HasForeignKey(s => s.UpdatedBy)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // =========================================================================
            // PHÂN HỆ TÀI CHÍNH, SỔ CÁI & CÔNG NỢ (FINANCE & LEDGER)
            // =========================================================================
            modelBuilder.Entity<FinancialLedger>(entity =>
            {
                entity.HasIndex(fl => fl.LedgerCode).IsUnique().HasDatabaseName("IX_FinancialLedgers_LedgerCode");
                entity.HasIndex(fl => fl.EntryDate).HasDatabaseName("IX_FinancialLedgers_EntryDate");

                entity.HasOne(fl => fl.Store)
                      .WithMany(s => s.FinancialLedgers)
                      .HasForeignKey(fl => fl.StoreId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(fl => fl.Order)
                      .WithMany()
                      .HasForeignKey(fl => fl.OrderId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(fl => fl.Supplier)
                      .WithMany(s => s.FinancialLedgers)
                      .HasForeignKey(fl => fl.SupplierId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(fl => fl.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(fl => fl.CreatedBy)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<SupplierDebt>(entity =>
            {
                entity.HasIndex(sd => sd.PurchaseOrderCode).HasDatabaseName("IX_SupplierDebts_PurchaseOrderCode");

                entity.HasOne(sd => sd.Supplier)
                      .WithMany(s => s.SupplierDebts)
                      .HasForeignKey(sd => sd.SupplierId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<CodReconciliation>(entity =>
            {
                entity.HasIndex(cr => cr.ReconciliationCode).IsUnique().HasDatabaseName("IX_CodReconciliations_ReconciliationCode");
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

            modelBuilder.Entity<ProductReview>(entity =>
            {
                entity.HasIndex(r => new { r.ProductId, r.IsApproved, r.CreatedAt })
                      .HasDatabaseName("IX_ProductReviews_Product_Approved_Created");

                entity.ToTable(t => t.HasCheckConstraint("CK_ProductReviews_Rating_Range", "[Rating] >= 1 AND [Rating] <= 5"));

                entity.HasOne(r => r.Product)
                      .WithMany(p => p.Reviews)
                      .HasForeignKey(r => r.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.User)
                      .WithMany()
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(r => r.Order)
                      .WithMany()
                      .HasForeignKey(r => r.OrderId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ReviewHelpfulVote>(entity =>
            {
                entity.HasIndex(v => new { v.ReviewId, v.UserId })
                      .HasFilter("[UserId] IS NOT NULL")
                      .IsUnique()
                      .HasDatabaseName("IX_ReviewHelpfulVotes_Review_User");

                entity.HasIndex(v => new { v.ReviewId, v.AnonymousKey })
                      .HasFilter("[AnonymousKey] IS NOT NULL")
                      .IsUnique()
                      .HasDatabaseName("IX_ReviewHelpfulVotes_Review_AnonymousKey");

                entity.HasOne(v => v.Review)
                      .WithMany(r => r.HelpfulVotes)
                      .HasForeignKey(v => v.ReviewId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(v => v.User)
                      .WithMany()
                      .HasForeignKey(v => v.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ProductQuestion>(entity =>
            {
                entity.HasIndex(q => new { q.ProductId, q.IsApproved, q.CreatedAt })
                      .HasDatabaseName("IX_ProductQuestions_Product_Approved_Created");

                entity.HasOne(q => q.Product)
                      .WithMany(p => p.Questions)
                      .HasForeignKey(q => q.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(q => q.User)
                      .WithMany()
                      .HasForeignKey(q => q.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ProductSubscription>(entity =>
            {
                entity.HasIndex(s => new { s.ProductId, s.Status, s.Type })
                      .HasDatabaseName("IX_ProductSubscriptions_Product_Status_Type");

                entity.HasIndex(s => new { s.UserId, s.ProductId, s.Type, s.Status })
                      .HasFilter("[UserId] IS NOT NULL")
                      .HasDatabaseName("IX_ProductSubscriptions_User_Product_Type_Status");

                entity.HasOne(s => s.Product)
                      .WithMany(p => p.Subscriptions)
                      .HasForeignKey(s => s.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.User)
                      .WithMany()
                      .HasForeignKey(s => s.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ProductAnswer>(entity =>
            {
                entity.HasIndex(a => new { a.QuestionId, a.IsApproved, a.CreatedAt })
                      .HasDatabaseName("IX_ProductAnswers_Question_Approved_Created");

                entity.HasOne(a => a.Question)
                      .WithMany(q => q.Answers)
                      .HasForeignKey(a => a.QuestionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.User)
                      .WithMany()
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
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
                entity.HasIndex(p => new { p.IsActive, p.IsFeatured }).HasDatabaseName("IX_Products_IsActive_IsFeatured");
                entity.HasIndex(p => p.BrandId).HasDatabaseName("IX_Products_BrandId");

                // Check constraints: SellingPrice >= 0, CostPrice >= 0
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_Products_SellingPrice_NonNegative", "[SellingPrice] >= 0");
                    t.HasCheckConstraint("CK_Products_CostPrice_NonNegative", "[CostPrice] >= 0");
                });

                entity.HasOne(p => p.Category)
                      .WithMany(c => c.Products)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Brand)
                      .WithMany()
                      .HasForeignKey(p => p.BrandId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(p => p.Supplier)
                      .WithMany(s => s.Products)
                      .HasForeignKey(p => p.SupplierId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProductVariant>(entity =>
            {
                entity.HasIndex(v => v.Sku).IsUnique().HasDatabaseName("IX_ProductVariants_Sku");
                entity.HasIndex(v => v.Barcode).IsUnique().HasDatabaseName("IX_ProductVariants_Barcode");
                entity.HasIndex(v => new { v.ProductId, v.IsActive }).HasDatabaseName("IX_ProductVariants_Product_Active");

                entity.ToTable(t => t.HasCheckConstraint("CK_ProductVariants_SellingPrice_NonNegative", "[SellingPrice] >= 0"));

                entity.HasOne(v => v.Product)
                      .WithMany(p => p.Variants)
                      .HasForeignKey(v => v.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductSwatch>(entity =>
            {
                entity.HasIndex(s => s.ProductVariantId).HasDatabaseName("IX_ProductSwatches_VariantId");

                entity.HasOne(s => s.ProductVariant)
                      .WithMany(v => v.Swatches)
                      .HasForeignKey(s => s.ProductVariantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductBatch>(entity =>
            {
                entity.HasIndex(b => new { b.ProductId, b.ExpDate }).HasDatabaseName("IX_ProductBatches_ProductId_ExpDate");

                entity.HasOne(b => b.ProductVariant)
                      .WithMany(v => v.Batches)
                      .HasForeignKey(b => b.ProductVariantId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ProductAttribute>(entity =>
            {
                entity.HasIndex(a => a.Code).IsUnique().HasDatabaseName("IX_ProductAttributes_Code");
            });

            modelBuilder.Entity<ProductAttributeValue>(entity =>
            {
                entity.HasOne(v => v.ProductAttribute)
                      .WithMany(a => a.Values)
                      .HasForeignKey(v => v.ProductAttributeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductVariantAttributeValue>(entity =>
            {
                entity.HasKey(pvav => new { pvav.ProductVariantId, pvav.ProductAttributeValueId });

                entity.HasOne(pvav => pvav.ProductVariant)
                      .WithMany(pv => pv.AttributeValues)
                      .HasForeignKey(pvav => pvav.ProductVariantId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pvav => pvav.ProductAttributeValue)
                      .WithMany(pav => pav.VariantMappings)
                      .HasForeignKey(pvav => pvav.ProductAttributeValueId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ActiveIngredient>(entity =>
            {
                entity.HasIndex(i => i.Slug).IsUnique().HasDatabaseName("IX_ActiveIngredients_Slug");
            });

            modelBuilder.Entity<ProductIngredient>(entity =>
            {
                entity.HasKey(pi => new { pi.ProductId, pi.ActiveIngredientId });

                entity.HasOne(pi => pi.Product)
                      .WithMany(p => p.ProductIngredients)
                      .HasForeignKey(pi => pi.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pi => pi.ActiveIngredient)
                      .WithMany(i => i.ProductIngredients)
                      .HasForeignKey(pi => pi.ActiveIngredientId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SkinType>(entity =>
            {
                entity.HasIndex(st => st.Slug).IsUnique().HasDatabaseName("IX_SkinTypes_Slug");
            });

            modelBuilder.Entity<ProductSkinType>(entity =>
            {
                entity.HasKey(pst => new { pst.ProductId, pst.SkinTypeId });

                entity.HasOne(pst => pst.Product)
                      .WithMany(p => p.ProductSkinTypes)
                      .HasForeignKey(pst => pst.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pst => pst.SkinType)
                      .WithMany(st => st.ProductSkinTypes)
                      .HasForeignKey(pst => pst.SkinTypeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SkinConcern>(entity =>
            {
                entity.HasIndex(sc => sc.Slug).IsUnique().HasDatabaseName("IX_SkinConcerns_Slug");
            });

            modelBuilder.Entity<ProductConcern>(entity =>
            {
                entity.HasKey(pc => new { pc.ProductId, pc.SkinConcernId });

                entity.HasOne(pc => pc.Product)
                      .WithMany(p => p.ProductConcerns)
                      .HasForeignKey(pc => pc.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pc => pc.SkinConcern)
                      .WithMany(sc => sc.ProductConcerns)
                      .HasForeignKey(pc => pc.SkinConcernId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProductBatch>(entity =>
            {
                // Check constraint: ExpDate > ManufacturingDate
                entity.ToTable(t => t.HasCheckConstraint("CK_ProductBatches_ExpDate_ManufacturingDate", "[ExpDate] > [ManufacturingDate]"));

                // Non-clustered Index FEFO: Sáº¯p xáº¿p ExpDate ASC Ä‘á»ƒ truy xuáº¥t lÃ´ cáº­n date nhanh nháº¥t
                entity.HasIndex(pb => new { pb.ProductId, pb.ExpDate, pb.Status })
                      .HasDatabaseName("IX_ProductBatches_FEFO_Lookup");

                entity.HasIndex(pb => pb.BatchNumber).HasDatabaseName("IX_ProductBatches_BatchNumber");
                entity.HasIndex(pb => new { pb.ProductId, pb.ExpDate }).HasDatabaseName("IX_ProductBatches_ProductId_ExpDate");

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

                // Check constraints: PhysicalQuantity >= ReservedQuantity, PhysicalQuantity >= 0, ReservedQuantity >= 0
                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_StoreInventories_Physical_Reserved", "[PhysicalQuantity] >= [ReservedQuantity]");
                    t.HasCheckConstraint("CK_StoreInventories_Physical_NonNegative", "[PhysicalQuantity] >= 0");
                    t.HasCheckConstraint("CK_StoreInventories_Reserved_NonNegative", "[ReservedQuantity] >= 0");
                });

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
                entity.HasIndex(o => o.CreatedAt).HasDatabaseName("IX_Orders_CreatedAt");
                entity.HasIndex(o => o.UserId).HasDatabaseName("IX_Orders_UserId");
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

                entity.HasOne(o => o.PickupStore)
                      .WithMany()
                      .HasForeignKey(o => o.PickupStoreId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.Property(o => o.DeliveryMethod)
                      .HasMaxLength(30)
                      .HasDefaultValue("HOME_DELIVERY");

                entity.HasIndex(o => new { o.DeliveryMethod, o.PickupStoreId, o.OrderStatus })
                      .HasDatabaseName("IX_Orders_PickupStore_Status");

                entity.HasIndex(o => o.PickupCode)
                      .HasFilter("[PickupCode] IS NOT NULL")
                      .HasDatabaseName("IX_Orders_PickupCode");

                entity.HasOne(o => o.PosShift)
                      .WithMany(ps => ps.Orders)
                      .HasForeignKey(o => o.PosShiftId)
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

            modelBuilder.Entity<OrderStatusHistory>(entity =>
            {
                entity.HasIndex(osh => new { osh.OrderId, osh.CreatedAt })
                      .HasDatabaseName("IX_OrderStatusHistories_OrderId_CreatedAt");

                entity.HasOne(osh => osh.Order)
                      .WithMany(o => o.StatusHistories)
                      .HasForeignKey(osh => osh.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderReturnRequest>(entity =>
            {
                entity.HasIndex(orr => orr.RequestCode)
                      .IsUnique()
                      .HasDatabaseName("IX_OrderReturnRequests_RequestCode");

                entity.HasIndex(orr => new { orr.OrderId, orr.Status })
                      .HasDatabaseName("IX_OrderReturnRequests_OrderId_Status");

                entity.HasOne(orr => orr.Order)
                      .WithMany(o => o.ReturnRequests)
                      .HasForeignKey(orr => orr.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(orr => orr.User)
                      .WithMany()
                      .HasForeignKey(orr => orr.UserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // =========================================================================
            // 5. CẤU HÌNH NHẬT KÝ & KIỂM TOÁN (AUDIT LOGS)
            // =========================================================================
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(a => new { a.UserId, a.CreatedAt }).HasDatabaseName("IX_AuditLogs_UserId_CreatedAt");
                entity.HasIndex(a => new { a.EntityType, a.EntityId }).HasDatabaseName("IX_AuditLogs_EntityType_EntityId");
            });

            // =========================================================================
            // 6. CẤU HÌNH QUẢN LÝ CA BÁN HÀNG POS (POS SHIFTS & DRAWER)
            // =========================================================================
            modelBuilder.Entity<PosShift>(entity =>
            {
                entity.HasIndex(s => s.ShiftCode).IsUnique().HasDatabaseName("IX_PosShifts_ShiftCode");
                entity.HasIndex(s => new { s.StoreId, s.OpenedAt }).HasDatabaseName("IX_PosShifts_Store_Opened");

                entity.HasOne(s => s.Store)
                      .WithMany()
                      .HasForeignKey(s => s.StoreId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.Cashier)
                      .WithMany()
                      .HasForeignKey(s => s.CashierId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<PosDrawerTransaction>(entity =>
            {
                entity.HasIndex(t => new { t.PosShiftId, t.CreatedAt }).HasDatabaseName("IX_PosDrawerTransactions_Shift_Created");

                entity.HasOne(t => t.PosShift)
                      .WithMany(s => s.DrawerTransactions)
                      .HasForeignKey(t => t.PosShiftId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(t => t.Performer)
                      .WithMany()
                      .HasForeignKey(t => t.PerformedBy)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =========================================================================
            // 7. CẤU HÌNH CRM & HỒ SƠ SẮC ĐẸP (CUSTOMER PROFILE & SKIN HISTORY)
            // =========================================================================
            modelBuilder.Entity<CustomerProfile>(entity =>
            {
                entity.HasIndex(cp => cp.UserId).IsUnique().HasDatabaseName("IX_CustomerProfiles_UserId");

                entity.HasOne(cp => cp.User)
                      .WithOne(u => u.CustomerProfile)
                      .HasForeignKey<CustomerProfile>(cp => cp.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(cp => cp.Consultant)
                      .WithMany()
                      .HasForeignKey(cp => cp.ConsultantUserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<SkinHistory>(entity =>
            {
                entity.HasIndex(sh => new { sh.CustomerProfileId, sh.RecordedAt })
                      .HasDatabaseName("IX_SkinHistories_Profile_Recorded");

                entity.HasOne(sh => sh.CustomerProfile)
                      .WithMany(cp => cp.SkinHistories)
                      .HasForeignKey(sh => sh.CustomerProfileId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(sh => sh.RecordedByUser)
                      .WithMany()
                      .HasForeignKey(sh => sh.RecordedBy)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<RepurchaseSchedule>(entity =>
            {
                entity.HasIndex(rs => new { rs.CustomerProfileId, rs.EstimatedDepletionDate })
                      .HasDatabaseName("IX_RepurchaseSchedules_Profile_Depletion");

                entity.HasOne(rs => rs.CustomerProfile)
                      .WithMany(cp => cp.RepurchaseSchedules)
                      .HasForeignKey(rs => rs.CustomerProfileId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rs => rs.Product)
                      .WithMany()
                      .HasForeignKey(rs => rs.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // =========================================================================
            // 8. CẤU HÌNH PHÂN HỆ VÍ ĐIỂM THÀNH VIÊN & THĂNG HẠNG (LOYALTY & TIER)
            // =========================================================================
            modelBuilder.Entity<MemberTier>(entity =>
            {
                entity.HasIndex(mt => mt.TierName).IsUnique().HasDatabaseName("IX_MemberTiers_TierName");
                entity.Property(mt => mt.MinSpend).HasColumnType("decimal(18,2)");
                entity.Property(mt => mt.MaxSpend).HasColumnType("decimal(18,2)");
                entity.Property(mt => mt.PointMultiplier).HasColumnType("decimal(5,2)");
            });

            modelBuilder.Entity<LoyaltyWallet>(entity =>
            {
                entity.HasIndex(lw => lw.UserId).IsUnique().HasDatabaseName("IX_LoyaltyWallets_UserId");
                entity.HasIndex(lw => lw.ReferralCode)
                      .HasFilter("[ReferralCode] IS NOT NULL")
                      .IsUnique()
                      .HasDatabaseName("IX_LoyaltyWallets_ReferralCode");

                entity.HasOne(lw => lw.User)
                      .WithOne(u => u.LoyaltyWallet)
                      .HasForeignKey<LoyaltyWallet>(lw => lw.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(lw => lw.CurrentTier)
                      .WithMany(mt => mt.Wallets)
                      .HasForeignKey(lw => lw.CurrentTierId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(lw => lw.TotalSpend12Months).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<LoyaltyCheckIn>(entity =>
            {
                entity.HasIndex(ci => new { ci.UserId, ci.CheckInDate })
                      .IsUnique()
                      .HasDatabaseName("IX_LoyaltyCheckIns_UserId_CheckInDate");

                entity.HasOne(ci => ci.User)
                      .WithMany()
                      .HasForeignKey(ci => ci.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ci => ci.Wallet)
                      .WithMany(lw => lw.CheckIns)
                      .HasForeignKey(ci => ci.WalletId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ci => ci.PointTransaction)
                      .WithMany()
                      .HasForeignKey(ci => ci.PointTransactionId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<LoyaltyReferral>(entity =>
            {
                entity.HasIndex(r => r.ReferredUserId)
                      .IsUnique()
                      .HasDatabaseName("IX_LoyaltyReferrals_ReferredUserId");

                entity.HasIndex(r => new { r.ReferrerUserId, r.Status })
                      .HasDatabaseName("IX_LoyaltyReferrals_ReferrerUserId_Status");

                entity.HasOne(r => r.ReferrerUser)
                      .WithMany()
                      .HasForeignKey(r => r.ReferrerUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.ReferredUser)
                      .WithMany()
                      .HasForeignKey(r => r.ReferredUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.CompletedOrder)
                      .WithMany()
                      .HasForeignKey(r => r.CompletedOrderId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<PointTransaction>(entity =>
            {
                entity.HasIndex(pt => new { pt.WalletId, pt.CreatedAt }).HasDatabaseName("IX_PointTransactions_Wallet_Created");

                entity.HasOne(pt => pt.Wallet)
                      .WithMany(lw => lw.Transactions)
                      .HasForeignKey(pt => pt.WalletId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pt => pt.Order)
                      .WithMany(o => o.PointTransactions)
                      .HasForeignKey(pt => pt.OrderId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Voucher>(entity =>
            {
                entity.HasIndex(v => v.VoucherCode).IsUnique().HasDatabaseName("IX_Vouchers_VoucherCode");

                entity.HasOne(v => v.User)
                      .WithMany(u => u.Vouchers)
                      .HasForeignKey(v => v.UserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(v => v.UsedOrder)
                      .WithMany(o => o.UsedVouchers)
                      .HasForeignKey(v => v.UsedOrderId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.Property(v => v.DiscountValue).HasColumnType("decimal(18,2)");
                entity.Property(v => v.MinOrderValue).HasColumnType("decimal(18,2)");
                entity.Property(v => v.MaxDiscountAmount).HasColumnType("decimal(18,2)");
            });

            // =========================================================================
            // 9. CẤU HÌNH PHÂN HỆ KHUYẾN MÃI & COUPON (PROMOTION ENGINE)
            // =========================================================================
            modelBuilder.Entity<Promotion>(entity =>
            {
                entity.HasIndex(p => p.PromotionCode).IsUnique().HasDatabaseName("IX_Promotions_PromotionCode");
                entity.Property(p => p.DiscountValue).HasColumnType("decimal(18,2)");
                entity.Property(p => p.MinOrderValue).HasColumnType("decimal(18,2)");
                entity.Property(p => p.MaxDiscountAmount).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<PromotionItem>(entity =>
            {
                entity.HasOne(pi => pi.Promotion)
                      .WithMany(p => p.PromotionItems)
                      .HasForeignKey(pi => pi.PromotionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pi => pi.Product)
                      .WithMany()
                      .HasForeignKey(pi => pi.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(pi => pi.Category)
                      .WithMany()
                      .HasForeignKey(pi => pi.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(pi => pi.GiftProduct)
                      .WithMany()
                      .HasForeignKey(pi => pi.GiftProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Coupon>(entity =>
            {
                entity.HasIndex(c => c.CouponCode).IsUnique().HasDatabaseName("IX_Coupons_CouponCode");
                entity.Property(c => c.DiscountValue).HasColumnType("decimal(18,2)");
                entity.Property(c => c.MinOrderValue).HasColumnType("decimal(18,2)");
                entity.Property(c => c.MaxDiscountAmount).HasColumnType("decimal(18,2)");
            });

            modelBuilder.Entity<CouponUsage>(entity =>
            {
                entity.HasOne(cu => cu.Coupon)
                      .WithMany(c => c.Usages)
                      .HasForeignKey(cu => cu.CouponId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cu => cu.User)
                      .WithMany(u => u.CouponUsages)
                      .HasForeignKey(cu => cu.UserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(cu => cu.Order)
                      .WithMany(o => o.CouponUsages)
                      .HasForeignKey(cu => cu.OrderId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.Property(cu => cu.DiscountApplied).HasColumnType("decimal(18,2)");
            });

            // =========================================================================
            // 10. CẤU HÌNH PHÂN HỆ ĐIỀU CHUYỂN KHO LIÊN CHI NHÁNH (STOCK TRANSFER)
            // =========================================================================
            modelBuilder.Entity<StockTransfer>(entity =>
            {
                entity.HasIndex(st => st.TransferCode).IsUnique().HasDatabaseName("IX_StockTransfers_TransferCode");

                entity.HasOne(st => st.SourceStore)
                      .WithMany(s => s.OutgoingTransfers)
                      .HasForeignKey(st => st.SourceStoreId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(st => st.DestinationStore)
                      .WithMany(s => s.IncomingTransfers)
                      .HasForeignKey(st => st.DestinationStoreId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(st => st.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(st => st.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(st => st.ApprovedByUser)
                      .WithMany()
                      .HasForeignKey(st => st.ApprovedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(st => st.ReceivedByUser)
                      .WithMany()
                      .HasForeignKey(st => st.ReceivedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StockTransferDetail>(entity =>
            {
                entity.HasOne(std => std.StockTransfer)
                      .WithMany(st => st.Details)
                      .HasForeignKey(std => std.StockTransferId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(std => std.Product)
                      .WithMany()
                      .HasForeignKey(std => std.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(std => std.ProductBatch)
                      .WithMany()
                      .HasForeignKey(std => std.ProductBatchId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 14. Phân hệ Cảnh báo Thông minh (AI Alert System)
            modelBuilder.Entity<Alert>(entity =>
            {
                entity.HasIndex(a => new { a.Type, a.EntityKey }).HasDatabaseName("IX_Alerts_Type_EntityKey");
                entity.HasIndex(a => new { a.EntityKey, a.CreatedAt }).HasDatabaseName("IX_Alerts_EntityKey_CreatedAt");
                entity.HasIndex(a => new { a.IsRead, a.CreatedAt }).HasDatabaseName("IX_Alerts_IsRead_CreatedAt");
            });

            // 15. Phân hệ Thông Báo Realtime (Realtime Notifications)
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt }).HasDatabaseName("IX_Notifications_UserId_IsRead");
                entity.HasIndex(n => new { n.Role, n.IsRead, n.CreatedAt }).HasDatabaseName("IX_Notifications_Role_IsRead");
                entity.HasIndex(n => new { n.StoreId, n.IsRead, n.CreatedAt }).HasDatabaseName("IX_Notifications_StoreId_IsRead");
                entity.HasIndex(n => n.Type).HasDatabaseName("IX_Notifications_Type");
            });

            // 16. Phân hệ Hộp Thư Nội Bộ (Internal Messages)
            modelBuilder.Entity<InternalMessage>(entity =>
            {
                entity.HasOne(m => m.Sender)
                      .WithMany()
                      .HasForeignKey(m => m.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.Receiver)
                      .WithMany()
                      .HasForeignKey(m => m.ReceiverId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(m => new { m.ReceiverId, m.IsRead, m.IsDeletedByReceiver }).HasDatabaseName("IX_InternalMessages_Receiver");
                entity.HasIndex(m => new { m.SenderId, m.IsDeletedBySender }).HasDatabaseName("IX_InternalMessages_Sender");
            });

            // =========================================================================
            // 17. CẤU HÌNH KIỂM SOÁT ĐỒNG THỜI LẠC QUAN (ROWVERSION CONCURRENCY TOKENS)
            // =========================================================================
            modelBuilder.Entity<StoreInventory>().Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            modelBuilder.Entity<ProductBatch>().Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            modelBuilder.Entity<LoyaltyWallet>().Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            modelBuilder.Entity<Coupon>().Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            modelBuilder.Entity<PosShift>().Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
            modelBuilder.Entity<Order>().Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

            // =========================================================================
            // 18. CẤU HÌNH OUTBOX MESSAGES & IDEMPOTENCY RECORDS
            // =========================================================================
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasIndex(o => new { o.ProcessedAtUtc, o.RetryCount }).HasDatabaseName("IX_OutboxMessages_Processing");
            });

            modelBuilder.Entity<IdempotencyRecord>(entity =>
            {
                entity.HasIndex(i => i.IdempotencyKey).IsUnique().HasDatabaseName("IX_IdempotencyRecords_Key");
            });

            // GS1 Application Identifier trên ProductBatches
            modelBuilder.Entity<ProductBatch>(entity =>
            {
                entity.HasIndex(b => b.Gs1ApplicationIdentifier)
                      .IsUnique()
                      .HasFilter("[Gs1ApplicationIdentifier] IS NOT NULL")
                      .HasDatabaseName("UQ_ProductBatches_Gs1AI");
            });

            // =========================================================================
            // 19. BỘ LỌC TRUY VẤN TOÀN CỤC (GLOBAL QUERY FILTERS CHO SOFT DELETE & ISACTIVE)
            // =========================================================================
            modelBuilder.Entity<User>().HasQueryFilter(e => e.IsActive);
            modelBuilder.Entity<Role>().HasQueryFilter(e => e.IsActive);
            modelBuilder.Entity<Product>().HasQueryFilter(e => e.IsActive);
            modelBuilder.Entity<Category>().HasQueryFilter(e => e.IsActive);
            modelBuilder.Entity<Brand>().HasQueryFilter(e => e.IsActive);
            modelBuilder.Entity<Supplier>().HasQueryFilter(e => e.IsActive);
            modelBuilder.Entity<Store>().HasQueryFilter(e => e.IsActive);
            modelBuilder.Entity<ProductAttribute>().HasQueryFilter(e => e.IsActive);
            modelBuilder.Entity<ProductAttributeValue>().HasQueryFilter(e => e.IsActive);
            modelBuilder.Entity<MemberTier>().HasQueryFilter(e => e.IsActive);
            modelBuilder.Entity<Promotion>().HasQueryFilter(e => e.IsActive);
            modelBuilder.Entity<Coupon>().HasQueryFilter(e => e.IsActive);
            modelBuilder.Entity<Banner>().HasQueryFilter(e => e.IsActive);
            modelBuilder.Entity<PostCategory>().HasQueryFilter(e => e.IsActive);
            modelBuilder.Entity<StaticPage>().HasQueryFilter(e => e.IsActive);
            modelBuilder.Entity<ActiveIngredient>().HasQueryFilter(e => e.IsActive);

            // =========================================================================
            // 20. CẤU HÌNH MEDIA ASSETS & DYNAMIC THEME ENGINE
            // =========================================================================
            // 1. Cấu hình bảng MediaAssets
            modelBuilder.Entity<MediaAsset>(entity =>
            {
                entity.HasIndex(e => e.ChecksumHash).HasDatabaseName("IX_MediaAssets_ChecksumHash");
                entity.HasIndex(e => e.Category).HasDatabaseName("IX_MediaAssets_Category");
                entity.Property(e => e.Data).HasColumnType("varbinary(max)");

                entity.HasOne(e => e.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.CreatedBy)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.UploadedByStaff)
                      .WithMany()
                      .HasForeignKey(e => e.UploadedByStaffId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 2. Cấu hình bảng ThemeConfigs
            modelBuilder.Entity<ThemeConfig>(entity =>
            {
                entity.HasOne(e => e.LogoMedia)
                      .WithMany()
                      .HasForeignKey(e => e.LogoMediaId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.FaviconMedia)
                      .WithMany()
                      .HasForeignKey(e => e.FaviconMediaId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.LogoMediaAsset)
                      .WithMany()
                      .HasForeignKey(e => e.LogoMediaAssetId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.FaviconMediaAsset)
                      .WithMany()
                      .HasForeignKey(e => e.FaviconMediaAssetId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 3. Cấu hình bảng UiSections
            modelBuilder.Entity<UiSection>(entity =>
            {
                entity.HasIndex(e => e.SectionCode).IsUnique().HasDatabaseName("IX_UiSections_SectionCode");
                entity.HasIndex(e => new { e.IsVisible, e.DisplayOrder }).HasDatabaseName("IX_UiSections_IsVisible_DisplayOrder");
                entity.HasOne(e => e.BackgroundMedia)
                      .WithMany()
                      .HasForeignKey(e => e.BackgroundMediaId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // 3b. Cấu hình bảng Dynamic CMS PageLayouts, LayoutSections, SectionItems
            modelBuilder.Entity<PageLayout>(entity =>
            {
                entity.HasIndex(e => e.PageKey).IsUnique().HasDatabaseName("IX_PageLayouts_PageKey");
            });

            modelBuilder.Entity<LayoutSection>(entity =>
            {
                entity.HasIndex(e => new { e.PageLayoutId, e.OrderIndex }).HasDatabaseName("IX_LayoutSections_PageLayout_Order");
                entity.HasOne(e => e.PageLayout)
                      .WithMany(p => p.LayoutSections)
                      .HasForeignKey(e => e.PageLayoutId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SectionItem>(entity =>
            {
                entity.HasIndex(e => new { e.LayoutSectionId, e.OrderIndex }).HasDatabaseName("IX_SectionItems_Section_Order");
                entity.HasOne(e => e.LayoutSection)
                      .WithMany(s => s.SectionItems)
                      .HasForeignKey(e => e.LayoutSectionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.MediaAsset)
                      .WithMany()
                      .HasForeignKey(e => e.MediaAssetId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 4. Cấu hình quan hệ khóa ngoại Media với Product, Category, Brand, Banner, Post, User
            modelBuilder.Entity<Product>()
                  .HasOne(p => p.ThumbnailMedia)
                  .WithMany()
                  .HasForeignKey(p => p.ThumbnailMediaId)
                  .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Category>()
                  .HasOne(c => c.MediaAsset)
                  .WithMany()
                  .HasForeignKey(c => c.MediaAssetId)
                  .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Brand>()
                  .HasOne(b => b.LogoMedia)
                  .WithMany()
                  .HasForeignKey(b => b.LogoMediaId)
                  .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Banner>()
                  .HasOne(b => b.DesktopMedia)
                  .WithMany()
                  .HasForeignKey(b => b.DesktopMediaId)
                  .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Banner>()
                  .HasOne(b => b.MobileMedia)
                  .WithMany()
                  .HasForeignKey(b => b.MobileMediaId)
                  .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Post>()
                  .HasOne(p => p.ThumbnailMedia)
                  .WithMany()
                  .HasForeignKey(p => p.ThumbnailMediaId)
                  .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                  .HasOne(u => u.AvatarMedia)
                  .WithMany()
                  .HasForeignKey(u => u.AvatarMediaId)
                  .OnDelete(DeleteBehavior.SetNull);
        }

        #region Automatic Audit Logging ChangeTracker Interceptor

        private class AuditEntry
        {
            public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry { get; set; } = null!;
            public string Action { get; set; } = string.Empty;
            public string EntityType { get; set; } = string.Empty;
            public string EntityId { get; set; } = string.Empty;
            public System.Collections.Generic.Dictionary<string, object?> OldValues { get; } = new();
            public System.Collections.Generic.Dictionary<string, object?> NewValues { get; } = new();
            public System.Collections.Generic.List<Microsoft.EntityFrameworkCore.ChangeTracking.PropertyEntry> TemporaryProperties { get; } = new();
        }

        public override Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            return base.SaveChangesAsync(cancellationToken);
        }

        private System.Collections.Generic.List<AuditEntry> CaptureAuditEntries()
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new System.Collections.Generic.List<AuditEntry>();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var entityType = entry.Entity.GetType().Name;
                if (entityType.EndsWith("Proxy") || entityType == "AuditLog")
                    continue;

                var auditEntry = new AuditEntry
                {
                    Entry = entry,
                    EntityType = entityType
                };

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.Action = "CREATE";
                        foreach (var property in entry.Properties)
                        {
                            if (property.IsTemporary)
                            {
                                auditEntry.TemporaryProperties.Add(property);
                                continue;
                            }
                            if (property.Metadata.IsPrimaryKey())
                            {
                                auditEntry.EntityId = property.CurrentValue?.ToString() ?? "0";
                            }
                            auditEntry.NewValues[property.Metadata.Name] = property.CurrentValue;
                        }
                        auditEntries.Add(auditEntry);
                        break;

                    case EntityState.Deleted:
                        auditEntry.Action = "DELETE";
                        foreach (var property in entry.Properties)
                        {
                            if (property.Metadata.IsPrimaryKey())
                            {
                                auditEntry.EntityId = property.OriginalValue?.ToString() ?? "0";
                            }
                            auditEntry.OldValues[property.Metadata.Name] = property.OriginalValue;
                        }
                        auditEntries.Add(auditEntry);
                        break;

                    case EntityState.Modified:
                        auditEntry.Action = "UPDATE";
                        bool hasRealChanges = false;
                        foreach (var property in entry.Properties)
                        {
                            if (property.Metadata.IsPrimaryKey())
                            {
                                auditEntry.EntityId = property.CurrentValue?.ToString() ?? "0";
                            }
                            if (property.IsModified)
                            {
                                auditEntry.OldValues[property.Metadata.Name] = property.OriginalValue;
                                auditEntry.NewValues[property.Metadata.Name] = property.CurrentValue;
                                hasRealChanges = true;
                            }
                        }
                        if (hasRealChanges)
                        {
                            auditEntries.Add(auditEntry);
                        }
                        break;
                }
            }

            return auditEntries;
        }

        private async Task PersistAuditEntriesAsync(System.Collections.Generic.List<AuditEntry> auditEntries, System.Threading.CancellationToken cancellationToken)
        {
            try
            {
                int? currentUserId = null;
                string? currentUserName = null;
                string? ipAddress = null;
                string? userAgent = null;

                var httpContext = _httpContextAccessor?.HttpContext;
                if (httpContext != null)
                {
                    var uidStr = httpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(uidStr, out var uid)) currentUserId = uid;
                    currentUserName = httpContext.User?.Identity?.Name;
                    ipAddress = httpContext.Connection?.RemoteIpAddress?.ToString();
                    userAgent = httpContext.Request?.Headers["User-Agent"].ToString();
                }

                foreach (var entry in auditEntries)
                {
                    foreach (var prop in entry.TemporaryProperties)
                    {
                        if (prop.Metadata.IsPrimaryKey())
                        {
                            entry.EntityId = prop.CurrentValue?.ToString() ?? entry.EntityId;
                        }
                        entry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                    }

                    if (string.IsNullOrEmpty(entry.EntityId) || entry.EntityId == "0")
                    {
                        var pk = entry.Entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                        if (pk != null) entry.EntityId = pk.CurrentValue?.ToString() ?? "0";
                    }

                    var log = new AuditLog
                    {
                        Action = entry.Action,
                        EntityType = entry.EntityType,
                        EntityId = string.IsNullOrEmpty(entry.EntityId) ? "0" : entry.EntityId,
                        OldValues = entry.OldValues.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(entry.OldValues) : null,
                        NewValues = entry.NewValues.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(entry.NewValues) : null,
                        UserId = currentUserId,
                        UserName = currentUserName,
                        IpAddress = ipAddress,
                        UserAgent = userAgent,
                        CreatedAt = DateTime.UtcNow
                    };

                    AuditLogs.Add(log);
                }

                await base.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AuditLog Warning] Save audit entries: {ex.Message}");
            }
        }

        #endregion
    }
}

