# Demarbit.Shared.Infrastructure

Shared EF Core infrastructure library for DDD / Clean Architecture projects. Provides an abstract `DbContext` base class with audit stamping, domain event collection, unit-of-work transaction management, repository base classes, entity configuration helpers, paging extensions, and DI wiring.

[![CI-CD](https://github.com/DemarbitBV/shared-infrastructure/actions/workflows/ci-cd.yml/badge.svg)](https://github.com/DemarbitBV/shared-infrastructure/actions/workflows/ci-cd.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=DemarbitBV_shared-infrastructure&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=DemarbitBV_shared-infrastructure)
[![NuGet](https://img.shields.io/nuget/v/Demarbit.Shared.Infrastructure.svg)](https://www.nuget.org/packages/Demarbit.Shared.Infrastructure/)

**Database provider agnostic** — consumers configure their own provider (PostgreSQL, SQL Server, SQLite, etc.).

## Installation

```bash
dotnet add package Demarbit.Shared.Infrastructure
```

The consumer must also install their chosen database provider:

```bash
# PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add package EFCore.NamingConventions          # optional, for snake_case

# SQL Server
dotnet add package Microsoft.EntityFrameworkCore.SqlServer

# SQLite (testing)
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

## Dependencies

| Package | Purpose |
|---------|---------|
| `Demarbit.Shared.Domain` | `EntityBase<TId>`, `AggregateRoot<TId>`, `IDomainEvent`, `IUnitOfWork`, `ITenantEntity`, `ProcessedEvent` |
| `Demarbit.Shared.Application` | `PagedResult<T>`å |
| `Microsoft.EntityFrameworkCore` | Core EF abstractions |
| `Microsoft.EntityFrameworkCore.Relational` | Transaction support, `IsRelational()`, `MigrateAsync()` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | DI registration |
| `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` | DbContext health check |
| `Microsoft.Extensions.Logging.Abstractions` | Bootstrap logging |

## Quick Start

### 1. Create Your DbContext

```csharp
using Demarbit.Shared.Infrastructure.Persistence;

internal sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUserProvider userProvider,
    ICurrentTenantProvider tenantProvider)
    : AppDbContextBase<AppDbContext>(options, userProvider, tenantProvider)
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        // Provider-specific setup (only if needed)
        modelBuilder.HasPostgresExtension("pg_trgm");

        // Scan for IEntityTypeConfiguration implementations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Per-user query filters
        modelBuilder.Entity<Client>().HasQueryFilter(x => x.UserId == SessionContext.UserId);
        modelBuilder.Entity<TimeEntry>().HasQueryFilter(x => x.UserId == SessionContext.UserId);
    }
}
```

### 2. Register in DI

```csharp
using Demarbit.Shared.Infrastructure.Extensions;

// PostgreSQL
services.AddSharedInfrastructure<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString, cfg =>
        cfg.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
    options.UseSnakeCaseNamingConvention();
},
typeof(MyUserProvider),
typeof(MyTenantProvider));

// SQL Server
services.AddSharedInfrastructure<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, cfg =>
        cfg.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
});
```

This registers:
- `TContext` (your DbContext)
- `IUnitOfWork` → your DbContext
- `IEventIdempotencyService`
- `ICurrentUserProvider` (optional, otherwise a null-provider is auto included)
- `ICurrentTenantProvider` (optional, otherwise a null-provider is auto included)

### 3. Create Entity Configurations

```csharp
using Demarbit.Shared.Infrastructure.Extensions;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        // One call configures: PK, audit fields, DomainEvents ignore, IUserEntity index
        builder.IsEntity();

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasMaxLength(320)
            .IsRequired(false);
    }
}
```

### 4. Create Repositories

```csharp
using Demarbit.Shared.Infrastructure.Repositories;

internal sealed class ClientRepository(AppDbContext context)
    : RepositoryBase<Client>(context), IClientRepository
{
    // GetByIdAsync, GetAllAsync, AddAsync, UpdateAsync, RemoveAsync, etc.
    // are all inherited. Add custom queries here:

    public async Task<Client?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(c => c.Email == email, ct);
    }
}
```

### 5. Database Bootstrap

```csharp
using Demarbit.Shared.Infrastructure.Bootstrap;

var app = builder.Build();

// Apply migrations on startup:
await app.Services.BootstrapDatabaseAsync<AppDbContext>();

app.Run();
```

### 6. Design-Time Migration Factory

```csharp
using Demarbit.Shared.Infrastructure.Persistence;

internal sealed class AppDesignTimeFactory
    : DesignTimeDbContextFactoryBase<AppDbContext>
{
    protected override string GetConnectionString()
        => "Host=localhost;Port=5432;Database=mydb;Username=postgres;Password=secret";

    protected override void ConfigureProvider(
        DbContextOptionsBuilder<AppDbContext> builder, string connectionString)
    {
        builder.UseNpgsql(connectionString, cfg =>
            cfg.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
        builder.UseSnakeCaseNamingConvention();
    }

    protected override AppDbContext CreateContext(DbContextOptions<AppDbContext> options)
        => new(options, new DesignTimeSessionContext(), new DesignTimeDateTimeProvider());
}

// Design-time stubs:
internal sealed class DesignTimeSessionContext : ISessionContext
{
    public Guid UserId => Guid.Empty;
    public string Locale => string.Empty;
    public string Timezone => "UTC";
}

internal sealed class DesignTimeDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
```

### 7. Health Checks

```csharp
using Demarbit.Shared.Infrastructure.Extensions;

// Provider-agnostic DbContext check (included in the library)
builder.Services.AddHealthChecks()
    .AddDbContextHealthCheck<AppDbContext>();

// Provider-specific checks (consumer adds their own NuGet + registration)
builder.Services.AddHealthChecks()
    .AddDbContextHealthCheck<AppDbContext>()
    .AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);
```

## What's Included

| Type | Purpose |
|------|---------|
| `AppDbContextBase<TContext>` | Abstract base: audit stamping, domain events, UoW transactions |
| `DesignTimeDbContextFactoryBase<TContext>` | Abstract base for design-time migration factories |
| `ProcessedEventConfiguration` | EF Core config for the idempotency tracking table |
| `RepositoryBase<T, TId>` / `RepositoryBase<T>` | Full CRUD repository with generic ID support |
| `ReadOnlyRepositoryBase<T, TId>` / `ReadOnlyRepositoryBase<T>` | No-tracking read-only queries |
| `EventIdempotencyService<TContext>` | `IEventIdempotencyService` — adds to change tracker only (no standalone save) |
| `ConfigurationException` | Missing/invalid configuration values |
| `EntityTypeBuilderExtensions.IsEntity()` | One-call entity configuration (PK, audit, events, user index) |
| `PropertyBuilderExtensions` | `IsPrimaryKey`, `IsForeignKey`, timestamps, dates — no column types |
| `QueryablePagingExtensions` | `IQueryable<T>` → `PagedResult<TVm>` |
| `ServiceCollectionExtensions` | `AddSharedInfrastructure<TContext>()` DI registration |
| `ServiceProviderExtensions` | `BootstrapDatabaseAsync<TContext>()` migration runner |

## Package Family

```
Demarbit.Shared.Domain               ← zero deps
    ↑
Demarbit.Shared.Application          ← Domain + M.E.DI + M.E.Logging
    ↑
Demarbit.Shared.Infrastructure       ← Domain + Application + M.EF.Core + M.EF.Core.Relational
    ↑
[Your Project]                       ← adds provider NuGet (Npgsql, SqlServer, etc.)
```
