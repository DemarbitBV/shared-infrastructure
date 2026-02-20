using Demarbit.Shared.Domain.Contracts;

namespace Demarbit.Shared.Infrastructure.Services;

/// <summary>
/// Default <see cref="ICurrentUserProvider"/> that provides no user context.
/// </summary>
public sealed class CurrentUserProvider : ICurrentUserProvider
{
    /// <inheritdoc/>
    public Guid? UserId { get; private set; }

    /// <inheritdoc/>
    public void SetUserId(Guid? userId)
    {
        UserId = userId;
    }
}