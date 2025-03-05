using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoTargetScaler : ITargetScaler
    {
        public const int DefaultMaxItemsPerInvocation = 1000;
        private readonly string _functionId;
        private readonly TargetScalerDescriptor _targetScalerDescriptor;
        private readonly CosmosDBMongoTriggerMetricsProvider _metricsProvider;
        private readonly ILogger<CosmosDBMongoTargetScaler> _logger;
        private readonly int _maxItemsPerInvocation;

        public CosmosDBMongoTargetScaler(
            string functionId,
            int maxItemsPerInvocation,
            CosmosDBMongoTriggerMetricsProvider metricsProvider,
            ILogger<CosmosDBMongoTargetScaler> logger)
        {
            _functionId = functionId ?? throw new ArgumentNullException(nameof(functionId));
            _targetScalerDescriptor = new TargetScalerDescriptor(functionId);
            _metricsProvider = metricsProvider ?? throw new ArgumentNullException(nameof(metricsProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxItemsPerInvocation = maxItemsPerInvocation;
        }

        public TargetScalerDescriptor TargetScalerDescriptor => _targetScalerDescriptor;

        public async Task<TargetScalerResult> GetScaleResultAsync(TargetScalerContext context)
        {
            var metrics = await _metricsProvider.GetMetricsAsync();
            long totalWork = metrics.PendingEventsCount + metrics.ProcessingEventsCount;

            return GetScaleResultInternal(context, totalWork);
        }

        internal TargetScalerResult GetScaleResultInternal(TargetScalerContext context, long totalWork)
        {
            int concurrency;

            if (!context.InstanceConcurrency.HasValue)
            {
                concurrency = _maxItemsPerInvocation > 0 ? _maxItemsPerInvocation : DefaultMaxItemsPerInvocation;
            }
            else
            {
                concurrency = context.InstanceConcurrency.Value;
            }

            if (concurrency <= 0)
            {
                _logger.LogWarning($"Concurrency value for target based scale must be greater than 0. Using default value of {DefaultMaxItemsPerInvocation} as concurrency value.");
                concurrency = DefaultMaxItemsPerInvocation;
            }

            int targetWorkerCount;

            try
            {
                checked
                {
                    targetWorkerCount = (int)Math.Ceiling(totalWork / (decimal)concurrency);
                }
            }
            catch (OverflowException)
            {
                targetWorkerCount = int.MaxValue;
            }

            // Ensure at least one worker
            targetWorkerCount = Math.Max(1, targetWorkerCount);

            string targetScaleMessage = $"Target worker count for function '{_functionId}' is '{targetWorkerCount}' (TotalWork='{totalWork}', Concurrency='{concurrency}').";
            _logger.LogInformation(targetScaleMessage);

            return new TargetScalerResult
            {
                TargetWorkerCount = targetWorkerCount
            };
        }
    }
}