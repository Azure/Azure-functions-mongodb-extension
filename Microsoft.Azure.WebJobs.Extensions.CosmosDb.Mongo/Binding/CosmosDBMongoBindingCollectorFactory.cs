using System;
using MongoDB.Driver;

namespace Microsoft.Azure.WebJobs.Extensions.CosmosDb.Mongo
{
    public class CosmosDBMongoBindingCollectorFactory : ICosmosDBMongoBindingCollectorFactory
    {
        public MongoClient CreateClient(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            return new MongoClient(connectionString);
        }
    }
}