using Demarbit.Shared.Domain.Contracts;

namespace Demarbit.Shared.Infrastructure.Services;

/// <summary>
/// Default <see cref="ICurrentTenantProvider"/> that provides no tenant context.
/// </summary>
public class CurrentTenantProvider : ICurrentTenantProvider
{
    /// <inheritdoc/>
    public Guid? TenantId { get; private set; }
    
    /// <inheritdoc/>
    public void SetTenantId(Guid? tenantId)
    {
        TenantId = tenantId;
    }
}