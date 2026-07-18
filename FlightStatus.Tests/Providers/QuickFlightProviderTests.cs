using FlightStatus.Api.Models;
using FlightStatus.Api.Providers;

namespace FlightStatus.Tests.Providers;

public class QuickFlightProviderTests
{
    private static readonly DateTime ScheduledDeparture = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ScheduledArrival = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    private static QuickFlightResponse MakeRaw(string flightState) =>
        new("SR000", flightState, ScheduledDeparture, ScheduledArrival, ScheduledDeparture);

    // Vocabulary table: 1:1 mapping, no timing involved (QuickFlight has no actual times).
    [Theory]
    [InlineData("ONTIME", UnifiedFlightStatus.OnTime)]
    [InlineData("DELAYED", UnifiedFlightStatus.Delayed)]
    [InlineData("CANCELLED", UnifiedFlightStatus.Cancelled)]
    [InlineData("DIVERTED", UnifiedFlightStatus.Diverted)]
    [InlineData("PENDING", UnifiedFlightStatus.Unknown)]
    [InlineData("", UnifiedFlightStatus.Unknown)]
    public void Normalize_VocabularyTable_MapsToExpectedUnifiedStatus(string flightState, UnifiedFlightStatus expected)
    {
        var provider = new QuickFlightProvider();
        var raw = MakeRaw(flightState);

        var result = provider.Normalize(raw);

        Assert.Equal(expected, result.Status);
    }

    [Fact]
    public void Normalize_AlwaysReturnsNullDetailFields()
    {
        var provider = new QuickFlightProvider();
        var raw = MakeRaw("ONTIME");

        var result = provider.Normalize(raw);

        Assert.Null(result.ActualDeparture);
        Assert.Null(result.ActualArrival);
        Assert.Null(result.Terminal);
        Assert.Null(result.Gate);
        Assert.Null(result.DelayReason);
    }

    // End-to-end: GetStatusAsync against the provider's own deterministic fixtures
    // (spec.md Deterministic Test Scenarios table).
    [Theory]
    [InlineData("SR101", UnifiedFlightStatus.OnTime)]
    [InlineData("SR201", UnifiedFlightStatus.Delayed)]
    [InlineData("SR301", UnifiedFlightStatus.Cancelled)]
    [InlineData("SR401", UnifiedFlightStatus.Diverted)]
    [InlineData("SR500", UnifiedFlightStatus.Unknown)]
    public async Task GetStatusAsync_KnownFixture_ReturnsExpectedStatus(string flightNumber, UnifiedFlightStatus expected)
    {
        var provider = new QuickFlightProvider();

        var result = await provider.GetStatusAsync(flightNumber, new DateOnly(2026, 1, 15), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(expected, result!.Status);
        Assert.Equal("QuickFlight", result.ProviderName);
    }

    [Fact]
    public async Task GetStatusAsync_NoFixture_ReturnsNull()
    {
        var provider = new QuickFlightProvider();

        var result = await provider.GetStatusAsync("SR999", new DateOnly(2026, 1, 15), CancellationToken.None);

        Assert.Null(result);
    }
}
