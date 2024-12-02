using MongoDB.Driver;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public interface ICosmosDBMongoBindingCollectorFactory
    {
        MongoClient CreateClient(string connectionString);
    }
}
