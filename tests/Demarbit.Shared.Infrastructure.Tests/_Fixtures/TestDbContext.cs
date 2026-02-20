using Demarbit.Shared.Domain.Contracts;
using Demarbit.Shared.Infrastructure.Extensions;
using Demarbit.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Demarbit.Shared.Infrastructure.Tests._Fixtures;

public class TestDbContext(
    DbContextOptions<TestDbContext> options,
    ICurrentUserProvider userProvider,
    ICurrentTenantProvider tenantProvider)
    : AppDbContextBase<TestDbContext>(options, userProvider, tenantProvider)
{
    public DbSet<TestAggregate> TestAggregates => Set<TestAggregate>();
    public DbSet<TestTenantAggregate> TestTenantAggregates => Set<TestTenantAggregate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestAggregate>(builder =>
        {
            builder.IsEntity<TestAggregate, Guid>();
            builder.Property(e => e.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<TestTenantAggregate>(builder =>
        {
            builder.IsEntity<TestTenantAggregate, Guid>();
            builder.Property(e => e.Name).HasMaxLength(200);
        });
    }
}
