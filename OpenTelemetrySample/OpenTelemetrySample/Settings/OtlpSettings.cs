namespace OpenTelemetrySample.Settings;

public class OtlpSettings
{
    public string ServiceName { get; set; }
    public Uri Endpoint { get; set; }
    public bool UseDynamoDb { get; set; }
}