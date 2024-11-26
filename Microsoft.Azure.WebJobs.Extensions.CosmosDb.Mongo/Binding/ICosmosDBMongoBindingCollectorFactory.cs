using System;
using MongoDB.Driver;

namespace Microsoft.Azure.WebJobs.Extensions.CosmosDb.Mongo
{
    public interface ICosmosDBMongoBindingCollectorFactory
    {
        MongoClient CreateClient(string connectionString);
    }
}
