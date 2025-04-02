using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using System.Globalization;
using SharpCompress.Common;

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

        public CosmosDBMongoScaleMonitor(
            string functionId,
            string databaseName,
            string collectionName,
            ILoggerFactory loggerFactory
            )
        {
            _functionId = functionId;
            _databaseName = databaseName;
            _collectionName = collectionName;
            _scaleMonitorDescriptor = new ScaleMonitorDescriptor($"{_functionId}-{_databaseName}-{_collectionName}".ToLower(CultureInfo.InvariantCulture), functionId);
            _logger = loggerFactory.CreateLogger<CosmosDBMongoScaleMonitor>();
            _cosmosDBMongoMetricsProvider = new CosmosDBMongoMetricsProvider(_functionId, _databaseName, _collectionName, loggerFactory);
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

            // At least 5 samples are required to make a scale decision for the rest of the checks.
            if (metrics.Length < CosmosDBMongoConstant.NumberOfSamplesToConsiderForScaling)
            {
                return status;
            }

            // Samples are in chronological order. Check for a continuous increase in message count.
            // If detected, this results in an automatic scale out for the site container.
            if (metrics[0].PendingEventsCount > 0)
            {
                bool PendingWorkCountIncreasing =
                IsTrueForLastN(
                    metrics,
                    CosmosDBMongoConstant.NumberOfSamplesToConsiderForScaling,
                    (prev, next) => prev.PendingEventsCount < next.PendingEventsCount) && metrics[0].PendingEventsCount > 0;
                if (PendingWorkCountIncreasing)
                {
                    status.Vote = ScaleVote.ScaleOut;
                    _logger.LogInformation(Events.OnScalingOut, $"PendingWorkCount is increasing for '{_functionId}-{_databaseName}-{_collectionName}'.");
                    return status;
                }
            }

            bool PendingWorkCountDecreasing =
                IsTrueForLastN(
                    metrics,
                    CosmosDBMongoConstant.NumberOfSamplesToConsiderForScaling,
                    (prev, next) => prev.PendingEventsCount > next.PendingEventsCount);
            if (PendingWorkCountDecreasing)
            {
                status.Vote = ScaleVote.ScaleIn;
                _logger.LogInformation(Events.OnScalingIn, $"PendingWorkCount is decreasing for '{_functionId}-{_databaseName}-{_collectionName}'.");
                return status;
            }

            _logger.LogInformation($"CosmosDB Mongo trigger function '{_functionId}-{_databaseName}-{_collectionName}' is steady.");

            return new ScaleStatus { Vote = ScaleVote.None };
        }

        private static bool IsTrueForLastN(IList<CosmosDBMongoTriggerMetrics> samples, int count, Func<CosmosDBMongoTriggerMetrics, CosmosDBMongoTriggerMetrics, bool> predicate)
        {
            // Walks through the list from left to right starting at len(samples) - count.
            for (int i = samples.Count - count; i < samples.Count - 1; i++)
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