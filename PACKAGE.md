# Demarbit.Shared.Infrastructure

Shared EF Core infrastructure layer implementing Unit of Work, Repository, and audit stamping for DDD/Clean Architecture projects.

- **Target Framework:** .NET 10 (`net10.0`)
- **Key Dependencies:** `Demarbit.Shared.Domain` (v1.0.4), `Demarbit.Shared.Application` (v1.0.4), `Microsoft.EntityFrameworkCore` (v10.0.3), `Microsoft.EntityFrameworkCore.Relational` (v10.0.3)

---

## Quick Start

### 1. Register services in DI

```csharp
using Demarbit.Shared.Infrastructure.Extensions;

services.AddSharedInfrastructure<AppDbContext>(
    configureDbContext: options =>
    {
        options.UseNpgsql(connectionString, cfg =>
            cfg.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
    },
    userProviderType: null,    // uses default CurrentUserProvider
    tenantProviderType: null   // uses default CurrentTenantProvider
);

services.AddHealthChecks()
    .AddDbContextHealthCheck<AppDbContext>();
```

### 2. Create your DbContext

```csharp
using Demarbit.Shared.Infrastructure.Persistence;

internal sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUserProvider userProvider,
    ICurrentTenantProvider tenantProvider)
    : AppDbContextBase<AppDbContext>(options, userProvider, tenantProvider)
{
    public DbSet<Client> Clients => Set<Client>();

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

### 3. Bootstrap the database on startup

```csharp
var app = builder.Build();
await app.Services.BootstrapDatabaseAsync<AppDbContext>();
app.Run();
```

---

## Core Concepts

### Database Provider Agnosticism

This package never specifies column types or provider-specific configuration. EF Core maps `DateTime`, `DateOnly`, and `Guid` to the appropriate column types for each database provider automatically. The consuming project configures its own provider (PostgreSQL, SQL Server, SQLite, etc.) via the `AddSharedInfrastructure` extension method.

### Automatic Audit Stamping

On every `SaveChanges`/`SaveChangesAsync`, `AppDbContextBase` intercepts tracked entities implementing `IAuditableEntity` (from `Demarbit.Shared.Domain`) and automatically sets:
- `CreatedAt`, `CreatedBy` on `EntityState.Added`
- `UpdatedAt`, `UpdatedBy` on `EntityState.Modified`
- `TenantId` on `EntityState.Added` for entities implementing `ITenantEntity`

The current user and tenant are sourced from `ICurrentUserProvider` and `ICurrentTenantProvider`.

### Domain Event Collection

`AppDbContextBase` collects domain events from all tracked `AggregateRoot` entities before each save. Events are dequeued from aggregates and buffered internally. After saving, call `GetAndClearPendingEvents()` to retrieve them for dispatching (typically done by a pipeline behavior in the application layer).

### Unit of Work + Transactions

`AppDbContextBase` implements `IUnitOfWork` (from `Demarbit.Shared.Domain`) providing explicit transaction management via `BeginTransactionAsync`, `CommitTransactionAsync`, and `RollbackTransactionAsync`. The InMemory provider is handled gracefully (transactions are no-ops).

### Event Idempotency

The `EventIdempotencyService` (registered automatically) tracks which domain events have already been processed by which handler, preventing duplicate handling. It persists records in the `ProcessedEvents` table managed by `AppDbContextBase`.

### Repository Pattern

`RepositoryBase<TAggregate, TId>` provides a standard CRUD implementation for aggregate roots using `IRepository<TAggregate, TId>` from `Demarbit.Shared.Domain`. Consuming projects create concrete repositories by inheriting from this base.

---

## Public API Reference

### Persistence (`Demarbit.Shared.Infrastructure.Persistence`)

#### `AppDbContextBase<TContext>`

```csharp
public abstract class AppDbContextBase<TContext> : DbContext, IUnitOfWork
    where TContext : DbContext
