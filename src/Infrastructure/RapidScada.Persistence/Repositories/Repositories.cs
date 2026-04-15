using Microsoft.EntityFrameworkCore;
using RapidScada.Application.Abstractions;
using RapidScada.Domain.Entities;
using RapidScada.Domain.ValueObjects;

namespace RapidScada.Persistence.Repositories;

/// <summary>
/// Generic repository implementation using EF Core
/// </summary>
public abstract class Repository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : Domain.Common.Entity<TId>
    where TId : notnull
{
    protected readonly ScadaDbContext Context;
    protected readonly DbSet<TEntity> DbSet;

    protected Repository(ScadaDbContext context)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    public virtual async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(e => e.Id.Equals(id), cancellationToken);
    }
}

/// <summary>
/// Device repository implementation
/// </summary>
public sealed class DeviceRepository : Repository<Device, DeviceId>, IDeviceRepository
{
    public DeviceRepository(ScadaDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Device>> GetByCommunicationLineAsync(
        CommunicationLineId lineId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(d => d.CommunicationLineId == lineId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Device>> GetByStatusAsync(
        DeviceStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(d => d.Status == status)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Device>> GetStaleDevicesAsync(
        DateTime threshold,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(d => d.LastCommunicationAt == null || d.LastCommunicationAt < threshold)
            .ToListAsync(cancellationToken);
    }

    public async Task<Device?> GetWithTagsAsync(
        DeviceId id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.Tags)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }
}

/// <summary>
/// Communication line repository implementation
/// </summary>
public sealed class CommunicationLineRepository : Repository<CommunicationLine, CommunicationLineId>, ICommunicationLineRepository
{
    public CommunicationLineRepository(ScadaDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<CommunicationLine>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(c => c.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CommunicationLine>> GetByChannelTypeAsync(
        ChannelType channelType,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(c => c.ChannelType == channelType)
            .ToListAsync(cancellationToken);
    }

    public async Task<CommunicationLine?> GetWithDevicesAsync(
        CommunicationLineId id,
        CancellationToken cancellationToken = default)
    {
        // Note: DeviceIds are stored as a collection in the entity
        // We would need to join with devices to get full device objects
        return await DbSet
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }
}

/// <summary>
/// Tag repository implementation
/// </summary>
public sealed class TagRepository : Repository<Tag, TagId>, ITagRepository
{
    public TagRepository(ScadaDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Tag>> GetByDeviceAsync(
        DeviceId deviceId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(t => t.DeviceId == deviceId)
            .OrderBy(t => t.Number)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tag?> GetByNumberAsync(
        DeviceId deviceId,
        int tagNumber,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(
                t => t.DeviceId == deviceId && t.Number == tagNumber,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Tag>> GetWithCurrentValuesAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(t => t.CurrentValue != null)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Tag>> GetAlarmsAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(t => t.Status == TagStatus.BelowLowLimit || 
                       t.Status == TagStatus.AboveHighLimit)
            .ToListAsync(cancellationToken);
    }

    public async Task BulkUpdateValuesAsync(
        IEnumerable<(TagId TagId, TagValue Value)> updates,
        CancellationToken cancellationToken = default)
    {
        var tagIds = updates.Select(u => u.TagId).ToList();
        var tags = await DbSet
            .Where(t => tagIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        foreach (var (tagId, value) in updates)
        {
            var tag = tags.FirstOrDefault(t => t.Id == tagId);
            if (tag is not null)
            {
                tag.UpdateValue(value);
            }
        }
    }
}
