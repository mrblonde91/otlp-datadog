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
        using var activity = ActivitySourcesSetup.ActivitySource.StartActivity("100 ms delay", ActivityKind.Server);
        if (activity == null)
        {
            _logger.LogError("Activity is null");
            return "Activity is null";
        }
        activity?.SetStartTime(DateTime.Now);
        Baggage.Current.SetBaggage("Life Issues", "Plenty of them");
        await Task.Delay(100);
        activity?.SetTag("Test", $"{name}");
        activity?.SetEndTime(DateTime.Now);
        _logger.LogInformation("Hello {Name}", name);
        return await Task.FromResult($"Hello {name}");
        
    }
}