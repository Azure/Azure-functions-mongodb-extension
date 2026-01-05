// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Config;
using Microsoft.Azure.WebJobs.Host.Scale;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public static class CosmosDBMongoWebJobsBuilderExtensions
    {
        public static IWebJobsBuilder AddCosmosDBMongo(this IWebJobsBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // Add Azure component factory for Entra ID authentication support
            builder.Services.AddAzureClientsCore();

            builder.AddExtension<CosmosDBMongoConfigProvider>();
            builder.Services.TryAddSingleton<CosmosDBMongoConfigProvider>();

            // Register the new factory that supports both connection strings and Entra ID
            builder.Services.TryAddSingleton<ICosmosDBMongoBindingCollectorFactory, DefaultMongoDBServiceFactory>();

            return builder;
        }

        public static IWebJobsBuilder AddCosmosDBMongoScaleForTrigger(this IWebJobsBuilder builder, TriggerMetadata triggerMetadata)
        {
 
            CosmosDBMongoScalerProvider cosmosDBMongoScalerProvider = null;
            builder.Services.AddSingleton(serviceProvider =>
            {
                cosmosDBMongoScalerProvider = new CosmosDBMongoScalerProvider(serviceProvider, triggerMetadata);
                return cosmosDBMongoScalerProvider;
            });
            builder.Services.AddSingleton<IScaleMonitorProvider>(serviceProvider => serviceProvider.GetServices<CosmosDBMongoScalerProvider>().Single(x => x == cosmosDBMongoScalerProvider));
            builder.Services.AddSingleton<ITargetScalerProvider>(serviceProvider => serviceProvider.GetServices<CosmosDBMongoScalerProvider>().Single(x => x == cosmosDBMongoScalerProvider));

            return builder;
        }
    }
}