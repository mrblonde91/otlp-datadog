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
        using var activity = ActivitySourcesSetup.ActivitySource.StartActivity("100 ms delay", ActivityKind.Server);
        if (activity == null)
        {
            _logger.LogError("Activity is null");
            return "Activity is null";
        }
        activity?.SetStartTime(DateTime.Now);
        Baggage.Current.SetBaggage("Life Issues", "Plenty of them");
        await Task.Delay(100);

        _logger.LogInformation("{@activity}", activity);
        var helloGrain = _grainFactory.GetGrain<IHelloGrain>(name);
        await helloGrain.SayHello(name);

        activity?.SetTag("Test", $"{name}");
        activity?.SetEndTime(DateTime.Now);
        _logger.LogInformation("Hello {Name}", name);
        return await Task.FromResult($"Hello {name}");

    }
}