using System.Diagnostics.CodeAnalysis;
using Demarbit.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Demarbit.Shared.Infrastructure.Extensions;

/// <summary>
/// Extension methods for bootstrapping the database on application startup.
/// </summary>
[SuppressMessage("Roslyn", "CA1873", Justification = "The code in this class only runs once on startup")]
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations (for relational providers) or ensures the database
    /// is created (for InMemory/testing). Call this during application startup.
    /// <para>
    /// <b>Production note:</b> Auto-running migrations on startup can be risky during
    /// rolling deployments. Consider using a separate migration step in your CI/CD pipeline
    /// and only calling this in development/staging environments.
    /// </para>
    /// </summary>
    /// <typeparam name="TContext">The concrete DbContext type.</typeparam>
    /// <param name="provider">The application's root service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <example>
    /// <code>
    /// var app = builder.Build();
    ///
    /// // Apply migrations on startup (development / staging):
    /// await app.Services.BootstrapDatabaseAsync&lt;AppDbContext&gt;();
    ///
    /// app.Run();
    /// </code>
    /// </example>
    public static async Task BootstrapDatabaseAsync<TContext>(
        this IServiceProvider provider,
        CancellationToken cancellationToken = default)
        where TContext : AppDbContextBase<TContext>
    {
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TContext>>();

        if (context.Database.IsRelational())
        {
            logger.LogInformation("Applying database migrations for {ContextType}...", typeof(TContext).Name);
            await context.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migrations completed for {ContextType}.", typeof(TContext).Name);
        }
        else
        {
            // InMemory or other non-relational providers — just ensure the database exists.
            logger.LogInformation("Ensuring database is created for {ContextType} (non-relational provider).", typeof(TContext).Name);
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Ensures the database schema exists without applying migrations.
    /// Useful for integration tests with InMemory or SQLite providers.
    /// </summary>
    public static async Task EnsureDatabaseCreatedAsync<TContext>(
        this IServiceProvider provider,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }
}
