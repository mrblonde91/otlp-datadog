using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
var tracingExporter = builder.Configuration.GetValue<string>("UseTracingExporter").ToLowerInvariant();
var log = new LoggerConfiguration()  // using Serilog;
    .Enrich.WithSpan()
    .WriteTo.Console(new JsonFormatter(renderMessage: true))

    .CreateLogger();
var rb = tracingExporter switch
{
    "otlp" => ResourceBuilder.CreateDefault().AddService(builder.Configuration.GetValue<string>("Otlp:ServiceName"),
        serviceVersion: assemblyVersion, serviceInstanceId: Environment.MachineName),
    "zipkin" => ResourceBuilder.CreateDefault().AddService(builder.Configuration.GetValue<string>("Zipkin:ServiceName")), 
    _ => ResourceBuilder.CreateDefault().AddService("sample-service", serviceVersion: "1.0.0",
            serviceInstanceId: Environment.MachineName),
};

builder.Services.AddOpenTelemetryTracing((options) =>
{
    switch (tracingExporter)
    {
        case "otlp":
            options.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
                otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
            break;
        case "zipkin":
            options.AddZipkinExporter(zipkinOptions =>
            {
                zipkinOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Zipkin:Endpoint"));
            });
            break;
        default:
            options.AddConsoleExporter();
            break;
    }
    
});
builder.Services.Configure<AspNetCoreInstrumentationOptions>(builder.Configuration.GetSection("AspNetCoreInstrumentation"));
builder.Logging.ClearProviders();
builder.Services.AddOpenTelemetryMetrics(options =>
{
    options.SetResourceBuilder(rb).AddHttpClientInstrumentation().AddAspNetCoreInstrumentation();

    var metricsExporter = builder.Configuration.GetValue<string>("UseMetricsExporter").ToLowerInvariant();
    switch(metricsExporter)
    {
        case "otlp":
            options.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
            });
            break;
        case "prometheus":
            options.AddPrometheusExporter();
            break;
        default:
            options.AddConsoleExporter();
            break;
    }

});
builder.Services.AddOpenTelemetryTracing();
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<OpenTelemetryLoggerOptions>(opt =>
{
    opt.IncludeScopes = true;
    opt.ParseStateValues = true;
    opt.IncludeFormattedMessage = true;
});

builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(rb);
    var logExporter = builder.Configuration.GetValue<string>("UseLogExporter").ToLowerInvariant();
    switch (logExporter)
    {
        case "otlp":
            options.AddOtlpExporter(otlpOptions =>
            {
                otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("Otlp:Endpoint"));
                otlpOptions.Protocol = OtlpExportProtocol.HttpProtobuf;
            });
            break;
        default:
            options.AddConsoleExporter();
            break;
    }
});
var metricsExporter = builder.Configuration.GetValue<string>("UseMetricsExporter").ToLowerInvariant();

builder.Services.AddSwaggerGen();

var app = builder.Build();
if (metricsExporter == "prometheus")
{
    app.UseOpenTelemetryPrometheusScrapingEndpoint();
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();