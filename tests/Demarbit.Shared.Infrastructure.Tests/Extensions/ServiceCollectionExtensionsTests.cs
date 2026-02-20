using Demarbit.Shared.Domain.Contracts;
using Demarbit.Shared.Infrastructure.Extensions;
using Demarbit.Shared.Infrastructure.Services;
using Demarbit.Shared.Infrastructure.Tests._Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Demarbit.Shared.Infrastructure.Tests.Extensions;

public class ServiceCollectionExtensionsTests
{
    private static ServiceProvider BuildProvider(Type? userProviderType = null, Type? tenantProviderType = null)
    {
        var services = new ServiceCollection();
        services.AddSharedInfrastructure<TestDbContext>(
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()),
            userProviderType,
            tenantProviderType);
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddSharedInfrastructure_RegistersDbContext()
    {
        using var provider = BuildProvider();

        var context = provider.GetService<TestDbContext>();

        Assert.NotNull(context);
    }

    [Fact]
    public void AddSharedInfrastructure_RegistersIUnitOfWork()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetService<IUnitOfWork>();

        Assert.NotNull(unitOfWork);
        Assert.IsType<TestDbContext>(unitOfWork);
    }

    [Fact]
    public void AddSharedInfrastructure_RegistersIEventIdempotencyService()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var service = scope.ServiceProvider.GetService<IEventIdempotencyService>();

        Assert.NotNull(service);
    }

    [Fact]
    public void AddSharedInfrastructure_WithNullProviders_RegistersEmptyCurrentUserProvider()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var userProvider = scope.ServiceProvider.GetService<ICurrentUserProvider>();

        Assert.NotNull(userProvider);
        Assert.IsType<CurrentUserProvider>(userProvider);
    }

    [Fact]
    public void AddSharedInfrastructure_WithNullProviders_RegistersEmptyCurrentTenantProvider()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var tenantProvider = scope.ServiceProvider.GetService<ICurrentTenantProvider>();

        Assert.NotNull(tenantProvider);
        Assert.IsType<CurrentTenantProvider>(tenantProvider);
    }

    [Fact]
    public void AddSharedInfrastructure_WithCustomUserProvider_RegistersCustomProvider()
    {
        using var provider = BuildProvider(userProviderType: typeof(TestCurrentUserProvider));
        using var scope = provider.CreateScope();

        var userProvider = scope.ServiceProvider.GetService<ICurrentUserProvider>();

        Assert.NotNull(userProvider);
        Assert.IsType<TestCurrentUserProvider>(userProvider);
    }

    [Fact]
    public void AddSharedInfrastructure_WithCustomTenantProvider_RegistersCustomProvider()
    {
        using var provider = BuildProvider(tenantProviderType: typeof(TestCurrentTenantProvider));
        using var scope = provider.CreateScope();

        var tenantProvider = scope.ServiceProvider.GetService<ICurrentTenantProvider>();

        Assert.NotNull(tenantProvider);
        Assert.IsType<TestCurrentTenantProvider>(tenantProvider);
    }

    [Fact]
    public void AddSharedInfrastructure_ReturnsServiceCollectionForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddSharedInfrastructure<TestDbContext>(
            options => options.UseInMemoryDatabase("test"),
            null,
            null);

        Assert.Same(services, result);
    }
}
