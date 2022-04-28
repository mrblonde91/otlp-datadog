using System.Diagnostics;
using System.Diagnostics.Metrics;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Datadog.Trace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetrySample.Services;
using OpenTelemetrySample.Settings;
using Orleans;
using Serilog.Context;

namespace OpenTelemetrySample.Controllers;

[ApiController]
[Route("[controller]")]
public class HelloController : ControllerBase
{
    private readonly ILogger<HelloController> _logger;
    private readonly IGrainFactory _grainFactory;
    private readonly IDynamoService _dynamoService;
    private readonly IOptionsMonitor<OtlpSettings> _monitor;

    public HelloController(ILogger<HelloController> logger, IGrainFactory grainFactory, IDynamoService dynamoService, IOptionsMonitor<OtlpSettings> monitor)
    {
        _logger = logger;
        _grainFactory = grainFactory;
        _dynamoService = dynamoService;
        _monitor = monitor;
    }

    [HttpGet]
    public async Task<string> Get(string name)
    {
        
        var meter = new Meter("OpenTelemetrySample.HelloController.Get");

        var counter = meter.CreateCounter<int>("OpenTelemetrySample.HelloController.Get.Requests");
        var histogram = meter.CreateHistogram<float>("OpenTelemetrySample.HelloController.Get.RequestDuration", unit: "ms");
        meter.CreateObservableGauge("OpenTelemetrySample.HelloController.Get.ThreadCount", () => new[] { new Measurement<int>(ThreadPool.ThreadCount) });
        
        using (LogContext.PushProperty("dd_trace_id", CorrelationIdentifier.TraceId.ToString()))
        using (LogContext.PushProperty("dd_span_id", CorrelationIdentifier.SpanId.ToString()))
        {
            using var activity = ActivitySourcesSetup.ActivitySource.StartActivity("100 ms delay");
            var stopwatch = Stopwatch.StartNew();
            activity?.SetStartTime(DateTime.Now);
            activity?.SetStatus(ActivityStatusCode.Ok);
            Baggage.Current = Baggage.Create(new Dictionary<string, string>(){{"name", name}});
            await Task.Delay(100);
            _logger.LogInformation("{@activity}", activity);
            var helloGrain = _grainFactory.GetGrain<IHelloGrain>(name);
            await helloGrain.SayHello(name);

            activity?.SetTag("Test", $"{name}");
            if (_monitor.CurrentValue.UseDynamoDb)
            {
                await _dynamoService.PutItem(name);
            }

            activity?.SetEndTime(DateTime.Now);
            _logger.LogInformation("Hello {Name}", name);
            
            histogram.Record(stopwatch.ElapsedMilliseconds,
                tag: KeyValuePair.Create<string, object?>("Host", "otlptest"));
            return await Task.FromResult($"Hello {name}");
        }

    }
}