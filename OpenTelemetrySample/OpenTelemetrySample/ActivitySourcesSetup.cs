using System.Diagnostics;

namespace OpenTelemetrySample;

public static class ActivitySourcesSetup
{
    public static ActivitySource ActivitySource = null;

    public static void Init()
    {
        ActivitySource = new ActivitySource("OpenTelemetrySample.Tracing","1.0.0");
        var listener = new ActivityListener()
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = activity => Console.WriteLine($"{activity.ParentId}:{activity.Id} - Start"),
            ActivityStopped = activity => Console.WriteLine($"{activity.ParentId}:{activity.Id} - Stop")
        };
        ActivitySource.AddActivityListener(listener);
    }
}