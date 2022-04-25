using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace OpenTelemetrySample.Services;

public class DynamoService : IDynamoService
{
    public ILogger<DynamoService> _logger;
    public DynamoService(ILogger<DynamoService> logger)
    {
        _logger = logger;  
    }
    public async Task PutItem(string name)
    {
        var chain = new CredentialProfileStoreChain("/home/app/.aws/credentials");
        chain.TryGetAWSCredentials("default", out var profile);

        AmazonDynamoDBClient client = new AmazonDynamoDBClient(profile, RegionEndpoint.USWest2);
        string tableName = "OtlpTestTable";
        Random random = new Random();
        var request = new PutItemRequest
        {
            TableName = tableName,
            Item = new Dictionary<string, AttributeValue>()
            {
                { "Id", new AttributeValue { N = random.NextInt64(2, 14515).ToString() } },
                { "Name", new AttributeValue { S = name } }
            }
        };
        await client.PutItemAsync(request);
    }
}

public interface IDynamoService
{
    Task PutItem(string name);
}