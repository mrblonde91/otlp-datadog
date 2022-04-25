using System.Diagnostics;
using System.Diagnostics.Metrics;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Datadog.Trace;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetrySample.Services;
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

    public HelloController(ILogger<HelloController> logger, IGrainFactory grainFactory, IDynamoService dynamoService)
    {
        _logger = logger;
        _grainFactory = grainFactory;
        _dynamoService = dynamoService;
    }

    [HttpGet]
    public async Task<string> Get(string name)
    {
        
        var meter = new Meter("OpenTelemetrySample.HelloController.Get");

        var counter = meter.CreateCounter<int>("Requests");
        var histogram = meter.CreateHistogram<float>("RequestDuration", unit: "ms");
        meter.CreateObservableGauge("ThreadCount", () => new[] { new Measurement<int>(ThreadPool.ThreadCount) });
        
        using (LogContext.PushProperty("dd_trace_id", CorrelationIdentifier.TraceId.ToString()))
        using (LogContext.PushProperty("dd_span_id", CorrelationIdentifier.SpanId.ToString()))
        {
            using var activity = ActivitySourcesSetup.ActivitySource.StartActivity("100 ms delay", ActivityKind.Server);
            var stopwatch = Stopwatch.StartNew();
            activity?.SetStartTime(DateTime.Now);
            Baggage.Current = Baggage.Create(new Dictionary<string, string>(){{"name", name}});

            _logger.LogInformation("{@activity}", activity);
            var helloGrain = _grainFactory.GetGrain<IHelloGrain>(name);
            await helloGrain.SayHello(name);

            activity?.SetTag("Test", $"{name}");
            await _dynamoService.PutItem(name);
            activity?.SetEndTime(DateTime.Now);
            _logger.LogInformation("Hello {Name}", name);
            
            histogram.Record(stopwatch.ElapsedMilliseconds,
                tag: KeyValuePair.Create<string, object?>("Host", "otlptest"));
            return await Task.FromResult($"Hello {name}");
        }

    }
}