using Demarbit.Shared.Domain.Contracts;
using Demarbit.Shared.Infrastructure.Services;

namespace Demarbit.Shared.Infrastructure.Tests.Services;

public class CurrentTenantProviderTests
{
    [Fact]
    public void TenantId_ReturnsNull()
    {
        var provider = new CurrentTenantProvider();

        Assert.Null(provider.TenantId);
    }

    [Fact]
    public void ImplementsICurrentTenantProvider()
    {
        var provider = new CurrentTenantProvider();

        Assert.IsAssignableFrom<ICurrentTenantProvider>(provider);
    }
}
