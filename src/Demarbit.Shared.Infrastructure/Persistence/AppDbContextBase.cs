using Demarbit.Shared.Domain.Contracts;
using Demarbit.Shared.Domain.Models;
using Demarbit.Shared.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace Demarbit.Shared.Infrastructure.Persistence;

/// <summary>
/// Abstract base DbContext that provides audit field stamping, domain event collection,
/// unit-of-work transaction management, and processed event tracking.
/// <para>
/// Database provider agnostic — the consuming project configures its own provider
/// (PostgreSQL, SQL Server, SQLite, etc.) via the <c>AddSharedInfrastructure</c> extension method.
/// </para>
/// </summary>
/// <example>
/// <code>
/// internal sealed class AppDbContext(
///     DbContextOptions&lt;AppDbContext&gt; options,
///     ISessionContext sessionContext,
///     IDateTimeProvider dateTimeProvider)
///     : AppDbContextBase&lt;AppDbContext&gt;(options, sessionContext, dateTimeProvider)
/// {
///     public DbSet&lt;Client&gt; Clients =&gt; Set&lt;Client&gt;();
///
///     protected override void ConfigureModel(ModelBuilder modelBuilder)
///     {
///         modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
///         modelBuilder.Entity&lt;Client&gt;().HasQueryFilter(x =&gt; x.UserId == SessionContext.UserId);
///     }
/// }
/// </code>
/// </example>
public abstract class AppDbContextBase<TContext>(
    DbContextOptions<TContext> options,
    ICurrentUserProvider userProvider,
    ICurrentTenantProvider tenantProvider
    ) : DbContext(options), IUnitOfWork
    where TContext : DbContext
{
    private IDbContextTransaction? _currentTransaction;
    private readonly List<IDomainEvent> _pendingEvents = [];
    
    /// <summary>Tracks processed domain events for idempotency.</summary>
    public DbSet<ProcessedEvent> ProcessedEvents { get; set; }

    /// <inheritdoc/>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SetAuditingFields();
        _pendingEvents.AddRange(CollectDomainEvents());
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc/>
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        SetAuditingFields();
        _pendingEvents.AddRange(CollectDomainEvents());
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new ProcessedEventConfiguration());
    }

    private void SetAuditingFields()
    {
        var now = TimeProvider.System.GetUtcNow().DateTime;
        var userId = userProvider.UserId;
        var tenantId = tenantProvider.TenantId;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            SetCreatedAuditingFields(entry, now, userId, tenantId);
            SetUpdateAuditingFields(entry, now, userId);
        }
    }

    private static void SetCreatedAuditingFields(EntityEntry<IAuditableEntity> entry,
        DateTime now,
        Guid? userId,
        Guid? tenantId)
    {
        if (entry.State is not EntityState.Added) return;
        
        entry.Entity.SetCreated(now, userId);

        // ReSharper disable once SuspiciousTypeConversion.Global
        if (entry.Entity is ITenantEntity)
            entry.Property(nameof(ITenantEntity.TenantId)).CurrentValue = tenantId ?? Guid.Empty;
    }

    private static void SetUpdateAuditingFields(EntityEntry<IAuditableEntity> entry,
        DateTime now,
        Guid? userId)
    {
        if (entry.State is not EntityState.Modified) return;
        
        entry.Entity.SetUpdated(now, userId);
    }
    
    /// <summary>
    /// Collects and dequeues all pending domain events from tracked aggregates.
    /// Called automatically before each SaveChanges.
    /// </summary>
    private List<IDomainEvent> CollectDomainEvents()
    {
        var aggregatesWithEvents = ChangeTracker
            .Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .Where(a => a.DomainEvents.Count > 0)
            .ToList();

        if (aggregatesWithEvents.Count == 0)
            return [];

        var events = new List<IDomainEvent>();
        foreach (var aggregate in aggregatesWithEvents)
        {
            events.AddRange(aggregate.DequeueDomainEvents());
        }

        return events;
    }

    /// <inheritdoc />
    public IReadOnlyList<IDomainEvent> GetAndClearPendingEvents()
    {
        var events = _pendingEvents.ToList();
        _pendingEvents.Clear();
        return events;
    }
    
    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
            throw new InvalidOperationException(
                "A transaction is already in progress. Nested transactions are not supported.");

        // InMemory provider does not support transactions.
        if (Database.IsRelational())
        {
            _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null)
        {
            // InMemory — SaveChanges already persisted, nothing to commit.
            if (!Database.IsRelational()) return;
            throw new InvalidOperationException("No transaction is currently in progress.");
        }

        try
        {
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await DisposeCurrentTransactionAsync();
        }
    }

    /// <inheritdoc />
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null) return;

        try
        {
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await DisposeCurrentTransactionAsync();
        }
    }

    private async Task DisposeCurrentTransactionAsync()
    {
        if (_currentTransaction is null) return;
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }
}