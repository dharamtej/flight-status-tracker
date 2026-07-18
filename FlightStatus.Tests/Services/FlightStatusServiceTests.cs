using FlightStatus.Api.Interfaces;
using FlightStatus.Api.Models;
using FlightStatus.Api.Services;
using FlightStatus.Tests.TestDoubles;

namespace FlightStatus.Tests.Services;

public class FlightStatusServiceTests
{
    private static readonly DateOnly Date = new(2026, 1, 15);
    private static readonly DateTime ScheduledDeparture = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ScheduledArrival = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    private static ProviderFlightStatus MakeResponse(string providerName, UnifiedFlightStatus status, DateTime lastUpdatedUtc) =>
        new(providerName, status, ScheduledDeparture, null, ScheduledArrival, null, null, null, null, lastUpdatedUtc);

    // Merge rule 1: both respond, later LastUpdatedUtc wins.
    [Fact]
    public async Task GetStatusAsync_BothRespond_LaterLastUpdatedUtcWins()
    {
        var aeroTrack = FakeFlightStatusProvider.Responding("AeroTrack",
            MakeResponse("AeroTrack", UnifiedFlightStatus.OnTime, new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc)));
        var quickFlight = FakeFlightStatusProvider.Responding("QuickFlight",
            MakeResponse("QuickFlight", UnifiedFlightStatus.Delayed, new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc)));

        var service = new FlightStatusService(new IFlightStatusProvider[] { aeroTrack, quickFlight });

        var result = await service.GetStatusAsync("SR600", Date, CancellationToken.None);

        Assert.Equal(UnifiedFlightStatus.Delayed, result.Status);
        Assert.Equal("QuickFlight", result.Source);
    }

    [Fact]
    public async Task GetStatusAsync_BothRespond_AeroTrackFresher_AeroTrackWins()
    {
        var aeroTrack = FakeFlightStatusProvider.Responding("AeroTrack",
            MakeResponse("AeroTrack", UnifiedFlightStatus.Delayed, new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc)));
        var quickFlight = FakeFlightStatusProvider.Responding("QuickFlight",
            MakeResponse("QuickFlight", UnifiedFlightStatus.OnTime, new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc)));

        var service = new FlightStatusService(new IFlightStatusProvider[] { aeroTrack, quickFlight });

        var result = await service.GetStatusAsync("SR601", Date, CancellationToken.None);

        Assert.Equal(UnifiedFlightStatus.Delayed, result.Status);
        Assert.Equal("AeroTrack", result.Source);
    }

    // Merge rule 1, tie-break: equal LastUpdatedUtc -> AeroTrack wins.
    [Fact]
    public async Task GetStatusAsync_BothRespond_EqualLastUpdatedUtc_AeroTrackWinsTiebreak()
    {
        var lastUpdated = new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc);
        var aeroTrack = FakeFlightStatusProvider.Responding("AeroTrack",
            MakeResponse("AeroTrack", UnifiedFlightStatus.OnTime, lastUpdated));
        var quickFlight = FakeFlightStatusProvider.Responding("QuickFlight",
            MakeResponse("QuickFlight", UnifiedFlightStatus.OnTime, lastUpdated));

        // Registration order deliberately reversed to prove the tie-break isn't just "first wins".
        var service = new FlightStatusService(new IFlightStatusProvider[] { quickFlight, aeroTrack });

        var result = await service.GetStatusAsync("SR602", Date, CancellationToken.None);

        Assert.Equal("AeroTrack", result.Source);
    }

    // Merge rule 2: only one provider responds.
    [Fact]
    public async Task GetStatusAsync_OnlyAeroTrackResponds_UsesAeroTrackResult()
    {
        var aeroTrack = FakeFlightStatusProvider.Responding("AeroTrack",
            MakeResponse("AeroTrack", UnifiedFlightStatus.OnTime, new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc)));
        var quickFlight = FakeFlightStatusProvider.NotResponding("QuickFlight");

        var service = new FlightStatusService(new IFlightStatusProvider[] { aeroTrack, quickFlight });

        var result = await service.GetStatusAsync("SR100", Date, CancellationToken.None);

        Assert.Equal(UnifiedFlightStatus.OnTime, result.Status);
        Assert.Equal("AeroTrack", result.Source);
    }

    [Fact]
    public async Task GetStatusAsync_OnlyQuickFlightResponds_UsesQuickFlightResult()
    {
        var aeroTrack = FakeFlightStatusProvider.NotResponding("AeroTrack");
        var quickFlight = FakeFlightStatusProvider.Responding("QuickFlight",
            MakeResponse("QuickFlight", UnifiedFlightStatus.Delayed, new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc)));

        var service = new FlightStatusService(new IFlightStatusProvider[] { aeroTrack, quickFlight });

        var result = await service.GetStatusAsync("SR201", Date, CancellationToken.None);

        Assert.Equal(UnifiedFlightStatus.Delayed, result.Status);
        Assert.Equal("QuickFlight", result.Source);
    }

    // Merge rule 3: neither provider responds.
    [Fact]
    public async Task GetStatusAsync_NeitherResponds_ReturnsUnknownWithMessage()
    {
        var aeroTrack = FakeFlightStatusProvider.NotResponding("AeroTrack");
        var quickFlight = FakeFlightStatusProvider.NotResponding("QuickFlight");

        var service = new FlightStatusService(new IFlightStatusProvider[] { aeroTrack, quickFlight });

        var result = await service.GetStatusAsync("SR800", Date, CancellationToken.None);

        Assert.Equal(UnifiedFlightStatus.Unknown, result.Status);
        Assert.Equal("None", result.Source);
        Assert.Null(result.LastUpdatedUtc);
        Assert.Equal("No status available from either provider for flight SR800 on 2026-01-15.", result.Message);
    }

    // A provider throwing is treated the same as "did not respond" (merge rule 2) —
    // the other provider's data is still used, and the request doesn't fail outright.
    [Fact]
    public async Task GetStatusAsync_OneProviderThrows_TreatedAsNoResponse_OtherProviderStillUsed()
    {
        var aeroTrack = FakeFlightStatusProvider.Throwing("AeroTrack", new InvalidOperationException("boom"));
        var quickFlight = FakeFlightStatusProvider.Responding("QuickFlight",
            MakeResponse("QuickFlight", UnifiedFlightStatus.OnTime, new DateTime(2026, 1, 15, 9, 0, 0, DateTimeKind.Utc)));

        var service = new FlightStatusService(new IFlightStatusProvider[] { aeroTrack, quickFlight });

        var result = await service.GetStatusAsync("SR101", Date, CancellationToken.None);

        Assert.Equal(UnifiedFlightStatus.OnTime, result.Status);
        Assert.Equal("QuickFlight", result.Source);
    }

    // A provider throwing is treated as "did not respond" (merge rule 3) when the other
    // provider also has nothing usable.
    [Fact]
    public async Task GetStatusAsync_BothProvidersThrow_ReturnsUnknownWithMessage()
    {
        var aeroTrack = FakeFlightStatusProvider.Throwing("AeroTrack", new InvalidOperationException("boom"));
        var quickFlight = FakeFlightStatusProvider.Throwing("QuickFlight", new TimeoutException("timeout"));

        var service = new FlightStatusService(new IFlightStatusProvider[] { aeroTrack, quickFlight });

        var result = await service.GetStatusAsync("SR999", Date, CancellationToken.None);

        Assert.Equal(UnifiedFlightStatus.Unknown, result.Status);
        Assert.Equal("None", result.Source);
    }
}
