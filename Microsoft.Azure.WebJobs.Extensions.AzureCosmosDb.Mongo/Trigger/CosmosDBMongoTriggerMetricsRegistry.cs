using System.Collections.Concurrent;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoTriggerMetricsRegistry
    {
        private readonly ConcurrentDictionary<string, CosmosDBMongoTriggerMetrics> _metricsMap 
            = new ConcurrentDictionary<string, CosmosDBMongoTriggerMetrics>();

        public CosmosDBMongoTriggerMetrics GetOrAdd(string functionId, string databaseName, string collectionName)
        {
            string key = $"{functionId}-{databaseName}-{collectionName}".ToLower();
            return _metricsMap.GetOrAdd(key, _ => new CosmosDBMongoTriggerMetrics());
        }
    }
}