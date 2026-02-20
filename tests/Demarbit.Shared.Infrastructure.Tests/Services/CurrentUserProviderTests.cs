using Demarbit.Shared.Domain.Contracts;
using Demarbit.Shared.Infrastructure.Services;

namespace Demarbit.Shared.Infrastructure.Tests.Services;

public class CurrentUserProviderTests
{
    [Fact]
    public void UserId_ReturnsNull()
    {
        var provider = new CurrentUserProvider();

        Assert.Null(provider.UserId);
    }

    [Fact]
    public void ImplementsICurrentUserProvider()
    {
        var provider = new CurrentUserProvider();

        Assert.IsType<ICurrentUserProvider>(provider, false);
    }

    [Fact]
    public void SetUserId_Updates_UserId()
    {
        var provider = new CurrentUserProvider();

        var userId = Guid.NewGuid();
        provider.SetUserId(userId);
        Assert.Equal(userId, provider.UserId);
    }
}
