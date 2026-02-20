using Demarbit.Shared.Infrastructure.Extensions;
using Demarbit.Shared.Infrastructure.Tests._Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Demarbit.Shared.Infrastructure.Tests.Extensions;

public class ServiceProviderExtensionsTests
{
    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSharedInfrastructure<TestDbContext>(
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()),
            null,
            null);
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task BootstrapDatabaseAsync_WithInMemory_CreatesDatabase()
    {
        using var provider = BuildProvider();

        var exception = await Record.ExceptionAsync(
            () => provider.BootstrapDatabaseAsync<TestDbContext>());

        Assert.Null(exception);
    }

    [Fact]
    public async Task BootstrapDatabaseAsync_AllowsSubsequentOperations()
    {
        using var provider = BuildProvider();
        await provider.BootstrapDatabaseAsync<TestDbContext>();

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        context.TestAggregates.Add(new TestAggregate("Test"));
        await context.SaveChangesAsync();

        var count = await context.TestAggregates.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task EnsureDatabaseCreatedAsync_CreatesDatabase()
    {
        using var provider = BuildProvider();

        var exception = await Record.ExceptionAsync(
            () => provider.EnsureDatabaseCreatedAsync<TestDbContext>());

        Assert.Null(exception);
    }

    [Fact]
    public async Task EnsureDatabaseCreatedAsync_AllowsSubsequentOperations()
    {
        using var provider = BuildProvider();
        await provider.EnsureDatabaseCreatedAsync<TestDbContext>();

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        context.TestAggregates.Add(new TestAggregate("Test"));
        await context.SaveChangesAsync();

        var count = await context.TestAggregates.CountAsync();
        Assert.Equal(1, count);
    }
}
