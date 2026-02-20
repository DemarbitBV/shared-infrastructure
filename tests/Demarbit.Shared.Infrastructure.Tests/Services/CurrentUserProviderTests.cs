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
}