```

Abstract base DbContext with audit stamping, domain event collection, and transaction management.

**Constructor parameters:**
- `DbContextOptions<TContext> options`
- `ICurrentUserProvider userProvider` — provides the current user ID for audit fields
- `ICurrentTenantProvider tenantProvider` — provides the current tenant ID for multi-tenant entities

**Properties:**
- `DbSet<ProcessedEvent> ProcessedEvents` — tracks processed domain events for idempotency

**Methods:**
| Method | Signature | Description |
|--------|-----------|-------------|
| `SaveChanges` | `override int SaveChanges(bool acceptAllChangesOnSuccess)` | Stamps audit fields and collects domain events before saving |
| `SaveChangesAsync` | `override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken)` | Async version of above |
| `GetAndClearPendingEvents` | `IReadOnlyList<IDomainEvent> GetAndClearPendingEvents()` | Returns buffered domain events and clears the internal buffer |
| `BeginTransactionAsync` | `Task BeginTransactionAsync(CancellationToken)` | Starts a new transaction (no-op for InMemory) |
| `CommitTransactionAsync` | `Task CommitTransactionAsync(CancellationToken)` | Commits current transaction; rolls back on failure |
| `RollbackTransactionAsync` | `Task RollbackTransactionAsync(CancellationToken)` | Rolls back current transaction |

**Subclassing:** Override `OnModelCreating` only via `base.OnModelCreating(modelBuilder)` (do not skip the base call — it registers `ProcessedEventConfiguration`). Apply your own configurations inside `OnModelCreating` after calling `base`.

> **Note:** The XML doc example references a `ConfigureModel` abstract method, but the actual implementation uses the standard EF Core `OnModelCreating` override. There is no abstract `ConfigureModel` method on this class.

#### `DesignTimeDbContextFactoryBase<TContext>`

```csharp
public abstract class DesignTimeDbContextFactoryBase<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
```

Base class for design-time DbContext factories used by EF Core tooling (`dotnet ef migrations`).

**Abstract methods to implement:**
| Method | Signature | Description |
|--------|-----------|-------------|
| `GetConnectionString` | `protected abstract string GetConnectionString()` | Returns a local dev connection string |
| `ConfigureProvider` | `protected abstract void ConfigureProvider(DbContextOptionsBuilder<TContext>, string connectionString)` | Configures the database provider |
| `CreateContext` | `protected abstract TContext CreateContext(DbContextOptions<TContext>)` | Instantiates the concrete DbContext |

**Example implementation:**

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
    }

    protected override AppDbContext CreateContext(DbContextOptions<AppDbContext> options)
        => new(options, new CurrentUserProvider(), new CurrentTenantProvider());
}
```

---

### Services (`Demarbit.Shared.Infrastructure.Services`)

#### `RepositoryBase<TAggregate, TId>`

```csharp
public abstract class RepositoryBase<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull, IEquatable<TId>
```

Generic base repository for aggregate roots. Accepts `DbContext` (not a concrete subclass) so it works with any `AppDbContextBase` derivative.

**Constructor parameters:**
- `DbContext context`

**Protected members:**
- `DbContext Context` — the underlying context
- `DbSet<TAggregate> DbSet` — the `DbSet` for the aggregate type

**Methods (all async):**
| Method | Signature |
|--------|-----------|
| `GetByIdAsync` | `Task<TAggregate?> GetByIdAsync(TId id, CancellationToken)` |
| `GetAllAsync` | `Task<List<TAggregate>> GetAllAsync(CancellationToken)` |
| `AddAsync` | `Task AddAsync(TAggregate, CancellationToken)` |
| `AddRangeAsync` | `Task AddRangeAsync(IEnumerable<TAggregate>, CancellationToken)` |
| `UpdateAsync` | `Task UpdateAsync(TAggregate, CancellationToken)` |
| `UpdateRangeAsync` | `Task UpdateRangeAsync(IEnumerable<TAggregate>, CancellationToken)` |
| `RemoveAsync` | `Task RemoveAsync(TAggregate, CancellationToken)` |
| `RemoveRangeAsync` | `Task RemoveRangeAsync(IEnumerable<TAggregate>, CancellationToken)` |
| `RemoveByIdAsync` | `Task RemoveByIdAsync(TId id, CancellationToken)` |

