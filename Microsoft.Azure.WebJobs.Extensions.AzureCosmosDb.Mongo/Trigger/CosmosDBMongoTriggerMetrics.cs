using Microsoft.Azure.WebJobs.Host.Scale;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoTriggerMetrics : ScaleMetrics
    {
        private readonly ConcurrentDictionary<Guid, DateTime> _processingEvents;
        private long _pendingEventsCount;

        public CosmosDBMongoTriggerMetrics()
        {
            _processingEvents = new ConcurrentDictionary<Guid, DateTime>();
            Timestamp = DateTime.UtcNow;
        }

        public DateTime Timestamp { get; set; }
        
        public long ProcessingEventsCount => GetProcessingCount();
        
        public long PendingEventsCount => Interlocked.Read(ref _pendingEventsCount);

        public void AddProcessingEvent(Guid eventId)
        {
            _processingEvents[eventId] = DateTime.UtcNow;
        }

        public void RemoveProcessingEvent(Guid eventId)
        {
            _processingEvents.TryRemove(eventId, out _);
        }

        public void IncrementPendingCount(long count = 1)
        {
            Interlocked.Add(ref _pendingEventsCount, count);
        }

        public void DecrementPendingCount()
        {
            Interlocked.Decrement(ref _pendingEventsCount);
        }

        private int GetProcessingCount()
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(CosmosDBMongoConstant.MaxDurationForTriggerProcessingEvent);
            var stalledEvents = _processingEvents.Where(kvp => kvp.Value < cutoffTime).ToList();
            
            foreach (var evt in stalledEvents)
            {
                _processingEvents.TryRemove(evt.Key, out _);
            }

            return _processingEvents.Count;
        }
    }
}