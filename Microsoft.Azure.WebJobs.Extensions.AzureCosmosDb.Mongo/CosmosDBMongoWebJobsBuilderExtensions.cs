using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.WebJobs.Host.Scale;
using System;

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

        public static IWebJobsBuilder AddCosmosDBMongoScaleForTrigger(this IWebJobsBuilder builder, TriggerMetadata triggerMetadata)
        {
            IServiceProvider serviceProvider = null;
            Lazy<CosmosDBMongoScalerProvider> scalerProvider = new Lazy<CosmosDBMongoScalerProvider>(() => new CosmosDBMongoScalerProvider(serviceProvider, triggerMetadata));

            builder.Services.AddSingleton<IScaleMonitorProvider>(resolvedServiceProvider =>
            {
                serviceProvider = serviceProvider ?? resolvedServiceProvider;
                return scalerProvider.Value;
            });

            return builder;
        }
    }
}