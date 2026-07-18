using FlightStatus.Api.Endpoints;
using FlightStatus.Api.Interfaces;
using FlightStatus.Api.Providers;
using FlightStatus.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

builder.Services.AddScoped<IFlightStatusProvider, AeroTrackProvider>();
builder.Services.AddScoped<IFlightStatusProvider, QuickFlightProvider>();
builder.Services.AddScoped<IFlightStatusService, FlightStatusService>();

var app = builder.Build();

app.MapFlightStatusEndpoints();

app.Run();
