namespace FlightStatus.Api.Models;

/// <summary>
/// Response shape for GET /flights/status.
/// </summary>
public sealed record FlightStatusResult(
    string FlightNumber,
    DateOnly Date,
    UnifiedFlightStatus Status,
    string Source,
    DateTime? ScheduledDeparture,
    DateTime? ActualDeparture,
    DateTime? ScheduledArrival,
    DateTime? ActualArrival,
    string? Terminal,
    string? Gate,
    string? DelayReason,
    DateTime? LastUpdatedUtc,
    string? Message
);
