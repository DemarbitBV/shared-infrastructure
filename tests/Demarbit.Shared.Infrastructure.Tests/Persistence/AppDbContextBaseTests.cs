using Demarbit.Shared.Domain.Contracts;
using Demarbit.Shared.Infrastructure.Tests._Fixtures;

namespace Demarbit.Shared.Infrastructure.Tests.Persistence;

public class AppDbContextBaseTests : IDisposable
{
    private readonly TestCurrentUserProvider _userProvider = new();
    private readonly TestCurrentTenantProvider _tenantProvider = new();
    private readonly TestDbContext _context;

    public AppDbContextBaseTests()
    {
        _context = DbContextFactory.Create(_userProvider, _tenantProvider);
    }

    [Fact]
    public async Task SaveChangesAsync_OnAdd_SetsCreatedAuditFields()
    {
        var userId = Guid.NewGuid();
        _userProvider.UserId = userId;

        var entity = new TestAggregate("Test");
        _context.TestAggregates.Add(entity);
        await _context.SaveChangesAsync();

        Assert.NotEqual(default, entity.CreatedAt);
        Assert.NotEqual(default, entity.UpdatedAt);
        Assert.Equal(entity.CreatedAt, entity.UpdatedAt);
        Assert.Equal(userId, entity.CreatedBy);
        Assert.Equal(userId, entity.UpdatedBy);
    }

    [Fact]
    public async Task SaveChangesAsync_OnModify_SetsUpdatedAuditFields()
    {
        var creatorId = Guid.NewGuid();
        _userProvider.UserId = creatorId;

        var entity = new TestAggregate("Test");
        _context.TestAggregates.Add(entity);
        await _context.SaveChangesAsync();

        var updaterId = Guid.NewGuid();
        _userProvider.UserId = updaterId;
        entity.Name = "Updated";
        _context.TestAggregates.Update(entity);
        await _context.SaveChangesAsync();

        Assert.Equal(creatorId, entity.CreatedBy);
        Assert.Equal(updaterId, entity.UpdatedBy);
        Assert.True(entity.UpdatedAt >= entity.CreatedAt);
    }

    [Fact]
    public async Task SaveChangesAsync_WithNullUserId_SetsNullAuditUserFields()
    {
        _userProvider.UserId = null;

        var entity = new TestAggregate("Test");
        _context.TestAggregates.Add(entity);
        await _context.SaveChangesAsync();

        Assert.Null(entity.CreatedBy);
        Assert.Null(entity.UpdatedBy);
    }

    [Fact]
    public async Task SaveChangesAsync_OnTenantEntityAdd_SetsTenantId()
    {
        var tenantId = Guid.NewGuid();
        _tenantProvider.TenantId = tenantId;

        var entity = new TestTenantAggregate("Tenant Test");
        _context.TestTenantAggregates.Add(entity);
        await _context.SaveChangesAsync();

        Assert.Equal(tenantId, entity.TenantId);
    }

    [Fact]
    public async Task SaveChangesAsync_OnTenantEntityAdd_WithNullTenant_SetsEmptyGuid()
    {
        _tenantProvider.TenantId = null;

        var entity = new TestTenantAggregate("Tenant Test");
        _context.TestTenantAggregates.Add(entity);
        await _context.SaveChangesAsync();

        Assert.Equal(Guid.Empty, entity.TenantId);
    }

    [Fact]
    public async Task SaveChangesAsync_CollectsDomainEvents()
    {
        var entity = new TestAggregate("Test");
        entity.RaiseTestEvent();

        _context.TestAggregates.Add(entity);
        await _context.SaveChangesAsync();

        var events = _context.GetAndClearPendingEvents();
        Assert.Single(events);
        var domainEvent = Assert.IsType<TestDomainEvent>(events[0]);
        Assert.Equal(entity.Id, domainEvent.AggregateId);
    }

    [Fact]
    public async Task SaveChangesAsync_DequeuesDomainEventsFromAggregate()
    {
        var entity = new TestAggregate("Test");
        entity.RaiseTestEvent();
        entity.RaiseTestEvent();

        _context.TestAggregates.Add(entity);
        await _context.SaveChangesAsync();

        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public async Task GetAndClearPendingEvents_ReturnsAndClearsEvents()
    {
        var entity = new TestAggregate("Test");
        entity.RaiseTestEvent();

        _context.TestAggregates.Add(entity);
        await _context.SaveChangesAsync();

        var firstCall = _context.GetAndClearPendingEvents();
        var secondCall = _context.GetAndClearPendingEvents();

        Assert.Single(firstCall);
        Assert.Empty(secondCall);
    }

    [Fact]
    public void SaveChanges_SetsAuditFieldsSynchronously()
    {
        var userId = Guid.NewGuid();
        _userProvider.UserId = userId;

        var entity = new TestAggregate("Test");
        _context.TestAggregates.Add(entity);
        _context.SaveChanges();

        Assert.NotEqual(default, entity.CreatedAt);
        Assert.Equal(userId, entity.CreatedBy);
    }

    [Fact]
    public async Task BeginTransactionAsync_WithInMemory_DoesNotThrow()
    {
        var exception = await Record.ExceptionAsync(() => _context.BeginTransactionAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task CommitTransactionAsync_WithInMemory_DoesNotThrow()
    {
        await _context.BeginTransactionAsync();

        var exception = await Record.ExceptionAsync(() => _context.CommitTransactionAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task RollbackTransactionAsync_WithInMemory_DoesNotThrow()
    {
        await _context.BeginTransactionAsync();

        var exception = await Record.ExceptionAsync(() => _context.RollbackTransactionAsync());

        Assert.Null(exception);
    }

    [Fact]
    public void OnModelCreating_ConfiguresProcessedEventsTable()
    {
        var entityType = _context.Model.FindEntityType(typeof(Demarbit.Shared.Domain.Models.ProcessedEvent));

        Assert.NotNull(entityType);
        Assert.NotNull(entityType.FindPrimaryKey());
    }

    [Fact]
    public void ImplementsIUnitOfWork()
    {
        Assert.IsType<IUnitOfWork>(_context, false);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _context.Dispose();
    }
}
