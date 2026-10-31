using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Omnichannel.Infrastructure.Data;

namespace Omnichannel.Tests.Helpers
{
    public static class TestDbContextFactory
    {
        public static ApplicationDbContext Create(string? dbName = null)
        {
            var databaseName = dbName ?? Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }
    }
}
