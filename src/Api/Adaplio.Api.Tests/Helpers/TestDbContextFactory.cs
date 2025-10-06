using Adaplio.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Adaplio.Api.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext CreateInMemoryContext(string databaseName = "")
    {
        var dbName = string.IsNullOrEmpty(databaseName)
            ? $"TestDb_{Guid.NewGuid()}"
            : databaseName;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new AppDbContext(options);
    }

    public static AppDbContext CreateSqliteContext(string databaseName = "")
    {
        var dbName = string.IsNullOrEmpty(databaseName)
            ? $"TestDb_{Guid.NewGuid()}.db"
            : databaseName;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"DataSource={dbName};Mode=Memory;Cache=Shared")
            .Options;

        var context = new AppDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        return context;
    }
}
