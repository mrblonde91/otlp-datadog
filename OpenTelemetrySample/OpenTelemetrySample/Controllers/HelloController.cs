using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Datadog.Trace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog.Context;

[ApiController]
[Route("[controller]")]
public class HelloController : ControllerBase
{
    private readonly ILogger<HelloController> _logger;
    private readonly TracerProvider _tracerProvider;

    public HelloController(ILogger<HelloController> logger, TracerProvider provider)
    {
        _logger = logger;
        _tracerProvider = provider;
    }

    [HttpGet]
    public async Task<string> Get(string name)
    {
        var activitySource = new ActivitySource("ExampleTrace");
        using var activity = activitySource.StartActivity("100 ms delay", ActivityKind.Server);
        await Task.Delay(100);
        
        using var activity2 = activitySource.StartActivity("Another computation", ActivityKind.Server);
        await Task.Delay(300);
        _logger.LogInformation("Hello {Name}", name);
        return await Task.FromResult($"Hello {name}");
        
    }
}