using System;
using MongoDB.Driver;

namespace Microsoft.Azure.WebJobs.Extensions.CosmosDb.Mongo
{
    internal class CosmosDBMongoBindingClientConverter : IConverter<CosmosDBMongoAttribute, MongoClient>
    {
        private readonly CosmosDBMongoConfigProvider configProvider;

        public CosmosDBMongoBindingClientConverter(CosmosDBMongoConfigProvider configProvider)
        {
            this.configProvider = configProvider;
        }

        public MongoClient Convert(CosmosDBMongoAttribute attribute)
        {
            if (attribute == null)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            return this.configProvider.GetService(attribute.ConnectionStringSetting, attribute.DatabaseName, attribute.CollectionName);
        }
    }
}