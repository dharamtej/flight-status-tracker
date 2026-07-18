namespace FlightStatus.Api.Models;

/// <summary>
/// Normalized shape every <see cref="Interfaces.IFlightStatusProvider"/> implementation must
/// translate its own raw vocabulary into before returning.
/// </summary>
public sealed record ProviderFlightStatus(
    string ProviderName,
    UnifiedFlightStatus Status,
    DateTime ScheduledDeparture,
    DateTime? ActualDeparture,
    DateTime ScheduledArrival,
    DateTime? ActualArrival,
    string? Terminal,
    string? Gate,
    string? DelayReason,
    DateTime LastUpdatedUtc
);
