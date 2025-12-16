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
            if (_leaseCollectionManager == null)
            {
                _logger.LogError("Lease collection manager is not configured.");
                throw new InvalidOperationException("Lease collection manager is not configured.");
            }
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
                
                _logger.LogInformation($"Retrieved lease collection metrics with pending count: {pendingCount} for function {_functionId}");
                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to get metrics from lease collection: {ex.Message}");
                throw new InvalidOperationException("Failed to get metrics from lease collection.", ex);
            }
        }

        public async Task<long> GetLatestPendingWorkCountAsync()
        {
            var metrics = await GetMetricsAsync();
            return metrics.PendingEventsCount;
        }
    }
}