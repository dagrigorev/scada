using RapidScada.Domain.Common;
using RapidScada.Domain.Events;
using RapidScada.Domain.ValueObjects;

namespace RapidScada.Domain.Entities;

/// <summary>
/// Represents a data tag (input channel) from a device
/// </summary>
public sealed class Tag : Entity<TagId>
{
    private Tag(TagId id) : base(id)
    {
    }

    public int Number { get; private set; }
    public string Name { get; private set; } = null!;
    public DeviceId DeviceId { get; private set; } = null!;
    public TagType TagType { get; private set; }
    public string? Units { get; private set; }
    public TagValue? CurrentValue { get; private set; }
    public DateTime? LastUpdateAt { get; private set; }
    public TagStatus Status { get; private set; }
    public double? LowLimit { get; private set; }
    public double? HighLimit { get; private set; }
    public string? Formula { get; private set; }
    public double? Quality { get; set; }

    /// <summary>
    /// Create a new tag
    /// </summary>
    public static Result<Tag> Create(
        TagId id,
        int number,
        string name,
        DeviceId deviceId,
        TagType tagType,
        string? units = null,
        double? lowLimit = null,
        double? highLimit = null,
        string? formula = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Tag>(Error.InvalidValue(nameof(name), "Name cannot be empty"));
        }

        if (number <= 0)
        {
            return Result.Failure<Tag>(Error.InvalidValue(nameof(number), "Number must be positive"));
        }

        var tag = new Tag(id)
        {
            Number = number,
            Name = name,
            DeviceId = deviceId,
            TagType = tagType,
            Units = units,
            LowLimit = lowLimit,
            HighLimit = highLimit,
            Formula = formula,
            Status = TagStatus.Undefined
        };

        tag.RaiseDomainEvent(new TagCreatedEvent(tag.Id, tag.Number, tag.Name));

        return Result.Success(tag);
    }

    /// <summary>
    /// Update the tag value
    /// </summary>
    public Result UpdateValue(TagValue value)
    {
        var previousValue = CurrentValue;
        CurrentValue = value;
        LastUpdateAt = DateTime.UtcNow;
        Status = TagStatus.Valid;

        // Check limits
        if (value.TryGetNumericValue(out var numericValue))
        {
            if (LowLimit.HasValue && numericValue < LowLimit.Value)
            {
                Status = TagStatus.BelowLowLimit;
            }
            else if (HighLimit.HasValue && numericValue > HighLimit.Value)
            {
                Status = TagStatus.AboveHighLimit;
            }
        }

        RaiseDomainEvent(new TagValueChangedEvent(
            Id,
            Number,
            Name,
            previousValue,
            CurrentValue,
            Status));

        return Result.Success();
    }

    /// <summary>
    /// Mark tag as invalid due to communication error
    /// </summary>
    public void MarkAsInvalid()
    {
        Status = TagStatus.Invalid;
        LastUpdateAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark tag as unreliable
    /// </summary>
    public void MarkAsUnreliable()
    {
        Status = TagStatus.Unreliable;
        LastUpdateAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Tag data types
/// </summary>
public enum TagType
{
    Real = 1,         // Floating point value
    Integer = 2,      // Integer value
    Boolean = 3,      // Boolean/Digital value
    String = 4        // Text value
}

/// <summary>
/// Tag quality status
/// </summary>
public enum TagStatus
{
    Undefined = 0,
    Valid = 1,
    Invalid = 2,
    Unreliable = 3,
    BelowLowLimit = 4,
    AboveHighLimit = 5
}
