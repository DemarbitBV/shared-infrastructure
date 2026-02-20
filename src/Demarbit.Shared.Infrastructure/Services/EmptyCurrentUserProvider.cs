using Demarbit.Shared.Domain.Contracts;

namespace Demarbit.Shared.Infrastructure.Services;

public sealed class EmptyCurrentUserProvider : ICurrentUserProvider
{
    public Guid? UserId => null;
}