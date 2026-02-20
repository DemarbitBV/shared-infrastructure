using Demarbit.Shared.Domain.Contracts;
using Demarbit.Shared.Domain.Models;
using Demarbit.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Demarbit.Shared.Infrastructure.Services;

/// <summary>
/// Base repository implementation for aggregates with a generic ID type.
/// Accepts <see cref="DbContext"/> (not a concrete subclass) so it works with any
/// <see cref="AppDbContextBase{TContext}"/> derivative.
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type.</typeparam>
/// <typeparam name="TId">The aggregate's identifier type.</typeparam>
public abstract class RepositoryBase<TAggregate, TId>(DbContext context) : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull, IEquatable<TId>
{
    /// <summary>The underlying <see cref="DbContext"/>.</summary>
    protected DbContext Context => context;
    /// <summary>The <see cref="DbSet{TEntity}"/> for the aggregate type.</summary>
    protected DbSet<TAggregate> DbSet => context.Set<TAggregate>();
    
    /// <inheritdoc/>
    public async Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = new CancellationToken())
    {
        return await DbSet.FirstOrDefaultAsync(c => c.Id.Equals(id), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<TAggregate>> GetAllAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        return await DbSet.ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(TAggregate aggregateRoot, CancellationToken cancellationToken = new CancellationToken())
    {
        await DbSet.AddAsync(aggregateRoot, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddRangeAsync(IEnumerable<TAggregate> aggregateRoots, CancellationToken cancellationToken = new CancellationToken())
    {
        await DbSet.AddRangeAsync(aggregateRoots, cancellationToken);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(TAggregate aggregateRoot, CancellationToken cancellationToken = new CancellationToken())
    {
        DbSet.Update(aggregateRoot);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UpdateRangeAsync(IEnumerable<TAggregate> aggregateRoots, CancellationToken cancellationToken = new CancellationToken())
    {
        DbSet.UpdateRange(aggregateRoots);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveAsync(TAggregate aggregateRoot, CancellationToken cancellationToken = new CancellationToken())
    {
        DbSet.Remove(aggregateRoot);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveRangeAsync(IEnumerable<TAggregate> aggregateRoots, CancellationToken cancellationToken = new CancellationToken())
    {
        DbSet.RemoveRange(aggregateRoots);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task RemoveByIdAsync(TId id, CancellationToken cancellationToken = new CancellationToken())
    {
        var aggregate = await GetByIdAsync(id, cancellationToken);
        if (aggregate is null) return;
        
        DbSet.Remove(aggregate);
    }
}