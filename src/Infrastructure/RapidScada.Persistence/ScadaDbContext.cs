using Microsoft.EntityFrameworkCore;
using RapidScada.Domain.Common;
using RapidScada.Domain.Entities;
using System.Reflection;

namespace RapidScada.Persistence;

/// <summary>
/// Main database context for Rapid SCADA
/// </summary>
public sealed class ScadaDbContext : DbContext
{
    public ScadaDbContext(DbContextOptions<ScadaDbContext> options)
        : base(options)
    {
    }

    public DbSet<Device> Devices => Set<Device>();
    public DbSet<CommunicationLine> CommunicationLines => Set<CommunicationLine>();
    public DbSet<Tag> Tags => Set<Tag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// Override SaveChanges to dispatch domain events
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Get all domain events before saving
        var domainEvents = ChangeTracker
            .Entries<Entity<object>>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Any())
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Save changes
        var result = await base.SaveChangesAsync(cancellationToken);

        // Domain events would be dispatched here via MediatR
        // This is handled by the UnitOfWork in production

        return result;
    }
}
