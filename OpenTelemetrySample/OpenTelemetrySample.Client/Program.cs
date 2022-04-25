using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Resources;
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
await SimpleCall();
await OrleansCall();

async Task SimpleCall()
{
    using var activity = source.StartActivity("Get Simple Api Call", ActivityKind.Client);
    Log.Information("Client Activity: {@Activity}", activity);
    var client = new HttpClient();
    var response = await client.GetAsync($"http://localhost:5009/{Endpoints.GetSimpleApiCall}");
    activity?.Stop();
}

async Task OrleansCall()
{
    using var activity = source.StartActivity("Get Orleans Api Call", ActivityKind.Client);
    Log.Information("Client Activity: {@Activity}", activity);
    var client = new HttpClient();
    var response = await client.GetAsync($"http://localhost:5009/{Endpoints.GetOrleansApiCall}");
    activity?.Stop();
}
