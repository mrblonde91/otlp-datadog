using System.Diagnostics;
using Datadog.Trace;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetrySample.Contracts;
using Serilog;
using Serilog.Context;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Shared;

ActivitySourcesSetup.Init();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.WithSpan()
    .WriteTo.Console()
    .Enrich.WithProperty("Application", "OpenTelemetrySample.Client")
    .Enrich.FromLogContext()
    .CreateLogger();

// Activity
var source = new ActivitySource("Client");
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("OpenTelemetrySample.Client"))
    .SetSampler(new AlwaysOnSampler())
    .AddSource("Client")
    .AddConsoleExporter()
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://opentelemetry-collector:4317/api/v1/trace");
    })
    .Build();

Activity.DefaultIdFormat = ActivityIdFormat.W3C;
Activity.ForceDefaultIdFormat = true;

// Make an api call
var activity = new Activity("Client.Get").Start();
Log.Information("Client started");
Log.Information("Current Activity: {@Activity}", activity);
var client = new HttpClient();
var response = await client.GetAsync($"http://localhost:5009/{Endpoints.GetSimpleApiCall}");
