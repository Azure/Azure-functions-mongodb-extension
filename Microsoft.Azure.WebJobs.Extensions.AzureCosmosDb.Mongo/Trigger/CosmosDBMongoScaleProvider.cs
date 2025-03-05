
using System;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using static Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.CosmosDBMongoScalerProvider;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    internal class CosmosDBMongoScalerProvider : IScaleMonitorProvider
#if NETSTANDARD2_1_OR_GREATER
        , ITargetScalerProvider
#endif
    {
        private readonly IScaleMonitor _scaleMonitor;
#if NETSTANDARD2_1_OR_GREATER
        private readonly ITargetScaler _targetScaler;
#endif

        public CosmosDBMongoScalerProvider(
            IServiceProvider serviceProvider, 
            TriggerMetadata triggerMetadata)
        {
            var registry = serviceProvider.GetRequiredService<CosmosDBMongoTriggerMetricsRegistry>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            var metadata = JsonConvert.DeserializeObject<CosmosDbMongoMetadata>(triggerMetadata.Metadata.ToString());
            
            var metricsProvider = new CosmosDBMongoTriggerMetricsProvider(
                triggerMetadata.FunctionName,
                metadata.DatabaseName,
                metadata.CollectionName,
                registry,
                loggerFactory.CreateLogger<CosmosDBMongoTriggerMetricsProvider>());

            _scaleMonitor = new CosmosDBMongoScaleMonitor(
                triggerMetadata.FunctionName,
                metadata.DatabaseName,
                metadata.CollectionName,
                metricsProvider,
                loggerFactory.CreateLogger<CosmosDBMongoScaleMonitor>());

#if NETSTANDARD2_1_OR_GREATER
            _targetScaler = new CosmosDBMongoTargetScaler(
                triggerMetadata.FunctionName,
                metadata.MaxItemsPerInvocation,
                metricsProvider,
                loggerFactory.CreateLogger<CosmosDBMongoTargetScaler>());
#endif
        }

        public IScaleMonitor GetMonitor()
        {
            return _scaleMonitor;
        }

#if NETSTANDARD2_1_OR_GREATER
        public ITargetScaler GetTargetScaler()
        {
            return _targetScaler;
        }
#endif

        internal class CosmosDbMongoMetadata
        {
            public string DatabaseName { get; set; }
            
            public string CollectionName { get; set; }
            
            public int MaxItemsPerInvocation { get; set; }
        }
    }
}