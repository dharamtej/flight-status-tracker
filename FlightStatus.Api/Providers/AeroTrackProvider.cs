using FlightStatus.Api.Interfaces;
using FlightStatus.Api.Models;

namespace FlightStatus.Api.Providers;

/// <summary>
/// Deterministic stub for the AeroTrack provider (full detail).
/// Fixture data covers every scenario in spec.md's Deterministic Test Scenarios table.
/// </summary>
public sealed class AeroTrackProvider : IFlightStatusProvider
{
    public string ProviderName => "AeroTrack";

    private static readonly DateOnly FixtureDate = new(2026, 1, 15);
    private static readonly DateTime ScheduledDeparture = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ScheduledArrival = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Dictionary<(string FlightNumber, DateOnly Date), AeroTrackResponse> Fixtures = new()
    {
        // #1 — OnTime via time-calc (actual arrival 5 min after scheduled)
        [("SR100", FixtureDate)] = new AeroTrackResponse(
            "SR100", "ON_TIME",
            ScheduledDeparture, null,
            ScheduledArrival, ScheduledArrival.AddMinutes(5),
            null, null, null,
            new DateTime(2026, 1, 15, 12, 5, 0, DateTimeKind.Utc)),

        // #3 — Delayed via time-calc (actual departure 40 min after scheduled)
        [("SR200", FixtureDate)] = new AeroTrackResponse(
            "SR200", "DELAYED",
            ScheduledDeparture, ScheduledDeparture.AddMinutes(40),
            ScheduledArrival, null,
            null, null, null,
            new DateTime(2026, 1, 15, 10, 40, 0, DateTimeKind.Utc)),

        // #5 — Cancelled, explicit
        [("SR300", FixtureDate)] = new AeroTrackResponse(
            "SR300", "CANCELLED",
            ScheduledDeparture, null,
            ScheduledArrival, null,
            null, null, null,
            new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc)),

        // #7 — Diverted, explicit
        [("SR400", FixtureDate)] = new AeroTrackResponse(
            "SR400", "DIVERTED",
            ScheduledDeparture, ScheduledDeparture,
            ScheduledArrival, ScheduledArrival.AddHours(1),
            null, null, null,
            new DateTime(2026, 1, 15, 13, 0, 0, DateTimeKind.Utc)),

        // #9 — Unknown, unrecognized raw status, older than QuickFlight's response for SR500
        [("SR500", FixtureDate)] = new AeroTrackResponse(
            "SR500", "GROUNDED",
            ScheduledDeparture, null,
            ScheduledArrival, null,
            null, null, null,
            new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc)),

        // #10 — both respond, QuickFlight fresher wins
        [("SR600", FixtureDate)] = new AeroTrackResponse(
            "SR600", "ON_TIME",
            ScheduledDeparture, null,
            ScheduledArrival, null,
            null, null, null,
            new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc)),

        // #11 — both respond, AeroTrack fresher wins
        [("SR601", FixtureDate)] = new AeroTrackResponse(
            "SR601", "DELAYED",
            ScheduledDeparture, ScheduledDeparture.AddMinutes(40),
            ScheduledArrival, null,
            null, null, null,
            new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc)),

        // #12 — equal LastUpdatedUtc, AeroTrack wins tie-break
        [("SR602", FixtureDate)] = new AeroTrackResponse(
            "SR602", "ON_TIME",
            ScheduledDeparture, null,
            ScheduledArrival, null,
            null, null, null,
            new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc)),

        // #13 — AeroTrack-only fields: terminal + gate
        [("SR700", FixtureDate)] = new AeroTrackResponse(
            "SR700", "ON_TIME",
            ScheduledDeparture, null,
            ScheduledArrival, null,
            "T2", "14", null,
            new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc)),

        // #15 — AeroTrack-only field: delay reason
        [("SR900", FixtureDate)] = new AeroTrackResponse(
            "SR900", "DELAYED",
            ScheduledDeparture, ScheduledDeparture.AddMinutes(40),
            ScheduledArrival, null,
            null, null, "WEATHER",
            new DateTime(2026, 1, 15, 10, 40, 0, DateTimeKind.Utc)),
    };

    public Task<ProviderFlightStatus?> GetStatusAsync(string flightNumber, DateOnly date, CancellationToken cancellationToken)
    {
        if (!Fixtures.TryGetValue((flightNumber, date), out var raw))
        {
            return Task.FromResult<ProviderFlightStatus?>(null);
        }

        return Task.FromResult<ProviderFlightStatus?>(Normalize(raw));
    }

    private ProviderFlightStatus Normalize(AeroTrackResponse raw)
    {
        var status = raw.Status switch
        {
            "CANCELLED" => UnifiedFlightStatus.Cancelled,
            "DIVERTED" => UnifiedFlightStatus.Diverted,
            "SCHEDULED" or "BOARDING" or "DEPARTED" or "LANDED" or "ON_TIME" or "DELAYED"
                => ComputeFromSchedule(raw),
            _ => UnifiedFlightStatus.Unknown
        };

        return new ProviderFlightStatus(
            ProviderName,
            status,
            raw.ScheduledDeparture,
            raw.ActualDeparture,
            raw.ScheduledArrival,
            raw.ActualArrival,
            raw.Terminal,
            raw.Gate,
            raw.DelayReason,
            raw.LastUpdatedUtc);
    }

    private static UnifiedFlightStatus ComputeFromSchedule(AeroTrackResponse raw)
    {
        TimeSpan delta;

        if (raw.ActualArrival is { } actualArrival)
        {
            delta = actualArrival - raw.ScheduledArrival;
        }
        else if (raw.ActualDeparture is { } actualDeparture)
        {
            delta = actualDeparture - raw.ScheduledDeparture;
        }
        else
        {
            return UnifiedFlightStatus.OnTime;
        }

        return delta.Duration() <= TimeSpan.FromMinutes(15)
            ? UnifiedFlightStatus.OnTime
            : UnifiedFlightStatus.Delayed;
    }
}
