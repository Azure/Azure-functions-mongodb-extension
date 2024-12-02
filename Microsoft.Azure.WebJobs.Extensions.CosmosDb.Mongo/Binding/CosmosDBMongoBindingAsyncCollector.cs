using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoBindingAsyncCollector<T> : IAsyncCollector<T>
    {
        private CosmosDBMongoContext _mongoContext;

        public CosmosDBMongoBindingAsyncCollector(CosmosDBMongoContext mongoContext) => this._mongoContext = mongoContext;

        public async Task AddAsync(T item, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (this._mongoContext.ResolvedAttribute.CreateIfNotExists)
            {
                await InitializeCollection(this._mongoContext);
            }

            await UpsertDocument(this._mongoContext, item);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // no-op
            return Task.FromResult(0);
        }

        private static async Task InitializeCollection(CosmosDBMongoContext context)
        {
            if (context.ResolvedAttribute.CreateIfNotExists)
            {
               await MongoUtility.CreateCollectionIfNotExistAsync(context);
            }

            var database = context.MongoClient.GetDatabase(context.ResolvedAttribute.DatabaseName);
            // should throw error if _collection not exist and createifnotexists is false
            var collection = database.GetCollection<T>(context.ResolvedAttribute.CollectionName);
        }

        private static async Task UpsertDocument(CosmosDBMongoContext context, T doc)
        {
            var database = context.MongoClient.GetDatabase(context.ResolvedAttribute.DatabaseName);
            var collection = database.GetCollection<T>(context.ResolvedAttribute.CollectionName);

            var idProperty = typeof(T).GetProperty("_id");

            try
            {
                if (idProperty != null)
                {
                    var idValue = idProperty.GetValue(doc);
                    var filter = Builders<T>.Filter.Eq("_id", idValue);
                    var update = Builders<T>.Update.Set("updatedAt", DateTime.UtcNow);
                    var options = new UpdateOptions { IsUpsert = true };

                    await collection.UpdateOneAsync(filter, update, options);
                }
                else
                {
                    await collection.InsertOneAsync(doc);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}