// using Microsoft.Azure.WebJobs.Host.Scale;
// using Microsoft.Extensions.Logging;
// using System;
// using System.Globalization;
// using System.Threading.Tasks;

// namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
// {
//     internal class CosmosDBMongoTargetScaler : ITargetScaler
//     {
//         private readonly string _functionId;
//         private readonly string _databaseName;
//         private readonly string _collectionName;
//         private readonly CosmosDBMongoMetricsProvider _cosmosDBMongoMetricsProvider;
//         private readonly TargetScalerDescriptor _targetScalerDescriptor;
//         private readonly ILogger _logger;

//         private int _defaultMaxWorkPerInstance = 1000;

//         public CosmosDBMongoTargetScaler(
//             string functionId,
//             string databaseName,
//             string collectionName,
//             ILoggerFactory loggerFactory
//             )
//         {
//             this._functionId = functionId;
//             this._databaseName = databaseName;
//             this._collectionName = collectionName;
//             this._cosmosDBMongoMetricsProvider = new CosmosDBMongoMetricsProvider(this._functionId, this._databaseName, this._collectionName, loggerFactory);
//             this._targetScalerDescriptor = new TargetScalerDescriptor(functionId);
//             this._logger = loggerFactory.CreateLogger<CosmosDBMongoTargetScaler>();
//         }

//         public TargetScalerDescriptor TargetScalerDescriptor => _targetScalerDescriptor;

//         public async Task<TargetScalerResult> GetScaleResultAsync(TargetScalerContext context)
//         {
//             try
//             {
//                 long pendingWorkCount = await _cosmosDBMongoMetricsProvider.GetLatestPendingWorkCountAsync().ConfigureAwait(false);
//                 return GetScaleResultInternal(context, pendingWorkCount);
//             }
//             catch (UnauthorizedAccessException ex)
//             {
//                 this._logger.LogError(Events.OnScalingError, $"Target scaler is not supported. Exception: {ex}");
//                 throw new NotSupportedException("Target scaler is not supported.", ex);
//             }
//         }

//         internal TargetScalerResult GetScaleResultInternal(TargetScalerContext context, long pendingWorkCount)
//         {
//             int concurrency = context.InstanceConcurrency.Value;

//             if (concurrency < 1)
//             {
//                 throw new ArgumentOutOfRangeException($"Unexpected concurrency='{concurrency}' - the value must be > 0.");
//             }

//             int targetWorkerCount;

//             try
//             {
//                 checked
//                 {
//                     targetWorkerCount = (int)Math.Ceiling(pendingWorkCount / (decimal)_defaultMaxWorkPerInstance);
//                 }
//             }
//             catch (OverflowException)
//             {
//                 targetWorkerCount = int.MaxValue;
//             }

//             _logger.LogInformation($"Target worker count for function '{_functionId}-{_databaseName}-{_collectionName}' is '{targetWorkerCount}' Concurrency='{concurrency}').");

//             return new TargetScalerResult
//             {
//                 TargetWorkerCount = targetWorkerCount
//             };
//         }
//     }
// }