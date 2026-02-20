using Demarbit.Shared.Domain.Contracts;
using Demarbit.Shared.Domain.Models;

namespace Demarbit.Shared.Infrastructure.Tests._Fixtures;

public class TestTenantAggregate : AggregateRoot, ITenantEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid TenantId { get; set; }

    public TestTenantAggregate() { }

    public TestTenantAggregate(string name)
    {
        Name = name;
    }
}
