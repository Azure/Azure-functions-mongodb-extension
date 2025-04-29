// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    internal class CosmosDBMongoScaleMonitor : IScaleMonitor<CosmosDBMongoTriggerMetrics>
    {
        private readonly string _functionId;
        private readonly string _databaseName;
        private readonly string _collectionName;
        private readonly ScaleMonitorDescriptor _scaleMonitorDescriptor;
        private readonly ILogger<CosmosDBMongoScaleMonitor> _logger;
        private readonly CosmosDBMongoMetricsProvider _cosmosDBMongoMetricsProvider;
        private int _maxWorkPerInstance;
        private int _minSampleCount;

        public CosmosDBMongoScaleMonitor(
            string functionName,
            string databaseName,
            string collectionName,
            ILoggerFactory loggerFactory,
            int maxWorkPerInstance = 1000,
            int minSampleCount = 5
            )
        {
            _functionId = functionName;
            _databaseName = databaseName;
            _collectionName = collectionName;
            _scaleMonitorDescriptor = new ScaleMonitorDescriptor($"{_functionId}-{_databaseName}-{_collectionName}", _functionId);
            _logger = loggerFactory.CreateLogger<CosmosDBMongoScaleMonitor>();
            _cosmosDBMongoMetricsProvider = new CosmosDBMongoMetricsProvider(_functionId, _databaseName, _collectionName, loggerFactory);
            _maxWorkPerInstance = maxWorkPerInstance;
            _minSampleCount = minSampleCount;
        }

        public ScaleMonitorDescriptor Descriptor
        {
            get
            {
                return _scaleMonitorDescriptor;
            }
        }

        async Task<ScaleMetrics> IScaleMonitor.GetMetricsAsync()
        {
            return await GetMetricsAsync().ConfigureAwait(false);
        }

        public async Task<CosmosDBMongoTriggerMetrics> GetMetricsAsync()
        {
            return await _cosmosDBMongoMetricsProvider.GetMetricsAsync().ConfigureAwait(false);
        }

        ScaleStatus IScaleMonitor.GetScaleStatus(ScaleStatusContext context)
        {
            return GetScaleStatusCore(context.WorkerCount, context.Metrics?.Cast<CosmosDBMongoTriggerMetrics>().ToArray());
        }

        public ScaleStatus GetScaleStatus(ScaleStatusContext<CosmosDBMongoTriggerMetrics> context)
        {
            return GetScaleStatusCore(context.WorkerCount, context.Metrics?.ToArray());
        }

        private ScaleStatus GetScaleStatusCore(int workerCount, CosmosDBMongoTriggerMetrics[] metrics)
        {
            ScaleStatus status = new ScaleStatus
            {
                Vote = ScaleVote.None
            };

            // Unable to determine the correct vote with no metrics.
            if (metrics == null || metrics.Length == 0)
            {
                return status;
            }

            // At least _minSampleCount samples are required to make a scale decision for the rest of the checks.
            if (metrics.Length < _minSampleCount)
            {
                return status;
            }

            // Samples are in chronological order. Check for a continuous increase in message count.
            // If detected, this results in an automatic scale out for the site container.
            if (metrics[metrics.Length-1].PendingEventsCount > 0)
            {
                bool PendingWorkCountHitMaxForN =
                IsTrueForLastN(
                    metrics,
                    _minSampleCount,
                    (prev, next) => prev.PendingEventsCount <= next.PendingEventsCount && next.PendingEventsCount >= _maxWorkPerInstance);
                if (PendingWorkCountHitMaxForN)
                {
                    status.Vote = ScaleVote.ScaleOut;
                    return status;
                }
            }

            bool PendingWorkCountDecreasing =
                IsTrueForLastN(
                    metrics,
                    _minSampleCount,
                    (prev, next) => prev.PendingEventsCount >= next.PendingEventsCount);
            if (PendingWorkCountDecreasing)
            {
                status.Vote = ScaleVote.ScaleIn;
                return status;
            }

            _logger.LogDebug($"CosmosDB Mongo trigger function '{_functionId}-{_databaseName}-{_collectionName}' is steady.");

            return status;
        }

        private static bool IsTrueForLastN(IList<CosmosDBMongoTriggerMetrics> samples, int count, Func<CosmosDBMongoTriggerMetrics, CosmosDBMongoTriggerMetrics, bool> predicate)
        {
            // Walks through the list from left to right starting at len(samples) - count.
            for (int i = samples.Count - count - 1; i < samples.Count - 1; i++)
            {
                if (!predicate(samples[i], samples[i + 1]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}