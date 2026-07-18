using FlightStatus.Api.Endpoints;
using FlightStatus.Api.Interfaces;
using FlightStatus.Api.Providers;
using FlightStatus.Api.Services;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

builder.Services.AddScoped<IFlightStatusProvider, AeroTrackProvider>();
builder.Services.AddScoped<IFlightStatusProvider, QuickFlightProvider>();
builder.Services.AddScoped<IFlightStatusService, FlightStatusService>();

var app = builder.Build();

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
            }
        });
    });
});

app.MapFlightStatusEndpoints();

app.Run();
