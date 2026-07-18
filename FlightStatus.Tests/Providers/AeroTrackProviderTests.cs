using FlightStatus.Api.Models;
using FlightStatus.Api.Providers;

namespace FlightStatus.Tests.Providers;

public class AeroTrackProviderTests
{
    private static readonly DateTime ScheduledDeparture = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ScheduledArrival = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    private static AeroTrackResponse MakeRaw(
        string status,
        DateTime? actualDeparture = null,
        DateTime? actualArrival = null) =>
        new(
            "SR000",
            status,
            ScheduledDeparture,
            actualDeparture,
            ScheduledArrival,
            actualArrival,
            null,
            null,
            null,
            ScheduledDeparture);

    // Vocabulary table: CANCELLED / DIVERTED are terminal, independent of timing.
    [Fact]
    public void Normalize_Cancelled_IsTerminal_IgnoresTiming()
    {
        var provider = new AeroTrackProvider();
        var raw = MakeRaw("CANCELLED", actualDeparture: ScheduledDeparture.AddHours(5));

        var result = provider.Normalize(raw);

        Assert.Equal(UnifiedFlightStatus.Cancelled, result.Status);
    }

    [Fact]
    public void Normalize_Diverted_IsTerminal_IgnoresTiming()
    {
        var provider = new AeroTrackProvider();
        var raw = MakeRaw("DIVERTED", actualDeparture: ScheduledDeparture);

        var result = provider.Normalize(raw);

        Assert.Equal(UnifiedFlightStatus.Diverted, result.Status);
    }

    // Vocabulary table: SCHEDULED, BOARDING, DEPARTED, LANDED, ON_TIME, DELAYED are all
    // computed via reference-time selection, regardless of which of these six raw values is used.
    [Theory]
    [InlineData("SCHEDULED")]
    [InlineData("BOARDING")]
    [InlineData("DEPARTED")]
    [InlineData("LANDED")]
    [InlineData("ON_TIME")]
    [InlineData("DELAYED")]
    public void Normalize_ComputedStatuses_RouteThroughReferenceTimeSelection(string rawStatus)
    {
        var provider = new AeroTrackProvider();

        var onTimeRaw = MakeRaw(rawStatus, actualArrival: ScheduledArrival.AddMinutes(5));
        var delayedRaw = MakeRaw(rawStatus, actualArrival: ScheduledArrival.AddMinutes(30));

        Assert.Equal(UnifiedFlightStatus.OnTime, provider.Normalize(onTimeRaw).Status);
        Assert.Equal(UnifiedFlightStatus.Delayed, provider.Normalize(delayedRaw).Status);
    }

    // Vocabulary table: anything unrecognized -> Unknown.
    [Theory]
    [InlineData("GROUNDED")]
    [InlineData("")]
    [InlineData("unknown-vocab")]
    public void Normalize_UnrecognizedStatus_MapsToUnknown(string rawStatus)
    {
        var provider = new AeroTrackProvider();
        var raw = MakeRaw(rawStatus);

        var result = provider.Normalize(raw);

        Assert.Equal(UnifiedFlightStatus.Unknown, result.Status);
    }

    // Reference-time selection: actual arrival present takes precedence over actual departure.
    [Fact]
    public void ComputeFromSchedule_ActualArrivalPresent_TakesPrecedenceOverDeparture()
    {
        var raw = MakeRaw(
            "ON_TIME",
            actualDeparture: ScheduledDeparture.AddMinutes(40), // would be Delayed on its own
            actualArrival: ScheduledArrival.AddMinutes(5));     // but arrival governs -> OnTime

        var result = AeroTrackProvider.ComputeFromSchedule(raw);

        Assert.Equal(UnifiedFlightStatus.OnTime, result);
    }

    [Fact]
    public void ComputeFromSchedule_OnlyActualDeparturePresent_UsesDeparture()
    {
        var raw = MakeRaw("DELAYED", actualDeparture: ScheduledDeparture.AddMinutes(40));

        var result = AeroTrackProvider.ComputeFromSchedule(raw);

        Assert.Equal(UnifiedFlightStatus.Delayed, result);
    }

    [Fact]
    public void ComputeFromSchedule_NoActualTimes_DefaultsToOnTime()
    {
        var raw = MakeRaw("SCHEDULED");

        var result = AeroTrackProvider.ComputeFromSchedule(raw);

        Assert.Equal(UnifiedFlightStatus.OnTime, result);
    }

    [Theory]
    [InlineData(15, UnifiedFlightStatus.OnTime)]  // exactly on the 15-minute boundary -> OnTime
    [InlineData(16, UnifiedFlightStatus.Delayed)] // just past the boundary -> Delayed
    [InlineData(-15, UnifiedFlightStatus.OnTime)] // early arrival, within 15 -> OnTime
    [InlineData(-16, UnifiedFlightStatus.Delayed)] // early arrival, beyond 15 -> still Delayed
    public void ComputeFromSchedule_FifteenMinuteBoundary(int minutesOffset, UnifiedFlightStatus expected)
    {
        var raw = MakeRaw("ON_TIME", actualArrival: ScheduledArrival.AddMinutes(minutesOffset));

        var result = AeroTrackProvider.ComputeFromSchedule(raw);

        Assert.Equal(expected, result);
    }

    // End-to-end: GetStatusAsync against the provider's own deterministic fixtures
    // (spec.md Deterministic Test Scenarios table).
    [Theory]
    [InlineData("SR100", UnifiedFlightStatus.OnTime)]
    [InlineData("SR200", UnifiedFlightStatus.Delayed)]
    [InlineData("SR300", UnifiedFlightStatus.Cancelled)]
    [InlineData("SR400", UnifiedFlightStatus.Diverted)]
    [InlineData("SR500", UnifiedFlightStatus.Unknown)]
    public async Task GetStatusAsync_KnownFixture_ReturnsExpectedStatus(string flightNumber, UnifiedFlightStatus expected)
    {
        var provider = new AeroTrackProvider();

        var result = await provider.GetStatusAsync(flightNumber, new DateOnly(2026, 1, 15), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(expected, result!.Status);
        Assert.Equal("AeroTrack", result.ProviderName);
    }

    [Fact]
    public async Task GetStatusAsync_NoFixture_ReturnsNull()
    {
        var provider = new AeroTrackProvider();

        var result = await provider.GetStatusAsync("SR999", new DateOnly(2026, 1, 15), CancellationToken.None);

        Assert.Null(result);
    }
}
