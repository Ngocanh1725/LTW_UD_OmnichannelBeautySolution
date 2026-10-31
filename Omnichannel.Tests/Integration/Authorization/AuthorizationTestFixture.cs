using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Omnichannel.Application.Interfaces.Security;
using Omnichannel.Domain.Entities;
using Omnichannel.Infrastructure.Data;
using Omnichannel.Infrastructure.Security;

namespace Omnichannel.Tests.Integration.Authorization
{
    public class AuthorizationTestFixture : IDisposable
    {
        public ServiceProvider ServiceProvider { get; }
        public ApplicationDbContext DbContext { get; }
        public IAuthorizationService AuthorizationService { get; }

        public AuthorizationTestFixture()
        {
            var services = new ServiceCollection();

            var dbName = "AuthTestDb_" + Guid.NewGuid().ToString("N");
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(dbName)
                       .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            services.AddMemoryCache();
            services.AddLogging();

            services.AddScoped<IPermissionService, PermissionService>();
            services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();
            services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddAuthorizationCore();

            ServiceProvider = services.BuildServiceProvider();
            DbContext = ServiceProvider.GetRequiredService<ApplicationDbContext>();
            AuthorizationService = ServiceProvider.GetRequiredService<IAuthorizationService>();

            // Seed full production-grade database
            DbContext.Database.EnsureCreated();
            DbInitializer.SeedAsync(DbContext).GetAwaiter().GetResult();
        }

        public async Task<ClaimsPrincipal> CreatePrincipalForUserAsync(string username)
        {
            var user = await DbContext.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                throw new InvalidOperationException($"User '{username}' không tồn tại trong database test.");
            }

            var claims = new System.Collections.Generic.List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username)
            };

            foreach (var ur in user.UserRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, ur.Role.Name));
            }

            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        public async Task<bool> CheckPermissionAsync(string username, string module, string action)
        {
            var principal = await CreatePrincipalForUserAsync(username);
            var policyName = $"PERM:{module.ToUpperInvariant()}:{action.ToUpperInvariant()}";
            var result = await AuthorizationService.AuthorizeAsync(principal, null, policyName);
            return result.Succeeded;
        }

        public void Dispose()
        {
            DbContext.Dispose();
            ServiceProvider.Dispose();
        }
    }
}
