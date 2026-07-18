using FlightStatus.Api.Interfaces;
using FlightStatus.Api.Models;

namespace FlightStatus.Tests.TestDoubles;

internal sealed class FakeFlightStatusProvider : IFlightStatusProvider
{
    private readonly ProviderFlightStatus? _response;
    private readonly Exception? _exception;

    private FakeFlightStatusProvider(string providerName, ProviderFlightStatus? response, Exception? exception)
    {
        ProviderName = providerName;
        _response = response;
        _exception = exception;
    }

    public string ProviderName { get; }

    public static FakeFlightStatusProvider Responding(string providerName, ProviderFlightStatus response) =>
        new(providerName, response, exception: null);

    public static FakeFlightStatusProvider NotResponding(string providerName) =>
        new(providerName, response: null, exception: null);

    public static FakeFlightStatusProvider Throwing(string providerName, Exception exception) =>
        new(providerName, response: null, exception);

    public Task<ProviderFlightStatus?> GetStatusAsync(string flightNumber, DateOnly date, CancellationToken cancellationToken)
    {
        if (_exception is not null)
        {
            throw _exception;
        }

        return Task.FromResult(_response);
    }
}
