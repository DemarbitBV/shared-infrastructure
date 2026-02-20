using Demarbit.Shared.Infrastructure.Persistence;
using Demarbit.Shared.Infrastructure.Services;
using Demarbit.Shared.Infrastructure.Tests._Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Demarbit.Shared.Infrastructure.Tests.Persistence;

file class TestDesignTimeDbContextFactory : DesignTimeDbContextFactoryBase<TestDbContext>
{
    protected override string GetConnectionString() => "TestDatabase";

    protected override void ConfigureProvider(DbContextOptionsBuilder<TestDbContext> builder, string connectionString)
    {
        builder.UseInMemoryDatabase(connectionString);
    }

    protected override TestDbContext CreateContext(DbContextOptions<TestDbContext> options)
    {
        return new TestDbContext(options, new CurrentUserProvider(), new CurrentTenantProvider());
    }
}

public class DesignTimeDbContextFactoryBaseTests
{
    [Fact]
    public void CreateDbContext_ReturnsConfiguredContext()
    {
        var factory = new TestDesignTimeDbContextFactory();

        using var context = factory.CreateDbContext([]);

        Assert.NotNull(context);
        Assert.IsType<TestDbContext>(context);
    }

    [Fact]
    public void CreateDbContext_ContextCanPerformOperations()
    {
        var factory = new TestDesignTimeDbContextFactory();

        using var context = factory.CreateDbContext([]);
        context.Database.EnsureCreated();

        var entity = new TestAggregate("Test");
        context.TestAggregates.Add(entity);
        context.SaveChanges();

        var found = context.TestAggregates.Find(entity.Id);
        Assert.NotNull(found);
    }

    [Fact]
    public void ImplementsIDesignTimeDbContextFactory()
    {
        var factory = new TestDesignTimeDbContextFactory();

        Assert.IsType<IDesignTimeDbContextFactory<TestDbContext>>(factory, false);
    }
}
