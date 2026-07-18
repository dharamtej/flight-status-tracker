using FlightStatus.Api.Models;

namespace FlightStatus.Api.Interfaces;

public interface IFlightStatusService
{
    Task<FlightStatusResult> GetStatusAsync(string flightNumber, DateOnly date, CancellationToken cancellationToken);
}
