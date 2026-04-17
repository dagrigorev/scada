using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RapidScada.Domain.Entities;
using RapidScada.Domain.ValueObjects;
using System.Text.Json;

namespace RapidScada.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Tag entity
/// </summary>
public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");

        // Primary key
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasConversion(
                id => id.Value,
                value => TagId.Create(value))
            .HasColumnName("id");

        // Properties
        builder.Property(t => t.Number)
            .HasColumnName("number")
            .IsRequired();

        builder.Property(t => t.Name)
            .HasMaxLength(100)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(t => t.DeviceId)
            .HasConversion(
                id => id.Value,
                value => DeviceId.Create(value))
            .HasColumnName("device_id")
            .IsRequired();

        builder.Property(t => t.TagType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("tag_type")
            .IsRequired();

        builder.Property(t => t.Units)
            .HasMaxLength(20)
            .HasColumnName("units");

        builder.Ignore(t => t.Quality);

        // Store current value as JSON
        builder.Property(t => t.CurrentValue)
            .HasConversion(
                v => v != null ? JsonSerializer.Serialize(new
                {
                    value = v.Value,           // Prevent EF from
                    timestamp = v.Timestamp,   // mapping these as
                    quality = v.Quality        // separate columns
                }, (JsonSerializerOptions?)null) : null,
                v => v != null ? DeserializeTagValue(v) : null)
            .HasColumnType("jsonb")
            .HasColumnName("current_value");

        builder.Property(t => t.LastUpdateAt)
            .HasColumnName("last_update_at");

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(t => t.LowLimit)
            .HasColumnName("low_limit");

        builder.Property(t => t.HighLimit)
            .HasColumnName("high_limit");

        builder.Property(t => t.Formula)
            .HasMaxLength(500)
            .HasColumnName("formula");

        // Indexes
        builder.HasIndex(t => new { t.DeviceId, t.Number })
            .IsUnique()
            .HasDatabaseName("idx_tags_device_id_number");

        builder.HasIndex(t => t.Status)
            .HasDatabaseName("idx_tags_status");

        builder.HasIndex(t => t.LastUpdateAt)
            .HasDatabaseName("idx_tags_last_update_at");

        // Ignore domain events
        builder.Ignore(t => t.DomainEvents);
    }

    private static TagValue? DeserializeTagValue(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var value = root.GetProperty("value").GetRawText();
        var timestamp = root.GetProperty("timestamp").GetDateTime();
        var quality = root.GetProperty("quality").GetDouble();

        object parsedValue = JsonSerializer.Deserialize<object>(value) ?? value;
        return TagValue.Create(parsedValue, timestamp, quality);
    }
}
