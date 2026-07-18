using FlightStatus.Api.Models;

namespace FlightStatus.Api.Interfaces;

public interface IFlightStatusProvider
{
    string ProviderName { get; }

    /// <summary>
    /// Returns null when the provider has no data for this flight/date at all
    /// (equivalent to "provider did not respond").
    /// </summary>
    Task<ProviderFlightStatus?> GetStatusAsync(string flightNumber, DateOnly date, CancellationToken cancellationToken);
}
