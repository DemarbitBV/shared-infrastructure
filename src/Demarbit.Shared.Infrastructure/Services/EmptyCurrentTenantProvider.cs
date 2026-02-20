using Demarbit.Shared.Domain.Contracts;

namespace Demarbit.Shared.Infrastructure.Services;

public class EmptyCurrentTenantProvider : ICurrentTenantProvider
{
    public Guid? TenantId => null;
}