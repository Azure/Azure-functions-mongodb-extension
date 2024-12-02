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
            CosmosDBMongoContext context = this._configProvider.CreateContext(attribute);

            List<T> finalResults = new List<T>();

            IMongoCollection<T> collection = context.MongoClient.GetDatabase(context.ResolvedAttribute.DatabaseName).GetCollection<T>(context.ResolvedAttribute.CollectionName);

            BsonDocument filter = null;
            if (!string.IsNullOrEmpty(attribute.QueryString))
            {
                filter = BsonDocument.Parse(attribute.QueryString);
            }

            return await collection.Find(filter).ToListAsync();
        }
    }
}