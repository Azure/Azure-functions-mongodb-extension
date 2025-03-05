using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoScaleMonitor : IScaleMonitor<CosmosDBMongoTriggerMetrics>
    {
        private readonly CosmosDBMongoTriggerMetricsProvider _metricsProvider;
        private readonly ILogger<CosmosDBMongoScaleMonitor> _logger;
        private readonly ScaleMonitorDescriptor _scaleMonitorDescriptor;
        private readonly string _functionId;

        private readonly int _scaleOutThreshold = 1000;
        private readonly int _scaleInThreshold = 5;

        public CosmosDBMongoScaleMonitor(
            string functionId,
            string databaseName,
            string collectionName,
            CosmosDBMongoTriggerMetricsProvider metricsProvider,
            ILogger<CosmosDBMongoScaleMonitor> logger)
        {
            _functionId = functionId ?? throw new ArgumentNullException(nameof(functionId));
            _metricsProvider = metricsProvider ?? throw new ArgumentNullException(nameof(metricsProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _scaleMonitorDescriptor = new ScaleMonitorDescriptor(
                $"{_functionId}-CosmosDBMongoTrigger-{databaseName}-{collectionName}".ToLower(), 
                _functionId);
        }

        public ScaleMonitorDescriptor  Descriptor => _scaleMonitorDescriptor;

        async Task<ScaleMetrics> IScaleMonitor.GetMetricsAsync()
        {
            return await GetMetricsAsync().ConfigureAwait(false);
        }

        public async Task<CosmosDBMongoTriggerMetrics> GetMetricsAsync()
        {
            return await this._metricsProvider.GetMetricsAsync().ConfigureAwait(false);
        }

        public ScaleStatus GetScaleStatus(ScaleStatusContext<CosmosDBMongoTriggerMetrics> context)
        {
            return GetScaleStatusCore(context.WorkerCount, context.Metrics?.ToArray());
        }

        public ScaleStatus GetScaleStatus(ScaleStatusContext context)
        {
            context.Metrics?.Cast<CosmosDBMongoTriggerMetrics>().ToArray();

            return GetScaleStatusCore(context.WorkerCount, context.Metrics?.Cast<CosmosDBMongoTriggerMetrics>().ToArray());
        }

        public ScaleStatus GetScaleStatusCore(int workerCount, CosmosDBMongoTriggerMetrics[] metrics)
        {            
            if (metrics == null || !metrics.Any())
            {
                return new ScaleStatus { Vote = ScaleVote.None };
            }

            var latestMetrics = metrics.Last();
            var totalWork = latestMetrics.PendingEventsCount + latestMetrics.ProcessingEventsCount;

            // Check if we need to scale up
            // When total work exceeds 1000 and we only have 1 worker, scale up to 2
            if (totalWork > _scaleOutThreshold && workerCount == 1)
            {
                _logger.LogInformation(Events.OnScaling, $"Scaling out due to high workload. Total work: {totalWork}");
                return new ScaleStatus { Vote = ScaleVote.ScaleOut };
            }

            // Check for scale down
            // We need at least 5 samples to make a scale down decision
            if (metrics.Count() >= _scaleInThreshold && workerCount > 1)
            {
                // Check if the function has been idle (no work) for the last 5 samples
                bool isIdle = metrics.Skip(Math.Max(0, metrics.Length - _scaleInThreshold)).Take(_scaleInThreshold).All(m => m.PendingEventsCount == 0 && m.ProcessingEventsCount == 0);

                if (isIdle)
                {
                    _logger.LogInformation(Events.OnScaling,"Scaling in due to sustained idle state");
                    return new ScaleStatus { Vote = ScaleVote.ScaleIn };
                }
            }

            return new ScaleStatus { Vote = ScaleVote.None };
        }
    }
}