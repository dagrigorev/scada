namespace RapidScada.Domain.ValueObjects;

/// <summary>
/// Device unique identifier
/// </summary>
public sealed record DeviceId(int Value)
{
    public static DeviceId Create(int value) => new(value);
    public static DeviceId New() => new(0); // Will be assigned by repository
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Communication line unique identifier
/// </summary>
public sealed record CommunicationLineId(int Value)
{
    public static CommunicationLineId Create(int value) => new(value);
    public static CommunicationLineId New() => new(0);
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Tag unique identifier
/// </summary>
public sealed record TagId(int Value)
{
    public static TagId Create(int value) => new(value);
    public static TagId New() => new(0);
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Device type unique identifier
/// </summary>
public sealed record DeviceTypeId(int Value)
{
    public static DeviceTypeId Create(int value) => new(value);
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Event unique identifier
/// </summary>
public sealed record EventId(long Value)
{
    public static EventId Create(long value) => new(value);
    public static EventId New() => new(0);
    public override string ToString() => Value.ToString();
}

/// <summary>
/// User unique identifier
/// </summary>
public sealed record UserId(int Value)
{
    public static UserId Create(int value) => new(value);
    public static UserId New() => new(0);
    public override string ToString() => Value.ToString();
}
