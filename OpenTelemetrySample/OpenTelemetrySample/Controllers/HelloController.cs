using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using Orleans;

namespace OpenTelemetrySample.Controllers;

[ApiController]
[Route("[controller]")]
public class HelloController : ControllerBase
{
    private readonly ILogger<HelloController> _logger;
    private readonly IGrainFactory _grainFactory;

    public HelloController(ILogger<HelloController> logger, IGrainFactory grainFactory)
    {
        _logger = logger;
        _grainFactory = grainFactory;
    }

    [HttpGet]
    public async Task<string> Get(string name)
    {
        var activitySource = new ActivitySource("ExampleTracer");
        using var activity = activitySource.StartActivity("100 ms delay", ActivityKind.Server);
        Baggage.Current.SetBaggage("Life Issues", "Plenty of them");
        await Task.Delay(100);
        var activity2 = System.Diagnostics.Activity.Current;
        _logger.LogInformation("{@activity}", activity2);
        var helloGrain = _grainFactory.GetGrain<IHelloGrain>(name);
        await helloGrain.SayHello(name);

        _logger.LogInformation("Hello {Name}", name);
        return await Task.FromResult($"Hello {name}");

    }
}