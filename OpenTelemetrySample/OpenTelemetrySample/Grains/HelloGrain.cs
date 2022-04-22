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
        
        // this code is probably wrong
        var activitySource = new ActivitySource("ActivitySourceName");
        var activityListener = new ActivityListener
        {
            ShouldListenTo = s => true,
            SampleUsingParentId = (ref ActivityCreationOptions<string> activityOptions) => ActivitySamplingResult.AllData,
            Sample = (ref ActivityCreationOptions<ActivityContext> activityOptions) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(activityListener);

        using var activity = activitySource.StartActivity("MethodType:/Path");


        _logger.LogInformation("entered simple api");



        _logger.LogInformation("{@activity}", activity);
        activity.AddEvent(new ActivityEvent(greeting));
        activity.Stop();
        return Task.FromResult($"You said: '{greeting}', I say: Hello!");
    }
}