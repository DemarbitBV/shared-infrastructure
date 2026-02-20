using Demarbit.Shared.Domain.Models;

namespace Demarbit.Shared.Infrastructure.Tests._Fixtures;

public class TestAggregate : AggregateRoot
{
    public string Name { get; set; } = string.Empty;

    public TestAggregate() { }

    public TestAggregate(string name)
    {
        Name = name;
    }

    public void ChangeName(string newName)
    {
        Name = newName;
        RaiseDomainEvent(new TestDomainEvent { AggregateId = Id });
    }

    public void RaiseTestEvent()
    {
        RaiseDomainEvent(new TestDomainEvent { AggregateId = Id });
    }
}
