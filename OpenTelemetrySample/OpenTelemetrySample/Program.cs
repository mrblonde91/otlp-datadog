using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetrySample;
using OpenTelemetrySample.Contracts;
using Orleans;
using Orleans.Configuration;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Microsoft.Extensions.Hosting;
using Orleans.Statistics;

ActivitySourcesSetup.Init();
var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.WithSpan()
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341")
    .Enrich.WithProperty("Application", "OpenTelemetrySample")
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();
// Add services to the container.
builder.SetupOrleansSilo();

builder.Services.AddControllers();
var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
var rb = ResourceBuilder.CreateDefault().AddService("OpenTelemetrySample.OTLP",
    serviceVersion: assemblyVersion, serviceInstanceId: Environment.MachineName);
using var traceprovider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(rb)
    .AddSource("OpenTelemetrySample.Tracing").Build();

builder.Services.AddOpenTelemetryTracing((options) =>
{
    options.AddSource("OpenTelemetrySample.Tracing");
    options.SetResourceBuilder(rb).SetSampler(new AlwaysOnSampler()).AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
    options.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri("http://opentelemetry-collector:4317/api/v1/trace");
    });
});

builder.Services.Configure<AspNetCoreInstrumentationOptions>(options =>
{
    options.RecordException = true;
});

builder.Services.AddOpenTelemetryMetrics(options =>
{
    options.SetResourceBuilder(rb)
        .AddAspNetCoreInstrumentation();
    options.AddMeter("OpenTelemetrySample.Meter");
    options.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri("http://opentelemetry-collector:4317/api/v1/metrics");

    });
});

builder.Services.AddEndpointsApiExplorer();

builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(rb);
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.IncludeFormattedMessage = true;
    options.AddOtlpExporter(otlpOptions =>
   {
       otlpOptions.Endpoint = new Uri("http://opentelemetry-collector:4317/api/v1/metrics");
   });
});

builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();



app.MapGet(Endpoints.GetSimpleApiCall, ([FromServices] ILogger<Program> logger) =>
{
    var currentActivity = Activity.Current;

    logger.LogInformation("Server Activity: {@Activity}", currentActivity);
});

app.Run();