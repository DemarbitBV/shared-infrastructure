using Demarbit.Shared.Domain.Models;
using Demarbit.Shared.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Demarbit.Shared.Infrastructure.Configurations;

internal sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .IsRequired();

        builder.Property(e => e.ProcessedAt)
            .IsRequiredTimestamp();

        builder.Property(e => e.EventId)
            .IsForeignKey();
        
        builder.Property(e => e.EventType)
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(e => e.HandlerType)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(e => new { e.EventId, e.HandlerType });
    }
}