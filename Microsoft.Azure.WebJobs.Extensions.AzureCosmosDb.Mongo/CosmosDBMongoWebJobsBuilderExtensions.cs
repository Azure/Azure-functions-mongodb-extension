using Microsoft.Extensions.DependencyInjection;
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

            builder.Services.AddSingleton<CosmosDBMongoTriggerMetricsRegistry>();
            builder.Services.AddSingleton<CosmosDBMongoScalerProvider>();
            return builder;
        }
    }
}