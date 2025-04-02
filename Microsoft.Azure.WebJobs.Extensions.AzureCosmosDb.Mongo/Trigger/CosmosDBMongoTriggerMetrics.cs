using Microsoft.Azure.WebJobs.Host.Scale;
using System;
using System.Threading;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    internal class CosmosDBMongoTriggerMetrics : ScaleMetrics
    {
        private long _pendingEventsCount;

        public CosmosDBMongoTriggerMetrics()
        {
            Timestamp = DateTime.UtcNow;
        }

        public CosmosDBMongoTriggerMetrics(long pendingCount)
        {
            IncrementPendingCount(pendingCount);
        }

        public long PendingEventsCount => Interlocked.Read(ref _pendingEventsCount);
        
        public void IncrementPendingCount(long count = 1)
        {
            Interlocked.Add(ref _pendingEventsCount, count);
            UpdateTimestamp();
        }

        public void DecrementPendingCount()
        {
            Interlocked.Decrement(ref _pendingEventsCount);
            UpdateTimestamp();
        }

        private void UpdateTimestamp()
        {
            Timestamp = DateTime.UtcNow;
        }
    }
}