using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Binding
{
    internal class CosmosDBMongoBindingEnumerableBuilder<T> : IAsyncConverter<CosmosDBMongoAttribute, IEnumerable<T>>
        where T : class
    {
        private readonly CosmosDBMongoConfigProvider _configProvider;

        public CosmosDBMongoBindingEnumerableBuilder(CosmosDBMongoConfigProvider configProvider)
        {
            this._configProvider = configProvider;
        }

        public async Task<IEnumerable<T>> ConvertAsync(CosmosDBMongoAttribute attribute, CancellationToken cancellationToken)
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
