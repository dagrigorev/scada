using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RapidScada.Domain.Entities;
using RapidScada.Domain.ValueObjects;
using System.Text.Json;

namespace RapidScada.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Device entity
/// </summary>
public sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("devices");

        // Primary key
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id)
            .HasConversion(
                id => id.Value,
                value => DeviceId.Create(value))
            .HasColumnName("id");

        // Value objects
        builder.Property(d => d.Name)
            .HasConversion(
                name => name.Value,
                value => DeviceName.Create(value).Value)
            .HasMaxLength(DeviceName.MaxLength)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(d => d.DeviceTypeId)
            .HasConversion(
                id => id.Value,
                value => DeviceTypeId.Create(value))
            .HasColumnName("device_type_id")
            .IsRequired();

        builder.Property(d => d.Address)
            .HasConversion(
                addr => addr.Value,
                value => DeviceAddress.Create(value).Value)
            .HasColumnName("address")
            .IsRequired();

        builder.Property(d => d.CallSign)
            .HasConversion(
                cs => cs != null ? cs.Value : null,
                value => value != null ? CallSign.Create(value).Value : null)
            .HasMaxLength(CallSign.MaxLength)
            .HasColumnName("call_sign");

        builder.Property(d => d.CommunicationLineId)
            .HasConversion(
                id => id.Value,
                value => CommunicationLineId.Create(value))
            .HasColumnName("communication_line_id")
            .IsRequired();

        // Simple properties
        builder.Property(d => d.Description)
            .HasMaxLength(500)
            .HasColumnName("description");

        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(d => d.LastCommunicationAt)
            .HasColumnName("last_communication_at");

        // Relationships
        builder.HasOne<CommunicationLine>()
            .WithMany()
            .HasForeignKey(d => d.CommunicationLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.Tags)
            .WithOne()
            .HasForeignKey(t => t.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(d => d.Name)
            .HasDatabaseName("idx_devices_name");

        builder.HasIndex(d => d.CommunicationLineId)
            .HasDatabaseName("idx_devices_communication_line_id");

        builder.HasIndex(d => d.Status)
            .HasDatabaseName("idx_devices_status");

        builder.HasIndex(d => d.LastCommunicationAt)
            .HasDatabaseName("idx_devices_last_communication_at");

        // Ignore domain events (not persisted)
        builder.Ignore(d => d.DomainEvents);
    }
}