**Example implementation:**

```csharp
using Demarbit.Shared.Infrastructure.Services;

internal sealed class ClientRepository(AppDbContext context)
    : RepositoryBase<Client, Guid>(context), IClientRepository
{
    // Add custom query methods here:
    public async Task<Client?> GetByEmailAsync(string email, CancellationToken ct)
        => await DbSet.FirstOrDefaultAsync(c => c.Email == email, ct);
}
```

#### `CurrentUserProvider`

```csharp
public sealed class CurrentUserProvider : ICurrentUserProvider
```

Default implementation of `ICurrentUserProvider`. Stores the current user ID in a scoped instance. Call `SetUserId(Guid?)` to set it (typically done by middleware).

**Properties:** `Guid? UserId { get; }`
**Methods:** `void SetUserId(Guid? userId)`

#### `CurrentTenantProvider`

```csharp
public class CurrentTenantProvider : ICurrentTenantProvider
```

Default implementation of `ICurrentTenantProvider`. Stores the current tenant ID in a scoped instance. Call `SetTenantId(Guid?)` to set it (typically done by middleware).

**Properties:** `Guid? TenantId { get; }`
**Methods:** `void SetTenantId(Guid? tenantId)`

---

### Extensions (`Demarbit.Shared.Infrastructure.Extensions`)

#### `ServiceCollectionExtensions`

```csharp
public static class ServiceCollectionExtensions
```

| Method | Signature | Description |
|--------|-----------|-------------|
| `AddSharedInfrastructure<TContext>` | `IServiceCollection AddSharedInfrastructure<TContext>(this IServiceCollection, Action<DbContextOptionsBuilder>, Type? userProviderType, Type? tenantProviderType) where TContext : AppDbContextBase<TContext>` | Registers DbContext, `IUnitOfWork`, `IEventIdempotencyService`, `ICurrentUserProvider`, and `ICurrentTenantProvider` |
| `AddDbContextHealthCheck<TContext>` | `IHealthChecksBuilder AddDbContextHealthCheck<TContext>(this IHealthChecksBuilder, string name = "dbcontext", params string[] tags) where TContext : DbContext` | Adds a DbContext connectivity health check (defaults to `"ready"` tag) |

**Custom providers:** Pass custom types for `userProviderType` / `tenantProviderType` to use your own implementations. Pass `null` for the defaults.

```csharp
// With custom providers:
services.AddSharedInfrastructure<AppDbContext>(
    options => options.UseNpgsql(connectionString),
    userProviderType: typeof(HttpContextUserProvider),
    tenantProviderType: typeof(HttpContextTenantProvider)
);
```

#### `ServiceProviderExtensions`

```csharp
public static class ServiceProviderExtensions
```

| Method | Signature | Description |
|--------|-----------|-------------|
| `BootstrapDatabaseAsync<TContext>` | `Task BootstrapDatabaseAsync<TContext>(this IServiceProvider, CancellationToken) where TContext : AppDbContextBase<TContext>` | Applies pending migrations (relational) or ensures DB created (InMemory) |
| `EnsureDatabaseCreatedAsync<TContext>` | `Task EnsureDatabaseCreatedAsync<TContext>(this IServiceProvider, CancellationToken) where TContext : DbContext` | Creates the schema without migrations (for integration tests) |

#### `EntityTypeBuilderExtensions`

```csharp
public static class EntityTypeBuilderExtensions
```

