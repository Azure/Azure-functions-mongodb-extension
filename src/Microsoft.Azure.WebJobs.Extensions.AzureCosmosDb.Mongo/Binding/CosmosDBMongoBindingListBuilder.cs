using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    internal class CosmosDBMongoBindingListBuilder<T> : IAsyncConverter<CosmosDBMongoAttribute, List<T>>
        where T : class
    {
        private readonly CosmosDBMongoConfigProvider _configProvider;

        public CosmosDBMongoBindingListBuilder(CosmosDBMongoConfigProvider configProvider)
        {
            this._configProvider = configProvider;
        }

        public async Task<List<T>> ConvertAsync(CosmosDBMongoAttribute attribute, CancellationToken cancellationToken)
        {
            MongoCollectionReference reference = this._configProvider.ResolveCollectionReference(attribute);

            List<T> finalResults = new List<T>();

            IMongoCollection<T> collection = reference.client.GetDatabase(reference.databaseName).GetCollection<T>(reference.collectionName);

            BsonDocument filter = null;
            if (!string.IsNullOrEmpty(attribute.QueryString))
            {
                filter = BsonDocument.Parse(attribute.QueryString);
            }

            return await collection.Find(filter).ToListAsync();
        }
    }
}