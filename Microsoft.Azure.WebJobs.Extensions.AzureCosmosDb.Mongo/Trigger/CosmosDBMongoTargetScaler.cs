// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    internal class CosmosDBMongoTargetScaler : ITargetScaler
    {
        private readonly string _functionId;
        private readonly string _databaseName;
        private readonly string _collectionName;
        private readonly CosmosDBMongoMetricsProvider _cosmosDBMongoMetricsProvider;
        private readonly TargetScalerDescriptor _targetScalerDescriptor;
        private readonly ILogger _logger;

        private int _maxWorkPerInstance;
        private int _maxWorkInstance;

        public CosmosDBMongoTargetScaler(
            string functionName,
            string databaseName,
            string collectionName,
            ILoggerFactory loggerFactory,
            int maxWorkPerInstance = 1000,
            int maxWorkInstance = 3
            )
        {
            this._functionId = functionName;
            this._databaseName = databaseName;
            this._collectionName = collectionName;
            this._cosmosDBMongoMetricsProvider = new CosmosDBMongoMetricsProvider(this._functionId, this._databaseName, this._collectionName, loggerFactory);
            this._targetScalerDescriptor = new TargetScalerDescriptor(_functionId);
            this._maxWorkPerInstance = maxWorkPerInstance;
            this._maxWorkInstance = maxWorkInstance;
            this._logger = loggerFactory.CreateLogger<CosmosDBMongoTargetScaler>();
        }

        public TargetScalerDescriptor TargetScalerDescriptor => _targetScalerDescriptor;

        public async Task<TargetScalerResult> GetScaleResultAsync(TargetScalerContext context)
        {
            try
            {
                CosmosDBMongoTriggerMetrics metrics = await _cosmosDBMongoMetricsProvider.GetMetricsAsync();
                return GetScaleResultInternal(context, metrics.PendingEventsCount);
            }
            catch (UnauthorizedAccessException ex)
            {
                this._logger.LogError(Events.OnScalingError, $"Target scaler is not supported. Exception: {ex}");
                throw new NotSupportedException("Target scaler is not supported.", ex);
            }
        }

        internal TargetScalerResult GetScaleResultInternal(TargetScalerContext context, long pendingWorkCount)
        {
            int concurrency;

            if (!context.InstanceConcurrency.HasValue)
            {
                concurrency = _maxWorkPerInstance;
            }
            else
            {
                concurrency = context.InstanceConcurrency.Value;
            }

            if (concurrency < 1)
            {
                throw new ArgumentOutOfRangeException($"Unexpected concurrency='{concurrency}' - the value must be > 0.");
            }

            int targetWorkerCount = 1;

            try
            {
                checked
                {
                    targetWorkerCount = (int)Math.Ceiling(pendingWorkCount / (decimal)_maxWorkPerInstance);
                }
            }
            catch (OverflowException)
            {
                targetWorkerCount = int.MaxValue;
            }

            _logger.LogDebug($"Target worker count for function '{_functionId}-{_databaseName}-{_collectionName}' is '{targetWorkerCount}' Concurrency='{concurrency}').");

            return new TargetScalerResult
            {
                TargetWorkerCount = targetWorkerCount > _maxWorkInstance ? _maxWorkInstance : targetWorkerCount,
            };
        }
    }
}