| Method | Signature | Description |
|--------|-----------|-------------|
| `IsEntity<TEntity, TId>` | `EntityTypeBuilder<TEntity> IsEntity<TEntity, TId>(this EntityTypeBuilder<TEntity>) where TEntity : EntityBase<TId> where TId : notnull, IEquatable<TId>` | Configures PK, audit fields, domain event ignore (for aggregates), and `TenantId` index (for `ITenantEntity`) |
| `IsEntity<TEntity>` | `EntityTypeBuilder<TEntity> IsEntity<TEntity>(this EntityTypeBuilder<TEntity>) where TEntity : EntityBase` | Convenience overload for `Guid` IDs — calls `IsEntity<TEntity, Guid>()` |

**Usage in an entity configuration:**

```csharp
using Demarbit.Shared.Infrastructure.Extensions;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        // Configures PK, audit fields, domain events ignore, tenant index
        builder.IsEntity();

        builder.Property(c => c.Name)
            .HasMaxLength(200)
            .IsRequired();
    }
}
```

**What `IsEntity` configures automatically:**
- Primary key on `Id` with `ValueGeneratedNever()`
- `CreatedAt` and `UpdatedAt` as required timestamps
- `CreatedBy` and `UpdatedBy` as optional foreign keys
- Ignores `DomainEvents` property if the entity is an `AggregateRoot`
- Adds required `TenantId` FK + index if the entity implements `ITenantEntity`

#### `PropertyBuilderExtensions`

```csharp
public static class PropertyBuilderExtensions
```

Provider-agnostic property configuration helpers (no `HasColumnType` calls).

| Method | Signature | Description |
|--------|-----------|-------------|
| `IsPrimaryKey<TId>` | `PropertyBuilder<TId> IsPrimaryKey<TId>(this PropertyBuilder<TId>)` | `ValueGeneratedNever()` + `IsRequired()` |
| `IsForeignKey` | `PropertyBuilder<Guid> IsForeignKey(this PropertyBuilder<Guid>)` | Required `Guid` FK |
| `IsOptionalForeignKey` | `PropertyBuilder<Guid?> IsOptionalForeignKey(this PropertyBuilder<Guid?>)` | Optional `Guid?` FK |
| `IsRequiredTimestamp` | `PropertyBuilder<DateTime> IsRequiredTimestamp(this PropertyBuilder<DateTime>)` | Required `DateTime` |
| `IsOptionalTimestamp` | `PropertyBuilder<DateTime?> IsOptionalTimestamp(this PropertyBuilder<DateTime?>)` | Optional `DateTime?` |
| `IsRequiredDate` | `PropertyBuilder<DateOnly> IsRequiredDate(this PropertyBuilder<DateOnly>)` | Required `DateOnly` |
| `IsOptionalDate` | `PropertyBuilder<DateOnly?> IsOptionalDate(this PropertyBuilder<DateOnly?>)` | Optional `DateOnly?` |

#### `QueryablePagingExtensions`

```csharp
public static class QueryablePagingExtensions
```

| Method | Signature | Description |
|--------|-----------|-------------|
| `ToPagedResultAsync<T, TVm>` | `Task<PagedResult<TVm>> ToPagedResultAsync<T, TVm>(this IQueryable<T>, int page, int pageSize, Func<IQueryable<T>, IQueryable<TVm>> projector, CancellationToken)` | Paginate with server-side projection |
| `ToPagedResultAsync<T>` | `Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T>, int page, int pageSize, CancellationToken)` | Paginate and materialize directly |

Returns `Demarbit.Shared.Application.Models.PagedResult<T>` which contains `Items`, `TotalCount`, `Page`, and `PageSize`.

**Page numbering is 1-based.**

