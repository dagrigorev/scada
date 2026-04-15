using RapidScada.Domain.Common;

namespace RapidScada.Domain.ValueObjects;

/// <summary>
/// Device name value object
/// </summary>
public sealed class DeviceName : ValueObject
{
    public const int MaxLength = 100;

    private DeviceName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<DeviceName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<DeviceName>(Error.InvalidValue(nameof(DeviceName), "Name cannot be empty"));
        }

        if (value.Length > MaxLength)
        {
            return Result.Failure<DeviceName>(Error.InvalidValue(nameof(DeviceName), $"Name cannot exceed {MaxLength} characters"));
        }

        return Result.Success(new DeviceName(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Communication line name value object
/// </summary>
public sealed class CommunicationLineName : ValueObject
{
    public const int MaxLength = 100;

    private CommunicationLineName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<CommunicationLineName> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<CommunicationLineName>(Error.InvalidValue(nameof(CommunicationLineName), "Name cannot be empty"));
        }

        if (value.Length > MaxLength)
        {
            return Result.Failure<CommunicationLineName>(Error.InvalidValue(nameof(CommunicationLineName), $"Name cannot exceed {MaxLength} characters"));
        }

        return Result.Success(new CommunicationLineName(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Device address value object
/// </summary>
public sealed class DeviceAddress : ValueObject
{
    private DeviceAddress(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public static Result<DeviceAddress> Create(int value)
    {
        if (value < 0 || value > 65535)
        {
            return Result.Failure<DeviceAddress>(Error.InvalidValue(nameof(DeviceAddress), "Address must be between 0 and 65535"));
        }

        return Result.Success(new DeviceAddress(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();
}

/// <summary>
/// Call sign for radio/communication devices
/// </summary>
public sealed class CallSign : ValueObject
{
    public const int MaxLength = 20;

    private CallSign(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static Result<CallSign> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<CallSign>(Error.InvalidValue(nameof(CallSign), "Call sign cannot be empty"));
        }

        if (value.Length > MaxLength)
        {
            return Result.Failure<CallSign>(Error.InvalidValue(nameof(CallSign), $"Call sign cannot exceed {MaxLength} characters"));
        }

        return Result.Success(new CallSign(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
