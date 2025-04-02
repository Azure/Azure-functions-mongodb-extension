using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Azure.WebJobs.Host.Scale;
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

            builder.AddExtension<CosmosDBMongoConfigProvider>();

            builder.Services.AddSingleton<ICosmosDBMongoBindingCollectorFactory, CosmosDBMongoBindingCollectorFactory>();

            return builder;
        }

        // public static IWebJobsBuilder AddCosmosDBMongoScaleForTrigger(this IWebJobsBuilder builder, TriggerMetadata triggerMetadata)
        // {
        //     // We need to register an instance of ServiceBusScalerProvider in the DI container and then map it to the interfaces IScaleMonitorProvider and ITargetScalerProvider.
        //     // Since there can be more than one instance of ServiceBusScalerProvider, we have to store a reference to the created instance to filter it out later.
        //     CosmosDBMongoScalerProvider cosmosDBMongoScalerProvider = null;
        //     builder.Services.AddSingleton(serviceProvider =>
        //     {
        //         cosmosDBMongoScalerProvider = new CosmosDBMongoScalerProvider(serviceProvider, triggerMetadata);
        //         return cosmosDBMongoScalerProvider;
        //     });
        //     builder.Services.AddSingleton<IScaleMonitorProvider>(serviceProvider => serviceProvider.GetServices<CosmosDBMongoScalerProvider>().Single(x => x == cosmosDBMongoScalerProvider));
        //     builder.Services.AddSingleton<ITargetScalerProvider>(serviceProvider => serviceProvider.GetServices<CosmosDBMongoScalerProvider>().Single(x => x == cosmosDBMongoScalerProvider));

        //     return builder;
        // }
    }
}