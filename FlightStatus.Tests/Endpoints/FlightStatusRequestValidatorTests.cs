using FlightStatus.Api.Endpoints;

namespace FlightStatus.Tests.Endpoints;

public class FlightStatusRequestValidatorTests
{
    [Fact]
    public void TryValidate_MissingFlightNumber_ReturnsFalseWithError()
    {
        var isValid = FlightStatusRequestValidator.TryValidate(null, "2026-01-15", out _, out var error);

        Assert.False(isValid);
        Assert.Equal("flightNumber is required.", error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryValidate_BlankFlightNumber_ReturnsFalseWithError(string flightNumber)
    {
        var isValid = FlightStatusRequestValidator.TryValidate(flightNumber, "2026-01-15", out _, out var error);

        Assert.False(isValid);
        Assert.Equal("flightNumber is required.", error);
    }

    [Fact]
    public void TryValidate_MissingDate_ReturnsFalseWithError()
    {
        var isValid = FlightStatusRequestValidator.TryValidate("SR100", null, out _, out var error);

        Assert.False(isValid);
        Assert.Equal("date is required.", error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryValidate_BlankDate_ReturnsFalseWithError(string date)
    {
        var isValid = FlightStatusRequestValidator.TryValidate("SR100", date, out _, out var error);

        Assert.False(isValid);
        Assert.Equal("date is required.", error);
    }

    [Theory]
    [InlineData("15-01-2026")]
    [InlineData("2026/01/15")]
    [InlineData("2026-13-01")]
    [InlineData("not-a-date")]
    public void TryValidate_MalformedDate_ReturnsFalseWithError(string date)
    {
        var isValid = FlightStatusRequestValidator.TryValidate("SR100", date, out _, out var error);

        Assert.False(isValid);
        Assert.Equal("date must be in yyyy-MM-dd format.", error);
    }

    [Fact]
    public void TryValidate_ValidRequest_ReturnsTrueWithParsedDateAndNoError()
    {
        var isValid = FlightStatusRequestValidator.TryValidate("SR100", "2026-01-15", out var parsedDate, out var error);

        Assert.True(isValid);
        Assert.Equal(new DateOnly(2026, 1, 15), parsedDate);
        Assert.Null(error);
    }
}
