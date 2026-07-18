using System.Globalization;
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
            if (string.IsNullOrWhiteSpace(flightNumber))
            {
                return Results.Problem(
                    detail: "flightNumber is required.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (string.IsNullOrWhiteSpace(date))
            {
                return Results.Problem(
                    detail: "date is required.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                return Results.Problem(
                    detail: "date must be in yyyy-MM-dd format.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var result = await flightStatusService.GetStatusAsync(flightNumber, parsedDate, cancellationToken);
            return Results.Ok(result);
        });
    }
}
