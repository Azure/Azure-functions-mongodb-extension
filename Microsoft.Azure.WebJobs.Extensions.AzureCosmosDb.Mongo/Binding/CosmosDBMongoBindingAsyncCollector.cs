using MongoDB.Driver;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo
{
    public class CosmosDBMongoBindingAsyncCollector<T> : IAsyncCollector<T>
    {
        private readonly ILogger _logger;
        private CosmosDBMongoContext _mongoContext;

        public CosmosDBMongoBindingAsyncCollector(CosmosDBMongoContext mongoContext, ILogger logger) 
        {
            this._mongoContext = mongoContext;
            this._logger = logger;
        }

        public async Task AddAsync(T item, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            try{
                if (this._mongoContext.ResolvedAttribute.CreateIfNotExists)
                {
                    await InitializeCollection(this._mongoContext);
                }

                await UpsertDocument(this._mongoContext, item);
                this._logger.LogDebug(Events.OnBindingDataAdded, "Document upserted successfully.");
            }
            catch (Exception ex)
            {
                this._logger.LogError(Events.OnBindingDataError, $"Error upserting document: {ex.Message}");
            }
        }

        public Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // no-op
            return Task.FromResult(0);
        }

        private async Task InitializeCollection(CosmosDBMongoContext context)
        {
            if (context.ResolvedAttribute.CreateIfNotExists)
            {
               await MongoUtility.CreateCollectionIfNotExistAsync(context);
               this._logger.LogDebug(Events.OnIntializedCollection, $"Collection {context.ResolvedAttribute.CollectionName} created successfully.");
            }

            var database = context.MongoClient.GetDatabase(context.ResolvedAttribute.DatabaseName);
            // should throw error if _collection not exist and createifnotexists is false
            var collection = database.GetCollection<T>(context.ResolvedAttribute.CollectionName);
        }

        private async Task UpsertDocument(CosmosDBMongoContext context, T doc)
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