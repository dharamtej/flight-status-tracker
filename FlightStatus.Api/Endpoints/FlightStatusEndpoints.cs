using FlightStatus.Api.Interfaces;

namespace FlightStatus.Api.Endpoints;

public static class FlightStatusEndpoints
{
    public static void MapFlightStatusEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/flights/status", async (
            string? flightNumber,
            string? date,
            IFlightStatusService flightStatusService,
            CancellationToken cancellationToken) =>
        {
            if (!FlightStatusRequestValidator.TryValidate(flightNumber, date, out var parsedDate, out var error))
            {
                return Results.Problem(detail: error, statusCode: StatusCodes.Status400BadRequest);
            }

            var result = await flightStatusService.GetStatusAsync(flightNumber, parsedDate, cancellationToken);
            return Results.Ok(result);
        });
    }
}
