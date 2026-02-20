using Demarbit.Shared.Infrastructure.Extensions;
using Demarbit.Shared.Infrastructure.Tests._Fixtures;

namespace Demarbit.Shared.Infrastructure.Tests.Extensions;

public class QueryablePagingExtensionsTests : IDisposable
{
    private readonly TestDbContext _context;

    public QueryablePagingExtensionsTests()
    {
        _context = DbContextFactory.Create();
        SeedData();
    }

    private void SeedData()
    {
        for (var i = 1; i <= 25; i++)
        {
            _context.TestAggregates.Add(new TestAggregate($"Item {i:D2}"));
        }
        _context.SaveChanges();
    }

    [Fact]
    public async Task ToPagedResultAsync_ReturnsCorrectPageSize()
    {
        var result = await _context.TestAggregates
            .OrderBy(e => e.Name)
            .ToPagedResultAsync(page: 1, pageSize: 10);

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task ToPagedResultAsync_SecondPage_ReturnsCorrectItems()
    {
        var result = await _context.TestAggregates
            .OrderBy(e => e.Name)
            .ToPagedResultAsync(page: 2, pageSize: 10);

        Assert.Equal(10, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(2, result.Page);
    }

    [Fact]
    public async Task ToPagedResultAsync_LastPage_ReturnsRemainingItems()
    {
        var result = await _context.TestAggregates
            .OrderBy(e => e.Name)
            .ToPagedResultAsync(page: 3, pageSize: 10);

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.Page);
    }

    [Fact]
    public async Task ToPagedResultAsync_EmptyDataSet_ReturnsEmptyResult()
    {
        var emptyContext = DbContextFactory.Create();

        var result = await emptyContext.TestAggregates
            .ToPagedResultAsync(page: 1, pageSize: 10);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task ToPagedResultAsync_WithProjection_ReturnsProjectedItems()
    {
        var result = await _context.TestAggregates
            .OrderBy(e => e.Name)
            .ToPagedResultAsync(
                page: 1,
                pageSize: 5,
                projector: q => q.Select(e => e.Name));

        Assert.Equal(5, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.All(result.Items, name => Assert.False(string.IsNullOrEmpty(name)));
    }

    [Fact]
    public async Task ToPagedResultAsync_WithProjection_AppliesProjectionAfterPaging()
    {
        var result = await _context.TestAggregates
            .OrderBy(e => e.Name)
            .ToPagedResultAsync(
                page: 1,
                pageSize: 3,
                projector: q => q.Select(e => new { e.Id, e.Name }));

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(25, result.TotalCount);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _context.Dispose();
    }
}
