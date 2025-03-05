using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoTriggerMetricsProvider
    {
        private readonly CosmosDBMongoTriggerMetrics _metrics;
        private readonly ILogger _logger;

        public CosmosDBMongoTriggerMetricsProvider(
            string functionId,
            string databaseName,
            string collectionName,
            CosmosDBMongoTriggerMetricsRegistry registry,
            ILogger logger)
        {
            this._metrics = registry.GetOrAdd(functionId, databaseName, collectionName);
            this._logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<CosmosDBMongoTriggerMetrics> GetMetricsAsync()
        {
            try
            {
                _metrics.Timestamp = DateTime.UtcNow;
                return _metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(Events.OnScalingError, $"Error getting metrics from MongoDB change stream. Exception: {ex.Message}");
                return new CosmosDBMongoTriggerMetrics();
            }
        }

        public void AddProcessingEvent(Guid eventId) => _metrics.AddProcessingEvent(eventId);
        public void RemoveProcessingEvent(Guid eventId) => _metrics.RemoveProcessingEvent(eventId);
        public void IncrementPendingCount(long count = 1) => _metrics.IncrementPendingCount(count);
        public void DecrementPendingCount() => _metrics.DecrementPendingCount();
    }
}