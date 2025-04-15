using MongoDB.Driver;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    internal static class MongoUtility
    {
        //internal static async Task CreateCollectionIfNotExistAsync(CosmosDBMongoContext context)
        //{
        //    await CreateCollectionIfNotExistAsync(context.MongoClient, context.ResolvedAttribute.DatabaseName, context.ResolvedAttribute.CollectionName);
        //}

        internal static async Task CreateCollectionIfNotExistAsync(MongoCollectionReference reference)
        {
            await CreateCollectionIfNotExistAsync(reference.client, reference.databaseName, reference.collectionName);
        }

        internal static async Task CreateCollectionIfNotExistAsync(IMongoClient client, string DatabaseName, string CollectionName)
        {
            var database = client.GetDatabase(DatabaseName);
            bool isCollectionExist = (await database.ListCollectionNames().ToListAsync()).Contains(CollectionName);
            if (!isCollectionExist)
            {
                await database.CreateCollectionAsync(CollectionName);
            }
        }
    }
}
