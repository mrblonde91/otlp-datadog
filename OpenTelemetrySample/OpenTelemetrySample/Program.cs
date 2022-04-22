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
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.WithSpan()
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341")
    .Enrich.WithProperty("Application", "Server")
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.

builder.Services.AddControllers();
var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
var rb = ResourceBuilder.CreateDefault().AddService("OpenTelemetrySample",
    serviceVersion: assemblyVersion, serviceInstanceId: Environment.MachineName);

 var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("OpenTelemetrySampleTracer")
    .SetResourceBuilder(
        ResourceBuilder.CreateDefault().AddTelemetrySdk()
            .AddService(serviceName: "ServiceA"))
    .AddOtlpExporter(options => 
        options.Endpoint = new Uri("http://opentelemetry-collector:4317/api/v1/trace"))
    .Build();
builder.Services.AddSingleton(tracerProvider);

builder.Services.AddOpenTelemetryTracing((options) =>
{
    options.AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri("http://opentelemetry-collector:4317/api/v1/trace");
    });
    options.SetResourceBuilder(rb).SetSampler(new AlwaysOnSampler()).AddHttpClientInstrumentation()
        .AddHttpClientInstrumentation().AddAspNetCoreInstrumentation();
});

builder.Services.Configure<AspNetCoreInstrumentationOptions>(options =>
{
    options.RecordException = true;
});

builder.Services.AddOpenTelemetryMetrics(options =>
{
    options.SetResourceBuilder(rb).AddHttpClientInstrumentation().AddHttpClientInstrumentation()
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapGet(Endpoints.GetSimpleApiCall, ([FromServices] ILogger<Program> logger) =>
{
    var currentActivity = Activity.Current;

    logger.LogInformation("Current Activity: {@Activity}", currentActivity);
    logger.LogInformation("entered simple api");
});

app.Run();