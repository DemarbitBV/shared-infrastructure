using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Demarbit.Shared.Infrastructure.Persistence;

/// <summary>
/// Abstract base class for EF Core design-time DbContext factories.
/// Handles the boilerplate; the consumer provides the connection string, provider configuration,
/// and context construction.
/// </summary>
/// <example>
/// <code>
/// internal sealed class AppDesignTimeFactory
///     : DesignTimeDbContextFactoryBase&lt;AppDbContext&gt;
/// {
///     protected override string GetConnectionString()
///         =&gt; "Host=localhost;Port=5432;Database=mydb;Username=postgres;Password=secret";
///
///     protected override void ConfigureProvider(DbContextOptionsBuilder&lt;AppDbContext&gt; builder, string connectionString)
///     {
///         builder.UseNpgsql(connectionString, cfg =&gt;
///             cfg.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
///         builder.UseSnakeCaseNamingConvention();
///     }
///
///     protected override AppDbContext CreateContext(DbContextOptions&lt;AppDbContext&gt; options)
///         =&gt; new(options, new DesignTimeSessionContext(), new DesignTimeDateTimeProvider());
/// }
/// </code>
/// </example>
public abstract class DesignTimeDbContextFactoryBase<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    public TContext CreateDbContext(string[] args)
    {
        var connectionString = GetConnectionString();

        var builder = new DbContextOptionsBuilder<TContext>();
        ConfigureProvider(builder, connectionString);

        return CreateContext(builder.Options);
    }

    /// <summary>
    /// Returns the connection string for design-time operations (migrations).
    /// Typically a local development database connection string.
    /// </summary>
    protected abstract string GetConnectionString();

    /// <summary>
    /// Configures the database provider (UseNpgsql, UseSqlServer, etc.) and any
    /// provider-specific options like naming conventions or migration assembly.
    /// </summary>
    protected abstract void ConfigureProvider(
        DbContextOptionsBuilder<TContext> builder,
        string connectionString);

    /// <summary>
    /// Creates the concrete DbContext instance with the configured options.
    /// </summary>
    protected abstract TContext CreateContext(DbContextOptions<TContext> options);
}