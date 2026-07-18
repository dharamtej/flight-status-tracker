namespace FlightStatus.Api.Models;

/// <summary>
/// Raw response shape for the AeroTrack stub provider (full detail).
/// Status vocabulary: CANCELLED, DIVERTED, SCHEDULED, BOARDING, DEPARTED, LANDED, ON_TIME, DELAYED.
/// </summary>
public sealed record AeroTrackResponse(
    string FlightNumber,
    string Status,
    DateTime ScheduledDeparture,
    DateTime? ActualDeparture,
    DateTime ScheduledArrival,
    DateTime? ActualArrival,
    string? Terminal,
    string? Gate,
    string? DelayReason,
    DateTime LastUpdatedUtc
);
