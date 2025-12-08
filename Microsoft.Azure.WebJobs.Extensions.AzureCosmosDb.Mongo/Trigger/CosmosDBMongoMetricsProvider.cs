// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    internal class CosmosDBMongoMetricsProvider
    {
        private readonly ILogger _logger;
        private readonly string _functionId;
        private readonly string _databaseName;
        private readonly string _collectionName;
        private readonly LeaseCollectionManager _leaseCollectionManager;

        public CosmosDBMongoMetricsProvider(
            string functionId, 
            string databaseName, 
            string collectionName,
            ILoggerFactory loggerFactory,
            LeaseCollectionManager leaseCollectionManager = null)
        {
            this._functionId = functionId;
            this._databaseName = databaseName;
            this._collectionName = collectionName;
            this._logger = loggerFactory.CreateLogger<CosmosDBMongoMetricsProvider>();
            this._leaseCollectionManager = leaseCollectionManager;
        }

        public async Task<CosmosDBMongoTriggerMetrics> GetMetricsAsync()
        {
            if (_leaseCollectionManager != null)
            {
                // Query lease collection for pending count
                try
                {
                    var pendingCount = await _leaseCollectionManager.CountPendingDocumentsAsync(
                        _functionId,
                        _databaseName,
                        _collectionName);
                    
                    var metrics = new CosmosDBMongoTriggerMetrics
                    {
                        PendingEventsCount = pendingCount,
                        Timestamp = DateTime.UtcNow
                    };
                    
                    _logger.LogDebug($"Retrieved lease collection metrics with pending count: {pendingCount} for function {_functionId}");
                    return metrics;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to get metrics from lease collection: {ex.Message}");
                    // Fall back to in-memory metrics
                }
            }
            
            // Fall back to in-memory metrics
            var metricsHistory = CosmosDBMongoMetricsStore.GetMetricsHistory(_functionId, _databaseName, _collectionName);
            var latestMetrics = metricsHistory.Length > 0
                ? metricsHistory[metricsHistory.Length - 1]
                : CosmosDBMongoMetricsStore.GetMetrics(_functionId, _databaseName, _collectionName);

            _logger.LogDebug($"Retrieved latest metrics with pending count: {latestMetrics.PendingEventsCount} for function {_functionId}");
            return latestMetrics;
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