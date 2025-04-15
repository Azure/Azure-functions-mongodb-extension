using MongoDB.Driver;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public interface ICosmosDBMongoBindingCollectorFactory
    {
        IMongoClient CreateClient(string connectionString);
    }
}
