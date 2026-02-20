using Demarbit.Shared.Domain.Contracts;

namespace Demarbit.Shared.Infrastructure.Tests._Fixtures;

public class TestCurrentUserProvider : ICurrentUserProvider
{
    public void SetUserId(Guid? userId)
    {
        UserId = userId;
    }

    public Guid? UserId { get; set; }
}
