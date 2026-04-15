using RapidScada.Domain.Common;
using RapidScada.Domain.Entities;
using RapidScada.Domain.ValueObjects;

namespace RapidScada.Domain.Events;

// Device Events
public sealed record DeviceCreatedEvent(DeviceId DeviceId, string DeviceName) : DomainEvent;

public sealed record DeviceConfigurationUpdatedEvent(DeviceId DeviceId, string DeviceName) : DomainEvent;

public sealed record DeviceStatusChangedEvent(
    DeviceId DeviceId,
    string DeviceName,
    DeviceStatus PreviousStatus,
    DeviceStatus NewStatus,
    DateTime Timestamp,
    string? ErrorMessage = null) : DomainEvent;

// Communication Line Events
public sealed record CommunicationLineCreatedEvent(CommunicationLineId LineId, string LineName) : DomainEvent;

public sealed record CommunicationLineActivatedEvent(CommunicationLineId LineId, string LineName) : DomainEvent;

public sealed record CommunicationLineDeactivatedEvent(CommunicationLineId LineId, string LineName) : DomainEvent;

// Tag Events
public sealed record TagCreatedEvent(TagId TagId, int TagNumber, string TagName) : DomainEvent;

public sealed record TagValueChangedEvent(
    TagId TagId,
    int TagNumber,
    string TagName,
    TagValue? PreviousValue,
    TagValue NewValue,
    TagStatus Status) : DomainEvent;

// System Events
public sealed record SystemEventOccurredEvent(
    EventId EventId,
    DateTime Timestamp,
    string EventType,
    string Source,
    string Message,
    Dictionary<string, object>? AdditionalData = null) : DomainEvent;