```csharp
// With projection:
var result = await context.Set<Client>()
    .Where(c => c.IsActive)
    .OrderBy(c => c.Name)
    .ToPagedResultAsync(
        page: 1,
        pageSize: 20,
        projector: q => q.Select(c => new ClientOverviewVm
        {
            Id = c.Id,
            Name = c.Name
        }),
        ct: cancellationToken);

// Without projection:
var result = await context.Set<Client>()
    .OrderBy(c => c.Name)
    .ToPagedResultAsync(page: 1, pageSize: 20, ct: cancellationToken);
```

---

### Exceptions (`Demarbit.Shared.Infrastructure.Exceptions`)

#### `ConfigurationException`

```csharp
public class ConfigurationException : Exception
```

Thrown when a required configuration value is missing or invalid.

**Properties:**
- `string ConfigurationKey` — the missing/invalid key
- `string? SectionPath` — optional configuration section path for context

**Constructors:**
- `ConfigurationException(string configurationKey)` — message: `"Configuration property '{key}' is missing or invalid."`
- `ConfigurationException(string configurationKey, string sectionPath)` — message: `"Configuration property '{key}' in section '{section}' is missing or invalid."`

---

### Internal Types (for context)

#### `ProcessedEventConfiguration`

Internal EF Core entity configuration for `ProcessedEvent` (from `Demarbit.Shared.Domain.Models`). Applied automatically by `AppDbContextBase.OnModelCreating`. Creates a composite index on `(EventId, HandlerType)` for idempotency lookups.

#### `EventIdempotencyService<TContext>`

Internal service implementing `IEventIdempotencyService` (from `Demarbit.Shared.Domain.Contracts`). Registered automatically by `AddSharedInfrastructure`. Uses the `ProcessedEvents` DbSet to check and record processed events.

---

## Usage Patterns & Examples

### Implementing a complete infrastructure layer

```csharp
// 1. Define your DbContext
internal sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    ICurrentUserProvider userProvider,
    ICurrentTenantProvider tenantProvider)
    : AppDbContextBase<AppDbContext>(options, userProvider, tenantProvider)
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // Required — registers ProcessedEvents
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

// 2. Define entity configurations
internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.IsEntity(); // PK, audit fields, domain events, tenant index

        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(300).IsRequired();
    }
}

// 3. Define repositories
internal sealed class ClientRepository(AppDbContext context)
    : RepositoryBase<Client, Guid>(context), IClientRepository
{
    public async Task<Client?> GetByEmailAsync(string email, CancellationToken ct)
        => await DbSet.FirstOrDefaultAsync(c => c.Email == email, ct);
}

// 4. Define design-time factory for migrations
internal sealed class AppDesignTimeFactory
    : DesignTimeDbContextFactoryBase<AppDbContext>
{
    protected override string GetConnectionString()
        => "Host=localhost;Port=5432;Database=myapp;Username=postgres;Password=secret";

    protected override void ConfigureProvider(
        DbContextOptionsBuilder<AppDbContext> builder, string connectionString)
    {
        builder.UseNpgsql(connectionString, cfg =>
            cfg.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
    }

    protected override AppDbContext CreateContext(DbContextOptions<AppDbContext> options)
        => new(options, new CurrentUserProvider(), new CurrentTenantProvider());
}

// 5. Register in DI (Program.cs / Startup)
services.AddSharedInfrastructure<AppDbContext>(
    options => options.UseNpgsql(connectionString),
    userProviderType: typeof(HttpContextUserProvider),
    tenantProviderType: null
);

services.AddHealthChecks()
    .AddDbContextHealthCheck<AppDbContext>();

services.AddScoped<IClientRepository, ClientRepository>();

// 6. Bootstrap on startup
var app = builder.Build();
await app.Services.BootstrapDatabaseAsync<AppDbContext>();
```

### Setting user/tenant context via middleware

```csharp
public class SessionContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext,
        ICurrentUserProvider userProvider,
        ICurrentTenantProvider tenantProvider)
    {
        var userId = httpContext.User.FindFirst("sub")?.Value;
        if (Guid.TryParse(userId, out var uid))
            userProvider.SetUserId(uid);

        var tenantId = httpContext.User.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(tenantId, out var tid))
            tenantProvider.SetTenantId(tid);

        await next(httpContext);
    }
}
```

