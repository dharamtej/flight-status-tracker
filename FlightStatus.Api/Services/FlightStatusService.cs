using FlightStatus.Api.Interfaces;
using FlightStatus.Api.Models;

namespace FlightStatus.Api.Services;

/// <summary>
/// Orchestrates the providers and applies the merge rules from spec.md.
/// </summary>
public sealed class FlightStatusService : IFlightStatusService
{
    private readonly IEnumerable<IFlightStatusProvider> _providers;

    public FlightStatusService(IEnumerable<IFlightStatusProvider> providers)
    {
        _providers = providers;
    }

    public async Task<FlightStatusResult> GetStatusAsync(string flightNumber, DateOnly date, CancellationToken cancellationToken)
    {
        var responseTasks = _providers
            .Select(provider => QuerySafelyAsync(provider, flightNumber, date, cancellationToken))
            .ToArray();

        var rawResponses = await Task.WhenAll(responseTasks);

        var responses = rawResponses
            .Where(response => response is not null)
            .Select(response => response!)
            .ToArray();

        // Merge rule 3: neither provider returned a result.
        if (responses.Length == 0)
        {
            return new FlightStatusResult(
                flightNumber,
                date,
                UnifiedFlightStatus.Unknown,
                "None",
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                $"No status available from either provider for flight {flightNumber} on {date:yyyy-MM-dd}.");
        }

        // Merge rule 2: only one provider responded.
        // Merge rule 1: both responded — later LastUpdatedUtc wins, AeroTrack wins on a tie.
        var chosen = responses.Length == 1
            ? responses[0]
            : SelectMostRecent(responses);

        return new FlightStatusResult(
            flightNumber,
            date,
            chosen.Status,
            chosen.ProviderName,
            chosen.ScheduledDeparture,
            chosen.ActualDeparture,
            chosen.ScheduledArrival,
            chosen.ActualArrival,
            chosen.Terminal,
            chosen.Gate,
            chosen.DelayReason,
            chosen.LastUpdatedUtc,
            null);
    }

    private static ProviderFlightStatus SelectMostRecent(IReadOnlyCollection<ProviderFlightStatus> responses)
    {
        return responses
            .OrderByDescending(response => response.LastUpdatedUtc)
            .ThenBy(response => response.ProviderName == "AeroTrack" ? 0 : 1)
            .First();
    }

    // A provider throwing (e.g. a real HTTP call timing out) is treated the same as "did not
    // respond" per merge rules 2/3, so one provider's failure doesn't fail the whole request
    // when the other provider still has data.
    private static async Task<ProviderFlightStatus?> QuerySafelyAsync(
        IFlightStatusProvider provider, string flightNumber, DateOnly date, CancellationToken cancellationToken)
    {
        try
        {
            return await provider.GetStatusAsync(flightNumber, date, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }
}
