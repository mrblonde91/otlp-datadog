﻿using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetrySample.Contracts;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.WithSpan()
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .Enrich.WithProperty("Application", "Client")
    .Enrich.FromLogContext()
    .CreateLogger();

// Activity
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetSampler(new AlwaysOnSampler())
    .AddSource("Client")
    .AddConsoleExporter()
    .Build();

Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

// Make an api call
var activity = new Activity("Client.Get").Start();
Log.Information("Client Activity: {@Activity}", activity);
var client = new HttpClient();
var response = await client.GetAsync($"http://localhost:5009/{Endpoints.GetSimpleApiCall}");
