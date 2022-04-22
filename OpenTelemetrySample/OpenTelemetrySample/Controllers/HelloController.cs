using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;

namespace OpenTelemetrySample.Controllers;

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

        _logger.LogInformation("Hello {Name}", name);
        return await Task.FromResult($"Hello {name}");
        
    }
}