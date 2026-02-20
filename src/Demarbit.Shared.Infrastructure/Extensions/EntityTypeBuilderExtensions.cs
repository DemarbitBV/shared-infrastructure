using Demarbit.Shared.Domain.Contracts;
using Demarbit.Shared.Domain.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Demarbit.Shared.Infrastructure.Extensions;

/// <summary>
/// Convenience extensions for <see cref="EntityTypeBuilder"/> that configure
/// the standard entity boilerplate: primary key, audit fields, domain event ignore,
/// and <see cref="ITenantEntity"/> indexing.
/// </summary>
public static class EntityTypeBuilderExtensions
{
    /// <summary>
    /// Configures standard entity properties for an entity with a generic ID type:
    /// primary key, audit timestamps, audit user references, domain event ignore (for aggregates),
    /// and <see cref="ITenantEntity.TenantId"/> index (for user-scoped entities).
    /// </summary>
    /// <typeparam name="TEntity">The entity type (must extend <see cref="EntityBase"/>).</typeparam>
    /// <typeparam name="TId">The entity's identifier type.</typeparam>
    public static EntityTypeBuilder<TEntity> IsEntity<TEntity, TId>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : EntityBase<TId>
        where TId : notnull, IEquatable<TId>
    {
        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).IsPrimaryKey();

        // Audit timestamps — provider-agnostic (no HasColumnType)
        builder.Property(e => e.CreatedAt).IsRequiredTimestamp();
        builder.Property(e => e.UpdatedAt).IsRequiredTimestamp();

        // Audit user references
        builder.Property(e => e.CreatedBy).IsOptionalForeignKey();
        builder.Property(e => e.UpdatedBy).IsOptionalForeignKey();

        // If this entity is an aggregate root, ignore the DomainEvents navigation
        // so EF Core doesn't try to map it as a column or relationship.
        if (typeof(AggregateRoot<TId>).IsAssignableFrom(typeof(TEntity)))
        {
            builder.Ignore(nameof(AggregateRoot<TId>.DomainEvents));
        }

        // If this entity implements ITenantEntity, configure the TenantId FK + index.
        if (!typeof(ITenantEntity).IsAssignableFrom(typeof(TEntity))) return builder;
        
        builder.Property<Guid>(nameof(ITenantEntity.TenantId))
            .IsForeignKey();
        builder.HasIndex(nameof(ITenantEntity.TenantId));

        return builder;
    }

    /// <summary>
    /// Convenience overload for entities using <see cref="Guid"/> as their identifier.
    /// This is the common case — call <c>builder.IsEntity()</c> in your configuration.
    /// </summary>
    /// <typeparam name="TEntity">The entity type (must extend <see cref="EntityBase"/>).</typeparam>
    public static EntityTypeBuilder<TEntity> IsEntity<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : EntityBase
        => builder.IsEntity<TEntity, Guid>();
}