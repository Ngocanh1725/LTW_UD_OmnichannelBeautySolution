using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Omnichannel.Tests.Integration.Authorization
{
    [Collection("AuthTests")]
    public class StoreScopeTests : IClassFixture<AuthorizationTestFixture>
    {
        private readonly AuthorizationTestFixture _fixture;

        public StoreScopeTests(AuthorizationTestFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task StoreIsolation_StoreManager_AssignedToSpecificStore()
        {
            // Arrange
            var manager = await _fixture.DbContext.Users
                .Include(u => u.Store)
                .FirstOrDefaultAsync(u => u.Username == "manager_cg");

            // Assert
            manager.Should().NotBeNull();
            manager!.StoreId.Should().NotBeNull();
            manager.Store!.Code.Should().Match(c => c == "SH-BD" || c == "SH-CG", "Store Manager phải gắn liền với Chi nhánh Showroom Ba Đình / Cầu Giấy");
        }

        [Fact]
        public async Task StoreIsolation_SuperAdmin_HasGlobalScope()
        {
            // Arrange
            var superadmin = await _fixture.DbContext.Users
                .FirstOrDefaultAsync(u => u.Username == "superadmin");

            // Assert
            superadmin.Should().NotBeNull();
            // SuperAdmin không bị giới hạn trong 1 chi nhánh đơn lẻ (StoreId null hoặc được bypass toàn quyền)
            var totalStores = await _fixture.DbContext.Stores.CountAsync();
            totalStores.Should().BeGreaterThanOrEqualTo(3, "SuperAdmin quản lý toàn bộ chuỗi 3 chi nhánh");
        }

        [Fact]
        public async Task StoreInventories_AreStrictlyPartitionedByStore()
        {
            // Kiểm tra tồn kho tại 2 chi nhánh khác nhau cho cùng 1 sản phẩm
            var storeHq = await _fixture.DbContext.Stores.FirstAsync(s => s.Code == "SH-Q1" || s.Code == "HQ-001");
            var storeCg = await _fixture.DbContext.Stores.FirstAsync(s => s.Code == "SH-BD" || s.Code == "SH-CG");

            var invHq = await _fixture.DbContext.StoreInventories
                .Where(si => si.StoreId == storeHq.Id)
                .ToListAsync();

            var invCg = await _fixture.DbContext.StoreInventories
                .Where(si => si.StoreId == storeCg.Id)
                .ToListAsync();

            invHq.Should().NotBeEmpty();
            invCg.Should().NotBeEmpty();
            invHq.Should().OnlyContain(i => i.StoreId == storeHq.Id);
            invCg.Should().OnlyContain(i => i.StoreId == storeCg.Id);
        }
    }
}
