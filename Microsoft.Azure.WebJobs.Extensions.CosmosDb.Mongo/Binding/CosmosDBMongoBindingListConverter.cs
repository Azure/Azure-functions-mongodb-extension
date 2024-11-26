using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Microsoft.Azure.WebJobs.Extensions.CosmosDb.Mongo
{
    internal class CosmosDBMongoBindingListConverter<T> : IAsyncConverter<CosmosDBMongoAttribute, List<T>>
        where T : class
    {
        private readonly CosmosDBMongoConfigProvider configProvider;

        public CosmosDBMongoBindingListConverter(CosmosDBMongoConfigProvider configProvider)
        {
            this.configProvider = configProvider;
        }

        public async Task<List<T>> ConvertAsync(CosmosDBMongoAttribute attribute, CancellationToken cancellationToken)
        {
            CosmosDBMongoContext context = this.configProvider.CreateContext(attribute);

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