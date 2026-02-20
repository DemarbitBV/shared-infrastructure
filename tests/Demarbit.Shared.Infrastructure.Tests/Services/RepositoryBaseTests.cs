using Demarbit.Shared.Infrastructure.Tests._Fixtures;

namespace Demarbit.Shared.Infrastructure.Tests.Services;

public class RepositoryBaseTests : IDisposable
{
    private readonly TestDbContext _context;
    private readonly TestRepository _sut;

    public RepositoryBaseTests()
    {
        _context = DbContextFactory.Create();
        _sut = new TestRepository(_context);
    }

    [Fact]
    public async Task AddAsync_AddsEntityToDbSet()
    {
        var entity = new TestAggregate("Test");

        await _sut.AddAsync(entity);
        await _context.SaveChangesAsync();

        var found = await _context.TestAggregates.FindAsync(entity.Id);
        Assert.NotNull(found);
        Assert.Equal("Test", found.Name);
    }

    [Fact]
    public async Task AddRangeAsync_AddsMultipleEntities()
    {
        var entities = new[]
        {
            new TestAggregate("First"),
            new TestAggregate("Second")
        };

        await _sut.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        var all = await _sut.GetAllAsync();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityExists_ReturnsEntity()
    {
        var entity = new TestAggregate("Test");
        await _sut.AddAsync(entity);
        await _context.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(entity.Id);

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WhenEntityDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        await _sut.AddAsync(new TestAggregate("First"));
        await _sut.AddAsync(new TestAggregate("Second"));
        await _sut.AddAsync(new TestAggregate("Third"));
        await _context.SaveChangesAsync();

        var result = await _sut.GetAllAsync();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        var result = await _sut.GetAllAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntity()
    {
        var entity = new TestAggregate("Original");
        await _sut.AddAsync(entity);
        await _context.SaveChangesAsync();

        entity.Name = "Updated";
        await _sut.UpdateAsync(entity);
        await _context.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(entity.Id);
        Assert.NotNull(result);
        Assert.Equal("Updated", result.Name);
    }

    [Fact]
    public async Task UpdateRangeAsync_UpdatesMultipleEntities()
    {
        var first = new TestAggregate("First");
        var second = new TestAggregate("Second");
        await _sut.AddRangeAsync([first, second]);
        await _context.SaveChangesAsync();

        first.Name = "Updated First";
        second.Name = "Updated Second";
        await _sut.UpdateRangeAsync([first, second]);
        await _context.SaveChangesAsync();

        var all = await _sut.GetAllAsync();
        Assert.Contains(all, e => e.Name == "Updated First");
        Assert.Contains(all, e => e.Name == "Updated Second");
    }

    [Fact]
    public async Task RemoveAsync_RemovesEntity()
    {
        var entity = new TestAggregate("Test");
        await _sut.AddAsync(entity);
        await _context.SaveChangesAsync();

        await _sut.RemoveAsync(entity);
        await _context.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(entity.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveRangeAsync_RemovesMultipleEntities()
    {
        var first = new TestAggregate("First");
        var second = new TestAggregate("Second");
        await _sut.AddRangeAsync([first, second]);
        await _context.SaveChangesAsync();

        await _sut.RemoveRangeAsync([first, second]);
        await _context.SaveChangesAsync();

        var all = await _sut.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task RemoveByIdAsync_WhenEntityExists_RemovesEntity()
    {
        var entity = new TestAggregate("Test");
        await _sut.AddAsync(entity);
        await _context.SaveChangesAsync();

        await _sut.RemoveByIdAsync(entity.Id);
        await _context.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(entity.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveByIdAsync_WhenEntityDoesNotExist_DoesNotThrow()
    {
        var exception = await Record.ExceptionAsync(() => _sut.RemoveByIdAsync(Guid.NewGuid()));

        Assert.Null(exception);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _context.Dispose();
    }
}
