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

        Assert.IsType<ICurrentTenantProvider>(provider, false);
    }

    [Fact]
    public void SetTenantId_Updates_TenantId()
    {
        var provider = new CurrentTenantProvider();

        var tenantId = Guid.NewGuid();
        provider.SetTenantId(tenantId);
        
        Assert.Equal(tenantId, provider.TenantId);
    }
}
