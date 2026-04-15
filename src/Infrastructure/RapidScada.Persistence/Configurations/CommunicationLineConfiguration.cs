using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RapidScada.Domain.Entities;
using RapidScada.Domain.ValueObjects;
using System.Text.Json;

namespace RapidScada.Persistence.Configurations;

/// <summary>
/// EF Core configuration for CommunicationLine entity
/// </summary>
public sealed class CommunicationLineConfiguration : IEntityTypeConfiguration<CommunicationLine>
{
    public void Configure(EntityTypeBuilder<CommunicationLine> builder)
    {
        builder.ToTable("communication_lines");

        // Primary key
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(
                id => id.Value,
                value => CommunicationLineId.Create(value))
            .HasColumnName("id");

        // Value objects
        builder.Property(c => c.Name)
            .HasConversion(
                name => name.Value,
                value => CommunicationLineName.Create(value).Value)
            .HasMaxLength(CommunicationLineName.MaxLength)
            .HasColumnName("name")
            .IsRequired();

        // Enums
        builder.Property(c => c.ChannelType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("channel_type")
            .IsRequired();

        // Store connection settings as JSON (polymorphic)
        builder.Property(c => c.ConnectionSettings)
            .HasConversion(
                settings => JsonSerializer.Serialize(settings, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<ConnectionSettings>(json, (JsonSerializerOptions?)null)!)
            .HasColumnType("jsonb")
            .HasColumnName("connection_settings")
            .IsRequired();

        // Simple properties
        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(c => c.LastActivityAt)
            .HasColumnName("last_activity_at");

        // Store device IDs as array (PostgreSQL specific)
        builder.Ignore(c => c.DeviceIds);

        // Indexes
        builder.HasIndex(c => c.Name)
            .IsUnique()
            .HasDatabaseName("idx_communication_lines_name");

        builder.HasIndex(c => c.IsActive)
            .HasDatabaseName("idx_communication_lines_is_active");

        builder.HasIndex(c => c.ChannelType)
            .HasDatabaseName("idx_communication_lines_channel_type");

        // Ignore domain events
        builder.Ignore(c => c.DomainEvents);
    }
}
