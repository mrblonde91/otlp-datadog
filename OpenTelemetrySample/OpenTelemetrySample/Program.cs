using System.Diagnostics;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetrySample.Contracts;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.WithSpan()
    .WriteTo.Console()
    .WriteTo.Seq("http://seq:5341")
    .Enrich.WithProperty("Application", "Server")
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddControllers();
var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
var rb = ResourceBuilder.CreateDefault().AddService("OpenTelemetrySample",
    serviceVersion: assemblyVersion, serviceInstanceId: Environment.MachineName);

 var tracerProvider = Sdk.CreateTracerProviderBuilder().Build();
builder.Services.AddSingleton(tracerProvider);

builder.Services.AddOpenTelemetryTracing((options) =>
{
    options.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri("http://opentelemetry-collector:4317/api/v1/trace");
    });
    options.SetResourceBuilder(rb).SetSampler(new AlwaysOnSampler())
        .AddHttpClientInstrumentation().AddAspNetCoreInstrumentation();
});

builder.Services.Configure<AspNetCoreInstrumentationOptions>(options =>
{
    options.RecordException = true;
});

builder.Services.AddOpenTelemetryMetrics(options =>
{
    options.SetResourceBuilder(rb).AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation();
    options.AddMeter("OpenTelemetrySample");
    options.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri("http://opentelemetry-collector:4317/api/v1/metrics");

    });
});

builder.Services.AddEndpointsApiExplorer();

builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.IncludeFormattedMessage = true;
    options.SetResourceBuilder(rb);
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