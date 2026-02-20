using Demarbit.Shared.Domain.Contracts;

namespace Demarbit.Shared.Infrastructure.Tests._Fixtures;

public class TestCurrentTenantProvider : ICurrentTenantProvider
{
    public void SetTenantId(Guid? tenantId)
    {
        TenantId = tenantId;
    }

    public Guid? TenantId { get; set; }
}
