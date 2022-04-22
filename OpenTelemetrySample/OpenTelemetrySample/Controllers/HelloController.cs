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

    public HelloController(ILogger<HelloController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<string> Get(string name)
    {
        var activitySource = new ActivitySource("ExampleTracer");
        using var activity = activitySource.StartActivity("100 ms delay", ActivityKind.Server);
        Baggage.Current.SetBaggage("Life Issues", "Plenty of them");
        await Task.Delay(100);
        
        using var client = new HttpClient();
        _ = await client.GetAsync("http://web-b:80/ping");
        _logger.LogInformation("Hello {Name}", name);
        return await Task.FromResult($"Hello {name}");
        
    }
}