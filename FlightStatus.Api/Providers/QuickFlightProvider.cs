using FlightStatus.Api.Interfaces;
using FlightStatus.Api.Models;

namespace FlightStatus.Api.Providers;

/// <summary>
/// Deterministic stub for the QuickFlight provider (minimal detail).
/// Fixture data covers every scenario in spec.md's Deterministic Test Scenarios table.
/// </summary>
public sealed class QuickFlightProvider : IFlightStatusProvider
{
    public string ProviderName => "QuickFlight";

    private static readonly DateOnly FixtureDate = new(2026, 1, 15);
    private static readonly DateTime ScheduledDeparture = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ScheduledArrival = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Dictionary<(string FlightNumber, DateOnly Date), QuickFlightResponse> Fixtures = new()
    {
        // #2 — OnTime via vocab
        [("SR101", FixtureDate)] = new QuickFlightResponse(
            "SR101", "ONTIME",
            ScheduledDeparture, ScheduledArrival,
            new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc)),

        // #4 — Delayed via vocab
        [("SR201", FixtureDate)] = new QuickFlightResponse(
            "SR201", "DELAYED",
            ScheduledDeparture, ScheduledArrival,
            new DateTime(2026, 1, 15, 10, 40, 0, DateTimeKind.Utc)),

        // #6 — Cancelled via vocab
        [("SR301", FixtureDate)] = new QuickFlightResponse(
            "SR301", "CANCELLED",
            ScheduledDeparture, ScheduledArrival,
            new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc)),

        // #8 — Diverted via vocab
        [("SR401", FixtureDate)] = new QuickFlightResponse(
            "SR401", "DIVERTED",
            ScheduledDeparture, ScheduledArrival,
            new DateTime(2026, 1, 15, 13, 0, 0, DateTimeKind.Utc)),

        // #9 — Unknown, unrecognized raw vocab, fresher than AeroTrack's response for SR500
        [("SR500", FixtureDate)] = new QuickFlightResponse(
            "SR500", "PENDING",
            ScheduledDeparture, ScheduledArrival,
            new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc)),

        // #10 — both respond, QuickFlight fresher wins
        [("SR600", FixtureDate)] = new QuickFlightResponse(
            "SR600", "DELAYED",
            ScheduledDeparture, ScheduledArrival,
            new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc)),

        // #11 — both respond, AeroTrack fresher wins
        [("SR601", FixtureDate)] = new QuickFlightResponse(
            "SR601", "ONTIME",
            ScheduledDeparture, ScheduledArrival,
            new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc)),

        // #12 — equal LastUpdatedUtc, AeroTrack wins tie-break
        [("SR602", FixtureDate)] = new QuickFlightResponse(
            "SR602", "ONTIME",
            ScheduledDeparture, ScheduledArrival,
            new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc)),
    };

    public Task<ProviderFlightStatus?> GetStatusAsync(string flightNumber, DateOnly date, CancellationToken cancellationToken)
    {
        if (!Fixtures.TryGetValue((flightNumber, date), out var raw))
        {
            return Task.FromResult<ProviderFlightStatus?>(null);
        }

        return Task.FromResult<ProviderFlightStatus?>(Normalize(raw));
    }

    private ProviderFlightStatus Normalize(QuickFlightResponse raw)
    {
        var status = raw.FlightState switch
        {
            "ONTIME" => UnifiedFlightStatus.OnTime,
            "DELAYED" => UnifiedFlightStatus.Delayed,
            "CANCELLED" => UnifiedFlightStatus.Cancelled,
            "DIVERTED" => UnifiedFlightStatus.Diverted,
            _ => UnifiedFlightStatus.Unknown
        };

        return new ProviderFlightStatus(
            ProviderName,
            status,
            raw.ScheduledDeparture,
            null,
            raw.ScheduledArrival,
            null,
            null,
            null,
            null,
            raw.LastUpdatedUtc);
    }
}
