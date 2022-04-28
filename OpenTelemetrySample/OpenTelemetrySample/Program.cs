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
using OpenTelemetrySample.Services;
using OpenTelemetrySample.Settings;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Events;
using Shared;

ActivitySourcesSetup.Init();
var builder = WebApplication.CreateBuilder(args);
var settings = new OtlpSettings();
builder.Configuration.GetSection("Otlp").Bind(settings);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.WithSpan()
    .WriteTo.Console()
    .Enrich.WithProperty("Application", settings.ServiceName)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();
// Add services to the container.
builder.SetupOrleansSilo();

builder.Services.AddControllers();
var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
var rb = ResourceBuilder.CreateDefault().AddService(settings.ServiceName,
    serviceVersion: assemblyVersion, serviceInstanceId: Environment.MachineName);
using var traceprovider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(rb)
    .AddSource("OpenTelemetrySample.Tracing").Build();

builder.Services.AddOpenTelemetryTracing(options =>
{
    options.AddSource("OpenTelemetrySample.Tracing");
    options.SetResourceBuilder(rb).SetSampler(new AlwaysOnSampler()).AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation();
    options.AddOtlpExporter(otlpOptions => { otlpOptions.Endpoint = new Uri(settings.Endpoint + "/api/v1/traces"); });
});

builder.Services.Configure<AspNetCoreInstrumentationOptions>(options => { options.RecordException = true; });

builder.Services.AddOpenTelemetryMetrics(options =>
{
    options.SetResourceBuilder(rb)
        .AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
    options.AddMeter("*");
    options.AddRuntimeMetrics(options =>
    {
        options.ThreadingEnabled = true;
        options.GcEnabled = true;
        options.ProcessEnabled = true;
    });
    options.AddOtlpExporter(otlpOptions => { otlpOptions.Endpoint = new Uri(settings.Endpoint + "/api/v1/metrics"); });
    options.AddConsoleExporter();
});

builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(rb);
    options.IncludeScopes = true;
    options.ParseStateValues = true;
    options.IncludeFormattedMessage = true;
    options.AddOtlpExporter(otlpOptions => { otlpOptions.Endpoint = settings.Endpoint; });
});

builder.Services.AddSwaggerGen();
builder.Services.AddTransient<IDynamoService, DynamoService>();
builder.Services.AddEndpointsApiExplorer();

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