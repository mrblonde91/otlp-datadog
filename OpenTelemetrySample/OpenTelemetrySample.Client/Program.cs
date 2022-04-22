using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetrySample.Contracts;

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
Console.WriteLine(activity.Id);
var client = new HttpClient();
var response = await client.GetAsync($"https://localhost:5000/{Endpoints.GetSimpleApiCall}");
