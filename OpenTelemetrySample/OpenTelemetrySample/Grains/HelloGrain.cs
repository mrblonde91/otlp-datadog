using System.Diagnostics;
using Datadog.Trace;
using OpenTelemetrySample;
using Serilog.Context;
using Shared;

public class HelloGrain : IHelloGrain
{
    private readonly ILogger _logger;

    public HelloGrain(ILogger<IHelloGrain> logger)
    {
        _logger = logger;
    }

    public Task<string> SayHello(string greeting)
    {   
        using (LogContext.PushProperty("dd_trace_id", CorrelationIdentifier.TraceId.ToString()))
        using (LogContext.PushProperty("dd_span_id", CorrelationIdentifier.SpanId.ToString()))
        {
            using var activity = ActivitySourcesSetup.ActivitySource.StartActivity("GrainEntered");

            _logger.LogInformation("entered simple api");
            activity.SetStatus(ActivityStatusCode.Ok);
            activity.DisplayName = "GrainEntered";
            _logger.LogInformation("{@activity}", activity);
            activity.AddEvent(new ActivityEvent(greeting));
            activity.Stop();
            return Task.FromResult($"You said: '{greeting}', I say: Hello!");
        }
    }
    
}