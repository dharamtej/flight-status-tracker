namespace FlightStatus.Api.Models;

/// <summary>
/// Raw response shape for the QuickFlight stub provider (minimal detail).
/// FlightState vocabulary: ONTIME, DELAYED, CANCELLED, DIVERTED, UNKNOWN.
/// </summary>
public sealed record QuickFlightResponse(
    string FlightNumber,
    string FlightState,
    DateTime ScheduledDeparture,
    DateTime ScheduledArrival,
    DateTime LastUpdatedUtc
);
