using MongoDB.Driver;
using System;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoBindingCollectorFactory : ICosmosDBMongoBindingCollectorFactory
    {
        public IMongoClient CreateClient(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentNullException(nameof(connectionString));
            }

            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.ApplicationName = CosmosDBMongoConstant.AzureFunctionApplicationName;

            return new MongoClient(settings);
        }
    }
}