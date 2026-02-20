using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Demarbit.Shared.Infrastructure.Extensions;

/// <summary>
/// Convenience extensions for <see cref="PropertyBuilder{TProperty}"/> that apply
/// common structural configuration. Provider-agnostic — no <c>HasColumnType</c> calls.
/// <para>
/// EF Core maps <c>DateTime</c>, <c>DateOnly</c>, and <c>Guid</c> to the appropriate
/// column types for each database provider automatically. If you need provider-specific
/// overrides (e.g. forcing <c>timestamp with time zone</c> in PostgreSQL), add your own
/// project-level extensions.
/// </para>
/// </summary>
public static class PropertyBuilderExtensions
{
    #region Primary / Foreign Key Helpers

    /// <summary>
    /// Configures a property as a primary key with no auto-generation.
    /// Use when the domain generates IDs (e.g. <c>Guid.NewGuid()</c> in the entity constructor).
    /// </summary>
    public static PropertyBuilder<TId> IsPrimaryKey<TId>(this PropertyBuilder<TId> builder)
        => builder
            .ValueGeneratedNever()
            .IsRequired();

    /// <summary>
    /// Configures a required <see cref="Guid"/> foreign key property.
    /// </summary>
    public static PropertyBuilder<Guid> IsForeignKey(this PropertyBuilder<Guid> builder)
        => builder
            .IsRequired();

    /// <summary>
    /// Configures an optional <see cref="Guid"/> foreign key property.
    /// </summary>
    public static PropertyBuilder<Guid?> IsOptionalForeignKey(this PropertyBuilder<Guid?> builder)
        => builder
            .IsRequired(false);

    #endregion

    #region Temporal Helpers

    /// <summary>
    /// Configures a required <see cref="DateTime"/> property.
    /// No column type is specified — EF Core maps to the appropriate provider type
    /// (<c>timestamptz</c> for PostgreSQL, <c>datetime2</c> for SQL Server, etc.).
    /// </summary>
    public static PropertyBuilder<DateTime> IsRequiredTimestamp(this PropertyBuilder<DateTime> builder)
        => builder
            .IsRequired();

    /// <summary>
    /// Configures an optional <see cref="DateTime"/> property.
    /// </summary>
    public static PropertyBuilder<DateTime?> IsOptionalTimestamp(this PropertyBuilder<DateTime?> builder)
        => builder
            .IsRequired(false);

    /// <summary>
    /// Configures a required <see cref="DateOnly"/> property.
    /// </summary>
    public static PropertyBuilder<DateOnly> IsRequiredDate(this PropertyBuilder<DateOnly> builder)
        => builder
            .IsRequired();

    /// <summary>
    /// Configures an optional <see cref="DateOnly"/> property.
    /// </summary>
    public static PropertyBuilder<DateOnly?> IsOptionalDate(this PropertyBuilder<DateOnly?> builder)
        => builder
            .IsRequired(false);

    #endregion
}
