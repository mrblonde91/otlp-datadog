using System.Diagnostics;

public class HelloGrain : IHelloGrain
{
    private readonly ILogger _logger;

    public HelloGrain(ILogger<IHelloGrain> logger)
    {
        _logger = logger;
    }

    public Task<string> SayHello(string greeting)
    {
        _logger.LogInformation("entered simple api");
        var activitysource = new System.Diagnostics.ActivitySource("HelloGrain");

        var activity = System.Diagnostics.Activity.Current;
        _logger.LogInformation("{@activity}", activity);
        activity?.AddEvent(new ActivityEvent(greeting));

        return Task.FromResult($"You said: '{greeting}', I say: Hello!");
    }
}