### Using the Unit of Work with domain events

```csharp
// In a pipeline behavior or command handler:
public async Task Handle(CreateClientCommand command, CancellationToken ct)
{
    var unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();

    await unitOfWork.BeginTransactionAsync(ct);
    try
    {
        // ... create aggregate, add to repository ...
        await unitOfWork.SaveChangesAsync(ct);

        // Domain events were collected during SaveChanges
        var events = unitOfWork.GetAndClearPendingEvents();

        // Dispatch events...
        foreach (var domainEvent in events)
        {
            // Check idempotency, then handle
        }

        await unitOfWork.CommitTransactionAsync(ct);
    }
    catch
    {
        await unitOfWork.RollbackTransactionAsync(ct);
        throw;
    }
}
```

---

## Integration Points

### Service Registration (`AddSharedInfrastructure<TContext>`)

Registers the following services:

| Service | Implementation | Lifetime |
|---------|---------------|----------|
| `TContext` (DbContext) | Configured via `Action<DbContextOptionsBuilder>` | Scoped |
| `IUnitOfWork` | Resolved from `TContext` | Scoped |
| `IEventIdempotencyService` | `EventIdempotencyService<TContext>` | Scoped |
| `ICurrentUserProvider` | `CurrentUserProvider` (or custom type) | Scoped |
| `ICurrentTenantProvider` | `CurrentTenantProvider` (or custom type) | Scoped |

### Health Check (`AddDbContextHealthCheck<TContext>`)

Adds a DbContext connectivity health check using `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`. Defaults to the `"ready"` tag. Pass custom tags as needed:

```csharp
services.AddHealthChecks()
    .AddDbContextHealthCheck<AppDbContext>(name: "database", tags: ["ready", "db"]);
```

### Database Bootstrap

- `BootstrapDatabaseAsync<TContext>` — For startup. Applies migrations on relational providers, calls `EnsureCreatedAsync` on non-relational (InMemory).
- `EnsureDatabaseCreatedAsync<TContext>` — For integration tests. Creates schema without migrations.

### EF Core Conventions

`AppDbContextBase.OnModelCreating` automatically applies `ProcessedEventConfiguration`, which creates the `ProcessedEvents` table with an index on `(EventId, HandlerType)`. Subclasses must call `base.OnModelCreating(modelBuilder)` to include this.

---

## Dependencies & Compatibility

### NuGet Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Demarbit.Shared.Domain` | 1.0.4 | Domain contracts (`IUnitOfWork`, `IRepository`, `IAuditableEntity`, `ITenantEntity`, `AggregateRoot`, `EntityBase`, `IDomainEvent`, `ICurrentUserProvider`, `ICurrentTenantProvider`, `IEventIdempotencyService`, `ProcessedEvent`) |
| `Demarbit.Shared.Application` | 1.0.4 | Application models (`PagedResult<T>`) |
| `Microsoft.EntityFrameworkCore` | 10.0.3 | Core EF functionality |
| `Microsoft.EntityFrameworkCore.Relational` | 10.0.3 | Relational provider support (migrations, transactions) |
| `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` | 10.0.3 | DbContext health checks |

### Peer Dependencies

- **`Demarbit.Shared.Domain`** must be available (transitive dependency). Domain types like `EntityBase`, `AggregateRoot`, `IAuditableEntity`, and `ITenantEntity` are required for entity configuration and audit stamping.
- **`Demarbit.Shared.Application`** must be available (transitive dependency). `PagedResult<T>` is used by the paging extensions.
- **A database provider package** must be installed by the consuming project (e.g., `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.SqlServer`, or `Microsoft.EntityFrameworkCore.InMemory` for tests).
