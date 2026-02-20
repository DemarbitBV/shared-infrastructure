using Demarbit.Shared.Domain.Contracts;
using Demarbit.Shared.Infrastructure.Extensions;
using Demarbit.Shared.Infrastructure.Tests._Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Demarbit.Shared.Infrastructure.Tests.Services;

public class EventIdempotencyServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IServiceScope _scope;
    private readonly TestDbContext _context;
    private readonly IEventIdempotencyService _sut;

    public EventIdempotencyServiceTests()
    {
        var services = new ServiceCollection();
        services.AddSharedInfrastructure<TestDbContext>(
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()),
            null,
            null);
        services.AddLogging();

        _serviceProvider = services.BuildServiceProvider();
        _scope = _serviceProvider.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<TestDbContext>();
        _sut = _scope.ServiceProvider.GetRequiredService<IEventIdempotencyService>();
    }

    [Fact]
    public async Task HasBeenProcessedAsync_WhenNotProcessed_ReturnsFalse()
    {
        var result = await _sut.HasBeenProcessedAsync(Guid.NewGuid(), "SomeHandler");

        Assert.False(result);
    }

    [Fact]
    public async Task HasBeenProcessedAsync_WhenAlreadyProcessed_ReturnsTrue()
    {
        var eventId = Guid.NewGuid();
        const string handlerType = "TestHandler";

        await _sut.MarkAsProcessedAsync(eventId, "TestEvent", handlerType);
        await _context.SaveChangesAsync();

        var result = await _sut.HasBeenProcessedAsync(eventId, handlerType);

        Assert.True(result);
    }

    [Fact]
    public async Task HasBeenProcessedAsync_WithDifferentHandler_ReturnsFalse()
    {
        var eventId = Guid.NewGuid();

        await _sut.MarkAsProcessedAsync(eventId, "TestEvent", "HandlerA");
        await _context.SaveChangesAsync();

        var result = await _sut.HasBeenProcessedAsync(eventId, "HandlerB");

        Assert.False(result);
    }

    [Fact]
    public async Task MarkAsProcessedAsync_AddsProcessedEvent()
    {
        var eventId = Guid.NewGuid();

        await _sut.MarkAsProcessedAsync(eventId, "TestEvent", "TestHandler");
        await _context.SaveChangesAsync();

        var processedEvent = await _context.ProcessedEvents
            .FirstOrDefaultAsync(e => e.EventId == eventId);

        Assert.NotNull(processedEvent);
        Assert.Equal("TestEvent", processedEvent.EventType);
        Assert.Equal("TestHandler", processedEvent.HandlerType);
    }

    public void Dispose()
    {
        _scope.Dispose();
        _serviceProvider.Dispose();
    }
}
