using Adaplio.Api.Data;

namespace Adaplio.Api.Tests.Helpers;

public abstract class DatabaseTestBase : IDisposable
{
    protected readonly AppDbContext Context;

    protected DatabaseTestBase()
    {
        Context = TestDbContextFactory.CreateInMemoryContext();
    }

    public virtual void Dispose()
    {
        Context.Dispose();
    }

    protected async Task SaveChangesAsync()
    {
        await Context.SaveChangesAsync();
    }
}
