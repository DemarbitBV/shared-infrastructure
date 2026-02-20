using Demarbit.Shared.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace Demarbit.Shared.Infrastructure.Extensions;

/// <summary>
/// Extension methods for <see cref="IQueryable{T}"/> that produce
/// <see cref="PagedResult{T}"/> results with EF Core async execution.
/// </summary>
public static class QueryablePagingExtensions
{
    /// <summary>
    /// Paginates a query and projects it to view models, returning a <see cref="PagedResult{TVm}"/>.
    /// </summary>
    /// <typeparam name="T">The source entity type.</typeparam>
    /// <typeparam name="TVm">The projected view model type.</typeparam>
    /// <param name="query">The source queryable (typically a DbSet or filtered query).</param>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="projector">
    /// A function that applies the projection from <typeparamref name="T"/> to <typeparamref name="TVm"/>.
    /// This runs inside the EF Core query pipeline — use <c>Select</c> for server-side projection.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="PagedResult{TVm}"/> with the projected items and pagination metadata.</returns>
    /// <example>
    /// <code>
    /// var result = await Context.Set&lt;Client&gt;()
    ///     .Where(c => c.IsActive)
    ///     .OrderBy(c => c.Name)
    ///     .ToPagedResultAsync(
    ///         page: 1,
    ///         pageSize: 20,
    ///         projector: q => q.Select(c => new ClientOverviewVm
    ///         {
    ///             Id = c.Id,
    ///             Name = c.Name
    ///         }),
    ///         ct: cancellationToken);
    /// </code>
    /// </example>
    public static async Task<PagedResult<TVm>> ToPagedResultAsync<T, TVm>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        Func<IQueryable<T>, IQueryable<TVm>> projector,
        CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var total = await query.CountAsync(ct);
        var items = await projector(query.Skip(skip).Take(pageSize)).ToListAsync(ct);

        return new PagedResult<TVm>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Paginates and materializes a query directly (no projection), returning a <see cref="PagedResult{T}"/>.
    /// Use when the query already produces the desired result type.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var total = await query.CountAsync(ct);
        var items = await query.Skip(skip).Take(pageSize).ToListAsync(ct);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }
}