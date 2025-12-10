// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;

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
            var configProvider = serviceProvider.GetService<CosmosDBMongoConfigProvider>();
            
            // Resolve connection string from setting name
            string leaseConnectionString = configProvider.ResolveConnectionString(cosmosDBMongoTriggerMetadata.LeaseConnectionStringSetting);
            var leaseClient = configProvider.GetService(leaseConnectionString);
            
            var leaseCollectionManager = new LeaseCollectionManager(
                leaseClient,
                cosmosDBMongoTriggerMetadata.LeaseDatabaseName,
                cosmosDBMongoTriggerMetadata.LeaseCollectionName,
                loggerFactory.CreateLogger<LeaseCollectionManager>());

            // Initialize lease collection manager
            leaseCollectionManager.InitializeAsync().GetAwaiter().GetResult(); 

            _scaleMonitor = new CosmosDBMongoScaleMonitor(
                cosmosDBMongoTriggerMetadata.FunctionName,
                cosmosDBMongoTriggerMetadata.DatabaseName,
                cosmosDBMongoTriggerMetadata.CollectionName,
                loggerFactory,
                leaseCollectionManager,
                maxWorkPerInstance: cosmosDBMongoTriggerMetadata.MaxWorkPerInstance,
                minSampleCount: cosmosDBMongoTriggerMetadata.MinSampleCount);
            _targetScaler = new CosmosDBMongoTargetScaler(
                cosmosDBMongoTriggerMetadata.FunctionName,
                cosmosDBMongoTriggerMetadata.DatabaseName,
                cosmosDBMongoTriggerMetadata.CollectionName,
                loggerFactory,
                leaseCollectionManager,
                maxWorkPerInstance: cosmosDBMongoTriggerMetadata.MaxWorkPerInstance,
                maxWorkInstance: cosmosDBMongoTriggerMetadata.MaxInstanceCount);
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
            public string FunctionName { get; set; } = string.Empty;

            [JsonProperty]
            public string DatabaseName { get; set; } = string.Empty;

            [JsonProperty]
            public string CollectionName { get; set; } = string.Empty;

            [JsonProperty]
            public string LeaseConnectionStringSetting { get; set; }

            [JsonProperty]
            public string LeaseDatabaseName { get; set; }

            [JsonProperty]
            public string LeaseCollectionName { get; set; }

            public int MaxWorkPerInstance { get; set; } = 1000;
            public int MaxInstanceCount { get; set; } = 3;
            public int MinSampleCount { get; set; } = 5;
        }
    }
}