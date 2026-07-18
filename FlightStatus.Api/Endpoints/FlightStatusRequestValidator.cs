using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace FlightStatus.Api.Endpoints;

internal static class FlightStatusRequestValidator
{
    public static bool TryValidate(
        [NotNullWhen(true)] string? flightNumber,
        string? date,
        out DateOnly parsedDate,
        [NotNullWhen(false)] out string? error)
    {
        parsedDate = default;

        if (string.IsNullOrWhiteSpace(flightNumber))
        {
            error = "flightNumber is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(date))
        {
            error = "date is required.";
            return false;
        }

        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
        {
            error = "date must be in yyyy-MM-dd format.";
            return false;
        }

        error = null;
        return true;
    }
}
