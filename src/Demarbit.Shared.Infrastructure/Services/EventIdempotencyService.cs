using Demarbit.Shared.Domain.Contracts;
using Demarbit.Shared.Domain.Models;
using Demarbit.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Demarbit.Shared.Infrastructure.Services;

internal sealed class EventIdempotencyService<TContext>(TContext context) : IEventIdempotencyService
    where TContext : AppDbContextBase<TContext>
{
    public async Task<bool> HasBeenProcessedAsync(Guid eventId, string handlerType,
        CancellationToken cancellationToken = new CancellationToken())
        => await context.ProcessedEvents
            .AnyAsync(x => x.EventId == eventId && x.HandlerType == handlerType, cancellationToken);

    public async Task MarkAsProcessedAsync(Guid eventId, string eventType, string handlerType,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var processedEvent = ProcessedEvent.Create(
            eventId,
            eventType,
            handlerType);
        await context.ProcessedEvents.AddAsync(processedEvent, cancellationToken);
    }
}