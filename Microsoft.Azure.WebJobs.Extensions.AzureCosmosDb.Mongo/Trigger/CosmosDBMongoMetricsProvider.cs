using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    internal class CosmosDBMongoMetricsProvider
    {
        private readonly ILogger _logger;
        private readonly string _functionId;
        private readonly string _databaseName;
        private readonly string _collectionName;

        public CosmosDBMongoMetricsProvider(string functionId, string databaseName, string collectionName,ILoggerFactory loggerFactory)
        {
            this._functionId = functionId;
            this._databaseName = databaseName;
            this._collectionName = collectionName;
            this._logger = loggerFactory.CreateLogger<CosmosDBMongoMetricsProvider>();
        }

        public Task<CosmosDBMongoTriggerMetrics> GetMetricsAsync()
        {
            var metricsHistory = CosmosDBMongoMetricsStore.GetMetricsHistory(_functionId, _databaseName, _collectionName);
            var latestMetrics = metricsHistory.Length > 0
                ? metricsHistory[metricsHistory.Length - 1]
                : CosmosDBMongoMetricsStore.GetMetrics(_functionId, _databaseName, _collectionName);

            _logger.LogDebug($"Retrieved latest metrics with pending count: {latestMetrics.PendingEventsCount} for function {_functionId}");
            return Task.FromResult(latestMetrics);
        }

        public Task<CosmosDBMongoTriggerMetrics[]> GetMetricsHistoryAsync()
        {
            var metricsHistory = CosmosDBMongoMetricsStore.GetMetricsHistory(_functionId, _databaseName, _collectionName);
            return Task.FromResult(metricsHistory);
        }

        public async Task<long> GetLatestPendingWorkCountAsync()
        {
            var metrics = await GetMetricsAsync();
            return metrics.PendingEventsCount;
        }
    }
}