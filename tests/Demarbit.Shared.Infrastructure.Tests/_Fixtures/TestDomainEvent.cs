using Demarbit.Shared.Domain.Models;

namespace Demarbit.Shared.Infrastructure.Tests._Fixtures;

public sealed record TestDomainEvent : DomainEventBase
{
    public Guid AggregateId { get; init; }
}
