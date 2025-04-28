using Microsoft.Azure.WebJobs.Host.Scale;
using System;
using System.Threading;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoTriggerMetrics : ScaleMetrics
    {
        public long PendingEventsCount { get; set; } = 0;
    }
}