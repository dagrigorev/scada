using Microsoft.Extensions.Logging;
using RapidScada.Application.Abstractions;
using RapidScada.Domain.Common;
using RapidScada.Domain.Entities;
using RapidScada.Domain.ValueObjects;
using System.Diagnostics;

namespace RapidScada.Drivers.Abstractions;

/// <summary>
/// Base class for all device drivers with common functionality
/// </summary>
public abstract class DeviceDriverBase : IDeviceDriver
{
    protected readonly ILogger Logger;
    protected Device? Device;
    protected ConnectionSettings? ConnectionSettings;

    private readonly object _statisticsLock = new();
    private int _successfulReads;
    private int _failedReads;
    private int _successfulWrites;
    private int _failedWrites;
    private readonly List<long> _readTimes = [];
    private readonly List<long> _writeTimes = [];
    private DateTime _lastCommunication;
    private string? _lastError;

    protected DeviceDriverBase(ILogger logger)
    {
        Logger = logger;
    }

    public abstract DriverInfo Info { get; }

    public bool IsConnected { get; protected set; }

    public virtual async Task<Result> InitializeAsync(
        Device device,
        ConnectionSettings connectionSettings,
        CancellationToken cancellationToken = default)
    {
        Device = device ?? throw new ArgumentNullException(nameof(device));
        ConnectionSettings = connectionSettings ?? throw new ArgumentNullException(nameof(connectionSettings));

        Logger.LogInformation(
            "Initializing driver {DriverName} for device {DeviceId} - {DeviceName}",
            Info.Name,
            device.Id,
            device.Name.Value);

        return await OnInitializeAsync(device, connectionSettings, cancellationToken);
    }

    public virtual async Task<Result> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (Device is null || ConnectionSettings is null)
        {
            return Result.Failure(Error.Validation("Driver not initialized"));
        }

        Logger.LogInformation("Connecting to device {DeviceId}", Device.Id);

        var result = await OnConnectAsync(cancellationToken);
        
        if (result.IsSuccess)
        {
            IsConnected = true;
            _lastCommunication = DateTime.UtcNow;
            Logger.LogInformation("Connected to device {DeviceId}", Device.Id);
        }
        else
        {
            Logger.LogError("Failed to connect to device {DeviceId}: {Error}", Device.Id, result.Error.Message);
            _lastError = result.Error.Message;
        }

        return result;
    }

    public virtual async Task DisconnectAsync()
    {
        if (!IsConnected)
        {
            return;
        }

        Logger.LogInformation("Disconnecting from device {DeviceId}", Device?.Id);

        await OnDisconnectAsync();
        IsConnected = false;

        Logger.LogInformation("Disconnected from device {DeviceId}", Device?.Id);
    }

    public virtual async Task<Result<IReadOnlyList<TagReading>>> ReadTagsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            return Result.Failure<IReadOnlyList<TagReading>>(Error.Validation("Not connected"));
        }

        var sw = Stopwatch.StartNew();
        var result = await OnReadTagsAsync(cancellationToken);
        sw.Stop();

        UpdateStatistics(isRead: true, success: result.IsSuccess, elapsedMs: sw.ElapsedMilliseconds, result.Error?.Message);

        return result;
    }

    public virtual async Task<Result<IReadOnlyList<TagReading>>> ReadTagsAsync(
        IEnumerable<int> tagNumbers,
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            return Result.Failure<IReadOnlyList<TagReading>>(Error.Validation("Not connected"));
        }

        var sw = Stopwatch.StartNew();
        var result = await OnReadTagsAsync(tagNumbers, cancellationToken);
        sw.Stop();

        UpdateStatistics(isRead: true, success: result.IsSuccess, elapsedMs: sw.ElapsedMilliseconds, result.Error?.Message);

        return result;
    }

    public virtual async Task<Result> WriteTagAsync(
        int tagNumber,
        object value,
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            return Result.Failure(Error.Validation("Not connected"));
        }

        var sw = Stopwatch.StartNew();
        var result = await OnWriteTagAsync(tagNumber, value, cancellationToken);
        sw.Stop();

        UpdateStatistics(isRead: false, success: result.IsSuccess, elapsedMs: sw.ElapsedMilliseconds, result.Error?.Message);

        return result;
    }

    public virtual async Task<Result> SendCommandAsync(
        string command,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            return Result.Failure(Error.Validation("Not connected"));
        }

        return await OnSendCommandAsync(command, parameters, cancellationToken);
    }

    public DriverStatistics GetStatistics()
    {
        lock (_statisticsLock)
        {
            var avgReadTime = _readTimes.Any()
                ? TimeSpan.FromMilliseconds(_readTimes.Average())
                : TimeSpan.Zero;

            var avgWriteTime = _writeTimes.Any()
                ? TimeSpan.FromMilliseconds(_writeTimes.Average())
                : TimeSpan.Zero;

            return new DriverStatistics(
                _successfulReads,
                _failedReads,
                _successfulWrites,
                _failedWrites,
                avgReadTime,
                avgWriteTime,
                _lastCommunication,
                _lastError);
        }
    }

    private void UpdateStatistics(bool isRead, bool success, long elapsedMs, string? error)
    {
        lock (_statisticsLock)
        {
            if (isRead)
            {
                if (success)
                {
                    _successfulReads++;
                    _readTimes.Add(elapsedMs);
                    if (_readTimes.Count > 100) _readTimes.RemoveAt(0);
                }
                else
                {
                    _failedReads++;
                }
            }
            else
            {
                if (success)
                {
                    _successfulWrites++;
                    _writeTimes.Add(elapsedMs);
                    if (_writeTimes.Count > 100) _writeTimes.RemoveAt(0);
                }
                else
                {
                    _failedWrites++;
                }
            }

            if (success)
            {
                _lastCommunication = DateTime.UtcNow;
            }
            else
            {
                _lastError = error;
            }
        }
    }

    // Abstract methods for derived classes to implement
    protected abstract Task<Result> OnInitializeAsync(
        Device device,
        ConnectionSettings connectionSettings,
        CancellationToken cancellationToken);

    protected abstract Task<Result> OnConnectAsync(CancellationToken cancellationToken);

    protected abstract Task OnDisconnectAsync();

    protected abstract Task<Result<IReadOnlyList<TagReading>>> OnReadTagsAsync(
        CancellationToken cancellationToken);

    protected abstract Task<Result<IReadOnlyList<TagReading>>> OnReadTagsAsync(
        IEnumerable<int> tagNumbers,
        CancellationToken cancellationToken);

    protected abstract Task<Result> OnWriteTagAsync(
        int tagNumber,
        object value,
        CancellationToken cancellationToken);

    protected abstract Task<Result> OnSendCommandAsync(
        string command,
        object? parameters,
        CancellationToken cancellationToken);
}
