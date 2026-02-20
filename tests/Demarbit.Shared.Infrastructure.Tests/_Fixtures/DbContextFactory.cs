using Demarbit.Shared.Domain.Contracts;
using Demarbit.Shared.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Demarbit.Shared.Infrastructure.Tests._Fixtures;

public static class DbContextFactory
{
    public static TestDbContext Create(
        ICurrentUserProvider? userProvider = null,
        ICurrentTenantProvider? tenantProvider = null,
        string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        return new TestDbContext(
            options,
            userProvider ?? new CurrentUserProvider(),
            tenantProvider ?? new CurrentTenantProvider());
    }
}
