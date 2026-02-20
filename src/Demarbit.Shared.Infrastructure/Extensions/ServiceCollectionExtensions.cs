using Demarbit.Shared.Domain.Contracts;
using Demarbit.Shared.Infrastructure.Persistence;
using Demarbit.Shared.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Demarbit.Shared.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the shared infrastructure services: DbContext, IUnitOfWork,
    /// IDateTimeProvider, IEventIdempotencyService, and a DbContext health check.
    /// <para>
    /// The consumer configures the database provider via <paramref name="configureDbContext"/>.
    /// This keeps the shared library database provider agnostic.
    /// </para>
    /// </summary>
    /// <typeparam name="TContext">
    /// The concrete DbContext type, inheriting from <see cref="AppDbContextBase{TContext}"/>.
    /// </typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureDbContext">
    /// Action to configure the database provider (e.g. <c>UseNpgsql</c>, <c>UseSqlServer</c>),
    /// naming conventions, and any other provider-specific options.
    /// </param>
    /// <param name="userProviderType">
    /// The service type for the implementation of the ICurrentUserProvider interface.
    /// If no type is specified, the standard <see cref="EmptyCurrentUserProvider" /> will be used which does not track session context
    /// </param>
    /// <param name="tenantProviderType">
    /// The service type for the implementation of the ICurrentTenantProvider interface.
    /// If no type is specified, the standard <see cref="EmptyCurrentTenantProvider" /> will be used which does not track session context
    /// </param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// // PostgreSQL setup:
    /// services.AddSharedInfrastructure&lt;AppDbContext&gt;(options =&gt;
    /// {
    ///     options.UseNpgsql(connectionString, cfg =&gt;
    ///         cfg.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
    ///     options.UseSnakeCaseNamingConvention();
    /// });
    ///
    /// // SQL Server setup:
    /// services.AddSharedInfrastructure&lt;AppDbContext&gt;(options =&gt;
    /// {
    ///     options.UseSqlServer(connectionString, cfg =&gt;
    ///         cfg.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddSharedInfrastructure<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext,
        Type? userProviderType,
        Type? tenantProviderType)
        where TContext : AppDbContextBase<TContext>
    {
        // Register the DbContext with the consumer's provider configuration.
        services.AddDbContext<TContext>(
            (_, options) => configureDbContext(options));

        // Wire up IUnitOfWork → the concrete DbContext.
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TContext>());

        // Infrastructure services.
        services.AddScoped<IEventIdempotencyService, EventIdempotencyService<TContext>>();
        
        // Session services
        services.AddScoped(typeof(ICurrentUserProvider), userProviderType ?? typeof(EmptyCurrentUserProvider));
        services.AddScoped(typeof(ICurrentTenantProvider),  tenantProviderType ?? typeof(EmptyCurrentTenantProvider));

        return services;
    }
    
    /// <summary>
    /// Adds a health check for the registered <typeparamref name="TContext"/>.
    /// This is provider-agnostic — it checks whether the DbContext can connect.
    /// <para>
    /// For provider-specific health checks (e.g. <c>AddNpgSql</c>), register them
    /// in your project's DI setup.
    /// </para>
    /// </summary>
    public static IHealthChecksBuilder AddDbContextHealthCheck<TContext>(
        this IHealthChecksBuilder builder,
        string name = "dbcontext",
        params string[] tags)
        where TContext : DbContext
    {
        builder.AddDbContextCheck<TContext>(
            name: name,
            tags: tags.Length > 0 ? tags : ["ready"]);

        return builder;
    }
}