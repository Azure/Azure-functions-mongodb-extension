using System;

using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    internal class CosmosDBMongoScalerProvider : IScaleMonitorProvider, ITargetScalerProvider
    {
        private readonly CosmosDBMongoScaleMonitor _scaleMonitor;
        private readonly CosmosDBMongoTargetScaler _targetScaler;

        public CosmosDBMongoScalerProvider(IServiceProvider serviceProvider, TriggerMetadata triggerMetadata)
        {
            CosmosDBMongoTriggerMetadata cosmosDBMongoTriggerMetadata = JsonConvert.DeserializeObject<CosmosDBMongoTriggerMetadata>(triggerMetadata.Metadata.ToString());
            ILoggerFactory loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            _scaleMonitor = new CosmosDBMongoScaleMonitor(
                cosmosDBMongoTriggerMetadata.FunctionId,
                cosmosDBMongoTriggerMetadata.DatabaseName,
                cosmosDBMongoTriggerMetadata.CollectionName,
                loggerFactory);
            _targetScaler = new CosmosDBMongoTargetScaler(
                cosmosDBMongoTriggerMetadata.FunctionId,
                cosmosDBMongoTriggerMetadata.DatabaseName,
                cosmosDBMongoTriggerMetadata.CollectionName,
                loggerFactory);
        }

        public IScaleMonitor GetMonitor()
        {
            return _scaleMonitor;
        }

        public ITargetScaler GetTargetScaler()
        {
            return _targetScaler;
        }

        internal class CosmosDBMongoTriggerMetadata
        {
            [JsonProperty]
            public string FunctionId { get; set; } = string.Empty;

            [JsonProperty]
            public string DatabaseName { get; set; } = string.Empty;

            [JsonProperty]
            public string CollectionName { get; set; } = string.Empty;
        }
    }
}