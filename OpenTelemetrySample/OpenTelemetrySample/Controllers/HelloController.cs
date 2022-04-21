using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Datadog.Trace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
    public Task<string> Get(string name)
    {
        var activitySource = new ActivitySource("HelloWorld");
        using (var activity = activitySource.StartActivity())
        {
            activity?.SetTag("Name", name);
            _logger.LogInformation("Hello {Name}", name);
            return Task.FromResult($"Hello {name}");
        }
    
}
}