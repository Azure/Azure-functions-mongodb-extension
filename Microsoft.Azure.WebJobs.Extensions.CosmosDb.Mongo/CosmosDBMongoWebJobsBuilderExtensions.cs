using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.WebJobs.Extensions.CosmosDb.Mongo
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
    }
}